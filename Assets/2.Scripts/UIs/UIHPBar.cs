﻿using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적의 HP바를 나타내주는 UI 스크립트
/// </summary>
public class UIHPBar : MonoBehaviour
{
    [SerializeField] Slider _hpSlider;
    [SerializeField] GameObject _contents;   // 켜고 끌 대상 (비워두면 슬라이더 오브젝트를 대신 사용)
    [SerializeField] float _fadeOutTime = 0.6f; // 사라질 때 서서히 투명해지는 시간

    CanvasGroup _canvasGroup; // 자식 UI 전체의 투명도를 한 번에 조절
    Tween _fadeTween;

    void Awake()
    {
        // _contents가 비어 있으면 Show/Hide가 아무 일도 하지 않아 HP바가 항상 보이게 된다.
        // 그래서 비어 있을 때는 슬라이더 오브젝트 자체를 켜고 끈다.
        if (_contents == null && _hpSlider != null)
            _contents = _hpSlider.gameObject;

        // 투명도 조절용 CanvasGroup (없으면 추가)
        if (_contents != null && !_contents.TryGetComponent(out _canvasGroup))
            _canvasGroup = _contents.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        Hide(immediate: true); // 시작할 때는 연출 없이 바로 숨김
    }

    public void Show()
    {
        _hpSlider.value = 1f;

        // 사라지는 중이었다면 멈추고 다시 완전히 보이게
        _fadeTween?.Kill();
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        if (_contents != null) _contents.SetActive(true);
    }

    // 적이 데미지를 받을 때마다 호출
    public void SetBarValue(float hpRate)
    {
        _hpSlider.value = hpRate;

        if (hpRate <= 0)
            Hide();
    }

    /// <param name="immediate">true면 연출 없이 바로 숨긴다</param>
    public void Hide(bool immediate = false)
    {
        if (_contents == null) return;

        _fadeTween?.Kill();

        // 이미 꺼져 있거나 바로 숨겨야 할 때
        if (immediate || !_contents.activeInHierarchy || _canvasGroup == null)
        {
            _contents.SetActive(false);
            return;
        }

        // 서서히 투명해진 뒤 끈다
        // (SetUpdate(true): 히트스톱 등으로 게임이 멈춘 상태에서도 자연스럽게 진행)
        _fadeTween = _canvasGroup.DOFade(0f, _fadeOutTime)
                                 .SetEase(Ease.InQuad)
                                 .SetUpdate(true)
                                 .OnComplete(() => _contents.SetActive(false));
    }
}
