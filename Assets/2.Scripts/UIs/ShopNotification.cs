using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 상점 Canvas 위에서 동작하는 구매 알림 텍스트.
/// FloatingText가 WorldCanvas라서 상점에서도 사용할 수 있게 하기 위한 코드
/// </summary>
public class ShopNotification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private RectTransform _rect;

    [Header("애니메이션")]
    [SerializeField] private float _floatDistance = 80f;  // 위로 올라가는 거리 (px)
    [SerializeField] private float _moveDuration = 0.7f;
    [SerializeField] private float _fadeDuration = 0.4f;
    [SerializeField] private float _fadeDelay = 0.3f;

    private Sequence _sequence;

    void Awake()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();

        if (_text == null)
            Debug.LogError("[ShopNotification] TextMeshProUGUI가 없습니다.");

        gameObject.SetActive(false);
    }

    public void Show(string content, Color color, Vector3 slotScreenPos)
    {
        if (_text == null) return;

        _sequence?.Kill();

        _text.text = content;
        Color c = color;
        c.a = 1f;
        _text.color = c;

        // 스크린 좌표 → Canvas 내 로컬 좌표 변환
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ShopNotification] 부모 Canvas를 찾을 수 없습니다.");
            return;
        }

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            slotScreenPos,
            canvas.worldCamera,
            out localPos
        );

        _rect.anchoredPosition = localPos;
        gameObject.SetActive(true);

        Vector2 targetPos = localPos + Vector2.up * _floatDistance;

        _sequence = DOTween.Sequence();
        _sequence.Append(
            _rect.DOAnchorPos(targetPos, _moveDuration).SetEase(Ease.OutCubic)
        );
        _sequence.Insert(
            _fadeDelay,
            _text.DOFade(0f, _fadeDuration)
        );
        _sequence.OnComplete(() => gameObject.SetActive(false));
    }

    void OnDestroy()
    {
        _sequence?.Kill();
    }
}