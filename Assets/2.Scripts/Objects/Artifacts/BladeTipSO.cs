using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Blade Tip", fileName = "BladeTip")]
public class BladeTipSO : ArtifactSO
{
    [Tooltip("공격 범위의 바깥쪽 몇 %로 맞히면 확정 치명타인지 (0.4 = 바깥 40%)")]
    [SerializeField, Range(0f, 1f)] float _tipRatio = 0.4f;

    public override ArtifactEffect CreateEffect() => new Artifact_BladeTip(_tipRatio);
}

/// <summary>
/// 칼끝 일격: 기본기·특수기의 판정 바깥쪽(칼끝)으로 적을 맞히면 확정 치명타
/// "맞은 위치 → 플레이어까지 거리"가 이번 공격 사거리(PlayerAttack.LastReach)의 60% 이상이면 칼끝으로 본다.
/// </summary>
public class Artifact_BladeTip : ArtifactEffect
{
    private readonly float _tipRatio;

    public Artifact_BladeTip(float tipRatio) => _tipRatio = tipRatio;

    protected override void OnActivate() => Player.AddCritRule(IsTipHit);
    protected override void OnDeactivate() => Player.RemoveCritRule(IsTipHit);

    private bool IsTipHit(Vector2 hitPoint)
    {
        float reach = Player.Attack.LastReach;
        if (reach <= 0f) return false;

        float dist = Vector2.Distance(Player.transform.position, hitPoint);
        return dist >= reach * (1f - _tipRatio);
    }
}
