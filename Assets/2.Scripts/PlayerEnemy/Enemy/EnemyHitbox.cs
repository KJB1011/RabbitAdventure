using UnityEngine;

/// <summary>
/// Enemy의 히트박스
/// 플레이어가 Enemy를 공격할 때 판정 검사를 담당한다.
/// </summary>
public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController owner;
    [SerializeField] private Transform _spriteTF;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerAttack")) return;
        if (owner.IsDead) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        int damage = player.DamageReturn(out bool isCrit);
        owner.DamageTaken(damage);

        // 아티팩트 발동 알림 — 맞은 적과 공격 종류를 함께 전달
        player.NotifyAttackHit(owner, player.GetAttackType(other));

        DamageTextManager.Instance.Show(damage, isCrit, owner.transform.position);
        Utils.Shake(_spriteTF, this);
        VFXManager.Instance.Play("Hit", transform.position);
    }
}