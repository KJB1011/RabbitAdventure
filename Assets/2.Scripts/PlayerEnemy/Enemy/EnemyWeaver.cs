using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy중 Weaver(실타래술사)의 행동을 담당하는 스크립트
/// 실의 벽(빈 칸이 남은 탄 벽) -> 바늘 기습(순간이동 후 부채꼴 탄)
/// 해당 행동을 반복한다.
/// </summary>
public class EnemyWeaver : EnemyController
{
    [Header("이동 범위")]
    [SerializeField] Vector2 _arenaMin = new Vector2(-8f, -4.3f);
    [SerializeField] Vector2 _arenaMax = new Vector2(8f, 4.3f);
    [SerializeField] Vector2 _homePosition = new Vector2(3.5f, 0f); // 등장 위치

    [Header("실의 벽")]
    [SerializeField] float _laneSpacing = 0.9f;       // 탄 줄 사이 간격
    [SerializeField] int _gapLanes = 2;               // 비워 두는 줄 수 (지나갈 틈)
    [SerializeField] float _wallBulletSpeed = 5f;
    [SerializeField] int _wallWaves = 2;
    [SerializeField] float _waveInterval = 0.9f;
    [SerializeField] float _wallWarnTime = 1f;        // 벽 쪽 경고 표시 시간
    [SerializeField] float _wallWarnThickness = 1.2f; // 경고 직사각형의 두께 (탄이 나오는 방향 쪽)
    [Tooltip("탄을 채우는 범위 — 플레이어가 움직일 수 있는 범위(±9, ±5)보다 조금 넓게 해야 양 끝으로 피하지 못한다")]
    [SerializeField] Vector2 _wallCoverMin = new Vector2(-9.3f, -5.3f);
    [SerializeField] Vector2 _wallCoverMax = new Vector2(9.3f, 5.3f);
    [SerializeField] float _offscreenMargin = 0.8f;   // 탄이 화면 밖 얼마나 먼 곳에서 출발할지

    [Header("바늘 기습")]
    [SerializeField] int _ambushCount = 2;
    [SerializeField] float _ambushDistance = 3.5f;    // 플레이어와의 거리
    [SerializeField] float _vanishTime = 0.25f;       // 사라지고 나타나는 시간
    [SerializeField] int _needleCount = 5;
    [SerializeField] float _needleSpread = 50f;       // 부채꼴 전체 각도
    [SerializeField] float _needleSpeed = 9f;

    int _patternIndex;
    Vector3 _spriteBaseScale;

    protected override void Awake()
    {
        base.Awake();
        InitEnemy(_totalHp);
        if (CharSprite != null) _spriteBaseScale = CharSprite.transform.localScale;
    }

    void Start()
    {
        StartCoroutine(WeaverLoop());
    }

    IEnumerator WeaverLoop()
    {
        yield return StartCoroutine(EntranceFromRight(_homePosition));
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (_patternIndex % 2 == 0) yield return StartCoroutine(PatternWall());
            else yield return StartCoroutine(PatternNeedleAmbush());

            _patternIndex++;
            yield return new WaitForSeconds(0.6f);
        }
    }

    // ── 실의 벽 ───────────────────────────────────────────────

    IEnumerator PatternWall()
    {
        FacePlayer();

        for (int wave = 0; wave < _wallWaves; wave++)
        {
            // 좌우에서 오는 벽(줄이 세로로 늘어섬) / 위아래에서 오는 벽(줄이 가로로 늘어섬)
            bool horizontalMove = Random.value < 0.6f; // 화면이 가로로 길어서 좌우에서 오는 쪽을 조금 더 자주
            bool fromMax = Random.value < 0.5f;        // 오른쪽(위)에서 / 왼쪽(아래)에서

            // 벽을 따라 줄의 위치 (좌우 벽이면 y값들, 위아래 벽이면 x값들)
            // 양 끝이 정확히 채워지도록 간격을 살짝 조정해서 처음~끝까지 고르게 놓는다
            float from = horizontalMove ? _wallCoverMin.y : _wallCoverMin.x;
            float to = horizontalMove ? _wallCoverMax.y : _wallCoverMax.x;
            int count = Mathf.CeilToInt((to - from) / _laneSpacing) + 1;
            float step = (to - from) / (count - 1);
            var lanes = new List<float>();
            for (int n = 0; n < count; n++) lanes.Add(from + step * n);

            // 지나갈 틈: 연속된 몇 줄을 비운다 (화면 가장자리가 아니라 안쪽 범위에서만)
            float gapMin = horizontalMove ? _arenaMin.y : _arenaMin.x;
            float gapMax = horizontalMove ? _arenaMax.y : _arenaMax.x;
            var gapCandidates = new List<int>();
            for (int n = 0; n + _gapLanes - 1 < lanes.Count; n++)
                if (lanes[n] >= gapMin && lanes[n + _gapLanes - 1] <= gapMax) gapCandidates.Add(n);
            if (gapCandidates.Count > 0)
                lanes.RemoveRange(gapCandidates[Random.Range(0, gapCandidates.Count)], _gapLanes);

            GetScreenBounds(out Vector2 screenMin, out Vector2 screenMax);

            // 예고: 탄이 튀어나올 화면 끝에 딱 붙은 길쭉한 직사각형 하나 (틈 위치는 알려주지 않는다)
            var warn = ShowWallWarning(horizontalMove, fromMax, screenMin, screenMax);
            yield return new WaitForSeconds(_wallWarnTime);
            _attack.HideLine(warn);

            // 발사: 화면 밖에서 출발해 안으로 들어온다
            Vector2 dir = horizontalMove ? (fromMax ? Vector2.left : Vector2.right)
                                         : (fromMax ? Vector2.down : Vector2.up);
            SoundManager.Instance.PlaySFX("SFX/BulletFire");
            foreach (float p in lanes)
            {
                Vector2 origin = horizontalMove
                    ? new Vector2(fromMax ? Mathf.Max(screenMax.x, _wallCoverMax.x) + _offscreenMargin
                                          : Mathf.Min(screenMin.x, _wallCoverMin.x) - _offscreenMargin, p)
                    : new Vector2(p, fromMax ? Mathf.Max(screenMax.y, _wallCoverMax.y) + _offscreenMargin
                                             : Mathf.Min(screenMin.y, _wallCoverMin.y) - _offscreenMargin);
                _attack.FireBullet(origin, dir * _wallBulletSpeed);
            }

            yield return new WaitForSeconds(_waveInterval);
        }
    }

    // 화면 끝에 딱 붙은 직사각형 (벽 쪽 화면 가장자리 전체 길이)
    BulletTelegraph ShowWallWarning(bool horizontalMove, bool fromMax, Vector2 screenMin, Vector2 screenMax)
    {
        float t = _wallWarnThickness;
        // 화면 가장자리에서 두께의 절반만큼 안쪽 = 직사각형의 바깥 변이 화면 끝과 딱 맞는다
        float edge = horizontalMove
            ? (fromMax ? screenMax.x - t * 0.5f : screenMin.x + t * 0.5f)
            : (fromMax ? screenMax.y - t * 0.5f : screenMin.y + t * 0.5f);

        Vector2 center = horizontalMove ? new Vector2(edge, (screenMin.y + screenMax.y) * 0.5f)
                                        : new Vector2((screenMin.x + screenMax.x) * 0.5f, edge);
        Vector2 size = horizontalMove ? new Vector2(t, screenMax.y - screenMin.y)
                                      : new Vector2(screenMax.x - screenMin.x, t);
        return _attack.ShowRect(center, size);
    }

    // 지금 카메라에 보이는 범위 (카메라가 없으면 탄 채우는 범위로 대신)
    void GetScreenBounds(out Vector2 min, out Vector2 max)
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic)
        {
            min = _wallCoverMin; max = _wallCoverMax;
            return;
        }
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        Vector2 c = cam.transform.position;
        min = c - new Vector2(halfW, halfH);
        max = c + new Vector2(halfW, halfH);
    }

    // ── 바늘 기습 ─────────────────────────────────────────────

    IEnumerator PatternNeedleAmbush()
    {
        for (int i = 0; i < _ambushCount; i++)
        {
            if (PlayerTF == null) yield break;

            // 실을 타고 사라짐 (스프라이트를 작게)
            yield return CharSprite.transform.DOScale(Vector3.zero, _vanishTime).SetEase(Ease.InBack).WaitForCompletion();

            // 플레이어 주변의 무작위 위치로
            Vector2 player = PlayerTF.position;
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector2 pos = player + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _ambushDistance;
            pos.x = Mathf.Clamp(pos.x, _arenaMin.x, _arenaMax.x);
            pos.y = Mathf.Clamp(pos.y, _arenaMin.y, _arenaMax.y);
            transform.position = pos;
            FacePlayer();

            yield return CharSprite.transform.DOScale(_spriteBaseScale, _vanishTime).SetEase(Ease.OutBack).WaitForCompletion();

            bool done = false;
            _attack.FireFanWithTelegraph(PlayerTF.position, _needleCount, _needleSpread, _needleSpeed, () => done = true);
            yield return new WaitUntil(() => done);
            yield return new WaitForSeconds(0.4f);
        }
    }

    protected override void Die()
    {
        // 사라지는 도중에 죽어도 쓰러지는 모습이 보이게 크기 복구
        if (CharSprite != null)
        {
            CharSprite.transform.DOKill();
            CharSprite.transform.localScale = _spriteBaseScale;
        }
        base.Die();
    }
}
