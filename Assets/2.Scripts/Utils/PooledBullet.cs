using UnityEngine;

/// <summary>
/// 풀에서 꺼내져 사용되는 탄환 하나를 담당하는 스크립트
/// 이동, 수명 관리를 처리합니다. (피격 판정은 PlayerHitbox 에서)
/// </summary>
public class PooledBullet : MonoBehaviour
{
    private Vector2 _velocity;
    private System.Action<PooledBullet> _releaseCallback;
    private float _remainingLife;   // 코루틴 대신 타이머 — 발사마다 할당 없음
    private bool _released;

    public void Launch(Vector2 velocity, float lifetime,
                       System.Action<PooledBullet> releaseCallback)
    {
        _velocity = velocity;
        _releaseCallback = releaseCallback;
        _remainingLife = lifetime;
        _released = false;

        // 이동 방향으로 오브젝트 회전
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.Translate(_velocity * Time.deltaTime, Space.World);

        _remainingLife -= Time.deltaTime;
        if (_remainingLife <= 0f)
            ReleaseSelf();
    }

    // 수명이 다하거나 맞았을 때 풀로 반납
    public void ReleaseSelf()
    {
        if (_released) return;
        _released = true;
        _releaseCallback?.Invoke(this);
    }
}