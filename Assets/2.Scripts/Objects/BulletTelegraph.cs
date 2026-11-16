using UnityEngine;
using DG.Tweening;

/// <summary>
/// 탄막 발사 전 궤적을 긴 사각형으로 표시합니다.
/// </summary>
public class BulletTelegraph : MonoBehaviour
{
    [Header("라인 비주얼")]
    [SerializeField] private SpriteRenderer _lineSprite;

    [Header("라인 설정")]
    [SerializeField] private float _lineLength = 12f;  // 궤적 길이
    [SerializeField] private float _lineWidth = 0.08f; // 궤적 두께

    [Header("색상")]
    [SerializeField] private Color _startColor = new Color(1f, 0.3f, 0.3f, 0f);   // 시작: 투명
    [SerializeField] private Color _visibleColor = new Color(1f, 0.3f, 0.3f, 0.5f); // 표시 색
    [SerializeField] private float _fadeInTime = 0.15f;

    void Awake()
    {
        if (_lineSprite != null)
        {
            // 길고 얇은 사각형으로 크기 설정
            transform.localScale = new Vector3(_lineLength, _lineWidth, 1f);
            _lineSprite.color = _startColor;
        }
    }

    /// <summary>
    /// 지정 위치에서 지정 방향으로 궤적을 표시합니다.
    /// origin: 발사 위치, direction: 발사 방향
    /// </summary>
    public void Show(Vector3 origin, Vector2 direction)
    {
        // 방향으로 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(_lineLength, _lineWidth, 1f); // 직사각형으로 쓰였다가 돌아온 경우 대비

        // 위치: 발사 위치에서 방향으로 반만큼 이동 (중심점 맞추기)
        transform.position = (Vector2)origin + direction.normalized * (_lineLength * 0.5f);

        gameObject.SetActive(true);

        // 페이드 인
        _lineSprite?.DOColor(_visibleColor, _fadeInTime);
    }

    /// <summary>
    /// 같은 경고 표시를 원하는 크기의 직사각형으로 보여줍니다. (예: 탄이 튀어나올 벽 쪽)
    /// </summary>
    public void ShowRect(Vector2 center, Vector2 size)
    {
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(size.x, size.y, 1f);
        transform.position = center;

        gameObject.SetActive(true);
        _lineSprite?.DOColor(_visibleColor, _fadeInTime);
    }

    /// <summary>
    /// 궤적 표시를 숨깁니다.
    /// </summary>
    public void Hide(System.Action onComplete = null)
    {
        _lineSprite?.DOColor(_startColor, _fadeInTime)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        onComplete?.Invoke();
                    });
    }
}