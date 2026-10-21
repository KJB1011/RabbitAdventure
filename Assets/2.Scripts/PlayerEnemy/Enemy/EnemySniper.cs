using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy중 Sniper의 행동을 담당하는 스크립트
/// 특정 위치로 이동(각 모서리) -> 빠른 탄막 발사 -> 특정 위치로 이동(각 모서리) -> 플레이어 위치에 범위공격 3번
/// 해당 행동을 반복한다.
/// </summary>
public class Enemy_Sniper : EnemyController
{
    [Header("코너 위치")]
    [SerializeField] private Vector2 _bottomLeft = new Vector2(-6f, -3f);
    [SerializeField] private Vector2 _topLeft = new Vector2(-6f, 3f);
    [SerializeField] private Vector2 _topRight = new Vector2(6f, 3f);
    [SerializeField] private Vector2 _bottomRight = new Vector2(6f, -3f);

    [Header("단발 패턴")]
    [SerializeField] private float _waitAfterMove = 1f;

    [Header("범위 공격 패턴")]
    [SerializeField] private Vector2 _areaSize = new Vector2(3f, 3f); // 추적 범위 크기
    [SerializeField] private float _areaDelay = 0.5f; // 범위 공격 간격

    [Header("전조 표시")]
    [SerializeField] private SpriteRenderer _bodySprite;
    private Color _normalColor;
    private readonly Color _telegraphColor = new Color(1f, 0.2f, 0.2f);

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(_totalHp);
        if (_bodySprite != null)
            _normalColor = _bodySprite.color;
    }

    void Start()
    {
        StartCoroutine(SniperLoop());
    }

    IEnumerator SniperLoop()
    {
        Vector2[] waypoints = new Vector2[]
        {
            _bottomLeft, _topLeft, _topRight, _bottomRight
        };

        yield return StartCoroutine(EntranceFromRight(waypoints[0]));

        int index = 0;
        while (true)
        {
            yield return StartCoroutine(MoveToPosition(waypoints[index]));
            yield return new WaitForSeconds(_waitAfterMove);

            // 짝수 코너: 단발 발사 / 홀수 코너: 플레이어 추적 범위 공격
            if (index % 2 == 0)
                yield return StartCoroutine(PatternSingleShot());
            else
                yield return StartCoroutine(PatternAreaChase());

            index = (index + 1) % waypoints.Length;
        }
    }

    // ── 단발 발사 ─────────────────────────────────────────────

    IEnumerator PatternSingleShot()
    {
        if (PlayerTF == null) yield break;

        bool done = false;
        _attack.FireSingleShotWithTelegraph(PlayerTF.position, () => done = true);
        yield return new WaitUntil(() => done);
    }

    // ── 플레이어 추적 범위 공격 ───────────────────────────────

    IEnumerator PatternAreaChase()
    {
        for (int i = 0; i < 3; i++)
        {
            if (PlayerTF == null) yield break;

            bool done = false;
            _attack.FireAreaAttack(PlayerTF.position, _areaSize, 0f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(_areaDelay);
        }
    }
}