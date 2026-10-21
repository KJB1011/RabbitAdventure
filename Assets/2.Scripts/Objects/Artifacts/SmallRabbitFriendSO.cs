using UnityEngine;
using static Defines;

[CreateAssetMenu(menuName = "Artifact/Small Rabbit Friend", fileName = "SmallRabbitFriend")]
public class SmallRabbitFriendSO : ArtifactSO
{
    [SerializeField] float _cooldown = 3f;

    public override ArtifactEffect CreateEffect() => new Artifact_SmallRabbitFriend(_cooldown);
}

/// <summary>
/// 기본 공격 적중 시 일정 쿨타임마다 공격력 100% 추가 데미지
/// </summary>
public class Artifact_SmallRabbitFriend : ArtifactEffect
{
    private readonly float _cooldown;
    private float _nextReadyTime;   // 코루틴 대신 시간 비교 — 노드 이동에 영향받지 않음

    public Artifact_SmallRabbitFriend(float cooldown) => _cooldown = cooldown;

    protected override void OnActivate()
    {
        _nextReadyTime = 0f;
        Player.OnAttackHit += HandleAttackHit;
    }

    protected override void OnDeactivate()
    {
        Player.OnAttackHit -= HandleAttackHit;
    }

    private void HandleAttackHit(EnemyBase target, AttackType type)
    {
        if (type != AttackType.Basic) return;
        if (Time.time < _nextReadyTime) return;
        if (target == null || target.IsDead) return;   // 방금 공격으로 죽은 적에게는 발동 안 함

        int bonusDamage = Mathf.Max(1, Player.GetBaseAtk());
        target.DamageTaken(bonusDamage);

        // 데미지 텍스트 (본 공격 텍스트와 겹치지 않게 약간 위)
        DamageTextManager.Instance.Show(bonusDamage, false,
            target.transform.position + Vector3.up * 0.8f);

        _nextReadyTime = Time.time + _cooldown;
    }
}