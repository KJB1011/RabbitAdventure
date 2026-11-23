using UnityEngine;
using System.Collections;

public class PooledVFX : MonoBehaviour
{
    private ParticleSystem[] _systems;
    private Coroutine _returnRoutine;
    private System.Action _onComplete;

    public void PlayAndScheduleReturn(float lifetime, System.Action onComplete)
    {
        _systems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in _systems) ps.Play();
        _onComplete = onComplete;
        _returnRoutine = StartCoroutine(ReturnAfter(lifetime));
    }

    private IEnumerator ReturnAfter(float t)
    {
        yield return new WaitForSeconds(t); // 히트스탑 있으면 WaitForSecondsRealtime 고려
        _returnRoutine = null;
        ReturnNow();
    }

    /// <summary>수명을 기다리지 않고 지금 바로 풀로 돌려보낸다 (로딩 중 미리 그려 본 이펙트 정리용)</summary>
    public void ReturnNow()
    {
        if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        _returnRoutine = null;

        var callback = _onComplete; // 두 번 돌려보내지 않게 비워 두고 부른다
        _onComplete = null;
        callback?.Invoke();
        gameObject.SetActive(false);
    }
}
