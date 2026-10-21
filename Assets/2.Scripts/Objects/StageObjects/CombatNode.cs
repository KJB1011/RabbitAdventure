using UnityEngine;

/// <summary>
/// 일반 전투 노드 — 적 1마리를 소환하고, 처치하면 완료
/// </summary>
public class CombatNode : NodeBase
{
    [Header("적 프리팹 · 스폰 위치")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform _spawnPoint; // 비우면 (0,0,0)

    private EnemyController _enemy;
    private UIHPBar _hpBar;

    protected override void OnNodeStart()
    {
        IngameManager.Instance._isBattle = true;

        if (_enemyPrefab == null)
        {
            Debug.LogError("[CombatNode] _enemyPrefab이 비어 있습니다.");
            Complete();
            return;
        }

        Vector3 pos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        var go = Instantiate(_enemyPrefab, pos, Quaternion.identity);
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