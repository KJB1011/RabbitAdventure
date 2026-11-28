using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 마우스를 올렸을 때 뜨는 설명창. 화면에 하나만 두고 모든 UITooltipTrigger가 같이 쓴다.
/// 마우스 커서를 따라다니며, 화면 밖으로 나가지 않게 반대쪽으로 뒤집힌다.
/// </summary>
public class UITooltip : MonoBehaviour
{
    static UITooltip _instance;

    [SerializeField] TextMeshProUGUI _titleText;
    [SerializeField] TextMeshProUGUI _descText;
    [SerializeField] Vector2 _offset = new Vector2(18f, 18f); // 커서에서 떨어진 거리 (Canvas 단위)

    RectTransform _rect;
    Canvas _canvas;

    void Awake()
    {
        _instance = this;
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();

        // 설명창이 마우스를 가로막으면, 아이콘에서 "마우스가 나갔다"로 판정되어 깜빡인다 → 클릭을 통과시킨다
        var group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        gameObject.SetActive(false);
    }

    // 오브젝트가 꺼진 채로 시작해 Awake가 아직 안 불렸을 때도 찾을 수 있게
    static UITooltip Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<UITooltip>(FindObjectsInactive.Include);
            return _instance;
        }
    }

    public static void Show(string title, string description)
    {
        var tip = Instance;
        if (tip == null) return;

        tip._titleText.text = title;
        tip._descText.text = description;
        tip._descText.gameObject.SetActive(!string.IsNullOrEmpty(description));

        tip.gameObject.SetActive(true);
        tip.transform.SetAsLastSibling(); // 다른 UI보다 앞에
        LayoutRebuilder.ForceRebuildLayoutImmediate(tip._rect); // 글자 수에 맞게 크기를 바로 계산
        tip.FollowMouse();
    }

    public static void Hide()
    {
        if (_instance != null) _instance.gameObject.SetActive(false);
    }

    void Update()
    {
        FollowMouse();
    }

    // 커서 오른쪽 위에 띄우고, 화면 밖으로 나가면 반대쪽(왼쪽/아래)으로 뒤집는다.
    // 뒤집어도 넘치면(화면 가장자리, 아주 긴 설명) 화면 안쪽으로 밀어 넣는다.
    void FollowMouse()
    {
        if (Mouse.current == null) return;

        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector2 size = _rect.rect.size * scale;     // 화면 픽셀 기준 크기
        Vector2 offset = _offset * scale;

        // 툴팁의 왼쪽 아래 모서리 위치를 먼저 정한다
        Vector2 corner = mouse + offset;
        if (corner.x + size.x > Screen.width) corner.x = mouse.x - offset.x - size.x;
        if (corner.y + size.y > Screen.height) corner.y = mouse.y - offset.y - size.y;

        // 화면 안에 가두기 (툴팁이 화면보다 크면 왼쪽·위를 맞춘다)
        corner.x = Mathf.Clamp(corner.x, 0f, Mathf.Max(0f, Screen.width - size.x));
        corner.y = Mathf.Clamp(corner.y, Mathf.Min(0f, Screen.height - size.y), Screen.height - size.y);

        // 기준점(Pivot)이 어디든 왼쪽 아래 모서리가 corner에 오도록
        _rect.position = corner + Vector2.Scale(_rect.pivot, size); // Screen Space - Overlay에서는 화면 좌표 = 월드 좌표
    }
}
