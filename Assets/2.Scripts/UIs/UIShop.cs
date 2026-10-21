using System.Linq;
using UnityEngine;
using static Defines;

/// <summary>
/// 상점을 관리하는 UI 스크립트
/// 상점 초기화와 구매를 담당합니다.
/// </summary>
public class UIShop : MonoBehaviour
{
    [Header("아티팩트 슬롯 (위 3개)")]
    [SerializeField] ShopSlot[] _artifactSlots;

    [Header("고정 아이템 슬롯 (아래 3개)")]
    [SerializeField] ShopSlot[] _fixedSlots;

    [Header("아티팩트 데이터 목록 (모든 ArtifactSO 연결)")]
    [SerializeField] ArtifactSO[] _allArtifacts;

    [Header("고정 아이템 설정")]
    [SerializeField] Sprite _hpIcon;
    [SerializeField] Sprite _atkIcon;
    [SerializeField] Sprite _critIcon;
    [SerializeField] int _hpPrice = 5;
    [SerializeField] int _atkPrice = 5;
    [SerializeField] int _critPrice = 5;

    // 구매 후 스탯 패널 갱신용
    private UIShopPlayerStats _statsUI;

    void Awake()
    {
        _statsUI = GetComponentInChildren<UIShopPlayerStats>();
    }

    void OnEnable()
    {
        RefreshShop();
    }

    public void RefreshShop()
    {
        SetupArtifactSlots();
        SetupFixedSlots();
        _statsUI?.Refresh();
    }

    // ── 아티팩트 슬롯 ─────────────────────────────────────────
    private void SetupArtifactSlots()
    {
        var available = _allArtifacts
            .Where(a => !ArtifactManager.Instance.HasArtifact(a))
            .OrderBy(_ => Random.value)
            .Take(_artifactSlots.Length)
            .ToList();

        for (int i = 0; i < _artifactSlots.Length; i++)
        {
            if (i < available.Count)
                _artifactSlots[i].SetupArtifact(available[i], OnPurchased);
            else
                _artifactSlots[i].gameObject.SetActive(false);
        }
    }

    // ── 고정 아이템 슬롯 ──────────────────────────────────────
    private void SetupFixedSlots()
    {
        if (_fixedSlots.Length < 3) return;

        _fixedSlots[0].SetupFixedItem(
            ITEM.HP, _hpIcon,
            "HP를 1 회복합니다.",
            _hpPrice, OnPurchased);

        _fixedSlots[1].SetupFixedItem(
            ITEM.ATKUP, _atkIcon,
            "공격력을 1 증가시킵니다.",
            _atkPrice, OnPurchased);

        _fixedSlots[2].SetupFixedItem(
            ITEM.CRITRATEUP, _critIcon,
            "치명타 확률을 1% 증가시킵니다.",
            _critPrice, OnPurchased);
    }

    // ── 구매 완료 콜백 ────────────────────────────────────────
    private void OnPurchased()
    {
        SoundManager.Instance.PlaySFX("SFX/DropCoin");
        // 구매 후 스탯 패널 즉시 갱신
        _statsUI?.Refresh();
    }
}