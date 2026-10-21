using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지를 제외한 HUD 텍스트들을 관리합니다.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [SerializeField] GameObject _prefab;
    [SerializeField] int _prewarmCount = 15;

    [Header("플레이어 피격")]
    [SerializeField] Color _damageColor = Color.red;
    [SerializeField] float _damageFontSize = 3f;

    [Header("아이템 획득")]
    [SerializeField] Color _itemColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] float _itemFontSize = 2.5f;

    [Header("겹침 방지")]
    [SerializeField] float _stackOffset = 0.5f; // 위로 밀어올리는 거리

    Queue<FloatingText> _pool = new();
    List<FloatingText> _activeList = new(); // 현재 활성화된 텍스트 추적

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_prefab == null) { Debug.LogError("[FloatingTextManager] _prefab 미연결"); return; }
        if (_prefab.GetComponent<FloatingText>() == null) { Debug.LogError("[FloatingTextManager] FloatingText 컴포넌트 없음"); return; }

        for (int i = 0; i < _prewarmCount; i++)
            _pool.Enqueue(CreateNew());
    }

    private FloatingText CreateNew()
    {
        var go = Instantiate(_prefab, transform);
        go.SetActive(false);
        return go.GetComponent<FloatingText>();
    }

    private FloatingText Get()
    {
        var ft = _pool.Count > 0 ? _pool.Dequeue() : CreateNew();
        ft.gameObject.SetActive(true);
        return ft;
    }

    private void Release(FloatingText ft)
    {
        _activeList.Remove(ft);
        ft.ReturnCleanup();
        _pool.Enqueue(ft);
    }

    private void PlayText(string content, Color color, float fontSize, Vector3 worldPos)
    {
        // 기존 활성 텍스트들을 위로 밀어올림
        foreach (var active in _activeList)
            active.PushUp(_stackOffset);

        var ft = Get();
        _activeList.Add(ft);
        ft.Play(content, color, fontSize, worldPos, Release);
    }

    public void ShowDamage(Vector3 worldPos)
        => PlayText("-1", _damageColor, _damageFontSize, worldPos);

    public void ShowItemEffect(string content, Vector3 worldPos)
        => PlayText(content, _itemColor, _itemFontSize, worldPos);

    public void ShowCustom(string content, Color color, float fontSize, Vector3 worldPos)
        => PlayText(content, color, fontSize, worldPos);
}