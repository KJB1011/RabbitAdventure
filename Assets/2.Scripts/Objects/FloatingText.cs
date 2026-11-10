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
    [SerializeField] float _fadeDelay = 0.6f;
    [SerializeField] float _pushDuration = 0.12f; // 새 텍스트에 밀려 올라갈 때 걸리는 시간

    TextMeshPro _text;
    System.Action<FloatingText> _releaseCallback;
    Sequence _sequence;
    Tween _pushTween;

    // 위치 = 시작 위치 + (떠오른 높이 + 밀려 올라간 높이)
    // 두 높이를 따로 저장해서, 떠오르는 트윈과 밀어 올리기가 서로의 값을 덮어쓰지 않게 한다
    Vector3 _basePos;    // 처음 나타난 위치
    float _riseY;        // 떠오르기 트윈이 바꾸는 값 (0 → _floatHeight)
    float _stackY;       // 밀어 올리기가 바꾸는 값 (현재값)
    float _stackTargetY; // 밀어 올리기의 목표값 (연달아 밀려도 누적되게)

    void Awake()
    {
        _text = GetComponent<TextMeshPro>();
    }

    public void Play(string content, Color color, float fontSize, Vector3 worldPos,
                     System.Action<FloatingText> releaseCallback)
    {
        _releaseCallback = releaseCallback;

        _basePos = worldPos + Vector3.up * 0.5f;
        _riseY = 0f;
        _stackY = 0f;
        _stackTargetY = 0f;
        ApplyPosition();

        _text.text = content;
        _text.fontSize = fontSize;
        Color c = color;
        c.a = 1f;
        _text.color = c;

        _sequence?.Kill();
        _pushTween?.Kill();
        _sequence = DOTween.Sequence();

        // transform을 직접 움직이지 않고 _riseY 값만 바꾼 뒤 위치를 다시 계산한다
        _sequence.Append(
            DOTween.To(() => _riseY, v => { _riseY = v; ApplyPosition(); }, _floatHeight, _floatDuration)
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
        _pushTween?.Kill();
        gameObject.SetActive(false);
    }

    /// <summary>새 텍스트가 나타나면 기존 텍스트를 offset만큼 부드럽게 밀어 올린다.</summary>
    public void PushUp(float offset)
    {
        // 목표를 누적해서, 밀려 올라가는 도중에 또 밀려도 정확히 그만큼 더 올라가게
        _stackTargetY += offset;

        _pushTween?.Kill();
        _pushTween = DOTween.To(() => _stackY, v => { _stackY = v; ApplyPosition(); }, _stackTargetY, _pushDuration)
                            .SetEase(Ease.OutQuad);
    }

    void ApplyPosition()
    {
        transform.position = _basePos + Vector3.up * (_riseY + _stackY);
    }
}