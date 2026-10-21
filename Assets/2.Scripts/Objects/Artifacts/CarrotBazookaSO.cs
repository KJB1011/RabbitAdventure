using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Carrot Bazooka", fileName = "CarrotBazooka")]
public class CarrotBazookaSO : ArtifactSO
{
    [SerializeField] float _bonus = 0.25f;

    public override ArtifactEffect CreateEffect() => new Artifact_CarrotBazooka(_bonus);
}

/// <summary>
/// 필살기 데미지 증가
/// </summary>
public class Artifact_CarrotBazooka : ArtifactEffect
{
    private readonly float _bonus;

    public Artifact_CarrotBazooka(float bonus) => _bonus = bonus;

    protected override void OnActivate() => Player.AddSpecialDamageBonus(_bonus);
    protected override void OnDeactivate() => Player.AddSpecialDamageBonus(-_bonus);
}