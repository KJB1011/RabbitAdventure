using UnityEngine;
using static Defines;

/// <summary>
/// 상점을 관리하는 UI 스크립트
/// 위: 스킬 숙련도 4개 (스킬마다 한 번만 구매) / 아래: 고정 아이템 3개
/// </summary>
public class UIShop : MonoBehaviour
{
    [Header("스킬 숙련도 슬롯 (위 4개, 순서: 기본공격·대쉬·강타·필살기)")]
    [SerializeField] ShopSlot[] _skillSlots = new ShopSlot[4];
    [SerializeField] Sprite[] _skillIcons = new Sprite[4];
    [SerializeField] int _masteryPrice = 20;

    [Header("고정 아이템 슬롯 (아래 3개)")]
    [SerializeField] ShopSlot[] _fixedSlots;

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
        SetupSkillSlots();
        SetupFixedSlots();
        _statsUI?.Refresh();
    }

    // ── 스킬 숙련도 슬롯 ──────────────────────────────────────
    private void SetupSkillSlots()
    {
        for (int i = 0; i < _skillSlots.Length; i++)
        {
            if (_skillSlots[i] == null) continue;

            var skill = (SKILL)i;
            string skillName = SkillMasteryManager.SkillName(skill);
            // 캐릭터 전용 아이콘이 있으면 그것 (총잡이 권총 등), 없으면 기본 아이콘
            var character = IngameManager.Instance != null ? IngameManager.Instance.CurrentCharacter : null;
            Sprite icon = character != null ? character.GetSkillIcon(i) : null;
            if (icon == null) icon = i < _skillIcons.Length ? _skillIcons[i] : null;

            _skillSlots[i].SetupMastery(
                skill, icon,
                $"{skillName} 숙련",
                $"{skillName} 숙련도를 가득 채웁니다.\n가득 차면 강화를 하나 고를 수 있습니다.\n(스킬마다 한 번만 구매 가능)",
                _masteryPrice, OnPurchased);
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
