using UnityEngine;
using System.Collections;

public class PooledVFX : MonoBehaviour
{
    private ParticleSystem[] _systems;
    private Coroutine _returnRoutine;

    public void PlayAndScheduleReturn(float lifetime, System.Action onComplete)
    {
        _systems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in _systems) ps.Play();
        _returnRoutine = StartCoroutine(ReturnAfter(lifetime, onComplete));
    }

    private IEnumerator ReturnAfter(float t, System.Action onComplete)
    {
        yield return new WaitForSeconds(t); // 히트스탑 있으면 WaitForSecondsRealtime 고려
        onComplete?.Invoke();
        gameObject.SetActive(false);
    }
}