using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 아티팩트 선택 창의 카드 한 장.
/// 아이콘 뒤의 빛(_glow)은 천천히 돌면서 숨쉬듯 밝아졌다 어두워진다.
/// </summary>
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class UIArtifactChoiceCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("연결")]
    [SerializeField] Image _icon;
    [SerializeField] TMP_Text _nameText;
    [SerializeField] TMP_Text _descText;
    [SerializeField] Image _glow;                         // 아이콘 뒤에서 빛나는 이미지

    [Header("빛 연출")]
    [SerializeField] float _glowSpinSpeed = 25f;          // 초당 회전 각도
    [SerializeField, Range(0f, 1f)] float _glowAlphaMin = 0.45f;
    [SerializeField, Range(0f, 1f)] float _glowAlphaMax = 0.95f;
    [SerializeField] float _glowPulseTime = 0.8f;

    [Header("크기 연출")]
    [SerializeField] float _hoverScale = 1.06f;           // 마우스를 올렸을 때
    [SerializeField] float _chosenScale = 1.15f;          // 골랐을 때

    /// <summary>이 카드에 표시 중인 아티팩트</summary>
    public ArtifactSO Data { get; private set; }

    Action<UIArtifactChoiceCard> _onClick;
    CanvasGroup _group;
    bool _canHover;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        GetComponent<Button>().onClick.AddListener(() => _onClick?.Invoke(this));
    }

    public void Setup(ArtifactSO data, Action<UIArtifactChoiceCard> onClick)
    {
        Data = data;
        _onClick = onClick;

        if (_icon != null)
        {
            _icon.sprite = data.icon;
            _icon.preserveAspect = true;
        }
        if (_nameText != null) _nameText.text = data.artifactName;
        if (_descText != null) _descText.text = data.description;
    }

    /// <summary>delay초 뒤에 작게 → 원래 크기로 튀어나온다</summary>
    public void PlayAppear(float delay)
    {
        KillTweens();
        _canHover = false;

        transform.localScale = Vector3.one * 0.6f;
        _group.alpha = 0f;

        _group.DOFade(1f, 0.2f).SetDelay(delay).SetUpdate(true);
        transform.DOScale(1f, 0.35f)
                 .SetDelay(delay)
                 .SetEase(Ease.OutBack)
                 .SetUpdate(true)
                 .OnComplete(() => _canHover = true);

        StartGlowPulse();
    }

    /// <summary>골라진 카드: 살짝 커지고 빛이 가장 밝게</summary>
    public void PlayChosen()
    {
        _canHover = false;
        transform.DOKill();
        transform.DOScale(_chosenScale, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

        if (_glow != null)
        {
            _glow.DOKill();
            _glow.DOFade(1f, 0.15f).SetUpdate(true);
        }
    }

    /// <summary>골라지지 않은 카드: 작아지며 흐려진다</summary>
    public void PlayNotChosen()
    {
        _canHover = false;
        transform.DOKill();
        transform.DOScale(0.9f, 0.25f).SetUpdate(true);
        _group.DOKill();
        _group.DOFade(0.25f, 0.25f).SetUpdate(true);
    }

    void StartGlowPulse()
    {
        if (_glow == null) return;

        Color c = _glow.color;
        c.a = _glowAlphaMin;
        _glow.color = c;
        _glow.DOFade(_glowAlphaMax, _glowPulseTime)
             .SetEase(Ease.InOutSine)
             .SetLoops(-1, LoopType.Yoyo) // 무한 왕복
             .SetUpdate(true);
    }

    void Update()
    {
        // 빛을 천천히 회전 (시간이 멈춰도 돌도록 unscaledDeltaTime)
        if (_glow != null)
            _glow.rectTransform.Rotate(0f, 0f, -_glowSpinSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_canHover) return;
        transform.DOKill();
        transform.DOScale(_hoverScale, 0.12f).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_canHover) return;
        transform.DOKill();
        transform.DOScale(1f, 0.12f).SetUpdate(true);
    }

    void KillTweens()
    {
        transform.DOKill();
        if (_group != null) _group.DOKill();
        if (_glow != null) _glow.DOKill();
    }

    void OnDisable() => KillTweens();
}
