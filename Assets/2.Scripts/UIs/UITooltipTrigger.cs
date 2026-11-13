using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 이 UI(아이콘 등)에 마우스를 올리면 설명창(UITooltip)을 띄운다.
/// 스킬처럼 내용이 고정이면 Inspector에 적고, 아티팩트처럼 바뀌면 코드에서 Set()으로 넣는다.
/// ※ 같은 오브젝트에 Raycast Target이 켜진 Image 등이 있어야 마우스를 감지한다.
/// </summary>
public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string _title;
    [SerializeField, TextArea] string _description;

    bool _isShowing;

    public void Set(string title, string description)
    {
        _title = title;
        _description = description;
    }

    // EventSystem이 "마우스가 들어왔다/나갔다"를 알려준다 (인터페이스를 구현하면 자동으로 불림)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(_title) && string.IsNullOrEmpty(_description)) return;
        UITooltip.Show(_title, _description);
        _isShowing = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideIfShowing();
    }

    // 마우스를 올려둔 채 아이콘이 꺼지면 "나갔다" 알림이 오지 않으므로 직접 숨긴다
    void OnDisable()
    {
        HideIfShowing();
    }

    void HideIfShowing()
    {
        if (!_isShowing) return;
        UITooltip.Hide();
        _isShowing = false;
    }
}
