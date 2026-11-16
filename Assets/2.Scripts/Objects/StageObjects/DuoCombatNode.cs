using System.Collections;
using UnityEngine;

/// <summary>
/// 중간보스 듀오 노드 — 돌격대장(Vanguard)과 포격대장(Artillery)을 동시에 소환하고,
/// 둘 다 쓰러지면 완료.
///
/// 두 적은 스스로 패턴을 반복하지 않고, 이 노드가 순서를 정해 동시에 지시한다 (지휘자 역할).
///   1) 연계 할퀴기: 포격대장이 추적 포격을 한 발 쏠 때마다 돌격대장이 한 번씩 할퀸다
///   2) 각자 공격: 충격파 + 산탄
///   3) 협공 - 포위 돌진: 돌진 경로 양옆을 포격으로 막는다
///   4) 협공 - 몰이 포격: 충격파에 밀려날 바깥쪽을 포격으로 막는다
/// 한 명이 쓰러지면 남은 쪽은 자기 패턴만 반복한다 (분노 없음).
/// </summary>
public class DuoCombatNode : NodeBase
{
    [Header("적 프리팹")]
    [SerializeField] EnemyVanguard _vanguardPrefab;
    [SerializeField] EnemyArtillery _artilleryPrefab;

    [Header("등장 위치")]
    [SerializeField] Vector2 _vanguardPosition = new Vector2(3.5f, -1.5f);
    [SerializeField] Vector2 _artilleryPosition = new Vector2(6.5f, 3f);

    [Header("패턴 사이 대기")]
    [SerializeField] float _patternInterval = 0.7f;

    EnemyVanguard _vanguard;
    EnemyArtillery _artillery;
    UIHPBar _vanguardBar, _artilleryBar;
    int _aliveCount;
    bool _isClawing; // 연계 할퀴기 중 (한 번에 하나씩만)

    protected override void OnNodeStart()
    {
        IngameManager.Instance._isBattle = true;

        if (_vanguardPrefab == null || _artilleryPrefab == null)
        {
            Debug.LogError("[DuoCombatNode] 적 프리팹이 비어 있습니다.");
            Complete();
            return;
        }

        // 화면 밖(오른쪽)에서 생성 → 등장 연출로 들어온다
        _vanguard = Instantiate(_vanguardPrefab, _vanguardPosition, Quaternion.identity);
        _artillery = Instantiate(_artilleryPrefab, _artilleryPosition, Quaternion.identity);
        _vanguard.SetDirected();   // Start보다 먼저 불려야 스스로 패턴을 돌리지 않는다
        _artillery.SetDirected();

        // 각자 다른 HP바 (돌격대장: 위, 포격대장: 아래)
        _vanguardBar = IngameManager.Instance.GetHPBar();
        _artilleryBar = IngameManager.Instance.GetHPBar2();
        _vanguard.BindHPBar(_vanguardBar);
        _artillery.BindHPBar(_artilleryBar);
        _vanguardBar?.Show();
        _artilleryBar?.Show();

        _aliveCount = 2;
        _isClawing = false; // 노드를 다시 시작할 때 이전 상태가 남지 않게
        _vanguard.OnDied += HandleEnemyDied;
        _artillery.OnDied += HandleEnemyDied;

        StartCoroutine(DirectorLoop());
    }

    void HandleEnemyDied()
    {
        _aliveCount--;
        if (_aliveCount <= 0)
        {
            StopAllCoroutines();
            Complete();
        }
    }

    bool Alive(EnemyController enemy) => enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy;

    // ── 지휘 ─────────────────────────────────────────────────

    IEnumerator DirectorLoop()
    {
        // 동시에 등장
        yield return Together(_vanguard, _vanguard.Entrance(_vanguardPosition),
                              _artillery, _artillery.Entrance(_artilleryPosition));
        yield return new WaitForSeconds(0.5f);

        int step = 0;
        while (Alive(_vanguard) || Alive(_artillery))
        {
            bool both = Alive(_vanguard) && Alive(_artillery);

            if (both)
            {
                switch (step % 4)
                {
                    case 0: // 연계 할퀴기: 포격 한 발마다 할퀴기 한 번
                        yield return Run(_artillery, _artillery.ChaseBarrage(() =>
                        {
                            if (!_isClawing && Alive(_vanguard)) StartCoroutine(ClawOnce());
                        }));
                        yield return new WaitUntil(() => !_isClawing); // 마지막 할퀴기가 끝날 때까지
                        break;
                    case 1:
                        yield return Together(_vanguard, _vanguard.Shockwave(),
                                              _artillery, _artillery.FanShot());
                        break;
                    case 2: // 협공: 포위 돌진
                        yield return Run(_vanguard, _vanguard.Charge(
                            (start, end) => _artillery.SupportCharge(start, end)));
                        break;
                    case 3: // 협공: 몰이 포격
                        yield return Run(_vanguard, _vanguard.Shockwave(
                            center => _artillery.SupportShockwave(center, _vanguard.ShockwaveRadius)));
                        break;
                }
            }
            else if (Alive(_vanguard))
            {
                switch (step % 3)
                {
                    case 0: yield return Run(_vanguard, _vanguard.Charge()); break;
                    case 1: yield return Run(_vanguard, _vanguard.Claw()); break;
                    case 2: yield return Run(_vanguard, _vanguard.Shockwave()); break;
                }
            }
            else
            {
                yield return Run(_artillery, step % 2 == 0 ? _artillery.ChaseBarrage() : _artillery.FanShot());
            }

            step++;
            yield return new WaitForSeconds(_patternInterval);
        }
    }

    IEnumerator ClawOnce()
    {
        _isClawing = true;
        yield return Run(_vanguard, _vanguard.Claw()); // 돌격대장이 죽으면 Run이 바로 끝난다
        _isClawing = false;
    }

    // 패턴은 적 자신에게서 실행한다 → 그 적이 죽으면(StopAllCoroutines) 패턴도 함께 멈춘다
    IEnumerator Run(EnemyController enemy, IEnumerator pattern)
    {
        if (!Alive(enemy)) yield break;

        bool done = false;
        enemy.StartCoroutine(Wrap(pattern, () => done = true));
        while (!done && Alive(enemy)) yield return null;
    }

    // 두 적의 패턴을 동시에 시작하고, 둘 다 끝날 때까지 기다린다
    IEnumerator Together(EnemyController a, IEnumerator patternA, EnemyController b, IEnumerator patternB)
    {
        var runA = StartCoroutine(Run(a, patternA));
        var runB = StartCoroutine(Run(b, patternB));
        yield return runA;
        yield return runB;
    }

    static IEnumerator Wrap(IEnumerator pattern, System.Action onDone)
    {
        yield return pattern;
        onDone();
    }

    void OnDisable()
    {
        if (_vanguard != null) _vanguard.OnDied -= HandleEnemyDied;
        if (_artillery != null) _artillery.OnDied -= HandleEnemyDied;

        _vanguardBar?.Hide();
        _artilleryBar?.Hide();
    }
}
