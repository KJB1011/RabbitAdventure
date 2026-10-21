using UnityEngine;
using DG.Tweening;

/// <summary>
/// BossEnemy가 등장하는 노드
/// </summary>
public class BossNode : NodeBase
{
    [Header("보스 프리팹")]
    [SerializeField] GameObject _bossPrefab;
    [SerializeField] Vector3 _spawnPosition = Vector3.zero;

    [Header("배경 연출")]
    [SerializeField] SpriteRenderer _background;
    [SerializeField] Color _bossBGColor = new Color(1f, 0.5f, 0.5f); // 흰색+빨간색 중간
    [SerializeField] float _colorFadeTime = 1.5f;

    Color _originalColor;
    EnemyController _boss;
    UIHPBar _hpBar;
    void Awake()
    {
        if (_background != null)
            _originalColor = _background.color;
    }
    protected override void OnNodeStart()
    {
        if (_background != null)
            _background.DOColor(_bossBGColor, _colorFadeTime).SetEase(Ease.InOutSine);

        IngameManager.Instance._isBattle = true;

        // 프리팹 스폰
        var go = Instantiate(_bossPrefab, _spawnPosition, Quaternion.identity);
        go.SetActive(true);

        _boss = go.GetComponent<EnemyController>();
        if (_boss == null)
        {
            Debug.LogError("[BossNode] 프리팹에 EnemyController 계열 컴포넌트가 없습니다.");
            return;
        }

        _boss.OnDied += HandleBossDied;

        _hpBar = IngameManager.Instance.GetHPBar();
        _hpBar?.Show();
    }

    private void HandleBossDied()
    {
        _boss.OnDied -= HandleBossDied;

        // 보스 처치 시 배경색 원복
        if (_background != null)
            _background.DOColor(_originalColor, _colorFadeTime).SetEase(Ease.InOutSine);

        Complete();
    }

    private void OnDisable()
    {
        if (_boss != null)
            _boss.OnDied -= HandleBossDied;

        if (_background != null && _originalColor != default)
            _background.DOColor(_originalColor, _colorFadeTime).SetEase(Ease.InOutSine);

        _hpBar?.Hide();
    }
}