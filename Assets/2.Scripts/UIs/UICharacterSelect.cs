using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 연습방 왼쪽 포털로 여는 캐릭터 선택 창.
/// 닉네임 입력 + 캐릭터 카드. 캐릭터 카드를 누르면 그 캐릭터로 바뀌고 창이 닫힌다.
/// 카드 수보다 캐릭터가 적으면 남는 카드는 "준비 중"으로 보인다.
/// 이 컴포넌트가 붙은 오브젝트는 항상 켜 두고, 실제로 보이는 부분(_panel)만 켜고 끈다.
/// 인게임 UI(UIOverlayCanvas)와 따로 있는 캔버스(UISelectCanvas)에 둔다
/// → 처음 시작할 때 인게임 UI가 아직 투명한 상태에서도 이 창은 보인다.
/// </summary>
public class UICharacterSelect : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] CanvasGroup _panel;               // 어두운 배경 + 창 전체
    [SerializeField] RectTransform _window;            // 튀어나오는 연출을 줄 창
    [SerializeField] UICharacterCard[] _cards;
    [SerializeField] TMP_InputField _nicknameInput;
    [SerializeField] Button _closeButton;

    [Header("설정")]
    [SerializeField] int _nicknameMaxLength = 10;
    [SerializeField] float _fadeTime = 0.2f;

    Action _onClosed;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    bool _mustChoose; // 처음 시작할 때: 캐릭터를 골라야만 닫힌다 (닫기 버튼·ESC 없음)

    void Awake()
    {
        if (_panel != null) _panel.gameObject.SetActive(false);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_nicknameInput != null)
        {
            _nicknameInput.characterLimit = _nicknameMaxLength;
            _nicknameInput.onEndEdit.AddListener(OnNicknameEdited);
        }
    }

    /// <param name="mustChoose">처음 시작할 때: 고른 캐릭터 표시 없이 열고, 캐릭터 카드를 눌러야만 닫힌다</param>
    public void Open(Action onClosed, bool mustChoose = false)
    {
        if (_isOpen || _panel == null) return;
        _isOpen = true;
        _onClosed = onClosed;
        _mustChoose = mustChoose;
        if (_closeButton != null) _closeButton.gameObject.SetActive(!mustChoose);

        var player = IngameManager.Instance.Player;
        if (player != null) player.SetControllable(false);

        _panel.gameObject.SetActive(true);
        RefreshCards();
        if (_nicknameInput != null) _nicknameInput.text = IngameManager.Instance.Nickname;

        _panel.DOKill();
        _panel.alpha = 0f;
        _panel.DOFade(1f, _fadeTime).SetUpdate(true);
        if (_window != null)
        {
            _window.DOKill();
            _window.localScale = Vector3.one * 0.85f;
            _window.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        SoundManager.Instance.PlaySFX("SFX/Button");
    }

    void RefreshCards()
    {
        var list = IngameManager.Instance.Characters;
        var current = _mustChoose ? null : IngameManager.Instance.CurrentCharacter; // 처음엔 아무것도 고르지 않은 상태로
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < list.Count && list[i] != null)
                _cards[i].Setup(list[i], list[i] == current, OnCardClicked);
            else
                _cards[i].SetupLocked();
        }
    }

    void OnCardClicked(CharacterSO character)
    {
        if (!_isOpen) return;
        SoundManager.Instance.PlaySFX("SFX/Button");
        IngameManager.Instance.ChangeCharacter(character); // 같은 캐릭터면 아무 일도 안 일어난다
        Close();
    }

    void OnNicknameEdited(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _nicknameInput.text = IngameManager.Instance.Nickname; // 비우면 원래 이름으로
            return;
        }
        IngameManager.Instance.SetNickname(value);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _mustChoose = false;

        // 입력 중이던 닉네임도 저장
        if (_nicknameInput != null && !string.IsNullOrWhiteSpace(_nicknameInput.text))
            IngameManager.Instance.SetNickname(_nicknameInput.text);

        _panel.DOKill();
        _panel.DOFade(0f, _fadeTime).SetUpdate(true).OnComplete(() =>
        {
            _panel.gameObject.SetActive(false);

            // 캐릭터가 바뀌었을 수 있으므로 지금 플레이어를 다시 찾아 조작을 풀어 준다
            var player = IngameManager.Instance.Player;
            if (player != null) player.SetControllable(true);

            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        });
    }

    void Update()
    {
        // ESC로도 닫기 (닉네임 입력 중에는 입력 칸이 먼저 받는다)
        if (_isOpen && !_mustChoose && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame &&
            (_nicknameInput == null || !_nicknameInput.isFocused))
            Close();
    }
}
