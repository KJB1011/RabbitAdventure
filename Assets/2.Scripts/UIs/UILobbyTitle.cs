using DG.Tweening;
using UnityEngine;

/// <summary>
/// 첫 로비의 전반적인 흐름과 연출을 담당하는 스크립트
/// </summary>
public class UILobbyTitle : MonoBehaviour
{
    [SerializeField] RectTransform titleLogo;
    [SerializeField] CanvasGroup _titleCanvas;

    [SerializeField] GameObject _uiStart;
    [SerializeField] GameObject _uiOptionWindow;
    [SerializeField] GameObject _uiCharSelectWindow;

    [Header("페이지 전환 설정")]
    [SerializeField] float _slideOffset = 800f;
    [SerializeField] float _outDuration = 0.35f;
    [SerializeField] float _inDuration = 0.4f;
    [SerializeField] Ease _outEase = Ease.InQuad;
    [SerializeField] Ease _inEase = Ease.OutBack;

    Vector2 _startOrigin;

    void Start()
    {
        _startOrigin = _uiStart.GetComponent<RectTransform>().anchoredPosition;

        _uiStart.SetActive(false);
        _uiOptionWindow.SetActive(false);
        _uiCharSelectWindow.SetActive(false);

        SoundManager.Instance.FadeInBGM("BGM/LobbyBGM", 3f);
        StartEffect();
    }

    public void StartEffect()
    {
        _uiStart.SetActive(true);

        titleLogo.anchoredPosition = new Vector2(400, 200);
        _titleCanvas.alpha = 0;

        Sequence seq = DOTween.Sequence();
        seq.Append(titleLogo.DOAnchorPos(new Vector2(400, 0), 1.0f).SetEase(Ease.OutBounce));
        seq.Join(_titleCanvas.DOFade(1, 1.5f).SetDelay(0.5f));
        seq.OnComplete(() =>
        {
            titleLogo.DOLocalMoveY(10, 2.0f)
                     .SetRelative()
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetEase(Ease.InOutSine);
        });
    }

    // ── 버튼 공통 ─────────────────────────────────────────────

    private void PlayButtonSFX()
    {
        SoundManager.Instance?.PlaySFX("SFX/Button");
    }

    // ── 페이지 전환 ───────────────────────────────────────────

    private void SlideOutToUp(GameObject targetUI, System.Action onComplete = null)
    {
        RectTransform target = targetUI.GetComponent<RectTransform>();
        Vector2 exitPos = target.anchoredPosition + Vector2.up * _slideOffset;
        target.DOAnchorPos(exitPos, _outDuration)
              .SetEase(_outEase)
              .OnComplete(() =>
              {
                  target.gameObject.SetActive(false);
                  onComplete?.Invoke();
              });
    }

    private void SlideOutToDown(GameObject targetUI, System.Action onComplete = null)
    {
        RectTransform target = targetUI.GetComponent<RectTransform>();
        Vector2 exitPos = target.anchoredPosition + Vector2.down * _slideOffset;
        target.DOAnchorPos(exitPos, _outDuration)
              .SetEase(_outEase)
              .OnComplete(() =>
              {
                  target.gameObject.SetActive(false);
                  onComplete?.Invoke();
              });
    }

    private void SlideInFromDown(GameObject targetUI, System.Action onComplete = null)
    {
        RectTransform target = targetUI.GetComponent<RectTransform>();
        target.anchoredPosition = _startOrigin + Vector2.down * _slideOffset;
        target.gameObject.SetActive(true);

        target.DOAnchorPos(_startOrigin, _inDuration)
              .SetEase(_inEase)
              .OnComplete(() => onComplete?.Invoke());
    }

    private void SlideInFromUp(GameObject targetUI, System.Action onComplete = null)
    {
        RectTransform target = targetUI.GetComponent<RectTransform>();
        target.anchoredPosition = _startOrigin + Vector2.up * _slideOffset;
        target.gameObject.SetActive(true);

        target.DOAnchorPos(_startOrigin, _inDuration)
              .SetEase(_inEase)
              .OnComplete(() => onComplete?.Invoke());
    }

    // ── 버튼 이벤트 ──────────────────────────────────────────

    public void ClickPlayButton()
    {
        PlayButtonSFX();
        SlideOutToUp(_uiStart, () => SlideInFromDown(_uiCharSelectWindow));
    }
    public void ClickCharCancelButton()
    {
        PlayButtonSFX();
        SlideOutToDown(_uiCharSelectWindow, () => SlideInFromUp(_uiStart));
    }
    public void ClickOptionButton()
    {
        PlayButtonSFX();
        SlideOutToUp(_uiStart, () => SlideInFromDown(_uiOptionWindow));
    }

    public void ClickQuitButton()
    {
        PlayButtonSFX();
        RememberManager.Instance.OpenExitWindow();
    }

    public void ClickOptionOKButton()
    {
        PlayButtonSFX();
        SlideOutToDown(_uiOptionWindow, () => SlideInFromUp(_uiStart));
    }
}