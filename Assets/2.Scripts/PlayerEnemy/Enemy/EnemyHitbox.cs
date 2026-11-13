using UnityEngine;
using static Defines;

/// <summary>
/// Enemy의 히트박스
/// 플레이어가 Enemy를 공격할 때 판정 검사를 담당한다.
/// </summary>
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController owner;
    [SerializeField] private Transform _spriteTF;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    // Q 스킬: 판정이 닿은 뒤 칼 이펙트가 적을 지나가는 순간까지 기다렸다가 멈춤·플래시·타격음
    const float HeavyImpactDelay = 0.04f;

    // 모든 적이 공유하는 값: 마지막으로 기본 공격 적중음을 낸 프레임 (중복 재생 방지)
    static int _lastSwordHitFrame = -1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        if (owner.IsDead) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        int damage = player.DamageReturn(out bool isCrit);
        owner.DamageTaken(damage);

        // 아티팩트 발동 알림 — 맞은 적과 공격 종류를 함께 전달
        AttackType atkType = player.GetAttackType(other);
        player.NotifyAttackHit(owner, atkType);

        DamageTextManager.Instance.Show(damage, isCrit, owner.transform.position);
        if(atkType == AttackType.Super) // SuperSkill: 휘두름 → 맞음 → 0.1초 뒤 플래시 + 멈춤 + 타격음
        {
            IngameManager.Instance.HitStop(0.1f, HeavyImpactDelay, "SFX/HeavyHit");
            IngameManager.Instance.Flash(_spriteRenderer, 0.1f, HeavyImpactDelay);
        }
        else
        {
            IngameManager.Instance.Flash(_spriteRenderer, 0.05f);

            // 기본 공격 적중음 (한 번의 공격에 여러 적이 맞아도 같은 프레임에는 한 번만)
            if (atkType == AttackType.Basic && _lastSwordHitFrame != Time.frameCount)
            {
                _lastSwordHitFrame = Time.frameCount;
                SoundManager.Instance.PlaySFX("SFX/Player_SwordHit");
            }
        }
        VFXManager.Instance.Play("Hit", transform.position);
        Utils.Shake(_spriteTF, this);
        PlayHitSpark(player.transform.position);
    }


    // 적의 중앙에서, 플레이어 반대쪽으로 흰색 스파크를 튀긴다
    void PlayHitSpark(Vector2 attackerPos)
    {
        // 방향: 플레이어 → 적 (맞은 쪽으로 밀려나듯 튀게)
        Vector2 dir = (Vector2)owner.transform.position - attackerPos;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right; // 겹쳐 있을 때 대비

        // 파티클의 Cone은 로컬 +Z 방향으로 뿜어지므로, +Z가 dir을 향하도록 회전을 만든다.
        // (VFXManager.Play가 프리팹의 회전을 덮어쓰기 때문에 여기서 방향을 완성해서 넘긴다)
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.back);

        // 위치: 히트박스 중앙 (기존 Hit 이펙트와 같은 위치)
        VFXManager.Instance.Play("HitSpark", transform.position, rot);
    }

}