using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Glass Carrot", fileName = "GlassCarrot")]
public class GlassCarrotSO : ArtifactSO
{
    [SerializeField] float _atkBonus = 0.6f;   // 공격력 +60%
    [SerializeField] int _maxHpPenalty = 2;    // 최대 HP -2

    public override ArtifactEffect CreateEffect() => new Artifact_GlassCarrot(_atkBonus, _maxHpPenalty);
}

/// <summary>
/// 공격력 크게 증가, 대신 최대 HP 감소 (고위험 고보상)
/// </summary>
public class Artifact_GlassCarrot : ArtifactEffect
{
    private readonly float _atkBonus;
    private readonly int _maxHpPenalty;

    public Artifact_GlassCarrot(float atkBonus, int maxHpPenalty)
    {
        _atkBonus = atkBonus;
        _maxHpPenalty = maxHpPenalty;
    }

    protected override void OnActivate()
    {
        Player.AddAtkMultiplier(_atkBonus);
        Player.AddMaxHp(-_maxHpPenalty);
    }

    protected override void OnDeactivate()
    {
        Player.RemoveAtkMultiplier(_atkBonus);
        Player.AddMaxHp(_maxHpPenalty);
    }
}
