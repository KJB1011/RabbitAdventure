using TMPro;
using UnityEngine;

/// <summary>
/// 상점 UI 안에 배치되어 있는 플레이어 스탯 표시 패널을 담당하는 스크립트
/// </summary>
public class UIShopPlayerStats : MonoBehaviour
{
    [SerializeField] PlayerController _player;

    [Header("스탯 TMP 연결")]
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] TextMeshProUGUI _atkText;
    [SerializeField] TextMeshProUGUI _critRateText;
    [SerializeField] TextMeshProUGUI _rangeText;

    void OnEnable()
    {
        Refresh();
        _player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    public void Refresh()
    {
        if (_player == null) return;

        if (_hpText != null)
            _hpText.text = $"{_player.CurrentHp} / {_player.MaxHp}";

        if (_atkText != null)
            _atkText.text = $"{_player.Atk:F0}";

        if (_critRateText != null)
            _critRateText.text = $"{_player.CritRate:F1}%";

        if (_rangeText != null)
            _rangeText.text = $"{_player.RangeMultiplier:F1}";
    }
}