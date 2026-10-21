using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 UI를 담당하는 스크립트
/// </summary>
public class UIOptionWindow : MonoBehaviour
{
    [SerializeField] Slider _sliderBGM;
    [SerializeField] Slider _sliderSFX;
    [SerializeField] TextMeshProUGUI _txtBGMRate;
    [SerializeField] TextMeshProUGUI _txtSFXRate;

    void OnEnable()
    {
        float bgmVol = SoundManager.Instance != null ? SoundManager.Instance.BGMVolume : 0.5f;
        float sfxVol = SoundManager.Instance != null ? SoundManager.Instance.SFXVolume : 1.0f;

        _sliderBGM.value = bgmVol;
        _sliderSFX.value = sfxVol;

        UpdateBGMText(bgmVol);
        UpdateSFXText(sfxVol);

        _sliderBGM.onValueChanged.AddListener(OnBGMChanged);
        _sliderSFX.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnBGMChanged(float value)
    {
        SoundManager.Instance?.SetBGMVolume(value);
        UpdateBGMText(value);
    }

    private void OnSFXChanged(float value)
    {
        SoundManager.Instance?.SetSFXVolume(value);
        UpdateSFXText(value);
    }

    void OnDisable()
    {
        _sliderBGM.onValueChanged.RemoveListener(OnBGMChanged);
        _sliderSFX.onValueChanged.RemoveListener(OnSFXChanged);
    }

    private void UpdateBGMText(float value)
    {
        _txtBGMRate.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void UpdateSFXText(float value)
    {
        _txtSFXRate.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    public void ClickCloseButton()
    {
        gameObject.SetActive(false);
    }
    public void ClickSoundButton()
    {
        SoundManager.Instance.PlaySFX("SFX/Hit");
    }
}