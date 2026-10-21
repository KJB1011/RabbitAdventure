using UnityEngine;
using DG.Tweening;

/// <summary>
/// 원모양 공격 전조 표시 컴포넌트
/// </summary>
public class TelegraphIndicator : MonoBehaviour
{
    [Header("모양 설정")]
    [SerializeField] SpriteRenderer _spriteRenderer;

    [Header("색상")]
    [SerializeField] Color _startColor = new Color(1f, 0.3f, 0.3f, 0.3f); // 반투명 빨강
    [SerializeField] Color _flashColor = new Color(1f, 0.3f, 0.3f, 0.8f); // 점점 진해짐

    [Header("애니메이션")]
    [SerializeField] float _pulseDuration = 0.3f; // 깜빡임 간격

    Tween _pulseTween;

    void Awake()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = _startColor;
    }

    public void Show(Vector3 pos, Vector2 size, float angle = 0f)
    {
        transform.position = pos;
        transform.localScale = new Vector3(size.x, size.y, 1f);
        transform.eulerAngles = new Vector3(0f, 0f, angle);
        gameObject.SetActive(true);

        if (_spriteRenderer != null)
            _spriteRenderer.color = _startColor;

        // 점점 밝아지는 펄스 효과
        _pulseTween?.Kill();
        _pulseTween = _spriteRenderer
            .DOColor(_flashColor, _pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void Hide()
    {
        _pulseTween?.Kill();
        gameObject.SetActive(false);
    }

    void OnDestroy() => _pulseTween?.Kill();
}