using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 데미지 숫자 하나의 연출을 담당합니다.
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("일반 타격")]
    [SerializeField] float _normalSize = 3f;
    [SerializeField] Color _normalColor = Color.white;

    [Header("치명타")]
    [SerializeField] float _critSize = 4.5f;
    [SerializeField] Color _critColor = new Color(1f, 0.8f, 0f); // 노란색

    [Header("연출")]
    [SerializeField] float _floatHeight = 1.5f;   // 위로 떠오르는 거리
    [SerializeField] float _floatDuration = 0.6f; // 떠오르는 시간
    [SerializeField] float _fadeDuration = 0.4f;  // 사라지는 시간
    [SerializeField] float _fadeDelay = 0.3f;     // 사라지기 시작까지 대기

    TextMeshPro _text;
    System.Action<DamageText> _releaseCallback;
    Sequence _sequence;

    void Awake()
    {
        _text = GetComponent<TextMeshPro>();
    }

    public void Play(int damage, bool isCrit, Vector3 worldPos, System.Action<DamageText> releaseCallback)
    {
        _releaseCallback = releaseCallback;

        transform.position = worldPos + Vector3.up * 0.5f;

        _text.text = isCrit ? $"{damage}!" : $"{damage}";

        _text.fontSize = isCrit ? _critSize : _normalSize;
        Color startColor = isCrit ? _critColor : _normalColor;
        startColor.a = 1f;
        _text.color = startColor;

        _sequence?.Kill();

        _sequence = DOTween.Sequence();

        _sequence.Append(
            transform.DOMoveY(worldPos.y + 0.5f + _floatHeight, _floatDuration)
                     .SetEase(Ease.OutCubic)
        );

        if (isCrit)
        {
            _sequence.Join(
                transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 5, 0.5f)
            );
        }

        _sequence.Insert(
            _fadeDelay,
            _text.DOFade(0f, _fadeDuration)
        );

        _sequence.OnComplete(() => _releaseCallback?.Invoke(this));
    }
    public void Play(string content, Color color, float fontSize, Vector3 worldPos,
                 System.Action<DamageText> releaseCallback)
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
        _sequence.Join(
            transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 0.5f) // 임팩트 강조
        );
        _sequence.Insert(
            _fadeDelay,
            _text.DOFade(0f, _fadeDuration)
        );
        _sequence.OnComplete(() => _releaseCallback?.Invoke(this));
    }

    // 풀로 반납될 때 정리
    public void ReturnCleanup()
    {
        _sequence?.Kill();
        gameObject.SetActive(false);
    }
}