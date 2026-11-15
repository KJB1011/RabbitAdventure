using UnityEngine;
using static Defines;

[CreateAssetMenu(menuName = "Artifact/Moon Rabbit Mortar", fileName = "MoonRabbitMortar")]
public class MoonRabbitMortarSO : ArtifactSO
{
    [SerializeField] int _hitsRequired = 10;       // 기본 공격 이만큼 맞히면
    [SerializeField] float _cooldownReduce = 1f;   // 필살기 쿨타임 감소 (초)

    public override ArtifactEffect CreateEffect() => new Artifact_MoonRabbitMortar(_hitsRequired, _cooldownReduce);
}

/// <summary>
/// 기본 공격을 일정 횟수 맞힐 때마다 필살기(Special) 쿨타임 감소
/// </summary>
public class Artifact_MoonRabbitMortar : ArtifactEffect
{
    private readonly int _hitsRequired;
    private readonly float _cooldownReduce;
    private int _hitCount;

    public Artifact_MoonRabbitMortar(int hitsRequired, float cooldownReduce)
    {
        _hitsRequired = Mathf.Max(1, hitsRequired);
        _cooldownReduce = cooldownReduce;
    }

    protected override void OnActivate()
    {
        _hitCount = 0;
        Player.OnAttackHit += HandleAttackHit;
    }

    protected override void OnDeactivate() => Player.OnAttackHit -= HandleAttackHit;

    private void HandleAttackHit(EnemyBase target, AttackType type)
    {
        if (type != AttackType.Basic) return;

        _hitCount++;
        if (_hitCount < _hitsRequired) return;

        _hitCount = 0;
        Player.ReduceSpecialSkillCooldown(_cooldownReduce);
    }
}
