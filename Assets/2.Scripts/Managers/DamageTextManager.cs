using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지를 나타내는 HUD 텍스트를 관리합니다.
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _prewarmCount = 20;

    [Header("Special 스킬")]
    [SerializeField] private float _specialFontSize = 6f;
    [SerializeField] private Color _specialColor = new Color(1f, 0.4f, 0f);

    private Queue<DamageText> _pool = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_prefab == null)
        {
            Debug.LogError("[DamageTextManager] _prefab이 연결되지 않았습니다.");
            return;
        }
        if (_prefab.GetComponent<DamageText>() == null)
        {
            Debug.LogError($"[DamageTextManager] '{_prefab.name}'에 DamageText 컴포넌트가 없습니다.");
            return;
        }

        for (int i = 0; i < _prewarmCount; i++)
            _pool.Enqueue(CreateNew());
    }

    private DamageText CreateNew()
    {
        var go = Instantiate(_prefab, transform);
        go.SetActive(false);
        return go.GetComponent<DamageText>();
    }

    private DamageText Get()
    {
        var dt = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        dt.gameObject.SetActive(true);
        return dt;
    }

    private void Release(DamageText dt)
    {
        dt.ReturnCleanup();
        _pool.Enqueue(dt);
    }

    /// <summary>
    /// 일반 / 치명타 데미지 표시
    /// </summary>
    public void Show(int damage, bool isCrit, Vector3 worldPos)
    {
        if (damage <= 0) return;
        var dt = Get();
        dt.Play(damage, isCrit, worldPos, Release);
    }

    /// <summary>
    /// Special 스킬 데미지 표시
    /// </summary>
    public void ShowSpecial(int damage, Vector3 worldPos)
    {
        if (damage <= 0) return;
        var dt = Get();
        dt.Play($"{damage}!!", _specialColor, _specialFontSize, worldPos, Release);
    }
}