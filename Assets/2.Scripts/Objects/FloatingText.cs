using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 데미지, 아이템 획득 등 모든 떠오르는 텍스트 연출을 담당합니다.
/// </summary>
public class FloatingText : MonoBehaviour
{
    [Header("연출 기본값")]
    [SerializeField] float _floatHeight = 1.2f;
    [SerializeField] float _floatDuration = 0.6f;
    [SerializeField] float _fadeDuration = 0.4f;
    [SerializeField] float _fadeDelay = 0.3f;

    TextMeshPro _text;
    System.Action<FloatingText> _releaseCallback;
    Sequence _sequence;

    void Awake()
    {
        _text = GetComponent<TextMeshPro>();
    }

    public void Play(string content, Color color, float fontSize, Vector3 worldPos,
                     System.Action<FloatingText> releaseCallback)
    {
        _releaseCallback = releaseCallback;

        transform.position = worldPos + Vector3.up * 0.5f;

        _text.text = content;
        _text.fontSize = fontSize;
        Color c = color;
        c.a = 1f;
        _text.color = c;

        _sequence?.Kill();
        _sequence = DOTween.Sequence();

        _sequence.Append(
            transform.DOMoveY(worldPos.y + 0.5f + _floatHeight, _floatDuration)
                     .SetEase(Ease.OutCubic)
        );
        _sequence.Insert(
            _fadeDelay,
            _text.DOFade(0f, _fadeDuration)
        );
        _sequence.OnComplete(() => _releaseCallback?.Invoke(this));
    }

    public void ReturnCleanup()
    {
        _sequence?.Kill();
        gameObject.SetActive(false);
    }

    public void PushUp(float offset)
    {
        // 현재 위치에서 offset만큼 위로 이동
        transform.position += Vector3.up * offset;
    }
}