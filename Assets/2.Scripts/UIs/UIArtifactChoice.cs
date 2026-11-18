using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 상자를 부쉈을 때 나오는 "아티팩트 3개 중 하나 고르기" 창.
/// 열리면 화면이 어두워지고 플레이어 조작이 잠기며, 하나를 고르면 획득 후 닫힌다.
/// 이 컴포넌트가 붙은 오브젝트는 항상 켜 두고, 실제로 보이는 부분(_panel)만 켜고 끈다.
/// </summary>
public class UIArtifactChoice : MonoBehaviour
{
    public static UIArtifactChoice Instance { get; private set; }

    [Header("연결")]
    [SerializeField] CanvasGroup _panel;                 // 어두운 배경 + 카드 전체 (켜고 끄는 부분)
    [SerializeField] UIArtifactChoiceCard[] _cards;      // 고를 수 있는 카드들 (보통 3장)
    [SerializeField] TMP_Text _titleText;

    [Header("연출")]
    [SerializeField] float _openDelay = 0.3f;            // 상자가 부서지는 모습을 잠깐 보여준 뒤 열기
    [SerializeField] float _fadeTime = 0.25f;            // 어두워지는/밝아지는 시간
    [SerializeField] float _cardPopInterval = 0.08f;     // 카드가 하나씩 튀어나오는 간격
    [SerializeField] float _afterChooseDelay = 0.6f;     // 고른 뒤 결과를 보여주는 시간

    Action _onClosed;
    PlayerController _player;
    bool _isChoosing;

    /// <summary>창이 열려 있는 중인지</summary>
    public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

    void Awake()
    {
        Instance = this;
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_panel != null) _panel.DOKill();
    }

    /// <summary>
    /// 고를 수 있는 아티팩트가 있으면 창을 연다.
    /// 칸이 가득 찼거나, 남은 아티팩트가 없거나, 이미 열려 있으면 열지 않고 false를 돌려준다.
    /// </summary>
    /// <param name="onClosed">하나를 골라 창이 완전히 닫힌 뒤 호출</param>
    public bool TryOpen(Action onClosed)
    {
        var manager = ArtifactManager.Instance;
        if (_panel == null || IsOpen || manager == null || manager.IsFull) return false;

        var choices = manager.GetRandomUnowned(_cards.Length);
        if (choices.Count == 0) return false;

        _onClosed = onClosed;
        _isChoosing = true;

        // 조작 잠금 (상점과 같은 방식)
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null) _player.SetControllable(false);

        // 패널을 먼저 켠다 (꺼진 상태에서는 카드의 Awake가 아직 실행되지 않았을 수 있다)
        _panel.gameObject.SetActive(true);

        // 카드 채우기 (고를 게 3개보다 적으면 남는 카드는 숨김)
        for (int i = 0; i < _cards.Length; i++)
        {
            bool used = i < choices.Count;
            _cards[i].gameObject.SetActive(used);
            if (!used) continue;

            _cards[i].Setup(choices[i], OnCardChosen);
            _cards[i].PlayAppear(_openDelay + _fadeTime * 0.5f + _cardPopInterval * i);
        }

        // 화면 어둡게
        _panel.alpha = 0f;
        _panel.interactable = false;
        _panel.blocksRaycasts = true;
        _panel.DOKill();
        _panel.DOFade(1f, _fadeTime)
              .SetDelay(_openDelay)
              .SetUpdate(true)
              .OnStart(() => SoundManager.Instance.PlaySFX("SFX/PhaseChange"))
              .OnComplete(() => _panel.interactable = true);

        return true;
    }

    // 카드 하나를 눌렀을 때
    void OnCardChosen(UIArtifactChoiceCard chosen)
    {
        if (!_isChoosing) return;
        _isChoosing = false;
        _panel.interactable = false; // 두 번 누르지 않게

        ArtifactManager.Instance.Acquire(chosen.Data);
        SoundManager.Instance.PlaySFX("SFX/DropCoin");

        foreach (var card in _cards)
        {
            if (!card.gameObject.activeSelf) continue;
            if (card == chosen) card.PlayChosen();
            else card.PlayNotChosen();
        }

        _panel.DOKill();
        _panel.DOFade(0f, _fadeTime)
              .SetDelay(_afterChooseDelay)
              .SetUpdate(true)
              .OnComplete(Close);
    }

    void Close()
    {
        _panel.gameObject.SetActive(false);
        if (_player != null) _player.SetControllable(true);

        // 콜백 안에서 다시 열 수도 있으니 비워 둔 뒤 호출
        var callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }
}
