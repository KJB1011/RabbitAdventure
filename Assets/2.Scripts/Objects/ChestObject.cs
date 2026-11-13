using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상자 오브젝트를 관리하는 스크립트.
/// 부서지면: 골드(고정 개수) + HP(반드시 1개) + 낮은 확률로 능력치 아이템 1개를 떨어뜨린다.
/// </summary>
public class ChestObject : MonoBehaviour
{
    [SerializeField] private int _hp = 3;

    [Header("아이템 드롭 설정")]
    [SerializeField] GameObject _itemPrefab;
    [SerializeField] int _goldDropCount = 3;                     // 골드 아이템 개수 (고정, 금액은 ItemPickup에서)
    [SerializeField] bool _alwaysDropHp = true;                   // HP 아이템을 반드시 1개 떨어뜨릴지
    [SerializeField, Range(0f, 1f)] float _bonusDropChance = 0.2f; // 능력치 아이템이 추가로 나올 확률 (0.2 = 20%)
    [SerializeField] Defines.ITEM[] _bonusItems =                 // 그중 하나가 랜덤으로 나옴
    {
        Defines.ITEM.ATKUP,
        Defines.ITEM.RANGEUP,
    };
    [SerializeField] float _dropDistanceMin = 1.0f; // 상자에서 떨어지는 거리 (최소~최대 사이 랜덤)
    [SerializeField] float _dropDistanceMax = 1.6f;
    [SerializeField, Range(0f, 0.5f)] float _dropAngleJitter = 0.3f; // 각도 흔들림 (0 = 완전히 균등, 클수록 불규칙)

    [Header("파괴 연출")]
    [SerializeField] SpriteRenderer _spriteRenderer;

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

        // 떨어뜨릴 아이템 목록을 먼저 만든다
        List<Defines.ITEM> drops = BuildDropList();
        int count = drops.Count;
        if (count == 0) return;

        // 360°를 아이템 수만큼 똑같이 나눠서 각자 다른 방향으로 떨어뜨린다 → 서로 겹치지 않음
        // (시작 각도를 랜덤으로 돌려서, 매번 같은 모양으로 떨어지지 않게)
        float sliceAngle = 360f / count;
        float startAngle = Random.Range(0f, 360f);
        Vector2 center = transform.position;

        for (int i = 0; i < count; i++)
        {
            Defines.ITEM itemType = drops[i];

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

    // 이번 상자에서 떨어질 아이템 목록
    private List<Defines.ITEM> BuildDropList()
    {
        var drops = new List<Defines.ITEM>();

        // ① 골드: 고정 개수
        for (int i = 0; i < _goldDropCount; i++)
            drops.Add(Defines.ITEM.GOLD);

        // ② HP: 반드시 1개
        if (_alwaysDropHp)
            drops.Add(Defines.ITEM.HP);

        // ③ 능력치: 낮은 확률로 1개 (목록 중 랜덤)
        // Random.value는 0~1 사이 랜덤 값 → 0.2보다 작을 확률이 정확히 20%
        if (_bonusItems.Length > 0 && Random.value < _bonusDropChance)
            drops.Add(_bonusItems[Random.Range(0, _bonusItems.Length)]);

        // 순서를 섞어서, 특별한 아이템이 항상 같은 방향으로만 튀어나가지 않게
        for (int i = drops.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (drops[i], drops[j]) = (drops[j], drops[i]);
        }

        return drops;
    }
}