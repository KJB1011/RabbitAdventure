using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 노드를 클리어하면 오른쪽에 나타나는 출구 포털.
/// 플레이어가 포털 안에 _holdTime초 동안 머무르면 다음 노드로 넘어간다.
/// 포털을 벗어나면 게이지가 서서히 줄어든다.
///
/// 사용법: IngameManager가 Open()으로 열고, 다 차면 등록한 콜백을 호출한 뒤 스스로 닫힌다.
/// 모양(테두리·게이지)은 Awake에서 자식 오브젝트로 자동 생성한다.
/// </summary>
public class ExitPortal : MonoBehaviour
{
    [Header("판정")]
    [SerializeField] float _radius = 1.2f;      // 포털 크기 (이 반경 안에 있으면 "안에 있음")
    [SerializeField] float _holdTime = 1.5f;    // 이만큼 머무르면 이동
    [SerializeField] float _drainSpeed = 1f;    // 벗어났을 때 줄어드는 속도 (차오르는 속도 대비 배율)

    [Header("모양")]
    [SerializeField] Sprite _ringSprite;        // 테두리 (ShockwaveRing)
    [SerializeField] Sprite _fillSprite;        // 차오르는 빛 (ItemGlow)
    [SerializeField] Color _ringColor = new Color(0.6f, 0.9f, 1f, 0.7f);
    [SerializeField] Color _ringActiveColor = new Color(0.8f, 1f, 1f, 1f); // 플레이어가 안에 있을 때
    [SerializeField] Color _fillColor = new Color(0.6f, 0.9f, 1f, 0.85f);
    [SerializeField] int _sortingOrder = 2;
    [SerializeField] string _enterSfx = "";     // 다 찼을 때 재생할 효과음 (비우면 없음)

    [Header("안내 글씨 + 게이지 바 (포털 위)")]
    [SerializeField] string _labelText = "머무르면 다음 구역으로";
    [SerializeField] TMP_FontAsset _labelFont;              // 한글이 나오는 폰트 (예: CookieRun Bold SDF)
    [SerializeField] float _labelFontSize = 3f;
    [SerializeField] Color _labelColor = Color.white;
    [SerializeField] float _labelGap = 0.9f;                // 포털 테두리 위로 글씨가 떨어진 거리
    [SerializeField] Vector2 _barSize = new Vector2(1.8f, 0.16f); // 게이지 바 가로·세로 (월드 단위)
    [SerializeField] float _barGap = 0.35f;                 // 포털 테두리 위로 바가 떨어진 거리 (글씨 바로 아래)
    [SerializeField] Color _barBackColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] Color _barFillColor = new Color(0.6f, 0.9f, 1f, 1f);

    SpriteRenderer _ring;
    SpriteRenderer _fill;
    Transform _barFill;  // 게이지 바의 채워지는 부분 (가로 크기로 진행도 표시)
    float _ringScale;   // 테두리를 _radius 크기로 맞추는 배율
    float _fillScale;   // 게이지가 가득 찼을 때의 배율

    Transform _player;
    System.Action _onEntered;
    float _progress;    // 0 ~ 1
    bool _isOpen;

    void Awake()
    {
        BuildVisual();

        // 처음에는 보이지 않게 (오브젝트는 켜둔 채 크기만 0 → Open에서 커진다)
        transform.localScale = Vector3.zero;
    }

    // ── 열기 / 닫기 ───────────────────────────────────────────

    /// <summary>포털을 열고, 플레이어가 다 채우면 onEntered를 한 번 호출한다.</summary>
    public void Open(Transform player, System.Action onEntered)
    {
        _player = player;
        _onEntered = onEntered;
        _progress = 0f;
        _isOpen = true;
        UpdateVisual(false);

        // 뿅 하고 나타나기
        transform.DOKill();
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        // 테두리가 살짝 숨쉬듯 커졌다 작아짐 → "들어갈 수 있는 곳"이라는 신호
        if (_ring != null)
        {
            _ring.transform.DOKill();
            _ring.transform.localScale = Vector3.one * _ringScale;
            _ring.transform.DOScale(_ringScale * 1.08f, 0.6f)
                           .SetEase(Ease.InOutSine)
                           .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <param name="immediate">true면 연출 없이 바로 숨긴다 (노드 재시작 등)</param>
    public void Close(bool immediate = false)
    {
        _isOpen = false;
        _onEntered = null;

        transform.DOKill();
        if (_ring != null) _ring.transform.DOKill();

        if (immediate)
            transform.localScale = Vector3.zero;
        else
            transform.DOScale(0f, 0.25f).SetEase(Ease.InBack);
    }

    // ── 머무르기 판정 ─────────────────────────────────────────

    void Update()
    {
        if (!_isOpen || _player == null) return;

        // 거리의 제곱끼리 비교 (제곱근 계산을 피함)
        Vector2 diff = _player.position - transform.position;
        bool inside = diff.sqrMagnitude <= _radius * _radius;

        // 안에 있으면 차오르고, 밖에 있으면 서서히 줄어든다
        float step = Time.deltaTime / _holdTime;
        _progress = Mathf.Clamp01(_progress + (inside ? step : -step * _drainSpeed));
        UpdateVisual(inside);

        if (_progress >= 1f)
        {
            // 콜백을 먼저 꺼내 두고 닫는다 (Close가 _onEntered를 비우므로)
            var callback = _onEntered;
            if (!string.IsNullOrEmpty(_enterSfx))
                SoundManager.Instance.PlaySFX(_enterSfx);
            Close();
            callback?.Invoke();
        }
    }

    // ── 모양 ─────────────────────────────────────────────────

    void BuildVisual()
    {
        float diameter = _radius * 2f;

        if (_ringSprite != null)
        {
            _ring = CreateChildSprite("Ring", _ringSprite, _ringColor, _sortingOrder + 1);
            _ringScale = diameter / _ringSprite.bounds.size.x;
            _ring.transform.localScale = Vector3.one * _ringScale;
        }

        if (_fillSprite != null)
        {
            _fill = CreateChildSprite("Fill", _fillSprite, _fillColor, _sortingOrder);
            _fillScale = diameter / _fillSprite.bounds.size.x;
            _fill.transform.localScale = Vector3.zero;
        }

        BuildLabel();
        BuildBar();
    }

    // 포털 위 안내 글씨 (월드 공간 TextMeshPro → Canvas 없이 포털과 함께 움직이고 커지고 작아진다)
    void BuildLabel()
    {
        var go = new GameObject("Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, _radius + _labelGap, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        if (_labelFont != null) tmp.font = _labelFont;
        tmp.text = _labelText;
        tmp.fontSize = _labelFontSize;
        tmp.color = _labelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap; // 한 줄로 (줄바꿈 없음)
        tmp.sortingOrder = _sortingOrder + 3;
    }

    // 글씨 바로 아래 게이지 바: 어두운 배경 + 왼쪽부터 차오르는 막대
    void BuildBar()
    {
        var bar = new GameObject("Bar").transform;
        bar.SetParent(transform, false);
        bar.localPosition = new Vector3(0f, _radius + _barGap, 0f);

        // 배경: 가운데 기준
        var back = CreateChildSprite("Back", MakeBoxSprite(new Vector2(0.5f, 0.5f)), _barBackColor, _sortingOrder + 2);
        back.transform.SetParent(bar, false);
        back.transform.localScale = new Vector3(_barSize.x, _barSize.y, 1f);

        // 채움: 왼쪽 끝 기준 (가로 크기만 늘리면 왼쪽부터 차오른다)
        var fill = CreateChildSprite("Fill", MakeBoxSprite(new Vector2(0f, 0.5f)), _barFillColor, _sortingOrder + 3);
        fill.transform.SetParent(bar, false);
        fill.transform.localPosition = new Vector3(-_barSize.x * 0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(0f, _barSize.y, 1f);
        _barFill = fill.transform;
    }

    // 크기 1×1(월드 단위)짜리 흰색 사각형 스프라이트를 코드로 만든다 (이미지 파일 없이 막대를 그리기 위함)
    static Sprite MakeBoxSprite(Vector2 pivot)
    {
        Texture2D tex = Texture2D.whiteTexture; // 4×4 흰색 텍스처 (Unity 기본 제공)
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, tex.width);
    }

    SpriteRenderer CreateChildSprite(string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    // 게이지(가운데 빛)는 진행도만큼 커지고, 테두리는 플레이어가 안에 있을 때 밝아진다
    void UpdateVisual(bool inside)
    {
        if (_fill != null)
            _fill.transform.localScale = Vector3.one * (_fillScale * _progress);

        if (_ring != null)
            _ring.color = inside ? _ringActiveColor : _ringColor;

        // 게이지 바: 진행도만큼 가로로 늘어난다
        if (_barFill != null)
            _barFill.localScale = new Vector3(_barSize.x * _progress, _barSize.y, 1f);
    }

    // 에디터 Scene 뷰에서 판정 범위를 원으로 보여준다 (게임에는 영향 없음)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
