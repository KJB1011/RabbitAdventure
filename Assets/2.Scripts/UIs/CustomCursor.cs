using UnityEngine;

/// <summary>
/// 커서를 따라다니는 스프라이트를 조정해주는 스크립트
/// </summary>
public class CustomCursor : MonoBehaviour
{
    RectTransform _rect;
    Canvas _parentCanvas;

    public float followSpeed = 25f;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        Vector2 localPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform,
            Input.mousePosition,
            _parentCanvas.worldCamera,
            out localPos
        );

        _rect.anchoredPosition = Vector2.Lerp(
            _rect.anchoredPosition,
            localPos,
            Time.unscaledDeltaTime * followSpeed
        );
    }

    void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}