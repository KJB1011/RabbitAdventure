using DG.Tweening;
using System.Collections;
using UnityEngine;

/// <summary>
/// 상자에서 나온 아이템들이 자동으로 먹어지는 스크립트
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] float _delay = 1f;
    [SerializeField] float _initialSpeed = 2f;
    [SerializeField] float _acceleration = 8f;
    [SerializeField] float _maxSpeed = 20f;
    [SerializeField] float _pickupRadius = 0.4f;

    [Header("골드 금액")]
    [SerializeField] int _goldMin = 6;  // 골드 아이템 하나당 금액 (최소~최대, 최대값 포함)
    [SerializeField] int _goldMax = 7;

    [Header("튀어나오기")]
    [SerializeField] float _jumpPowerMin = 0.6f;    // 점프 높이 (최소~최대 사이 랜덤)
    [SerializeField] float _jumpPowerMax = 1.0f;
    [SerializeField] float _jumpDurationMin = 0.35f; // 점프 시간 (최소~최대 사이 랜덤, _delay보다 짧아야 함)
    [SerializeField] float _jumpDurationMax = 0.5f;

    [Header("아이템 표시")]
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] Sprite _goldSprite;
    [SerializeField] Sprite _hpSprite;
    [SerializeField] Sprite _atkSprite;
    [SerializeField] Sprite _rangeSprite;
    [SerializeField] Sprite _critSprite;

    [Header("뒤쪽 빛 (Glow)")]
    [SerializeField] Sprite _glowSprite;                 // 부드러운 원형 빛 이미지 (ItemGlow)
    [SerializeField] float _glowSize = 1.8f;             // 아이템 크기 대비 빛 크기 (배율)
    [SerializeField] Color _goldGlow = new Color(1f, 0.85f, 0.2f);   // 노랑
    [SerializeField] Color _hpGlow = new Color(1f, 0.45f, 0.75f);    // 분홍
    [SerializeField] Color _atkGlow = new Color(1f, 0.2f, 0.2f);     // 빨강
    [SerializeField] Color _rangeGlow = new Color(0.3f, 1f, 0.4f);   // 초록
    [SerializeField] Color _critGlow = new Color(1f, 0.55f, 0.1f);   // 주황
    [SerializeField, Range(0f, 1f)] float _glowAlphaMin = 0.35f;     // 숨쉬듯 깜빡일 때 가장 흐릴 때
    [SerializeField, Range(0f, 1f)] float _glowAlphaMax = 0.8f;      // 가장 밝을 때
    [SerializeField] float _glowPulseTime = 0.5f;                    // 흐림 → 밝음까지 걸리는 시간

    SpriteRenderer _glowRenderer; // Awake에서 자동으로 만든다

    Defines.ITEM _itemType;
    Transform _playerTF;
    PlayerController _player;
    float _currentSpeed;
    bool _isMoving = false; // true가 되면 플레이어에게 날아가고, 닿으면 먹어진다

    /// <summary>
    /// 현재 위치(상자 중심)에서 target으로 포물선을 그리며 튀어나온다.
    /// 점프가 끝난 뒤 _delay 시점부터 플레이어에게 날아가 먹어진다.
    /// </summary>
    public void JumpTo(Vector2 target)
    {
        float power = Random.Range(_jumpPowerMin, _jumpPowerMax);
        // 점프가 끝나기 전에 날아가기 시작하면 두 움직임이 겹치므로, _delay보다 길어지지 않게 한다
        float duration = Mathf.Min(Random.Range(_jumpDurationMin, _jumpDurationMax), _delay);

        transform.DOKill();
        transform.DOJump(target, power, 1, duration)
                 .SetEase(Ease.Linear);  // DOJump는 높이 곡선을 알아서 만들므로 이동은 일정하게
    }

    public void SetItemType(Defines.ITEM type)
    {
        _itemType = type;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (_spriteRenderer == null) return;
        _spriteRenderer.sprite = _itemType switch
        {
            Defines.ITEM.GOLD => _goldSprite,
            Defines.ITEM.HP => _hpSprite,
            Defines.ITEM.ATKUP => _atkSprite,
            Defines.ITEM.RANGEUP => _rangeSprite,
            Defines.ITEM.CRITRATEUP => _critSprite,
            _ => _goldSprite
        };

        UpdateGlow();
    }

    // ── 뒤쪽 빛 ───────────────────────────────────────────────

    void Awake()
    {
        CreateGlow();
    }

    // 아이템 그림의 자식으로 빛 오브젝트를 하나 만든다 (프리팹을 직접 고치지 않아도 되게)
    void CreateGlow()
    {
        if (_glowSprite == null || _spriteRenderer == null) return;

        var go = new GameObject("Glow");
        go.transform.SetParent(_spriteRenderer.transform, false); // 아이템 그림을 따라 움직인다

        _glowRenderer = go.AddComponent<SpriteRenderer>();
        _glowRenderer.sprite = _glowSprite;
        _glowRenderer.sharedMaterial = _spriteRenderer.sharedMaterial;
        _glowRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
        _glowRenderer.sortingOrder = _spriteRenderer.sortingOrder - 1; // 아이템 바로 뒤에 그린다
    }

    // 아이템 종류에 맞는 색으로 바꾸고, 크기를 맞춘 뒤 숨쉬듯 깜빡이게 한다
    void UpdateGlow()
    {
        if (_glowRenderer == null) return;

        Color color = GetItemColor();
        color.a = _glowAlphaMin;
        _glowRenderer.color = color;

        // 아이템 그림 크기에 맞춰 빛 크기를 정한다 (아이템마다 그림 크기가 달라도 일정한 비율로)
        if (_spriteRenderer.sprite != null)
        {
            Vector2 itemSize = _spriteRenderer.sprite.bounds.size;
            float glowSize = _glowSprite.bounds.size.x;
            float scale = Mathf.Max(itemSize.x, itemSize.y) * _glowSize / glowSize;
            _glowRenderer.transform.localScale = Vector3.one * scale;
        }

        // 투명도만 흐림 ↔ 밝음을 계속 왕복 (Yoyo = 갔다가 되돌아오기, -1 = 무한 반복)
        _glowRenderer.DOKill();
        _glowRenderer.DOFade(_glowAlphaMax, _glowPulseTime)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo);
    }

    void OnEnable()
    {
        _currentSpeed = _initialSpeed;
        _isMoving = false;

        // FindWithTag로 플레이어 루트 오브젝트 찾기
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _playerTF = playerObj.transform;
            // 루트에 없으면 자식/부모까지 탐색
            _player = playerObj.GetComponent<PlayerController>()
                   ?? playerObj.GetComponentInChildren<PlayerController>()
                   ?? playerObj.GetComponentInParent<PlayerController>();

            if (_player == null)
                Debug.LogWarning("[ItemPickup] PlayerController를 찾지 못했습니다. Player 태그 오브젝트를 확인해주세요.");
        }
        else
        {
            Debug.LogWarning("[ItemPickup] 'Player' 태그를 가진 오브젝트가 없습니다.");
        }

        StartCoroutine(DelayedMove());
    }

    void OnDisable()
    {
        // 먹어져서 꺼질 때 남아 있는 점프 트윈과 빛 깜빡임 트윈을 정리한다
        transform.DOKill();
        if (_glowRenderer != null) _glowRenderer.DOKill();
    }

    IEnumerator DelayedMove()
    {
        yield return new WaitForSeconds(_delay);
        _isMoving = true;
    }

    void Update()
    {
        if (_playerTF == null) return;

        // 상자에서 나온 뒤 _delay(1초)가 지나기 전까지는 움직이지도, 먹어지지도 않는다
        // (점프 중에 플레이어가 가까이 있어도 바로 먹어지지 않게)
        if (!_isMoving) return;

        _currentSpeed = Mathf.Min(_currentSpeed + _acceleration * Time.deltaTime, _maxSpeed);
        Vector2 dir = (_playerTF.position - transform.position).normalized;
        transform.Translate(dir * _currentSpeed * Time.deltaTime, Space.World);

        if (Vector2.Distance(transform.position, _playerTF.position) < _pickupRadius)
        {
            // ApplyEffect 먼저 호출 후 비활성화
            ApplyEffect();
            SoundManager.Instance.PlaySFX("SFX/DropCoin");
            gameObject.SetActive(false);
        }
    }

    private void ApplyEffect()
    {
        string effectText = "";

        switch (_itemType)
        {
            case Defines.ITEM.GOLD:
                // int용 Random.Range는 최대값을 포함하지 않으므로 +1 (6~7이면 Range(6, 8))
                int gold = Random.Range(_goldMin, _goldMax + 1);
                IngameManager.Instance.AddGold(gold);
                effectText = $"Gold + {gold}";
                break;

            case Defines.ITEM.HP:
                if (_player != null) _player.RecoverHp(1);
                effectText = "HP + 1";
                break;

            case Defines.ITEM.ATKUP:
                if (_player != null) _player.AddAtk(1f);
                effectText = "공격력 + 1";
                break;

            case Defines.ITEM.RANGEUP:
                if (_player != null) _player.AddRange(0.1f);
                effectText = "공격범위 + 0.1";
                break;

            case Defines.ITEM.CRITRATEUP:
                if (_player != null) _player.AddCritRate(1f);
                effectText = "치명타 확률 + 1%";
                break;
        }

        // 텍스트는 _player null 여부와 무관하게 표시 (뒤쪽 빛과 같은 색으로)
        if (!string.IsNullOrEmpty(effectText) && _playerTF != null)
            FloatingTextManager.Instance.ShowItemEffect(effectText, GetItemColor(), _playerTF.position);
    }

    // 아이템 종류별 대표 색 (뒤쪽 빛과 획득 텍스트가 함께 사용 → 한 곳에서만 관리)
    Color GetItemColor()
    {
        return _itemType switch
        {
            Defines.ITEM.GOLD => _goldGlow,
            Defines.ITEM.HP => _hpGlow,
            Defines.ITEM.ATKUP => _atkGlow,
            Defines.ITEM.RANGEUP => _rangeGlow,
            Defines.ITEM.CRITRATEUP => _critGlow,
            _ => _goldGlow
        };
    }
}