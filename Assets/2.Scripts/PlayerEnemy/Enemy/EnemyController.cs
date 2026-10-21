using UnityEngine;
using System.Collections;

/// <summary>
/// 모든 Enemy의 초기화 및 이동 기능을 담당합니다.
/// </summary>
public class EnemyController : EnemyBase
{
    [Header("이동")]
    public float moveDuration = 1.5f;

    private Transform _playerTF;
    protected EnemyAttack _attack;

    // 플레이어 Transform — 처음 사용할 때 한 번만 검색
    // (Player 태그가 프리팹 안에 여러 개라 FindWithTag 대신 타입으로 검색)
    protected Transform PlayerTF
    {
        get
        {
            if (_playerTF == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) _playerTF = player.transform;
            }
            return _playerTF;
        }
    }

    protected virtual void Awake()
    {
        _attack = GetComponent<EnemyAttack>();
    }

    protected void InitEnemy(int hp)
    {
        _totalHp = hp;
        _nowHp = hp;
        IsDead = false;
    }

    // 특정 위치로 부드럽게 이동
    public IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector2.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        transform.position = targetPosition;
    }
}