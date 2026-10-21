using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy중 Chaser의 행동을 담당하는 스크립트
/// 플레이어에게 이동 -> 탄막 흩뿌리기 -> 십자범위에 원형범위공격
/// 해당 행동을 반복한다.
/// </summary>
public class Enemy_Chaser : EnemyController
{
    [Header("패턴 설정")]
    [SerializeField] private float _waitBeforeShot = 1f;
    [SerializeField] private float _waitAfterShot = 1.5f;

    [Header("십자 범위 공격")]
    [SerializeField] private float _crossOffset = 3f;   // 십자 간격
    [SerializeField] private Vector2 _crossAreaSize = new Vector2(2.5f, 2.5f);

    private int _patternCount = 0;

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(_totalHp);
    }

    void Start()
    {
        StartCoroutine(ChaserLoop());
    }

    IEnumerator ChaserLoop()
    {
        yield return StartCoroutine(EntranceFromRight(transform.position));

        while (true)
        {
            if (_patternCount % 2 == 0)
                yield return StartCoroutine(PatternDash()); // 돌진 + 탄막
            else
                yield return StartCoroutine(PatternCrossArea()); // 십자 범위

            _patternCount++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ── 돌진 + 방사형 탄막 ───────────────────────────────────

    IEnumerator PatternDash()
    {
        if (PlayerTF == null) yield break;

        Vector2 targetPos = PlayerTF.position;
        yield return StartCoroutine(MoveToPosition(targetPos));
        yield return new WaitForSeconds(_waitBeforeShot);

        bool done = false;
        _attack.FireRadialAttackWithTelegraph(() => done = true);
        yield return new WaitUntil(() => done);

        yield return new WaitForSeconds(_waitAfterShot);
    }

    // ── 십자 범위 공격 ────────────────────────────────────────

    IEnumerator PatternCrossArea()
    {
        // 자기 위치 기준 십자 4방향 범위 공격
        Vector3 origin = transform.position;
        Vector3[] positions = new Vector3[]
        {
            origin + Vector3.right  * _crossOffset,
            origin + Vector3.left   * _crossOffset,
            origin + Vector3.up     * _crossOffset,
            origin + Vector3.down   * _crossOffset,
        };

        bool done = false;
        _attack.FireMultiAreaAttack(positions, _crossAreaSize, () => done = true);
        yield return new WaitUntil(() => done);
    }

}