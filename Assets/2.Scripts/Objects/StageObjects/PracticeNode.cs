using System.Collections;
using UnityEngine;

/// <summary>
/// 연습방 (게임 시작 전 준비하는 곳, 노드 0).
/// - 가운데: 허수아비 (때리면 DPS 측정)
/// - 오른쪽 포털: 머무르면 게임 시작 (= 이 노드 완료)
/// - 왼쪽 포털: 머무르면 캐릭터 선택 창
/// 전투가 아니므로 숙련도·골드는 쌓이지 않는다.
/// </summary>
public class PracticeNode : NodeBase
{
    [SerializeField] TrainingDummy _dummy;
    [SerializeField] ExitPortal _startPortal;       // 오른쪽 (IngameManager의 출구 포털과 같은 것을 써도 된다)
    [SerializeField] ExitPortal _characterPortal;   // 왼쪽
    [SerializeField] UICharacterSelect _characterSelect;
    [SerializeField] string _startLabel = "머무르면 게임 시작";

    // 오른쪽 포털로 이미 나가기를 끝냈으므로 IngameManager가 포털을 또 열지 않게
    public override bool UsesOwnExitPortal => true;

    bool _isRunning;

    protected override void OnNodeStart()
    {
        _isRunning = true;
        if (_dummy != null)
        {
            _dummy.gameObject.SetActive(true);
            _dummy.ResetRecord();
        }
        OpenStartPortal();
        OpenCharacterPortal();
    }

    Transform PlayerTF => IngameManager.Instance.Player != null ? IngameManager.Instance.Player.transform : null;

    // ── 오른쪽: 게임 시작 ─────────────────────────────────────
    void OpenStartPortal()
    {
        if (_startPortal == null || PlayerTF == null) return;
        _startPortal.SetLabel(_startLabel);
        _startPortal.Open(PlayerTF, OnStartEntered);
    }

    void OnStartEntered()
    {
        _isRunning = false;
        _startPortal.SetLabel(null); // 이후 노드에서는 원래 문구("다음 구역으로")
        if (_characterPortal != null) _characterPortal.Close();
        if (_dummy != null) _dummy.gameObject.SetActive(false);
        Complete();
    }

    // ── 왼쪽: 캐릭터 선택 ─────────────────────────────────────
    void OpenCharacterPortal()
    {
        if (_characterPortal == null || PlayerTF == null) return;
        _characterPortal.Open(PlayerTF, () =>
        {
            if (_characterSelect != null) _characterSelect.Open(OnCharacterWindowClosed);
            else OnCharacterWindowClosed();
        });
    }

    void OnCharacterWindowClosed()
    {
        if (!_isRunning) return;
        // 캐릭터가 바뀌었으면 포털이 쫓던 플레이어도 바뀌었으므로 오른쪽 포털도 새 플레이어로 다시 연다
        OpenStartPortal();
        StartCoroutine(ReopenCharacterPortalWhenAway());
    }

    // 왼쪽 포털 안에 서 있는 채로 다시 열면 곧바로 또 선택 창이 열리므로, 포털 밖으로 나간 뒤에 연다
    IEnumerator ReopenCharacterPortalWhenAway()
    {
        while (_isRunning && PlayerTF != null && _characterPortal != null &&
               Vector2.Distance(PlayerTF.position, _characterPortal.transform.position) < _characterPortal.Radius + 0.4f)
            yield return null;

        if (_isRunning) OpenCharacterPortal();
    }

    void OnDisable()
    {
        _isRunning = false;
    }
}
