using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Enemy의 공격을 담당하는 스크립트
/// 1. 방사형 탄막 - 주변에 흩뿌리는 형식의 탄막
/// 2. 단발 탄막 - 특정 위치로 빠르게 발사하는 적은 수의 탄막
/// 3. 범위 공격 - 원모양의 넓은 범위에 데미지를 입히는 공격
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("방사형 탄막")]
    [FormerlySerializedAs("bulletCount")]
    [SerializeField] public int _defaultBulletCount = 16;
    [SerializeField] private float bulletSpeed = 4f;

    [Header("단발 탄막")]
    [SerializeField] private float fastBulletSpeed = 12f;

    [Header("범위 공격")]
    [SerializeField] private GameObject _areaDamageZonePrefab;
    [SerializeField] private float _areaTelegraphDuration = 1.5f;

    [Header("탄막 궤적 전조")]
    [SerializeField] private GameObject _bulletTelegraphPrefab; // BulletTelegraph 프리팹
    [SerializeField] private float _telegraphDuration = 0.8f;  // 전조 표시 시간

    // 경고선·범위 공격 인스턴스 풀
    private readonly List<BulletTelegraph> _telegraphPool = new();
    private readonly List<AreaDamageZone> _allZones = new();     // 생성된 전부 (ClearAll 용)
    private readonly Queue<AreaDamageZone> _idleZones = new();   // 대기 중 (재사용 용)
    private Transform _attackRoot;

    void Awake()
    {
        _attackRoot = new GameObject($"{name}_Attacks").transform;
    }

    void OnDestroy()
    {
        // 노드 재시작 시 적이 Destroy 되면 공격 오브젝트도 함께 정리
        if (_attackRoot != null)
            Destroy(_attackRoot.gameObject);
    }

    // ── 방사형 탄막 (전조 없음) ───────────────────────────────
    private void FireRadialAttack(int bulletCount)
    {
        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = angleStep * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));
            VFXManager.Instance.FireBullet("VFX/TestBullet", transform.position,
                dir * bulletSpeed);
        }
    }

    // ── 방사형 탄막 (궤적 전조 있음) ─────────────────────────

    public void FireRadialAttackWithTelegraph(System.Action onComplete = null)
    => FireRadialAttackWithTelegraph(_defaultBulletCount, onComplete);

    public void FireRadialAttackWithTelegraph(int bulletCount, System.Action onComplete = null)
    {
        StartCoroutine(RadialTelegraphRoutine(bulletCount, onComplete));
    }

    private IEnumerator RadialTelegraphRoutine(int bulletCount, System.Action onComplete)
    {
        // 1) 방향별 궤적 표시
        float angleStep = 360f / bulletCount;
        var telegraphs = new List<BulletTelegraph>();

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = angleStep * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad));

            var telegraph = GetTelegraph();
            if (telegraph == null) continue;   // 프리팹 미연결 시에도 탄막은 발사
            telegraph.Show(transform.position, dir);
            telegraphs.Add(telegraph);
        }

        // 2) 전조 시간 대기
        yield return new WaitForSeconds(_telegraphDuration);

        // 3) 궤적 페이드 아웃 + 동시에 탄막 발사
        foreach (var t in telegraphs)
            t.Hide(() => ReturnTelegraph(t));

        SoundManager.Instance.PlaySFX("SFX/BulletFire");
        FireRadialAttack(bulletCount);

        onComplete?.Invoke();
    }

    // ── 단발 발사 ─────────────────────────────────────────────

    public void FireSingleShot(Vector2 targetPosition)
    {
        Vector2 dir = (targetPosition - (Vector2)transform.position).normalized;
        VFXManager.Instance.FireBullet("VFX/TestBullet", transform.position,
            dir * fastBulletSpeed);
    }

    // ── 단발 발사 (궤적 전조 있음) ───────────────────────────

    public void FireSingleShotWithTelegraph(Vector2 targetPosition,
                                             System.Action onComplete = null)
    {
        StartCoroutine(SingleTelegraphRoutine(targetPosition, onComplete));
    }

    private IEnumerator SingleTelegraphRoutine(Vector2 targetPosition,
                                                System.Action onComplete)
    {
        Vector2 dir = (targetPosition - (Vector2)transform.position).normalized;

        var telegraph = GetTelegraph();
        telegraph?.Show(transform.position, dir);

        yield return new WaitForSeconds(_telegraphDuration);

        telegraph?.Hide(() => ReturnTelegraph(telegraph));
        SoundManager.Instance.PlaySFX("SFX/BulletFire");
        FireSingleShot(targetPosition);

        onComplete?.Invoke();
    }

    // ── 범위 공격 ─────────────────────────────────────────────

    public void FireAreaAttack(Vector3 pos, Vector2 size, float angle = 0f, System.Action onComplete = null)
    {
        var zone = GetAreaZone();
        if (zone == null)
        {
            onComplete?.Invoke();   // 생성 실패해도 기다리는 패턴이 멈추지 않게
            return;
        }

        zone.Fire(pos, size, _areaTelegraphDuration, angle, () =>
        {
            ReturnAreaZone(zone);
            onComplete?.Invoke();
        });
    }

    public void FireAreaAttackAtPlayer(Vector2 size, System.Action onComplete = null)
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return;
        FireAreaAttack(playerObj.transform.position, size, 0f, onComplete);
    }

    public void FireMultiAreaAttack(Vector3[] positions, Vector2 size, System.Action onComplete = null)
    {
        if (positions.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int remaining = positions.Length;
        foreach (var pos in positions)
        {
            FireAreaAttack(pos, size, 0f, () =>
            {
                remaining--;
                if (remaining <= 0) onComplete?.Invoke();
            });
        }
    }
    // ── 패턴 중지(죽었을때 사용) ────────────────────────────────────
    public void ClearAll()
    {
        StopAllCoroutines();

        foreach (var zone in _allZones)
        {
            if (!zone.gameObject.activeSelf) continue;   // 활성 = 사용 중 (대기 중인 것은 항상 비활성)
            zone.Cancel();
            _idleZones.Enqueue(zone);
        }

        foreach (var t in _telegraphPool)
            if (t.gameObject.activeSelf)
                t.Hide();
    }
    // ── BulletTelegraph 풀 ────────────────────────────────────

    private BulletTelegraph GetTelegraph()
    {
        // 비활성 상태인 풀 항목 재사용
        foreach (var t in _telegraphPool)
            if (!t.gameObject.activeSelf) return t;

        // 없으면 새로 생성
        if (_bulletTelegraphPrefab == null)
        {
            Debug.LogWarning("[EnemyAttack] _bulletTelegraphPrefab이 연결되지 않았습니다.");
            return null;
        }

        var go = Instantiate(_bulletTelegraphPrefab, _attackRoot);
        var bt = go.GetComponent<BulletTelegraph>();
        _telegraphPool.Add(bt);
        return bt;
    }

    private void ReturnTelegraph(BulletTelegraph telegraph)
    {
        if (telegraph != null)
            telegraph.gameObject.SetActive(false);
    }
    // ── AreaDamageZone 풀 ────────────────────────────────────
    private AreaDamageZone GetAreaZone()
    {
        if (_idleZones.Count > 0)
            return _idleZones.Dequeue();   // Fire() 안에서 SetActive(true) 하므로 여기서는 안 켬

        if (_areaDamageZonePrefab == null) return null;

        var zone = Instantiate(_areaDamageZonePrefab, _attackRoot).GetComponent<AreaDamageZone>();
        _allZones.Add(zone);
        return zone;
    }

    private void ReturnAreaZone(AreaDamageZone zone)
    {
        zone.gameObject.SetActive(false);
        _idleZones.Enqueue(zone);
    }
}