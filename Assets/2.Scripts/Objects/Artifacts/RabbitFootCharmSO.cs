using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Rabbit Foot Charm", fileName = "RabbitFootCharm")]
public class RabbitFootCharmSO : ArtifactSO
{
    [SerializeField] float _critRate = 15f;        // 치명타 확률 (% 단위, 15 = +15%)
    [SerializeField] float _critDamageRate = 0.5f; // 치명타 배율 (+0.5 = 치명타 피해 +50%)

    public override ArtifactEffect CreateEffect() => new Artifact_RabbitFootCharm(_critRate, _critDamageRate);
}

/// <summary>
/// 치명타 확률 증가 + 치명타 피해 증가
/// </summary>
public class Artifact_RabbitFootCharm : ArtifactEffect
{
    private readonly float _critRate;
    private readonly float _critDamageRate;

    public Artifact_RabbitFootCharm(float critRate, float critDamageRate)
    {
        _critRate = critRate;
        _critDamageRate = critDamageRate;
    }

    protected override void OnActivate()
    {
        Player.AddCritRate(_critRate);
        Player.AddCritDamageRate(_critDamageRate);
    }

    protected override void OnDeactivate()
    {
        Player.AddCritRate(-_critRate);
        Player.AddCritDamageRate(-_critDamageRate);
    }
}
