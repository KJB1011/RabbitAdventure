using UnityEngine;

/// <summary>
/// 모든 노드(전투/상자/상점/보스)의 기본형태
/// GameManager가 StartNode()를 호출하고, 노드가 끝나면 _onComplete()를 불러서 다음으로 넘어갑니다.
/// </summary>
public abstract class NodeBase : MonoBehaviour
{
    protected System.Action _onComplete;
    
    public void StartNode(System.Action onComplete)
    {
        _onComplete = onComplete;
        OnNodeStart();
    }

    // 각 노드가 반드시 구현해야 하는 시작 로직
    protected abstract void OnNodeStart();

    // 노드 완료 시 호출 (각 노드 내부에서 조건 달성 시 호출)
    protected void Complete()
    {
        _onComplete?.Invoke();
    }
}