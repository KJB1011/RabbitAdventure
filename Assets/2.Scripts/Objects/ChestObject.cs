using UnityEngine;

/// <summary>
/// 상자 오브젝트를 관리하는 스크립트.
/// </summary>
public class ChestObject : MonoBehaviour
{
    [SerializeField] private int _hp = 3;

    [Header("아이템 드롭 설정")]
    [SerializeField] GameObject _itemPrefab;
    [SerializeField] int _minDropCount = 3;
    [SerializeField] int _maxDropCount = 5;

    [Header("파괴 연출")]
    [SerializeField] SpriteRenderer _spriteRenderer;

    // 아이템별 드롭 가중치 (GOLD, HP, ATKUP, RANGEUP, CRITRATEUP)
    readonly int[] _weights = { 60, 10, 10, 10, 10 };

    public event System.Action OnBroken;

    public void TakeDamage(int damage)
    {
        _hp -= damage;

        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.red;
        Invoke(nameof(ResetColor), 0.1f);
        SoundManager.Instance.PlaySFX("SFX/CoinDropping");

        if (_hp <= 0)
            Break();
    }

    private void ResetColor()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.white;
    }

    private void Break()
    {
        DropItems();
        OnBroken?.Invoke();
        gameObject.SetActive(false);
    }

    private void DropItems()
    {
        if (_itemPrefab == null)
        {
            Debug.LogWarning("[ChestObject] _itemPrefab이 연결되지 않았습니다.");
            return;
        }

        int count = Random.Range(_minDropCount, _maxDropCount + 1);

        for (int i = 0; i < count; i++)
        {
            Defines.ITEM itemType = GetRandomItemType();

            Vector2 offset = Random.insideUnitCircle * 0.8f;
            GameObject go = Instantiate(_itemPrefab,
                (Vector2)transform.position + offset, Quaternion.identity);

            // 아이템 타입 세팅
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
                pickup.SetItemType(itemType);
        }
    }

    // 가중치 기반 랜덤 아이템 선택
    private Defines.ITEM GetRandomItemType()
    {
        int total = 0;
        foreach (int w in _weights) total += w;

        int roll = Random.Range(0, total);
        int cumulative = 0;

        for (int i = 0; i < _weights.Length; i++)
        {
            cumulative += _weights[i];
            if (roll < cumulative)
                return (Defines.ITEM)i;
        }

        return Defines.ITEM.GOLD;
    }
}