using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점에서 판매하는 아이템 하나의 데이터.
/// </summary>
[System.Serializable]
public class ShopItemData
{
    public string itemName;
    public Sprite icon;
    public int price;
}

/// <summary>
/// 상점 UI의 아이템 슬롯 하나.
/// </summary>
public class ShopItemSlot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private Button _buyButton;

    private ShopItemData _item;
    private System.Action<ShopItemData> _onPurchase;

    public void Setup(ShopItemData item, System.Action<ShopItemData> onPurchase)
    {
        _item = item;
        _onPurchase = onPurchase;

        if (_icon != null) _icon.sprite = item.icon;
        if (_nameText != null) _nameText.text = item.itemName;
        if (_priceText != null) _priceText.text = $"{item.price}G";

        _buyButton.onClick.RemoveAllListeners();
        _buyButton.onClick.AddListener(() => _onPurchase?.Invoke(_item));
    }

    public void SetSoldOut()
    {
        _buyButton.interactable = false;
        if (_nameText != null) _nameText.text = "품절";
    }
}