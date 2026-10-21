using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적의 HP바를 나타내주는 UI 스크립트
/// </summary>
public class UIHPBar : MonoBehaviour
{
    [SerializeField] Slider _hpSlider;
    [SerializeField] GameObject _contents;

    void Start()
    {
        Hide();
    }

    public void Show()
    {
        _hpSlider.value = 1f;
        if (_contents != null) _contents.SetActive(true);
    }

    // 적이 데미지를 받을 때마다 호출
    public void SetBarValue(float hpRate)
    {
        _hpSlider.value = hpRate;

        if (hpRate <= 0)
            Hide();
    }

    public void Hide()
    {
        if (_contents != null) _contents.SetActive(false);
    }
}