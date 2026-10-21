using UnityEngine;
using static Defines;

[CreateAssetMenu(menuName = "Artifact/Acceleration Watch", fileName = "AccelerationWatch")]
public class AccelerationWatchSO : ArtifactSO
{
    [SerializeField] float _cooldownReduce = 0.2f;

    public override ArtifactEffect CreateEffect()
        => new Artifact_AccelerationWatch(_cooldownReduce);
}

/// <summary>
/// 기본 공격 적중 시 Q 스킬 쿨타임 감소
/// </summary>
public class Artifact_AccelerationWatch : ArtifactEffect
{
    private readonly float _cooldownReduce;

    public Artifact_AccelerationWatch(float cooldownReduce) => _cooldownReduce = cooldownReduce;

    protected override void OnActivate() => Player.OnAttackHit += HandleAttackHit;
    protected override void OnDeactivate() => Player.OnAttackHit -= HandleAttackHit;

    private void HandleAttackHit(EnemyBase target, AttackType type)
    {
        if (type != AttackType.Basic) return;
        Player.ReduceSuperSkillCooldown(_cooldownReduce);
    }
}