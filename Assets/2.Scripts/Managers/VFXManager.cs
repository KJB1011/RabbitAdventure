using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static Defines;

/// <summary>
/// VFX / 탄환 재생을 담당하는 매니저.
/// Resources/VFX/ 폴더에서 프리팹을 로드합니다.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("풀 설정")]
    [SerializeField] private int _vfxDefaultCapacity = 10;
    [SerializeField] private int _vfxMaxSize = 50;
    [SerializeField] private int _bulletDefaultCapacity = 32;
    [SerializeField] private int _bulletMaxSize = 200;

    [Header("성능 비교용")]
    [Tooltip("끄면 풀링 없이 매번 Instantiate/Destroy로 생성합니다. GC Alloc 비교 측정용.")]
    [SerializeField] private bool _usePooling = true;

    // 프리팹 캐싱 (SoundManager의 clipCache와 동일한 구조)
    private Dictionary<string, GameObject> _prefabCache = new();

    // 풀
    private Dictionary<string, ObjectPool<PooledVFX>> _vfxPools = new();
    private Dictionary<string, ObjectPool<PooledBullet>> _bulletPools = new();

    // 이펙트 최대 지속 시간
    float _lifetime = 3f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── VFX ──────────────────────────────────────────────────

    public PooledVFX Play(string id, Vector3 position, Quaternion rotation = default)
    {
        if (!_usePooling)
            return PlayDirect(id, position, rotation);

        var pool = GetOrCreateVFXPool(id);
        if (pool == null) return null;

        var instance = pool.Get();
        if (instance == null) return null;

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.PlayAndScheduleReturn(_lifetime, () => pool.Release(instance));
        return instance;
    }

    /// <summary>풀 없이 매번 새로 생성/파괴 (비교 측정용)</summary>
    private PooledVFX PlayDirect(string id, Vector3 position, Quaternion rotation)
    {
        var prefab = LoadPrefab($"VFX/{id}");
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 프리팹을 찾을 수 없습니다: Resources/VFX/{id}");
            return null;
        }

        var instance = Instantiate(prefab, position, rotation, transform).GetComponent<PooledVFX>();
        instance.PlayAndScheduleReturn(_lifetime, () => Destroy(instance.gameObject));
        return instance;
    }

    private ObjectPool<PooledVFX> GetOrCreateVFXPool(string id)
    {
        if (_vfxPools.TryGetValue(id, out var existing)) return existing;

        string path = $"VFX/{id}";
        var prefab = LoadPrefab(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 프리팹을 찾을 수 없습니다: Resources/{path}");
            return null;
        }

        var pool = new ObjectPool<PooledVFX>(
            createFunc: () => Instantiate(prefab, transform).GetComponent<PooledVFX>(),
            actionOnGet: vfx => vfx.gameObject.SetActive(true),
            actionOnRelease: vfx => vfx.gameObject.SetActive(false),
            actionOnDestroy: vfx => Destroy(vfx.gameObject),
            defaultCapacity: _vfxDefaultCapacity,
            maxSize: _vfxMaxSize
        );
        _vfxPools[id] = pool;
        return pool;
    }

    /// <summary>
    /// 플레이어에게 붙어서 재생되는 VFX
    /// </summary>
    public PooledVFX PlayAttached(string id, Transform target, Vector3 localOffset = default)
    {
        if (!_usePooling)
            return PlayAttachedDirect(id, target, localOffset);

        var pool = GetOrCreateVFXPool(id);
        if (pool == null) return null;

        var instance = pool.Get();
        if (instance == null) return null;

        instance.transform.SetParent(target);
        instance.transform.localPosition = localOffset;
        instance.transform.localRotation = Quaternion.identity;

        var systems = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        instance.PlayAndScheduleReturn(_lifetime, () =>
        {
            instance.transform.SetParent(transform);
            pool.Release(instance);
        });
        return instance;
    }

    private PooledVFX PlayAttachedDirect(string id, Transform target, Vector3 localOffset)
    {
        var prefab = LoadPrefab($"VFX/{id}");
        if (prefab == null) return null;

        var instance = Instantiate(prefab).GetComponent<PooledVFX>();
        instance.transform.SetParent(target);
        instance.transform.localPosition = localOffset;
        instance.transform.localRotation = Quaternion.identity;

        var systems = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        instance.PlayAndScheduleReturn(_lifetime, () => Destroy(instance.gameObject));
        return instance;
    }

    // ── 탄환 ─────────────────────────────────────────────────

    public PooledBullet FireBullet(string id, Vector3 position, Vector2 velocity, float lifetime = 5f)
    {
        if (!_usePooling)
            return FireBulletDirect(id, position, velocity, lifetime);

        var pool = GetOrCreateBulletPool(id);
        if (pool == null) return null;

        var bullet = pool.Get();
        if (bullet == null) return null;

        bullet.transform.position = position;
        bullet.Launch(velocity, lifetime, b => pool.Release(b));
        return bullet;
    }

    private PooledBullet FireBulletDirect(string id, Vector3 position, Vector2 velocity, float lifetime)
    {
        var prefab = LoadPrefab(id);
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 탄환 프리팹을 찾을 수 없습니다: Resources/VFX/{id}");
            return null;
        }

        var bullet = Instantiate(prefab, transform).GetComponent<PooledBullet>();
        bullet.transform.position = position;
        bullet.Launch(velocity, lifetime, b => Destroy(b.gameObject));
        return bullet;
    }

    private ObjectPool<PooledBullet> GetOrCreateBulletPool(string id)
    {
        if (_bulletPools.TryGetValue(id, out var existing)) return existing;

        string path = id;
        var prefab = LoadPrefab(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[VFXManager] 탄환 프리팹을 찾을 수 없습니다: Resources/VFX/{path}");
            return null;
        }

        var pool = new ObjectPool<PooledBullet>(
            createFunc: () => Instantiate(prefab, transform).GetComponent<PooledBullet>(),
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            defaultCapacity: _bulletDefaultCapacity,
            maxSize: _bulletMaxSize
        );
        _bulletPools[id] = pool;
        return pool;
    }

    public void ClearAllBullets()
    {
        var bullets = GetComponentsInChildren<PooledBullet>();
        foreach (var b in bullets)
            if (b.gameObject.activeSelf)
                b.ReleaseSelf();
    }

    // ── 유틸 ─────────────────────────────────────────────────

    private GameObject LoadPrefab(string path)
    {
        if (_prefabCache.TryGetValue(path, out var cached)) return cached;

        var prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
            _prefabCache[path] = prefab;
        return prefab;
    }
}