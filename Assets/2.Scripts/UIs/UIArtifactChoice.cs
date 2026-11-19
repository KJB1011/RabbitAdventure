using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// "3개 중 하나 고르기" 창. (상자의 아티팩트 보상, 스킬 숙련도 강화에서 함께 사용)
/// 열리면 화면이 어두워지고 플레이어 조작이 잠기며, 하나를 고르면 닫힌다.
/// 이 컴포넌트가 붙은 오브젝트는 항상 켜 두고, 실제로 보이는 부분(_panel)만 켜고 끈다.
/// </summary>
public class UIArtifactChoice : MonoBehaviour
{
    public static UIArtifactChoice Instance { get; private set; }

    /// <summary>카드 한 장에 보여줄 내용</summary>
    public readonly struct Option
    {
        public readonly Sprite Icon;
        public readonly string Name;
        public readonly string Description;

        public Option(Sprite icon, string name, string description)
        {
            Icon = icon;
            Name = name;
            Description = description;
        }
    }

    [Header("연결")]
    [SerializeField] CanvasGroup _panel;                 // 어두운 배경 + 카드 전체 (켜고 끄는 부분)
    [SerializeField] UIArtifactChoiceCard[] _cards;      // 고를 수 있는 카드들 (보통 3장)
    [SerializeField] TMP_Text _titleText;

    [Header("연출")]
    [SerializeField] float _openDelay = 0.3f;            // 상자가 부서지는 모습을 잠깐 보여준 뒤 열기
    [SerializeField] float _fadeTime = 0.25f;            // 어두워지는/밝아지는 시간
    [SerializeField] float _cardPopInterval = 0.08f;     // 카드가 하나씩 튀어나오는 간격
    [SerializeField] float _afterChooseDelay = 0.6f;     // 고른 뒤 결과를 보여주는 시간

    Action<int> _onChosen;   // 고른 카드 번호
    Action _onClosed;
    PlayerController _player;
    bool _wasControllable;   // 열기 전의 조작 상태 (상점 안에서 열려도 닫을 때 원래대로)
    bool _isChoosing;
    string _defaultTitle;

    /// <summary>창이 열려 있는 중인지</summary>
    public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

    void Awake()
    {
        Instance = this;
        if (_titleText != null) _defaultTitle = _titleText.text;
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_panel != null) _panel.DOKill();
    }

    /// <summary>
    /// 아직 없는 아티팩트 중에서 골라 획득하는 창을 연다. (상자 보상)
    /// 칸이 가득 찼거나, 남은 아티팩트가 없거나, 이미 열려 있으면 열지 않고 false를 돌려준다.
    /// </summary>
    /// <param name="onClosed">하나를 골라 창이 완전히 닫힌 뒤 호출</param>
    public bool TryOpen(Action onClosed)
    {
        var manager = ArtifactManager.Instance;
        if (manager == null || manager.IsFull) return false;

        var artifacts = manager.GetRandomUnowned(_cards.Length);
        var options = new List<Option>();
        foreach (var a in artifacts)
            options.Add(new Option(a.icon, a.artifactName, a.description));

        return TryOpenOptions(_defaultTitle, options,
            index => ArtifactManager.Instance.Acquire(artifacts[index]),
            onClosed);
    }

    /// <summary>
    /// 원하는 내용으로 창을 연다. (스킬 강화 등)
    /// </summary>
    /// <param name="onChosen">카드를 고른 순간 호출 (고른 카드 번호)</param>
    /// <param name="onClosed">창이 완전히 닫힌 뒤 호출</param>
    public bool TryOpenOptions(string title, IList<Option> options, Action<int> onChosen, Action onClosed = null)
    {
        if (_panel == null || IsOpen || options == null || options.Count == 0) return false;

        _onChosen = onChosen;
        _onClosed = onClosed;
        _isChoosing = true;

        if (_titleText != null) _titleText.text = string.IsNullOrEmpty(title) ? _defaultTitle : title;

        // 조작 잠금 (상점과 같은 방식). 닫을 때 원래 상태로 되돌리기 위해 기억해 둔다
        _player = FindFirstObjectByType<PlayerController>();
        if (_player != null)
        {
            _wasControllable = _player.IsControllable;
            _player.SetControllable(false);
        }

        // 패널을 먼저 켠다 (꺼진 상태에서는 카드의 Awake가 아직 실행되지 않았을 수 있다)
        _panel.gameObject.SetActive(true);

        // 카드 채우기 (고를 게 카드 수보다 적으면 남는 카드는 숨김)
        for (int i = 0; i < _cards.Length; i++)
        {
            bool used = i < options.Count;
            _cards[i].gameObject.SetActive(used);
            if (!used) continue;

            _cards[i].Setup(i, options[i], OnCardChosen);
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

        _onChosen?.Invoke(chosen.Index);
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
        if (_player != null) _player.SetControllable(_wasControllable);

        // 콜백 안에서 다시 열 수도 있으니 비워 둔 뒤 호출
        var callback = _onClosed;
        _onClosed = null;
        _onChosen = null;
        callback?.Invoke();
    }
}
