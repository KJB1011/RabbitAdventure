using System.Collections;
using UnityEngine;

/// <summary>
/// 중간보스 듀오 중 Vanguard(돌격대장) — 근접 압박 담당
/// 돌진(경고선 후 플레이어 쪽으로 돌진, 부딪히면 피해) / 충격파(자기 주변 원형 범위공격)
/// 할퀴기(부채꼴 경고 후 앞으로 살짝 뛰어들며 할퀴기)
/// 혼자 있으면 스스로 패턴을 반복하고, DuoCombatNode에 배치되면 노드가 동료와 맞춰 패턴을 지시한다.
/// </summary>
public class EnemyVanguard : EnemyController
{
    [Header("이동 범위")]
    [SerializeField] Vector2 _arenaMin = new Vector2(-8f, -4.3f);
    [SerializeField] Vector2 _arenaMax = new Vector2(8f, 4.3f);

    [Header("돌진")]
    [SerializeField] float _chargeDistance = 7f;
    [SerializeField] float _chargeWarnTime = 0.8f;   // 경고선 표시 시간
    [SerializeField] float _chargeTime = 0.35f;      // 돌진에 걸리는 시간
    [SerializeField] float _contactRadius = 0.9f;    // 돌진 중 부딪힘 판정 반경
    [SerializeField] float _afterChargeWait = 0.6f;

    [Header("충격파")]
    [SerializeField] Vector2 _shockwaveSize = new Vector2(4.5f, 4.5f);

    [Header("할퀴기")]
    [SerializeField] float _clawWarnTime = 0.6f;     // 부채꼴 경고 표시 시간
    [SerializeField] float _clawLunge = 1.2f;        // 할퀴면서 앞으로 나가는 거리
    [SerializeField] float _clawRange = 2f;          // 할퀴는 손이 닿는 거리 (나간 뒤 기준)
    [SerializeField] float _clawAngle = 100f;        // 부채꼴 전체 각도
    [SerializeField] float _clawLungeTime = 0.12f;
    [SerializeField] float _clawRecover = 0.25f;     // 할퀸 뒤 잠깐 멈춤
    [SerializeField] float _clawApproachTime = 0.3f; // 너무 멀면 먼저 성큼 다가가는 시간

    bool _isDirected; // 노드가 패턴을 지시하는 중이면 스스로 반복하지 않는다

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
        yield return StartCoroutine(Entrance(new Vector2(4f, -1.5f)));
        while (true)
        {
            yield return StartCoroutine(Charge());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(Claw());
            yield return StartCoroutine(Claw());
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(Shockwave());
            yield return new WaitForSeconds(0.5f);
        }
    }

    // ── 할퀴기 ───────────────────────────────────────────────
    public IEnumerator Claw()
    {
        if (PlayerTF == null) yield break;

        float reach = _clawLunge + _clawRange; // 뛰어드는 거리까지 포함한 실제 사정거리

        // 너무 멀면 먼저 성큼 다가간다 (사정거리 안쪽까지)
        Vector2 pos = transform.position;
        Vector2 player = PlayerTF.position;
        if (Vector2.Distance(pos, player) > reach)
        {
            Vector2 near = ClampToArena(player + (pos - player).normalized * (reach * 0.7f));
            FaceTo(near);
            yield return StartCoroutine(MoveQuick(near, _clawApproachTime));
        }

        FacePlayer();
        Vector2 start = transform.position;
        Vector2 dir = ((Vector2)PlayerTF.position - start).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;

        // 경고: 지금 위치에서 (뛰어드는 거리 + 손 길이)까지 부채꼴 → 실제로 맞는 범위와 같다
        var warn = _attack.ShowSector(start, dir, reach, _clawAngle);
        yield return new WaitForSeconds(_clawWarnTime);
        _attack.HideSector(warn);

        // 앞으로 살짝 뛰어들며 할퀸다
        Vector2 end = ClampToArena(start + dir * _clawLunge);
        yield return StartCoroutine(MoveQuick(end, _clawLungeTime));

        SoundManager.Instance.PlaySFX("SFX/Scratch");
        IngameManager.Instance.ShakeCamera(0.08f, 0.08f);
        VFXManager.Instance.Play("HitSpark", end + dir * 1f, Quaternion.LookRotation(dir, Vector3.back));
        HitPlayerInSector(start, dir, reach, _clawAngle);

        yield return new WaitForSeconds(_clawRecover);
    }

    // 경고와 같은 부채꼴 안에 플레이어 몸이 있으면 피해
    void HitPlayerInSector(Vector2 apex, Vector2 dir, float radius, float angle)
    {
        foreach (var hit in Physics2D.OverlapCircleAll(apex, radius))
        {
            if (!hit.CompareTag("PlayerHitbox")) continue;

            Vector2 to = (Vector2)hit.bounds.center - apex;
            if (to.sqrMagnitude > 0.01f && Vector2.Angle(dir, to) > angle * 0.5f) continue;

            var player = hit.GetComponentInParent<PlayerController>();
            if (player != null) player.DamageTaken();
            return;
        }
    }

    // 짧은 거리를 빠르게 이동 (끝에서 감속)
    IEnumerator MoveQuick(Vector2 target, float time)
    {
        Vector2 from = transform.position;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            transform.position = Vector2.Lerp(from, target, 1f - (1f - t) * (1f - t));
            yield return null;
        }
        transform.position = target;
    }

    public IEnumerator Entrance(Vector2 targetPos)
    {
        yield return StartCoroutine(EntranceFromRight(targetPos));
    }

    // ── 돌진 ─────────────────────────────────────────────────
    /// <param name="onWarn">경고선이 뜬 순간 (출발점, 도착점) — 동료가 돌진 경로 양옆을 막을 때 사용</param>
    public IEnumerator Charge(System.Action<Vector2, Vector2> onWarn = null)
    {
        if (PlayerTF == null) yield break;
        FacePlayer();

        Vector2 start = transform.position;
        Vector2 dir = ((Vector2)PlayerTF.position - start).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;
        Vector2 end = ClampToArena(start + dir * _chargeDistance);

        var line = _attack.ShowLine(start, dir);
        onWarn?.Invoke(start, end);
        yield return new WaitForSeconds(_chargeWarnTime);
        _attack.HideLine(line);

        SoundManager.Instance.PlaySFX("SFX/Player_Dash");
        float elapsed = 0f;
        while (elapsed < _chargeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _chargeTime);
            transform.position = Vector2.Lerp(start, end, 1f - (1f - t) * (1f - t)); // 빠르게 출발해서 끝에서 감속
            HitPlayerOnContact();
            yield return null;
        }

        IngameManager.Instance.ShakeCamera(0.12f, 0.12f);
        yield return new WaitForSeconds(_afterChargeWait);
    }

    // 돌진하는 몸에 플레이어가 닿으면 피해 (피격 무적이 있어서 한 번의 돌진에 여러 번 맞지 않는다)
    void HitPlayerOnContact()
    {
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, _contactRadius))
        {
            if (!hit.CompareTag("PlayerHitbox")) continue;
            var player = hit.GetComponentInParent<PlayerController>();
            if (player != null) player.DamageTaken();
            return;
        }
    }

    // ── 충격파 ───────────────────────────────────────────────
    /// <param name="onWarn">범위가 표시된 순간 (중심) — 동료가 밀려날 바깥쪽을 막을 때 사용</param>
    public IEnumerator Shockwave(System.Action<Vector2> onWarn = null)
    {
        FacePlayer();
        Vector2 center = transform.position;

        bool done = false;
        _attack.FireAreaAttack(center, _shockwaveSize, 0f, () => done = true);
        onWarn?.Invoke(center);
        yield return new WaitUntil(() => done);
    }

    public float ShockwaveRadius => _shockwaveSize.x * 0.5f;

    Vector2 ClampToArena(Vector2 p) =>
        new Vector2(Mathf.Clamp(p.x, _arenaMin.x, _arenaMax.x), Mathf.Clamp(p.y, _arenaMin.y, _arenaMax.y));
}
