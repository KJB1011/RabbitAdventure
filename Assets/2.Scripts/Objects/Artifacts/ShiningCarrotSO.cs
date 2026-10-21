using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Shining Carrot", fileName = "ShiningCarrot")]
public class ShiningCarrotSO : ArtifactSO
{
    [SerializeField] float _extraTime = 0.5f;

    public override ArtifactEffect CreateEffect() => new Artifact_ShiningCarrot(_extraTime);
}

/// <summary>
/// 대쉬 중 무적 지속시간 증가
/// </summary>
public class Artifact_ShiningCarrot : ArtifactEffect
{
    private readonly float _extraTime;

    public Artifact_ShiningCarrot(float extraTime) => _extraTime = extraTime;

    protected override void OnActivate() => Player.AddDashInvincibleTime(_extraTime);
    protected override void OnDeactivate() => Player.AddDashInvincibleTime(-_extraTime);
}