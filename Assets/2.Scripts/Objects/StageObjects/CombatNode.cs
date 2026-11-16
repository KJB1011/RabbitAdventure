using UnityEngine;

/// <summary>
/// 일반 전투 노드 — 적 1마리를 소환하고, 처치하면 완료
/// </summary>
public class CombatNode : NodeBase
{
    [Header("적 프리팹 · 스폰 위치")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform _spawnPoint; // 비우면 (0,0,0)

    [Header("무작위 등장 (비워두면 위의 _enemyPrefab 사용)")]
    [Tooltip("이 중 하나를 무작위로 골라 소환")]
    [SerializeField] private GameObject[] _randomEnemyPrefabs;
    [Tooltip("이 노드가 고른 적과는 겹치지 않게 (예: 두 번째 전투 노드에 첫 번째 전투 노드를 지정)")]
    [SerializeField] private CombatNode _avoidSameAs;

    private EnemyController _enemy;
    private UIHPBar _hpBar;
    private GameObject _chosenPrefab; // 한 번 고르면 노드를 다시 시작해도 같은 적

    public GameObject ChosenPrefab => _chosenPrefab;

    protected override void OnNodeStart()
    {
        IngameManager.Instance._isBattle = true;

        if (_chosenPrefab == null) _chosenPrefab = ChooseEnemy();
        if (_chosenPrefab == null)
        {
            Debug.LogError("[CombatNode] 소환할 적 프리팹이 비어 있습니다.");
            Complete();
            return;
        }

        Vector3 pos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        var go = Instantiate(_chosenPrefab, pos, Quaternion.identity);
        go.SetActive(true);

        _enemy = go.GetComponent<EnemyController>();
        if (_enemy == null)
        {
            Debug.LogError($"[CombatNode] {go.name}에 EnemyController가 없습니다.");
            Complete();
            return;
        }

        _enemy.OnDied += HandleEnemyDied;

        _hpBar = IngameManager.Instance.GetHPBar();
        _hpBar?.Show();
    }

    private GameObject ChooseEnemy()
    {
        if (_randomEnemyPrefabs == null || _randomEnemyPrefabs.Length == 0)
            return _enemyPrefab;

        GameObject avoid = _avoidSameAs != null ? _avoidSameAs.ChosenPrefab : null;
        var candidates = new System.Collections.Generic.List<GameObject>();
        foreach (var prefab in _randomEnemyPrefabs)
            if (prefab != null && prefab != avoid) candidates.Add(prefab);

        if (candidates.Count == 0) return _enemyPrefab;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private void HandleEnemyDied()
    {
        _enemy.OnDied -= HandleEnemyDied;
        Complete();
    }

    private void OnDisable()
    {
        if (_enemy != null)
            _enemy.OnDied -= HandleEnemyDied;

        _hpBar?.Hide();
    }
}