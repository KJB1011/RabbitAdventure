using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy중 Boss의 행동을 담당하는 스크립트
/// 체력이 50%이하로 내려가면 2페이즈가 되며,
/// 1페이즈는 4개, 2페이즈는 2개 더 더하여 총 6개의 패턴이 있다.
/// </summary>
public class Enemy_Boss : EnemyController
{
    [Header("Phase 설정")]
    [SerializeField] int _phase1BulletCount = 12;
    [SerializeField] int _phase2BulletCount = 20;
    [SerializeField] float _phase2Threshold = 0.5f;

    [Header("Phase 2 전환 연출")]
    [SerializeField] SpriteRenderer _bodySprite;
    [SerializeField] float _phase2ScaleMultiplier = 1.2f;
    [SerializeField] float _phase2FlashDuration = 0.15f;
    [SerializeField] int _phase2FlashCount = 5;

    [Header("웨이포인트")]
    [SerializeField]
    Vector2[] _waypoints = new Vector2[]
    {
        new Vector2( 0f,  2f),
        new Vector2(-5f,  0f),
        new Vector2( 0f, -2f),
        new Vector2( 5f,  0f),
    };

    [Header("나선형 패턴")]
    [SerializeField] int _spiralArms = 3;
    [SerializeField] int _spiralRounds = 3;
    [SerializeField] float _spiralRotateSpeed = 180f;
    [SerializeField] float _spiralFireInterval = 0.05f;

    [Header("집중 발사 패턴")]
    [SerializeField] int _focusShotCount = 7;
    [SerializeField] float _focusSpreadAngle = 30f;
    [SerializeField] int _focusWaveCount = 3;
    [SerializeField] float _focusWaveDelay = 0.4f;
    [SerializeField] float _focusBulletSpeed = 7f;

    [Header("회전 레이저 (Phase 2)")]
    [SerializeField] int _laserCount = 4;
    [SerializeField] float _laserRotateSpeed = 90f;
    [SerializeField] float _laserDuration = 4f;
    [SerializeField] float _laserFireInterval = 0.06f;
    [SerializeField] float _laserBulletSpeed = 6f;

    [Header("범위 공격 패턴")]
    [SerializeField] Vector2 _areaSize = new Vector2(3f, 3f);
    [SerializeField] Vector2 _areaLargeSize = new Vector2(5f, 5f);

    bool _isPhase2 = false;
    int _patternIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(_totalHp);
    }

    void Start()
    {
        StartCoroutine(BossPatternLoop());
    }

    IEnumerator BossPatternLoop()
    {
        yield return StartCoroutine(EntranceFromRight(Vector2.zero));
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            int patternCount = _isPhase2 ? 6 : 4;
            int pattern = _patternIndex % patternCount;
            _patternIndex++;

            switch (pattern)
            {
                case 0: yield return StartCoroutine(PatternRadial()); break;
                case 1: yield return StartCoroutine(PatternSpiral()); break;
                case 2: yield return StartCoroutine(PatternFocus()); break;
                case 3: yield return StartCoroutine(PatternSurroundArea()); break;
                case 4: yield return StartCoroutine(PatternLaser()); break; // Phase2
                case 5: yield return StartCoroutine(PatternGridBomb()); break; // Phase2
            }

            yield return new WaitForSeconds(0.5f);

            if (!_isPhase2 && hpRate <= _phase2Threshold)
                yield return StartCoroutine(Phase2EntranceRoutine());
        }
    }

    // ── 패턴 1: 웨이포인트 방사형 ────────────────────────────

    IEnumerator PatternRadial()
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 target = _waypoints[(_patternIndex + i) % _waypoints.Length];
            yield return StartCoroutine(MoveToPosition(target));
            yield return new WaitForSeconds(0.2f);

            int bulletCount = _isPhase2 ? _phase2BulletCount : _phase1BulletCount;

            bool done = false;
            _attack.FireRadialAttackWithTelegraph(bulletCount, () => done = true);
            yield return new WaitUntil(() => done);

            yield return new WaitForSeconds(1f);
        }
    }

    // ── 패턴 2: 나선형 탄막 ──────────────────────────────────

    IEnumerator PatternSpiral()
    {
        yield return StartCoroutine(MoveToPosition(Vector2.zero));
        yield return new WaitForSeconds(0.3f);

        float angle = 0f, rotated = 0f;
        float totalRotation = 360f * _spiralRounds;

        while (rotated < totalRotation)
        {
            SoundManager.Instance.PlaySFX("SFX/BulletFire");

            for (int arm = 0; arm < _spiralArms; arm++)
            {
                float armAngle = angle + (360f / _spiralArms) * arm;
                float rad = armAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float speed = _isPhase2 ? 6f : 4.5f;
                VFXManager.Instance.FireBullet("VFX/TestBullet",
                    transform.position, dir * speed);
            }

            float delta = _spiralRotateSpeed * _spiralFireInterval;
            angle += delta; rotated += delta;
            yield return new WaitForSeconds(_spiralFireInterval);
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ── 패턴 3: 집중 발사 ────────────────────────────────────

    IEnumerator PatternFocus()
    {
        Vector2 playerPos = PlayerTF != null ? (Vector2)PlayerTF.position : Vector2.zero;

        Vector2 moveTarget = playerPos.x > 0 ? new Vector2(-5f, 0f) : new Vector2(5f, 0f);

        yield return StartCoroutine(MoveToPosition(moveTarget));
        yield return new WaitForSeconds(0.3f);

        int waves = _isPhase2 ? _focusWaveCount + 1 : _focusWaveCount;
        for (int w = 0; w < waves; w++)
        {
            if (PlayerTF != null) playerPos = PlayerTF.position;
            SoundManager.Instance?.PlaySFX("SFX/BulletFire");

            Vector2 baseDir = (playerPos - (Vector2)transform.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float step = _focusShotCount > 1 ? _focusSpreadAngle / (_focusShotCount - 1) : 0f;

            for (int i = 0; i < _focusShotCount; i++)
            {
                float shotAngle = baseAngle - _focusSpreadAngle * 0.5f + step * i;
                float rad = shotAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                VFXManager.Instance.FireBullet("VFX/TestBullet",
                    transform.position, dir * _focusBulletSpeed);
            }

            yield return new WaitForSeconds(_focusWaveDelay);
        }

        yield return new WaitForSeconds(0.5f);
    }

    // ── 패턴 4: 포위 범위 공격 ─────────────────────────

    IEnumerator PatternSurroundArea()
    {
        yield return StartCoroutine(MoveToPosition(Vector2.zero));
        yield return new WaitForSeconds(0.3f);

        if (PlayerTF == null) yield break;

        Vector3 pPos = PlayerTF.position;

        // 플레이어 바깥쪽 4곳에 범위 공격
        Vector3[] outerPositions = new Vector3[]
        {
            pPos + Vector3.right  * 4f,
            pPos + Vector3.left   * 4f,
            pPos + Vector3.up     * 4f,
            pPos + Vector3.down   * 4f,
        };

        bool outerDone = false;
        _attack.FireMultiAreaAttack(outerPositions, _areaSize, () => outerDone = true);
        yield return new WaitUntil(() => outerDone);

        yield return new WaitForSeconds(0.1f);

        // 플레이어 바로 위치에 큰 범위 공격
        bool finalDone = false;
        _attack.FireAreaAttack(PlayerTF.position, _areaLargeSize, 0f, () => finalDone = true);
        yield return new WaitUntil(() => finalDone);
    }

    // ── 패턴 5: 회전 레이저 (Phase 2) ───────────────────────

    IEnumerator PatternLaser()
    {
        yield return StartCoroutine(MoveToPosition(Vector2.zero));
        yield return new WaitForSeconds(0.3f);

        float angle = 0f;
        float elapsed = 0f;
        float soundTimer = 0f; // 사운드 타이머

        while (elapsed < _laserDuration)
        {
            soundTimer += _laserFireInterval;
            // 0.25초마다 사운드 재생
            if (soundTimer >= 0.25f)
            {
                SoundManager.Instance?.PlaySFX("SFX/BulletFire");
                soundTimer = 0f;
            }

            for (int i = 0; i < _laserCount; i++)
            {
                float armAngle = angle + (360f / _laserCount) * i;
                float rad = armAngle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                VFXManager.Instance.FireBullet("VFX/TestBullet",
                    transform.position, dir * _laserBulletSpeed);
            }

            angle += _laserRotateSpeed * _laserFireInterval;
            elapsed += _laserFireInterval;
            yield return new WaitForSeconds(_laserFireInterval);
        }

        yield return new WaitForSeconds(0.8f);
    }

    // ── 패턴 6: 격자 폭격 (Phase 2) ─────────────────────
    // 맵을 격자로 나눠서 순서대로 폭발

    IEnumerator PatternGridBomb()
    {
        yield return StartCoroutine(MoveToPosition(new Vector2(0f, 4f)));
        yield return new WaitForSeconds(0.2f);

        // 5열 x 3행
        float[] colX = { -8f, -4f, 0f, 4f, 8f };
        float[] rowY = { -3f, 0f, 3f };

        // 열 순서 셔플
        int[] colOrder = { 0, 1, 2, 3, 4 };
        for (int i = colOrder.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (colOrder[i], colOrder[j]) = (colOrder[j], colOrder[i]);
        }

        // 열 단위로 순차 발사 - 같은 열의 3개는 동시에
        foreach (int col in colOrder)
        {
            foreach (float y in rowY)
            {
                Vector3 pos = new Vector3(colX[col], y, 0f);
                _attack.FireAreaAttack(pos, _areaSize, 0f);
            }

            // 다음 열 발사까지 텀
            yield return new WaitForSeconds(0.5f);
        }

        // 마지막 열 폭발은 다음 패턴과 겹쳐 진행됨 (공격 오브젝트는 보스와 분리되어 있어 이동해도 무관)
        yield return new WaitForSeconds(0.5f);
    }

    // ── Phase 2 전환 연출 ─────────────────────────────────────

    private IEnumerator Phase2EntranceRoutine()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        yield return StartCoroutine(MoveToPosition(Vector2.zero));
        yield return new WaitForSeconds(0.2f);

        Color originalColor = _bodySprite != null ? _bodySprite.color : Color.white;
        for (int i = 0; i < _phase2FlashCount; i++)
        {
            if (_bodySprite != null) _bodySprite.color = Color.black;
            yield return new WaitForSeconds(_phase2FlashDuration);
            if (_bodySprite != null) _bodySprite.color = originalColor;
            yield return new WaitForSeconds(_phase2FlashDuration);
        }

        transform.localScale *= _phase2ScaleMultiplier;

        yield return new WaitForSeconds(0.3f);

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = true;

        _isPhase2 = true;
        Debug.Log("[Boss] Phase 2 시작!");
    }
    // Phase 2 진입 전에는 HP가 임계값(50%) 아래로 안 내려감
    protected override int ClampHp(int hp)
    {
        if (_isPhase2) return Mathf.Max(hp, 0);
        return Mathf.Max(hp, Mathf.CeilToInt(_totalHp * _phase2Threshold));
    }
}