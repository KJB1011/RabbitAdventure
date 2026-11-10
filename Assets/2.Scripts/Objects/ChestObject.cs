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
    [SerializeField] float _dropDistanceMin = 1.0f; // 상자에서 떨어지는 거리 (최소~최대 사이 랜덤)
    [SerializeField] float _dropDistanceMax = 1.6f;
    [SerializeField, Range(0f, 0.5f)] float _dropAngleJitter = 0.3f; // 각도 흔들림 (0 = 완전히 균등, 클수록 불규칙)

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

        // 360°를 아이템 수만큼 똑같이 나눠서 각자 다른 방향으로 떨어뜨린다 → 서로 겹치지 않음
        // (시작 각도를 랜덤으로 돌려서, 매번 같은 모양으로 떨어지지 않게)
        float sliceAngle = 360f / count;
        float startAngle = Random.Range(0f, 360f);
        Vector2 center = transform.position;

        for (int i = 0; i < count; i++)
        {
            Defines.ITEM itemType = GetRandomItemType();

            // i번째 방향 + 약간의 흔들림 (칸 크기의 ±jitter 비율만큼)
            float jitter = Random.Range(-_dropAngleJitter, _dropAngleJitter) * sliceAngle;
            float angle = (startAngle + sliceAngle * i + jitter) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float distance = Random.Range(_dropDistanceMin, _dropDistanceMax);
            Vector2 landPos = center + dir * distance;

            // 상자 중심에서 생겨나서, 정해진 위치로 튀어나간다
            GameObject go = Instantiate(_itemPrefab, center, Quaternion.identity);

            // 아이템 타입 세팅 + 점프
            // (점프 트윈은 아이템 쪽에서 돌아가므로, 바로 아래에서 상자가 꺼져도 끊기지 않는다)
            var pickup = go.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                pickup.SetItemType(itemType);
                pickup.JumpTo(landPos);
            }
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