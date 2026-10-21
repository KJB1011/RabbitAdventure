using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// UI 패널을 화면 위쪽에서 중앙으로 슬라이드 인/아웃 합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UISlidePanel : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private Ease _ease = Ease.InOutQuad;

    [Header("오프셋 (화면 밖 시작/끝 위치)")]
    [Tooltip("화면 높이 대비 얼마나 위에서 시작할지 (1 = 화면 높이만큼 위)")]
    [SerializeField] private float _offscreenMultiplier = 1.2f;

    private RectTransform _rect;
    private Vector2 _centerPos;  // 원래 중앙 위치
    private Vector2 _offscreenPos; // 화면 밖 위치
    private Tweener _tween;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        // 현재 위치를 중앙(목표) 위치로 저장
        _centerPos = _rect.anchoredPosition;

        // 화면 높이만큼 위를 오프스크린 위치로 설정
        float screenHeight = Screen.height;
        _offscreenPos = _centerPos + Vector2.up * screenHeight * _offscreenMultiplier;

        // 시작 시 화면 밖에서 대기
        _rect.anchoredPosition = _offscreenPos;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 화면 위에서 중앙으로 슬라이드 인
    /// </summary>
    public void Show(System.Action onComplete = null)
    {
        gameObject.SetActive(true);
        _rect.anchoredPosition = _offscreenPos;

        _tween?.Kill();
        _tween = _rect.DOAnchorPos(_centerPos, _duration)
                      .SetEase(_ease)
                      .SetUpdate(true) // timeScale 영향 받지 않음
                      .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 중앙에서 화면 위로 슬라이드 아웃
    /// </summary>
    public void Hide(System.Action onComplete = null)
    {
        _tween?.Kill();
        _tween = _rect.DOAnchorPos(_offscreenPos, _duration)
                      .SetEase(_ease)
                      .SetUpdate(true)
                      .OnComplete(() =>
                      {
                          gameObject.SetActive(false);
                          onComplete?.Invoke();
                      });
    }

    void OnDestroy()
    {
        _tween?.Kill();
    }
}