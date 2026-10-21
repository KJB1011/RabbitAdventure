using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택창UI 스크립트
/// </summary>
public class UICharSelectWindow : MonoBehaviour
{
    [SerializeField] Button _playButton;
    [SerializeField] TMP_InputField _nickInputField;
    [SerializeField] TextMeshProUGUI _nickNoticeText;

    [Header("닉네임 설정")]
    [SerializeField] int _maxLength = 10;

    void Start()
    {
        _nickInputField.characterLimit = _maxLength;
        _nickNoticeText.text = "";
        _playButton.interactable = false;

        _nickInputField.onValueChanged.AddListener(OnNickChanged);
    }

    private void OnNickChanged(string value)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(value);

        // 버튼 활성화
        _playButton.interactable = !isEmpty;

        // 안내 텍스트
        if (isEmpty)
        {
            _nickNoticeText.text = "닉네임을 입력해주세요.";
            _nickNoticeText.color = Color.red;
        }
        else if (value.Length >= _maxLength)
        {
            _nickNoticeText.text = $"최대 {_maxLength}글자까지 입력 가능합니다.";
            _nickNoticeText.color = Color.yellow;
        }
        else
        {
            _nickNoticeText.text = "";
        }
    }

    public void ClickPlayButton()
    {
        string nickname = _nickInputField.text.Trim();
        if (string.IsNullOrWhiteSpace(nickname)) return;
        SoundManager.Instance.PlaySFX("SFX/Button");

        RememberManager.Instance.GameStart(nickname);
    }
}