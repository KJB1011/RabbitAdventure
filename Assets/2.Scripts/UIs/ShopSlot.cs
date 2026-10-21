using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using static Defines;

/// <summary>
/// 상점에 있는 아이템 슬롯(버튼)을 담당하는 스크립트
/// </summary>
public class ShopSlot : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI 참조")]
    [SerializeField] Image _itemImage;
    [SerializeField] TextMeshProUGUI _priceText;
    [SerializeField] RectTransform _imageRect;
    [SerializeField] GameObject _descPanel;
    [SerializeField] TextMeshProUGUI _descText;

    [Header("슬롯 설정")]
    [SerializeField] bool _isFixedItem = false; // 고정 아이템이면 체크

    [Header("호버 설정")]
    [SerializeField] float _hoverScale = 1.15f;
    [SerializeField] float _hoverDuration = 0.15f;

    [Header("구매 알림")]
    [SerializeField] ShopNotification _notification;

    public enum SlotType { Artifact, FixedItem }

    SlotType _slotType;
    ArtifactSO _artifactData;
    ITEM _fixedItem;
    int _price;
    bool _sold = false;
    Vector3 _originalScale;
    Action _onPurchased;

    // ── 슬롯 초기화 ───────────────────────────────────────────

    public void SetupArtifact(ArtifactSO data, Action onPurchased)
    {
        _slotType = SlotType.Artifact;
        _artifactData = data;
        _onPurchased = onPurchased;
        _price = data.price;
        _sold = false;

        _itemImage.sprite = data.icon;
        _itemImage.color = Color.white;
        _priceText.text = $"{data.price} G";

        if (_descText != null) _descText.text = data.description;
        if (_descPanel != null) _descPanel.SetActive(false);

        gameObject.SetActive(true);
    }

    public void SetupFixedItem(ITEM item, Sprite icon, string desc, int price,
                                Action onPurchased = null)
    {
        _slotType = SlotType.FixedItem;
        _fixedItem = item;
        _price = price;
        _onPurchased = onPurchased;
        _sold = false;

        _itemImage.sprite = icon;
        _itemImage.color = Color.white;
        _priceText.text = $"{price} G";

        if (_descText != null) _descText.text = desc;
        if (_descPanel != null) _descPanel.SetActive(false);

        gameObject.SetActive(true);
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

        IngameManager.Instance.SpendGold(_price);

        switch (_slotType)
        {
            case SlotType.Artifact:
                ArtifactManager.Instance.Acquire(_artifactData);
                _sold = true;
                _itemImage.color = new Color(1f, 1f, 1f, 0.3f);
                _priceText.text = "구매 완료";
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