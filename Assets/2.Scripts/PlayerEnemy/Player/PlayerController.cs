using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Defines;

/// <summary>
/// 플레이어의 이동, 4가지 스킬(기본기·회피기·특수기·필살기), 체력, 쿨타임을 담당한다.
/// 스킬 강화(숙련도)는 PlayerSkillUpgrades 컴포넌트가 담당하고,
/// 여기서는 스킬을 쓸 때 "강화가 있으면 이렇게 해줘"하고 그 컴포넌트에 물어본다. (_upgrades)
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] float _dashDistance = 4f;
    [SerializeField] float _dashDuration = 1f;
    [SerializeField] AnimationCurve _dashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] PlayerAttack _playerAttack;
    [SerializeField] UIPlayerSkill _uiPlayerSkill;
    [SerializeField] Collider2D _playerHitBox;
    [SerializeField] TextMeshProUGUI _hpTxt;
    [SerializeField] TextMeshProUGUI _nameTxt;

    [Header("캐릭터 능력치")]
    [SerializeField] float _speed = 1f;
    [SerializeField, Range(0f, 1f)] float _superChargeSpeedRate = 0.5f; // 특수기 차징 중 이동속도 배율 (0.5 = 절반)
    [SerializeField] int _hp = 10;
    [SerializeField] int _maxHp = 10;
    [SerializeField] float _atk;
    [SerializeField] float _critRate;
    [SerializeField] float _critDamageRate;
    [SerializeField] float _rangeMultiplier = 1.0f; // PlayerAttack에서 이동

    [Header("쿨다운 (초)")]
    [SerializeField] float _mainSkillCooldownDuration = 0.5f;
    [SerializeField] float _subSkillCooldownDuration = 2f;
    [SerializeField] float _superSkillCooldownDuration = 8f;
    [SerializeField] float _specialSkillCooldownDuration = 15f;

    [Header("피격 무적")]
    [SerializeField] float _invisibleTime = 1.5f;
    [SerializeField] float _blinkInterval = 0.1f;

    [Header("필살기 (Special)")]
    [SerializeField] float _specialDamageMultiplier = 30f;
    [SerializeField] float _specialInvincibleDelay = 1.0f;
    [SerializeField] SpecialSkillEffect _specialSkillEffect;

    [Header("노드 이동")]
    [SerializeField] float _nodeMoveDuration = 1f; // 이동 시간

    [Header("맵 경계")]
    [SerializeField] Vector2 _mapMin = new Vector2(-9f, -5f); // 왼쪽 하단
    [SerializeField] Vector2 _mapMax = new Vector2(9f, 5f); // 오른쪽 상단

    TrailRenderer _trailRenderer;
    SpriteRenderer _spriteRenderer;
    Animator _animController;
    PlayerSkillUpgrades _upgrades;

    // 흰색 플래시 (PlayerShaderGraph의 _FlashAmount 제어)
    static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    MaterialPropertyBlock _flashBlock;

    Vector2 _dir;
    bool _isDashing = false;
    bool _isAttack = false;
    bool _isGameOver = false;
    bool _isControllable = true;
    bool _boundaryEnabled = true;

    // 바라보는 방향 고정 (노드 이동 연출 중에는 마우스와 상관없이 한 방향만 보게)
    bool _isFacingLocked = false;
    bool _lockedFaceLeft = false;

    int _invincibleCount = 0;

    float _damageMultiplier = 1f;
    float _moveSpeedRate = 1f; // 이동속도에 곱하는 배율 (1 = 평소, 스킬 사용 중 느려질 때 낮아짐)

    float _mainSkillCooldown;
    float _subSkillCooldown;
    float _superSkillCooldown;
    float _specialSkillCooldown;

    float _atkMultiplierBonus = 0f;       // 외부(아티팩트·버프) 공격력 배율 보너스
    float _specialDamageBonus = 0f;       // 외부 필살기 데미지 보너스
    float _extraDashInvincibleTime = 0f;  // 외부 대쉬 추가 무적 시간

    int _maxDashCharges = 1;              // 회피기 충전 수 (강화하면 2)
    int _dashCharges = 1;
    float _attackEmpowerRate = 1f;        // 지금 하는 공격에 곱하는 배율 (도약의 일격으로 강화된 공격이면 1.3)
    float _basicHitRate = 1f;             // 기본기 한 타의 데미지 배율 (PlayerAttack이 타마다 바꾼다. 3연타 1·2타 = 0.5 등)

    public event System.Action<SKILL> OnSkillUsed;                    // 스킬을 사용한 순간 (숙련도용)
    public event System.Action OnDashStarted;
    public event System.Action OnDashEnded;                          // 대쉬 이동이 끝난 순간 (도착 지점에서)
    public event System.Action<EnemyBase, AttackType> OnAttackHit;   // 맞은 적 + 공격 종류
    public event System.Action OnRangeChanged;                       // 공격범위가 바뀐 순간

    // "이 위치에 맞으면 확정 치명타" 조건들 (아티팩트: 칼끝 일격). 하나라도 true면 치명타
    readonly System.Collections.Generic.List<System.Func<Vector2, bool>> _critRules = new();
    public void AddCritRule(System.Func<Vector2, bool> rule) => _critRules.Add(rule);
    public void RemoveCritRule(System.Func<Vector2, bool> rule) => _critRules.Remove(rule);

    public bool IsInvincible => _invincibleCount > 0;
    public int CurrentHp => _hp;
    public int MaxHp => _maxHp;
    public float Atk => _atk;
    public float CritRate => _critRate;
    public float RangeMultiplier => _rangeMultiplier;

    public float RangeMul => _rangeMultiplier;
    public float MainSkillCooldownRatio => _mainSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_mainSkillCooldown / _mainSkillCooldownDuration);
    // 회피기는 충전이 하나라도 남아 있으면 "사용 가능"으로 보여준다
    public float SubSkillCooldownRatio => _dashCharges > 0 || _subSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_subSkillCooldown / _subSkillCooldownDuration);
    public float SuperSkillCooldownRatio => _superSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_superSkillCooldown / _superSkillCooldownDuration);
    // 필살기 강화(투지 충전)면 쿨타임 대신 게이지가 덜 찬 만큼 가린다
    public float SpecialSkillCooldownRatio => _upgrades != null && _upgrades.UsesSpiritGauge ? _upgrades.SpiritShadow
        : _specialSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_specialSkillCooldown / _specialSkillCooldownDuration);

    // 남은 쿨타임 (초, 0 이상)
    public float MainSkillCooldownLeft => Mathf.Max(0f, _mainSkillCooldown);
    public float SubSkillCooldownLeft => _dashCharges > 0 ? 0f : Mathf.Max(0f, _subSkillCooldown);
    public float SuperSkillCooldownLeft => Mathf.Max(0f, _superSkillCooldown);
    public float SpecialSkillCooldownLeft => _upgrades != null && _upgrades.UsesSpiritGauge ? _upgrades.SpiritHitsLeft // 투지 충전: 남은 타수
        : Mathf.Max(0f, _specialSkillCooldown);
    public int DashCharges => _dashCharges;
    public int MaxDashCharges => _maxDashCharges;
    public bool IsControllable => _isControllable;
    public float AttackEmpowerRate => _attackEmpowerRate;

    // 외부 효과(아티팩트·버프)용 스탯 변경
    public void AddAtkMultiplier(float bonus) => _atkMultiplierBonus += bonus;
    public void RemoveAtkMultiplier(float bonus) => _atkMultiplierBonus -= bonus;
    public void AddSpecialDamageBonus(float bonus) => _specialDamageBonus += bonus;
    public void AddDashInvincibleTime(float time) => _extraDashInvincibleTime += time;
    public void AddCritDamageRate(float amount) => _critDamageRate += amount; // 치명타 배율 (1.5 → 2.0 등)
    public int GetBaseAtk() => (int)_atk;

    // 최대 HP 변경. 줄어들면 현재 HP도 넘지 않게 맞추고, 늘어나면 늘어난 만큼 회복 (최소 1)
    public void AddMaxHp(int amount)
    {
        _maxHp = Mathf.Max(1, _maxHp + amount);
        _hp = Mathf.Clamp(_hp + Mathf.Max(0, amount), 1, _maxHp);
        if (_hpTxt != null) _hpTxt.text = _hp.ToString(); // 플레이어가 파괴된 뒤 불려도 안전하게
    }

    // EnemyHitbox 등에서 플레이어 공격 적중 시 호출
    public void NotifyAttackHit(EnemyBase target, AttackType type)
    {
        _upgrades.OnEnemyHit(); // 필살기 강화(투지 충전): 적을 맞힐 때마다 게이지
        OnAttackHit?.Invoke(target, type);
    }
    public AttackType GetAttackType(Collider2D attackCollider) => _playerAttack.GetAttackType(attackCollider);

    // 특수기 쿨타임 감소 (외부 효과용)
    public void ReduceSuperSkillCooldown(float amount)
    {
        _superSkillCooldown = Mathf.Max(0, _superSkillCooldown - amount);
        _uiPlayerSkill.SetCoolDown(SKILL.SUPER, SuperSkillCooldownRatio);   // 0이 되면 Update가 갱신하지 않으므로 직접 갱신
    }

    // 필살기(Special) 쿨타임 감소 (외부 효과용)
    public void ReduceSpecialSkillCooldown(float amount)
    {
        _specialSkillCooldown = Mathf.Max(0, _specialSkillCooldown - amount);
        _uiPlayerSkill.SetCoolDown(SKILL.SPECIAL, SpecialSkillCooldownRatio);
    }

    // 플레이어 움직임 제어 함수
    public void SetControllable(bool value)
    {
        _isControllable = value;
    }

    // 캐릭터 맵 제한 제어 함수
    public void SetBoundary(bool enabled)
    {
        _boundaryEnabled = enabled;
    }

    // 바라보는 방향을 고정 / 해제 (해제하면 다시 마우스 방향을 따라간다)
    public void LockFacing(bool faceRight)
    {
        _isFacingLocked = true;
        _lockedFaceLeft = !faceRight;
    }
    public void UnlockFacing()
    {
        _isFacingLocked = false;
    }
    public void GameOver()
    {
        _isGameOver = true;
    }

    // 꾹 누르고 있는지 확인하기 위한 입력 액션 (Awake에서 한 번만 찾아 둔다)
    InputAction _attackAction;
    InputAction _dashAction;
    InputAction _superSkillAction;
    InputAction _specialSkillAction;

    void Awake()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            // FindAction: 이름으로 찾되, 없으면 오류 대신 null을 돌려준다
            _attackAction = playerInput.actions.FindAction("Attack");
            _dashAction = playerInput.actions.FindAction("Dash");
            _superSkillAction = playerInput.actions.FindAction("SuperSkill");
            _specialSkillAction = playerInput.actions.FindAction("SpecialSkill");
        }

        _trailRenderer = GetComponent<TrailRenderer>();
        _spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _trailRenderer.enabled = false;
        _spriteRenderer.flipX = true;
        _animController = transform.GetChild(0).GetComponent<Animator>();
        _flashBlock = new MaterialPropertyBlock();
        _upgrades = GetComponent<PlayerSkillUpgrades>();
        if (_upgrades == null) _upgrades = gameObject.AddComponent<PlayerSkillUpgrades>(); // 프리팹에 없으면 기본값으로 추가

        _playerAttack.SetAttackRange(_rangeMultiplier);
    }

    void Update()
    {
        if (_mainSkillCooldown > 0)
        {
            _mainSkillCooldown -= Time.deltaTime;
            _uiPlayerSkill.SetCoolDown(Defines.SKILL.MAIN, MainSkillCooldownRatio);
        }
        if (_subSkillCooldown > 0)
        {
            // 회피기는 충전식: 쿨타임이 끝날 때마다 하나씩 차고, 덜 찼으면 바로 다음 충전을 시작
            _subSkillCooldown -= Time.deltaTime;
            if (_subSkillCooldown <= 0)
            {
                _dashCharges = Mathf.Min(_dashCharges + 1, _maxDashCharges);
                _subSkillCooldown = _dashCharges < _maxDashCharges ? _subSkillCooldown + _subSkillCooldownDuration : 0f;
            }
            _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUB, SubSkillCooldownRatio);
        }
        if (_superSkillCooldown > 0)
        {
            _superSkillCooldown -= Time.deltaTime;
            _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUPER, SuperSkillCooldownRatio);
        }
        if (_specialSkillCooldown > 0)
        {
            _specialSkillCooldown -= Time.deltaTime;
            _uiPlayerSkill.SetCoolDown(Defines.SKILL.SPECIAL, SpecialSkillCooldownRatio);
        }

        // 꾹 누르고 있으면 쿨타임이 끝나는 즉시 다시 발동
        // (각 함수 안에서 쿨타임·공격 중·대시 중 등을 검사하므로, 조건이 맞을 때만 실제로 나간다)
        // 회피기 → 필살기 → 특수기 → 기본기 순서: 같이 누르고 있으면 회피가 가장 먼저, 그다음 큰 스킬이 먼저 나가도록
        if (_dashAction != null && _dashAction.IsPressed()) OnDash();
        if (_specialSkillAction != null && _specialSkillAction.IsPressed()) OnSpecialSkill();
        if (_superSkillAction != null && _superSkillAction.IsPressed()) OnSuperSkill();
        if (_attackAction != null && _attackAction.IsPressed()) OnAttack();

        Vector2 mousePos = Utils.GetMousePos();
        Vector2 dir = (mousePos - (Vector2)_playerAttack.transform.position).normalized;

        if (!_isAttack)
            _playerAttack.transform.right = dir;

        if (!_isDashing && _isControllable && !_isGameOver)
            transform.Translate(_dir * _speed * _moveSpeedRate * Time.deltaTime);

        if (_boundaryEnabled)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, _mapMin.x, _mapMax.x),
                Mathf.Clamp(transform.position.y, _mapMin.y, _mapMax.y),
                transform.position.z
            );
        }

        // 바라보는 방향: 고정 중이면 고정된 방향, 아니면 마우스 방향
        bool faceLeft = (_isFacingLocked || _isGameOver) ? _lockedFaceLeft : mousePos.x < transform.position.x;

        if (faceLeft)
        {
            _uiPlayerSkill.transform.position = transform.position + (Vector3.right / 10 * 7f);
            _spriteRenderer.flipX = true;
        }
        else
        {
            _uiPlayerSkill.transform.position = transform.position - (Vector3.right / 10 * 7f);
            _spriteRenderer.flipX = false;
        }
    }

    // 조작은 InputActionSystem을 활용
    void OnMove(InputValue iValue) => _dir = iValue.Get<Vector2>();

    void OnAttack()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_mainSkillCooldown > 0) return;
        if (_isDashing) return;
        if (_isAttack) return;

        _mainSkillCooldown = _mainSkillCooldownDuration;
        _isAttack = true;
        BeginEmpoweredAttack();
        _playerAttack.AttackStart(EndAttack);
        OnSkillUsed?.Invoke(SKILL.MAIN);
    }

    void OnDash()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_isDashing) return;
        if (_isAttack) return;
        if (_dashCharges <= 0) return;

        _dashCharges--;
        if (_subSkillCooldown <= 0) // 충전 중이 아니었다면 충전 시작
            _subSkillCooldown = _subSkillCooldownDuration;
        _uiPlayerSkill.SetCoolDown(SKILL.SUB, SubSkillCooldownRatio);

        StartCoroutine(DashRoutine());
        OnSkillUsed?.Invoke(SKILL.SUB);
    }

    void OnSuperSkill()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_superSkillCooldown > 0) return;
        if (_isAttack) return;

        _superSkillCooldown = _superSkillCooldownDuration;
        // 특수기 강화가 있으면 강화된 동작, 없으면 기본 동작
        StartCoroutine(_upgrades.HasSuperUpgrade ? _upgrades.SuperRoutine() : SuperSkillRoutine());
        OnSkillUsed?.Invoke(SKILL.SUPER);
    }

    void OnSpecialSkill()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_specialSkillCooldown > 0) return;
        if (_isAttack) return;

        // 투지 충전: 쿨타임 대신 게이지가 가득일 때만 쓸 수 있다
        if (_upgrades.UsesSpiritGauge)
        {
            if (!_upgrades.TryUseSpiritGauge()) return;
        }
        else
        {
            _specialSkillCooldown = _specialSkillCooldownDuration;
        }

        StartCoroutine(SpecialSkillRoutine());
        OnSkillUsed?.Invoke(SKILL.SPECIAL);
    }

    // 플레이어에 표시되는 텍스트 초기화 및 필살기 연출 캔버스 연결
    public void SetInitChar(string name)
    {
        _hpTxt.text = _maxHp.ToString();
        _nameTxt.text = name;

        _specialSkillEffect = GameObject.FindWithTag("SpecialEffect").GetComponent<SpecialSkillEffect>();
        _specialSkillEffect.Close();
    }
    public IEnumerator MoveToPositionRoutine(Vector2 target)
    {
        // 진행 중인 스킬 강제 중단 + 상태 초기화
        StopAllCoroutines(); // 특수기, 대쉬 등 모든 코루틴 중단
        _playerAttack.ForceStop();
        _upgrades.OnNodeMove(); // 강화 쪽 코루틴·버프도 정리

        _isAttack = false;
        _isDashing = false;
        _damageMultiplier = 1f;
        _attackEmpowerRate = 1f;
        _trailRenderer.enabled = false;
        SetFlash(0f);
        _moveSpeedRate = 1f; // 특수기 차징 중에 끊겼을 수 있으므로 원래대로

        // Time.timeScale이 0일 경우 대비 복구
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        SetControllable(false);

        Vector2 start = transform.position;
        float elapsed = 0f;

        while (elapsed < _nodeMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _nodeMoveDuration);
            transform.position = Vector2.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;

        // 무적/히트박스 상태도 초기화
        _invincibleCount = 0;
        _playerHitBox.enabled = true;
        _spriteRenderer.enabled = true;

        SetControllable(true);
    }
    // 지속시간동안 무적 부여
    public void StartInvincible(float duration)
    {
        StartCoroutine(ExternalInvincibleRoutine(duration));
    }

    private IEnumerator ExternalInvincibleRoutine(float duration)
    {
        _invincibleCount++;
        _playerHitBox.enabled = false;

        yield return new WaitForSeconds(duration);

        _invincibleCount--;
        if (_invincibleCount <= 0)
            _playerHitBox.enabled = true;
    }
    // 노드 재시작용 초기화
    public void ResetState()
    {
        _hp = _maxHp;
        _isAttack = false;
        _isDashing = false;
        _isGameOver = false;
        _invincibleCount = 0;
        _damageMultiplier = 1f;
        _attackEmpowerRate = 1f;
        _moveSpeedRate = 1f;
        _dashCharges = _maxDashCharges;

        _playerHitBox.enabled = true;
        _spriteRenderer.enabled = true;
        _trailRenderer.enabled = false;

        // 쓰러진 모습에서 기본 자세로 되돌린다
        _animController.ResetTrigger("isDead");
        _animController.Play("Idle", 0, 0f);

        // 쿨다운 초기화
        _mainSkillCooldown = 0f;
        _subSkillCooldown = 0f;
        _superSkillCooldown = 0f;
        _specialSkillCooldown = 0f;
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.MAIN, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUB, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUPER, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SPECIAL, 0f);
        _upgrades.ResetStates(); // 받아치기 자세, 투지 게이지, 다음 공격 강화 등

        // HP 텍스트 갱신
        if (_hpTxt != null)
            _hpTxt.text = _hp.ToString();

        SetControllable(true);
    }
    // ── 아이템 효과 적용 메서드 ─────────────────────────────
    public void RecoverHp(int amount)
    {
        _hp = Mathf.Min(_hp + amount, _maxHp);
        _hpTxt.text = _hp.ToString();
    }

    public void AddAtk(float amount)
    {
        _atk += amount;
    }

    public void AddRange(float amount)
    {
        _rangeMultiplier += amount;
        _playerAttack.SetAttackRange(_rangeMultiplier);
        OnRangeChanged?.Invoke(); // 공격범위에 따라 효과가 바뀌는 아티팩트(돋보기 안경)용
    }

    public void AddCritRate(float amount)
    {
        _critRate += amount;
    }

    // ── 필살기 ───────────────────────────────────────────────
    IEnumerator SpecialSkillRoutine()
    {
        _isAttack = true;
        BeginEmpoweredAttack();

        _animController.SetTrigger("isSpecial");
        SoundManager.Instance.PlaySFX("SFX/Player_SpecialCharge");
        VFXManager.Instance.PlayAttached("SpecialCharge", transform);
        yield return new WaitForSecondsRealtime(_specialInvincibleDelay);

        // 차징 중에 맞아서 죽었다면 연출을 발동하지 않음
        if (_isGameOver)
        {
            EndAttack();
            yield break;
        }

        // 차징이 끝나고 연출이 발동되는 순간부터 무적
        _invincibleCount++;
        _playerHitBox.enabled = false;

        // PlayerController(항상 활성)에서 스냅샷 찍기
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        if (_specialSkillEffect != null)
            yield return StartCoroutine(
                _specialSkillEffect.PlayRoutine(screenshot, () => DealSpecialDamage())
            );
        else
        {
            Destroy(screenshot);
            DealSpecialDamage();
        }

        _invincibleCount--;
        if (_invincibleCount <= 0)
            _playerHitBox.enabled = true;

        EndAttack();
    }

    private void DealSpecialDamage()
    {
        DealSpecialHit(_upgrades.SpecialDamageRate(_specialDamageMultiplier)); // 강화가 있으면 강화된 배율
        _upgrades.OnSpecialDamageDealt(); // 필살기 강화별 추가 효과 (연속 일섬, 달의 가호, 탄막 반사)
    }

    /// <summary>화면의 모든 적·상자에 한 번 피해 (필살기 강화 "연속 일섬"이 여러 번 부른다)</summary>
    public void DealSpecialHit(float rate)
    {
        float totalMultiplier = rate * (1f + _specialDamageBonus);
        int damage = Mathf.RoundToInt(DamageReturn() * totalMultiplier);
        bool hitAny = false; // 하나라도 맞았는지 (적중 사운드용)

        // 적 데미지
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (!enemy.gameObject.activeSelf || enemy.IsDead) continue;
            enemy.DamageTaken(damage);
            IngameManager.Instance.Flash(enemy.CharSprite, 0.5f);
            DamageTextManager.Instance.ShowSpecial(damage, enemy.transform.position);
            VFXManager.Instance.Play("SpecialHit", enemy.transform.position);
            hitAny = true;
        }

        // 상자 데미지 (ChestObject는 EnemyBase 미상속)
        var chests = FindObjectsByType<ChestObject>(FindObjectsSortMode.None);
        foreach (var chest in chests)
        {
            if (!chest.gameObject.activeSelf) continue;
            chest.TakeDamage(damage);
            hitAny = true;
        }

        // 적중 사운드는 맞은 대상 수와 상관없이 한 번만 (여러 번 겹쳐 재생되면 소리가 뭉개짐)
        // (화면 흔들림은 화면이 드러나는 유리 깨짐 순간에 SpecialSkillEffect가 담당)
        if (hitAny)
            SoundManager.Instance.PlaySFX("SFX/Player_SwordHit");
    }

    // ── 특수기 (기본 동작) ───────────────────────────────────
    IEnumerator SuperSkillRoutine()
    {
        _isAttack = true;
        BeginEmpoweredAttack();
        _animController.SetTrigger("isSuper");

        // 누른 순간부터 이펙트가 나가기 전까지 이동속도 감소
        _moveSpeedRate = _superChargeSpeedRate;

        // 차징
        Coroutine blinkRoutine = StartCoroutine(BlinkCharge(0.4f));
        yield return new WaitForSeconds(0.5f);
        StopCoroutine(blinkRoutine);

        // 깜빡임 끝나면 원래 색으로 복구 (흰색 상태에서 멈췄을 수도 있으므로)
        SetFlash(0f);

        // 이펙트가 발동하는 순간 이동속도 원래대로
        _moveSpeedRate = 1f;

        _damageMultiplier = 8f;
        bool attackDone = false;
        _playerAttack.SuperAttackStart(() => attackDone = true);

        yield return new WaitUntil(() => attackDone);
        _damageMultiplier = 1f;
        _attackEmpowerRate = 1f;

        yield return new WaitForSeconds(0.5f);
        _isAttack = false;
    }

    IEnumerator BlinkCharge(float duration)
    {
        float elapsed = 0f;
        float blinkInterval = 0.08f; // 깜빡임 간격 (작을수록 빠름)
        bool isWhite = false;

        while (elapsed < duration)
        {
            isWhite = !isWhite;
            SetFlash(isWhite ? 1f : 0f); // 1 = 완전 흰색, 0 = 원래 그림

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetFlash(0f);
    }

    // 머티리얼 에셋은 그대로 두고, 이 SpriteRenderer에만 _FlashAmount 값을 적용 (1 = 완전 흰색, 0 = 원래 그림)
    public void SetFlash(float amount)
    {
        _spriteRenderer.GetPropertyBlock(_flashBlock); // 스프라이트 텍스처 등 기존 값 유지
        _flashBlock.SetFloat(FlashAmountId, amount);
        _spriteRenderer.SetPropertyBlock(_flashBlock);
    }

    // ── 데미지 계산 ──────────────────────────────────────────
    public int DamageReturn()
    {
        return DamageReturn(out _);
    }
    public int DamageReturn(out bool isCrit) => CalcDamage(false, out isCrit);

    /// <summary>
    /// 맞은 위치를 아는 공격용 (EnemyHitbox). 확정 치명타 조건(칼끝 일격 등)에 맞으면 무조건 치명타
    /// </summary>
    public int DamageReturn(Vector2 hitPoint, out bool isCrit)
    {
        bool forceCrit = false;
        foreach (var rule in _critRules)
            if (rule(hitPoint)) { forceCrit = true; break; }
        return CalcDamage(forceCrit, out isCrit);
    }

    int CalcDamage(bool forceCrit, out bool isCrit)
    {
        float base_ = Random.Range(_atk * 0.9f, _atk * 1.1f);
        isCrit = forceCrit || Random.Range(0, 100f) < _critRate;
        float damage = isCrit ? base_ * _critDamageRate : base_;
        damage *= (1f + _atkMultiplierBonus);
        return (int)(damage * _damageMultiplier * _attackEmpowerRate * _basicHitRate);
    }

    /// <summary>PlayerAttack이 기본기 판정을 켜고 끌 때 그 타의 데미지 배율을 알려준다</summary>
    public void SetBasicHitRate(float rate) => _basicHitRate = rate;

    public bool DamageTaken()
    {
        if (IsInvincible || _isGameOver) return false;
        if (_upgrades.TryParry()) return true; // 특수기 강화(받아치기): 자세 중이면 피해 대신 반격 (탄환은 사라진다)

        _hp--;
        _hpTxt.text = _hp.ToString();

        SoundManager.Instance?.PlaySFX("SFX/Hit");
        FloatingTextManager.Instance.ShowDamage(transform.position);

        if (_hp <= 0)
        {
            _isGameOver = true;
            _animController.SetTrigger("isDead"); // 쓰러지는 애니메이션 (Any State에서 바로 전환)
            StartCoroutine(GameOverRoutine());
            return true;
        }

        StartCoroutine(InvincibleRoutine());
        return true;
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        // 카메라 줌인
        var cam = Camera.main;
        if (cam != null)
        {
            // 흔들리는 중이었다면 즉시 끝내서 원래 위치로 되돌린 뒤 줌인 (두 연출이 위치를 다투지 않게)
            DOTween.Complete(IngameManager.CamShakeId);

            float zoomDuration = 1.5f;
            Vector3 targetPos = new Vector3(
                transform.position.x,
                transform.position.y,
                cam.transform.position.z);

            cam.transform.DOMove(targetPos, zoomDuration)
                         .SetEase(Ease.InOutQuad)
                         .SetUpdate(true); // timeScale 0이어도 동작

            cam.DOOrthoSize(cam.orthographicSize * 0.4f, zoomDuration)
               .SetEase(Ease.InOutQuad)
               .SetUpdate(true);

            yield return new WaitForSecondsRealtime(zoomDuration);
        }

        Time.timeScale = 0f;

        IngameManager.Instance.ShowGameover();
    }

    // ── 피격 무적 + 깜빡임 ───────────────────────────────────
    IEnumerator InvincibleRoutine()
    {
        _invincibleCount++;
        _playerHitBox.enabled = false;

        float elapsed = 0f;
        while (elapsed < _invisibleTime)
        {
            _spriteRenderer.enabled = false;
            yield return new WaitForSeconds(_blinkInterval);
            _spriteRenderer.enabled = true;
            yield return new WaitForSeconds(_blinkInterval);
            elapsed += _blinkInterval * 2f;
        }

        _spriteRenderer.enabled = true;
        _invincibleCount--;
        if (_invincibleCount <= 0)
            _playerHitBox.enabled = true;
    }

    // ── 스킬 강화 (숙련도) ──────────────────────────────────
    /// <summary>SkillMasteryManager에서 고른 강화를 켠다 (실제 처리는 PlayerSkillUpgrades)</summary>
    public void ApplySkillUpgrade(SkillUpgradeType type) => _upgrades.Apply(type);

    // ── PlayerSkillUpgrades가 쓰는 함수들 ────────────────────
    // (강화 컴포넌트가 플레이어 상태를 바꿀 때는 private 변수를 직접 건드리지 않고 이 함수들을 거친다)

    public PlayerAttack Attack => _playerAttack;
    public SpriteRenderer CharSprite => _spriteRenderer;
    public bool IsGameOver => _isGameOver;
    public bool IsAttacking { get => _isAttack; set => _isAttack = value; }
    public bool IsSuperKeyHeld => _superSkillAction != null && _superSkillAction.IsPressed();
    public float SuperChargeSpeedRate => _superChargeSpeedRate;
    public float DashDistance => _dashDistance;
    public float ExtraDashInvincibleTime => _extraDashInvincibleTime;

    public void SetMoveSpeedRate(float rate) => _moveSpeedRate = rate;
    public void SetDamageMultiplier(float multiplier) => _damageMultiplier = multiplier;
    public void PlayAnim(string trigger) => _animController.SetTrigger(trigger);

    /// <summary>공격을 시작할 때: "다음 공격 강화"가 준비돼 있으면 이번 공격에 적용</summary>
    public void BeginEmpoweredAttack() => _attackEmpowerRate = _upgrades.ConsumeAttackEmpower();

    /// <summary>공격이 끝났을 때: 공격 중 상태와 강화 배율을 원래대로</summary>
    public void EndAttack()
    {
        _isAttack = false;
        _attackEmpowerRate = 1f;
    }

    /// <summary>기본기 쿨타임 변경 (쿨타임 × multiply + add)</summary>
    public void ModifyMainCooldown(float multiply, float add)
        => _mainSkillCooldownDuration = _mainSkillCooldownDuration * multiply + add;

    /// <summary>회피기 최대 충전 수 변경. 고르자마자 하나 더 쓸 수 있게 충전도 하나 채운다</summary>
    public void SetMaxDashCharges(int max)
    {
        _maxDashCharges = Mathf.Max(_maxDashCharges, max);
        _dashCharges = Mathf.Min(_dashCharges + 1, _maxDashCharges);
        // 이미 가득 찼는데 쿨타임이 돌고 있었다면 멈추고, 덜 찼는데 멈춰 있었다면 충전 시작
        if (_dashCharges >= _maxDashCharges) _subSkillCooldown = 0f;
        else if (_subSkillCooldown <= 0f) _subSkillCooldown = _subSkillCooldownDuration;
        RefreshSkillUI(SKILL.SUB);
    }

    /// <summary>필살기 쿨타임 길이 변경 (달의 가호 9초, 투지 충전 0초)</summary>
    public void SetSpecialCooldownDuration(float duration)
    {
        _specialSkillCooldownDuration = duration;
        _specialSkillCooldown = Mathf.Min(_specialSkillCooldown, duration);
        RefreshSkillUI(SKILL.SPECIAL);
    }

    /// <summary>특수기 쿨타임을 전체 길이의 ratio만큼 돌려받는다 (받아치기 실패)</summary>
    public void RefundSuperCooldown(float ratio)
    {
        _superSkillCooldown = Mathf.Max(0f, _superSkillCooldown - _superSkillCooldownDuration * ratio);
        RefreshSkillUI(SKILL.SUPER);
    }

    /// <summary>플레이어 옆 스킬 UI의 쿨타임 표시를 지금 값으로 다시 그린다</summary>
    public void RefreshSkillUI(SKILL skill)
    {
        if (_uiPlayerSkill == null) return;
        float ratio = skill switch
        {
            SKILL.MAIN => MainSkillCooldownRatio,
            SKILL.SUB => SubSkillCooldownRatio,
            SKILL.SUPER => SuperSkillCooldownRatio,
            _ => SpecialSkillCooldownRatio,
        };
        _uiPlayerSkill.SetCoolDown(skill, ratio);
    }

    /// <summary>맵 경계 안으로 좌표를 맞춘다 (경계가 꺼져 있으면 그대로)</summary>
    public Vector2 ClampToMap(Vector2 p) => _boundaryEnabled
        ? new Vector2(Mathf.Clamp(p.x, _mapMin.x, _mapMax.x), Mathf.Clamp(p.y, _mapMin.y, _mapMax.y))
        : p;

    /// <summary>순간이동 (꼬리가 이동 경로를 따라 길게 그려지지 않게 지운다)</summary>
    public void TeleportTo(Vector2 target)
    {
        _trailRenderer.Clear();
        transform.position = new Vector3(target.x, target.y, transform.position.z);
        _trailRenderer.Clear();
    }

    public void RaiseDashStarted() => OnDashStarted?.Invoke();
    public void RaiseDashEnded() => OnDashEnded?.Invoke();

    // ── 회피기 ──────────────────────────────────────────────
    IEnumerator DashRoutine()
    {
        // 회피기 강화(순간이동): 미끄러지지 않고 바로 이동한다
        if (_upgrades.HasBlink)
        {
            yield return StartCoroutine(_upgrades.BlinkRoutine());
            yield break;
        }

        _isDashing = true;
        _invincibleCount++;
        _playerHitBox.enabled = false;
        _trailRenderer.enabled = true;

        OnDashStarted?.Invoke();
        _upgrades.OnDashBegin();

        // 질풍베기 강화면 더 빠르고 조금 더 멀리
        float maxDistance = _dashDistance * _upgrades.DashDistanceRate;
        float duration = _dashDuration * _upgrades.DashDurationRate;

        Vector2 startPos = transform.position;
        Vector2 mouseWorldPos = Utils.GetMousePos();
        Vector2 dirToMouse = mouseWorldPos - startPos;
        Vector2 dashDirection = dirToMouse.normalized;
        float finalDashDistance = Mathf.Min(maxDistance, dirToMouse.magnitude);
        Vector2 targetPos = startPos + dashDirection * finalDashDistance;

        _animController.SetTrigger("isDash");
        SoundManager.Instance.PlaySFX("SFX/Player_Dash");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float curveT = _dashCurve.Evaluate(elapsed / duration);
            transform.position = Vector2.Lerp(startPos, targetPos, curveT);

            if (_boundaryEnabled)
            {
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, _mapMin.x, _mapMax.x),
                    Mathf.Clamp(transform.position.y, _mapMin.y, _mapMax.y),
                    transform.position.z
                );
            }

            _upgrades.OnDashMoving(); // 질풍베기: 지나가는 자리의 적을 벤다

            if (Vector2.Distance(startPos, transform.position) >= finalDashDistance * 0.95f)
                break;

            yield return null;
        }

        _trailRenderer.enabled = false;
        _isDashing = false;
        _upgrades.OnDashFinished(); // 도약의 일격: 다음 공격 강화 준비
        OnDashEnded?.Invoke();

        if (_extraDashInvincibleTime > 0)
            yield return new WaitForSeconds(_extraDashInvincibleTime);

        _invincibleCount--;
        if (_invincibleCount <= 0)
            _playerHitBox.enabled = true;
    }
}
