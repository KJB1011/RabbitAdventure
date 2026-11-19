using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using static Defines;

/// <summary>
/// 상점에 있는 아이템 슬롯(버튼)을 담당하는 스크립트
/// 고정 아이템(HP·공격력 등, 여러 번 구매 가능) / 스킬 숙련도(스킬마다 한 번만 구매) 두 종류가 있다.
/// </summary>
public class ShopSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 참조")]
    [SerializeField] Image _itemImage;
    [SerializeField] TextMeshProUGUI _priceText;
    [SerializeField] RectTransform _imageRect;
    [SerializeField] GameObject _descPanel;
    [SerializeField] TextMeshProUGUI _nameText;   // 설명창의 이름 (비워 둬도 된다)
    [SerializeField] TextMeshProUGUI _descText;

    [Header("슬롯 설정")]
    [SerializeField] bool _isFixedItem = false; // 고정 아이템이면 체크 (마우스를 올려도 설명창이 안 열림)

    [Header("호버 설정")]
    [SerializeField] float _hoverScale = 1.15f;
    [SerializeField] float _hoverDuration = 0.15f;

    [Header("구매 알림")]
    [SerializeField] ShopNotification _notification;

    public enum SlotType { FixedItem, Mastery }

    SlotType _slotType;
    ITEM _fixedItem;
    SKILL _masterySkill;
    int _price;
    bool _sold = false;
    Vector3 _originalScale;
    Action _onPurchased;

    // ── 슬롯 초기화 ───────────────────────────────────────────

    public void SetupFixedItem(ITEM item, Sprite icon, string desc, int price,
                                Action onPurchased = null)
    {
        _slotType = SlotType.FixedItem;
        _fixedItem = item;
        _price = price;
        _onPurchased = onPurchased;

        _itemImage.sprite = icon;
        if (_descText != null) _descText.text = desc;
        if (_descPanel != null) _descPanel.SetActive(false);

        SetSoldLook(false, $"{price} G");
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 스킬 숙련도 구매 슬롯 (스킬마다 한 번만 살 수 있다).
    /// 숙련도 시스템이 없는 스킬은 "준비 중", 이미 샀거나 숙련도가 다 찼으면 "구매 불가"로 표시된다.
    /// </summary>
    public void SetupMastery(SKILL skill, Sprite icon, string name, string desc, int price, Action onPurchased = null)
    {
        _slotType = SlotType.Mastery;
        _masterySkill = skill;
        _price = price;
        _onPurchased = onPurchased;

        _itemImage.sprite = icon;
        if (_nameText != null) _nameText.text = name;
        if (_descText != null) _descText.text = desc;
        if (_descPanel != null) _descPanel.SetActive(false);

        var mastery = SkillMasteryManager.Instance;
        if (mastery == null || !mastery.HasTrack(skill))
            SetSoldLook(true, "준비 중");
        else if (!mastery.CanPurchase(skill))
            SetSoldLook(true, "구매 불가");
        else
            SetSoldLook(false, $"{price} G");

        gameObject.SetActive(true);
    }

    void SetSoldLook(bool sold, string priceText)
    {
        _sold = sold;
        _itemImage.color = sold ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
        _priceText.text = priceText;
    }

    void Awake()
    {
        _originalScale = _imageRect != null ? _imageRect.localScale : Vector3.one;
        if (_descPanel != null) _descPanel.SetActive(false);
    }

    // ── 호버 ─────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        if (_imageRect != null)
            _imageRect.DOScale(_originalScale * _hoverScale, _hoverDuration)
                      .SetEase(Ease.OutBack);

        // 고정 아이템은 설명창 안 열림
        if (!_isFixedItem && _descPanel != null)
            _descPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_imageRect != null)
            _imageRect.DOScale(_originalScale, _hoverDuration)
                      .SetEase(Ease.InBack);

        if (_descPanel != null)
            _descPanel.SetActive(false);
    }

    // ── 클릭 (구매) ──────────────────────────────────────────

    public void OnPointerClick(PointerEventData _)
    {
        if (_sold) return;

        if (IngameManager.Instance.Gold < _price)
        {
            ShowFloatingText("자금 부족!", Color.red);
            return;
        }

        // 숙련도는 스킬마다 한 번만 살 수 있다 (돈을 쓰기 전에 확인)
        if (_slotType == SlotType.Mastery &&
            (SkillMasteryManager.Instance == null || !SkillMasteryManager.Instance.CanPurchase(_masterySkill)))
        {
            ShowFloatingText("더 이상 살 수 없어요!", Color.red);
            return;
        }

        IngameManager.Instance.SpendGold(_price);

        switch (_slotType)
        {
            case SlotType.Mastery:
                SetSoldLook(true, "구매 완료");
                SkillMasteryManager.Instance.Purchase(_masterySkill); // 가득 차면 강화 선택 창이 바로 열린다
                break;

            case SlotType.FixedItem:
                ApplyFixedItem();
                break;
        }

        ShowFloatingText("구매완료!", new Color(0.2f, 0.9f, 0.2f));
        _onPurchased?.Invoke();
    }

    private void ApplyFixedItem()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        switch (_fixedItem)
        {
            case ITEM.HP: player.RecoverHp(1); break;
            case ITEM.ATKUP: player.AddAtk(1f); break;
            case ITEM.CRITRATEUP: player.AddCritRate(1f); break;
        }
    }

    // ── 알림 텍스트 ──────────────────────────────────────────

    private void ShowFloatingText(string content, Color color)
    {
        if (_notification == null)
        {
            Debug.LogWarning("[ShopSlot] _notification이 연결되지 않았습니다.");
            return;
        }
        _notification.Show(content, color, transform.position);
    }
}
