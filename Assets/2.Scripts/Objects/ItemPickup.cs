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

    [Header("아이템 표시")]
    [SerializeField] SpriteRenderer _spriteRenderer;
    [SerializeField] Sprite _goldSprite;
    [SerializeField] Sprite _hpSprite;
    [SerializeField] Sprite _atkSprite;
    [SerializeField] Sprite _rangeSprite;
    [SerializeField] Sprite _critSprite;

    Defines.ITEM _itemType;
    Transform _playerTF;
    PlayerController _player;
    float _currentSpeed;
    bool _isMoving = false;

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

    IEnumerator DelayedMove()
    {
        yield return new WaitForSeconds(_delay);
        _isMoving = true;
    }

    void Update()
    {
        if (_playerTF == null) return;

        if (_isMoving)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + _acceleration * Time.deltaTime, _maxSpeed);
            Vector2 dir = (_playerTF.position - transform.position).normalized;
            transform.Translate(dir * _currentSpeed * Time.deltaTime, Space.World);
        }

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
                int gold = Random.Range(5, 9);
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

        // 텍스트는 _player null 여부와 무관하게 표시
        if (!string.IsNullOrEmpty(effectText) && _playerTF != null)
            FloatingTextManager.Instance.ShowItemEffect(effectText, _playerTF.position);
    }
}