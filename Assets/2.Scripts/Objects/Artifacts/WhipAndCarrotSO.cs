using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Whip And Carrot", fileName = "WhipAndCarrot")]
public class WhipAndCarrotSO : ArtifactSO
{
    [SerializeField] float _atkBonus = 0.15f;
    [SerializeField] float _duration = 2f;

    public override ArtifactEffect CreateEffect()
        => new Artifact_WhipAndCarrot(_atkBonus, _duration);
}

/// <summary>
/// 대쉬 후 일정 시간 공격력 증가 (재대쉬 시 지속시간만 갱신)
/// </summary>
public class Artifact_WhipAndCarrot : ArtifactEffect
{
    private readonly float _atkBonus;
    private readonly float _duration;
    private Coroutine _routine;
    private bool _boosted;   // 버프 적용 여부 — 중복 가산 / 과다 차감 방지

    public Artifact_WhipAndCarrot(float atkBonus, float duration)
    {
        _atkBonus = atkBonus;
        _duration = duration;
    }

    protected override void OnActivate()
    {
        Player.OnDashStarted += HandleDash;
    }

    protected override void OnDeactivate()
    {
        Player.OnDashStarted -= HandleDash;
        StopRoutine(_routine);
        _routine = null;
        SetBoost(false);
    }

    private void HandleDash()
    {
        StopRoutine(_routine);
        _routine = StartRoutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        SetBoost(true);
        yield return new WaitForSeconds(_duration);
        SetBoost(false);
        _routine = null;
    }

    private void SetBoost(bool on)
    {
        if (_boosted == on) return;
        _boosted = on;

        if (on) Player.AddAtkMultiplier(_atkBonus);
        else Player.RemoveAtkMultiplier(_atkBonus);
    }
}