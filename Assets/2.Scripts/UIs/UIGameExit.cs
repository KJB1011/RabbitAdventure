using UnityEngine;

/// <summary>
/// 게임 종료창 UI 스크립트
/// </summary>
public class UIGameExit : MonoBehaviour
{
    public void Open()
    {
        gameObject.SetActive(true);
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void ClickYesButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
    public void ClickNoButton()
    {
        Close();
    }
}
