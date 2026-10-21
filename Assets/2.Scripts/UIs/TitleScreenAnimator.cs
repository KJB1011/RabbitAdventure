using UnityEngine;
using DG.Tweening;

/// <summary>
/// 로비화면 타이틀 연출 담당
/// </summary>
public class TitleScreenAnimator : MonoBehaviour
{
    public RectTransform titleLogo;
    public CanvasGroup menuButtonsGroup;

    void Start()
    {
        titleLogo.anchoredPosition = new Vector2(400, 200);
        menuButtonsGroup.alpha = 0;

        Sequence seq = DOTween.Sequence();

        // 로고가 아래로 내려옴
        seq.Append(titleLogo.DOAnchorPos(new Vector2(400, 0), 1.0f).SetEase(Ease.OutBounce));

        // 버튼 페이드인
        seq.Join(menuButtonsGroup.DOFade(1, 1.5f).SetDelay(0.5f));

        // 위아래 움직임
        seq.OnComplete(() =>
        {
            titleLogo.DOLocalMoveY(10, 2.0f)
                     .SetRelative()
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetEase(Ease.InOutSine)
                     .SetLink(titleLogo.gameObject); // ← 추가
        });
    }
}