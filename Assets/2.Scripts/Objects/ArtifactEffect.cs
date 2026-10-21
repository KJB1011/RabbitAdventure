using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 아티팩트 효과의 추상 기반.
/// PlayerController 이벤트를 구독해 효과를 적용하고,
/// Deactivate 시 구독과 효과를 해제합니다.
/// </summary>
public abstract class ArtifactEffect
{
    protected PlayerController Player { get; private set; }
    private MonoBehaviour _runner;   // 코루틴 실행 주체 (ArtifactManager)

    public void Activate(PlayerController player, MonoBehaviour runner)
    {
        Player = player;
        _runner = runner;
        OnActivate();
    }

    public void Deactivate()
    {
        OnDeactivate();
        Player = null;
    }

    // 노드 이동 시 Player.StopAllCoroutines()에 끊기지 않도록 Manager에서 실행
    protected Coroutine StartRoutine(IEnumerator routine) => _runner.StartCoroutine(routine);

    protected void StopRoutine(Coroutine routine)
    {
        if (routine != null) _runner.StopCoroutine(routine);
    }

    protected abstract void OnActivate();
    protected abstract void OnDeactivate();
}