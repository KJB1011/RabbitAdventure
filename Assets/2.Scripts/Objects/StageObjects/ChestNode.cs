using System.Collections;
using UnityEngine;

/// <summary>
/// 상자가 나오는 노드
/// </summary>
public class ChestNode : NodeBase
{
    [SerializeField] private ChestObject[] _chests;
    [SerializeField] private float _delayBeforeComplete = 1.5f; // 모든 상자 파괴 후 대기

    private int _remainingChests;

    protected override void OnNodeStart()
    {
        _remainingChests = _chests.Length;

        foreach (var chest in _chests)
        {
            chest.gameObject.SetActive(true);
            chest.OnBroken += HandleChestBroken;
        }
    }

    private void HandleChestBroken()
    {
        _remainingChests--;
        if (_remainingChests <= 0)
            StartCoroutine(CompleteAfterDelay());
    }

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(_delayBeforeComplete);
        Complete();
    }

    private void OnDisable()
    {
        if (_chests == null) return;
        foreach (var chest in _chests)
        {
            if (chest != null)
                chest.OnBroken -= HandleChestBroken;
        }
    }
}