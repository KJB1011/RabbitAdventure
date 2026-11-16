using System.Collections;
using UnityEngine;

/// <summary>
/// 중간보스 듀오 중 Artillery(포격대장) — 원거리 봉쇄 담당
/// 추적 포격(플레이어 위치에 범위공격 3연발) / 산탄(부채꼴 탄막)
/// 협공: 돌격대장의 돌진 경로 양옆, 충격파 바깥쪽에 범위공격을 깔아 피할 곳을 좁힌다.
/// 혼자 있으면 스스로 패턴을 반복하고, DuoCombatNode에 배치되면 노드가 패턴을 지시한다.
/// </summary>
public class EnemyArtillery : EnemyController
{
    [Header("자리 잡기 (플레이어에게서 먼 모서리로 이동)")]
    [SerializeField] Vector2[] _positions =
    {
        new Vector2(-6.5f, 3f), new Vector2(6.5f, 3f),
        new Vector2(-6.5f, -3f), new Vector2(6.5f, -3f),
    };

    [Header("추적 포격")]
    [SerializeField] int _barrageCount = 3;
    [SerializeField] Vector2 _barrageSize = new Vector2(2.5f, 2.5f);
    [SerializeField] float _barrageInterval = 0.5f;

    [Header("산탄")]
    [SerializeField] int _fanCount = 5;
    [SerializeField] float _fanSpread = 40f;
    [SerializeField] float _fanSpeed = 8f;

    [Header("협공")]
    [SerializeField] Vector2 _supportSize = new Vector2(2.2f, 2.2f);
    [SerializeField] float _chargeSideOffset = 1.8f;  // 돌진 경로에서 옆으로 떨어진 거리
    [SerializeField] float _ringExtra = 1.6f;         // 충격파 가장자리에서 바깥으로 더 떨어진 거리

    bool _isDirected;

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(_totalHp);
    }

    /// <summary>DuoCombatNode가 생성 직후 호출 (Start보다 먼저)</summary>
    public void SetDirected() => _isDirected = true;

    void Start()
    {
        if (!_isDirected) StartCoroutine(SoloLoop());
    }

    IEnumerator SoloLoop()
    {
        yield return StartCoroutine(Entrance(_positions[1]));
        while (true)
        {
            yield return StartCoroutine(ChaseBarrage());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(FanShot());
            yield return new WaitForSeconds(0.5f);
        }
    }

    public IEnumerator Entrance(Vector2 targetPos)
    {
        yield return StartCoroutine(EntranceFromRight(targetPos));
    }

    // 플레이어에게서 가장 먼 자리로 이동
    public IEnumerator Reposition()
    {
        if (PlayerTF == null || _positions.Length == 0) yield break;

        Vector2 player = PlayerTF.position;
        Vector2 best = _positions[0];
        foreach (var p in _positions)
            if ((p - player).sqrMagnitude > (best - player).sqrMagnitude) best = p;

        if (((Vector2)transform.position - best).sqrMagnitude > 0.01f)
            yield return StartCoroutine(MoveToPosition(best));
    }

    // ── 추적 포격 ─────────────────────────────────────────────
    /// <param name="onShot">한 발 쏠 때마다 호출 — 동료가 그 박자에 맞춰 할퀴기를 할 때 사용</param>
    public IEnumerator ChaseBarrage(System.Action onShot = null)
    {
        yield return StartCoroutine(Reposition());
        for (int i = 0; i < _barrageCount; i++)
        {
            if (PlayerTF == null) yield break;
            bool done = false;
            _attack.FireAreaAttack(PlayerTF.position, _barrageSize, 0f, () => done = true);
            onShot?.Invoke();
            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(_barrageInterval);
        }
    }

    // ── 산탄 ─────────────────────────────────────────────────
    public IEnumerator FanShot()
    {
        yield return StartCoroutine(Reposition());
        if (PlayerTF == null) yield break;

        bool done = false;
        _attack.FireFanWithTelegraph(PlayerTF.position, _fanCount, _fanSpread, _fanSpeed, () => done = true);
        yield return new WaitUntil(() => done);
    }

    // ── 협공 ─────────────────────────────────────────────────

    /// <summary>돌진 경로의 양옆에 범위공격 → 옆으로 피하지 못하게</summary>
    public void SupportCharge(Vector2 start, Vector2 end)
    {
        if (IsDead) return;
        Vector2 dir = (end - start).normalized;
        Vector2 side = new Vector2(-dir.y, dir.x) * _chargeSideOffset;

        foreach (float t in new[] { 0.35f, 0.75f })
        {
            Vector2 p = Vector2.Lerp(start, end, t);
            _attack.FireAreaAttack(p + side, _supportSize);
            _attack.FireAreaAttack(p - side, _supportSize);
        }
    }

    /// <summary>충격파에 밀려 나갈 바깥쪽(플레이어가 있는 쪽)에 범위공격</summary>
    public void SupportShockwave(Vector2 center, float shockwaveRadius)
    {
        if (IsDead || PlayerTF == null) return;
        Vector2 toPlayer = (Vector2)PlayerTF.position - center;
        float baseAngle = toPlayer.sqrMagnitude > 0.01f ? Mathf.Atan2(toPlayer.y, toPlayer.x) : 0f;
        float dist = shockwaveRadius + _ringExtra;

        foreach (float offset in new[] { -40f, 0f, 40f })
        {
            float a = baseAngle + offset * Mathf.Deg2Rad;
            _attack.FireAreaAttack(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist, _supportSize);
        }
    }
}
