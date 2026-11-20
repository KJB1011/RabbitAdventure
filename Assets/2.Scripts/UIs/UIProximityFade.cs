using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어가 이 UI 바로 밑(화면에서 겹치는 위치)까지 오면 UI를 반투명하게 만든다.
/// 하단 상태 패널, 골드, 보스 HP바처럼 화면 가장자리에 있는 UI에 붙인다. (Screen Space - Overlay 캔버스 기준)
///
/// 투명도는 CanvasGroup 대신 각 그림(Graphic)의 CanvasRenderer 투명도로 조절한다.
/// → UIHPBar처럼 이미 CanvasGroup으로 사라지는 연출을 하는 UI와 겹쳐도 서로 방해하지 않는다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIProximityFade : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float _fadedAlpha = 0.35f;   // 가까이 왔을 때의 투명도
    [SerializeField] float _fadeTime = 0.15f;
    [Tooltip("UI 영역을 바깥으로 몇 픽셀 넓혀서 판정할지 (클수록 더 멀리서부터 흐려짐)")]
    [SerializeField] float _margin = 20f;

    RectTransform _rect;
    Transform _player;
    SpriteRenderer _playerSprite;
    bool _isFaded;
    float _refreshTimer;
    readonly Vector3[] _corners = new Vector3[4];

    void Awake() => _rect = (RectTransform)transform;

    void OnEnable()
    {
        // 다시 켜질 때는 항상 또렷한 상태에서 시작
        _isFaded = false;
        SetAlpha(1f, instant: true);
    }

    void Update()
    {
        if (_player == null && !TryFindPlayer()) return;

        bool near = IsPlayerNear();
        if (near != _isFaded)
        {
            _isFaded = near;
            SetAlpha(near ? _fadedAlpha : 1f, instant: false);
            _refreshTimer = _fadeTime + 0.5f; // 흐려지는 연출이 끝난 뒤부터 다시 맞추기
            return;
        }

        // 흐려진 동안 새로 생긴 UI(예: 아티팩트 칸)도 같이 흐려지도록 가끔 다시 맞춘다
        if (_isFaded && (_refreshTimer -= Time.unscaledDeltaTime) <= 0f)
        {
            _refreshTimer = 0.5f;
            SetAlpha(_fadedAlpha, instant: true);
        }
    }

    bool TryFindPlayer()
    {
        var go = GameObject.FindWithTag("Player");
        if (go == null) return false;

        _player = go.transform;
        // 캐릭터 그림은 첫 번째 자식에 있다 (PlayerController와 같은 방식)
        _playerSprite = _player.childCount > 0 ? _player.GetChild(0).GetComponent<SpriteRenderer>() : null;
        if (_playerSprite == null) _playerSprite = go.GetComponentInChildren<SpriteRenderer>();
        return true;
    }

    // 플레이어 그림이 화면에서 차지하는 사각형과 UI 사각형(+여유)이 겹치는지
    bool IsPlayerNear()
    {
        var cam = Camera.main;
        if (cam == null) return false;

        Bounds b = _playerSprite != null ? _playerSprite.bounds : new Bounds(_player.position, Vector3.one);
        Vector2 pMin = cam.WorldToScreenPoint(b.min);
        Vector2 pMax = cam.WorldToScreenPoint(b.max);

        _rect.GetWorldCorners(_corners); // Overlay 캔버스에서는 이 값이 곧 화면 픽셀 좌표 (0 = 왼쪽 아래, 2 = 오른쪽 위)
        float xMin = _corners[0].x - _margin, yMin = _corners[0].y - _margin;
        float xMax = _corners[2].x + _margin, yMax = _corners[2].y + _margin;

        return pMax.x >= xMin && pMin.x <= xMax && pMax.y >= yMin && pMin.y <= yMax;
    }

    void SetAlpha(float alpha, bool instant)
    {
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            if (instant || !g.isActiveAndEnabled) g.canvasRenderer.SetAlpha(alpha);
            else g.CrossFadeAlpha(alpha, _fadeTime, true);
        }
    }
}
