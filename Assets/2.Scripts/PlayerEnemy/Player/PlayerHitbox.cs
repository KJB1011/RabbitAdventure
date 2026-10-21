using UnityEngine;

/// <summary>
/// Player의 히트박스 판정
/// 플레이어가 Enemy의 탄환에 맞는 판정을 한 곳에서 처리합니다.
/// </summary>
public class PlayerHitbox : MonoBehaviour
{
    [SerializeField] PlayerController owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("EnemyBullet")) return;

        // 무적 · 게임오버면 false — 이펙트 없이 무시
        if (!owner.DamageTaken()) return;

        VFXManager.Instance.Play("PlayerHit", transform.position);

        // 맞은 탄환은 즉시 풀로 반납
        if (other.TryGetComponent(out PooledBullet bullet))
            bullet.ReleaseSelf();
    }
}