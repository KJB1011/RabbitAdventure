using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Rubber Carrot Sword", fileName = "RubberCarrotSword")]
public class RubberCarrotSwordSO : ArtifactSO
{
    [SerializeField] float _rangeBonus = 0.2f;   // 공격범위 +20%

    public override ArtifactEffect CreateEffect() => new Artifact_RubberCarrotSword(_rangeBonus);
}

/// <summary>
/// 고무 당근 검: 공격범위 증가 (범위 빌드의 기본)
/// </summary>
public class Artifact_RubberCarrotSword : ArtifactEffect
{
    private readonly float _rangeBonus;

    public Artifact_RubberCarrotSword(float rangeBonus) => _rangeBonus = rangeBonus;

    protected override void OnActivate() => Player.AddRange(_rangeBonus);
    protected override void OnDeactivate() => Player.AddRange(-_rangeBonus);
}
