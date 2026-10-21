using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 잡다한 헬퍼함수들이 있는 스크립트
/// 마우스 위치찾기, 오브젝트 흔들기 기능이 있습니다.
/// </summary>
public class Utils : MonoBehaviour
{
    public static Vector2 GetMousePos()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        return new Vector2(mouseWorldPos.x, mouseWorldPos.y);
    }
    public static void Shake(Transform target, MonoBehaviour runner, float duration = 0.1f, float magnitude = 0.1f)
    {
        runner.StartCoroutine(ShakeRoutine(target, duration, magnitude));
    }

    private static IEnumerator ShakeRoutine(Transform target, float duration, float magnitude)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            target.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localPosition = originalPos;
    }
}
