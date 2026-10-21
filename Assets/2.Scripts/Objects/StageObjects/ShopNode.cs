using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점을 이용할 수 있는 노드
/// 상점 조작시에는 플레이어의 움직임은 잠기게 된다.
/// </summary>
public class ShopNode : NodeBase
{
    [SerializeField] private UISlidePanel _slidePanel;
    [SerializeField] private Button _leaveButton;

    private PlayerController _player;

    protected override void OnNodeStart()
    {
        _player = FindFirstObjectByType<PlayerController>();

        // 상점 진입 시 플레이어 조작 잠금
        _player?.SetControllable(false);

        if (_slidePanel != null)
            _slidePanel.Show();

        _leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    private void OnLeaveClicked()
    {
        _leaveButton.onClick.RemoveListener(OnLeaveClicked);

        // 조작 해제
        _player?.SetControllable(true);

        if (_slidePanel != null)
            _slidePanel.Hide(() => Complete());
        else
            Complete();
    }

    private void OnDisable()
    {
        _leaveButton.onClick.RemoveListener(OnLeaveClicked);

        // 혹시 비정상 종료 시에도 조작 잠금 해제
        _player?.SetControllable(true);
    }
}