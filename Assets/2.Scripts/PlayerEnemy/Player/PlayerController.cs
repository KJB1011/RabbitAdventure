using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Defines;
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

    Vector2 _dir;
    bool _isDashing = false;
    bool _isAttack = false;
    bool _isGameOver = false;
    bool _isControllable = true;
    bool _boundaryEnabled = true;

    int _invincibleCount = 0;

    float _damageMultiplier = 1f;

    float _mainSkillCooldown;
    float _subSkillCooldown;
    float _superSkillCooldown;
    float _specialSkillCooldown;

    float _atkMultiplierBonus = 0f;       // 외부(아티팩트·버프) 공격력 배율 보너스
    float _specialDamageBonus = 0f;       // 외부 필살기 데미지 보너스
    float _extraDashInvincibleTime = 0f;  // 외부 대쉬 추가 무적 시간

    public event System.Action OnDashStarted;
    public event System.Action<EnemyBase, AttackType> OnAttackHit;   // 맞은 적 + 공격 종류

    public bool IsInvincible => _invincibleCount > 0;
    public int CurrentHp => _hp;
    public int MaxHp => _maxHp;
    public float Atk => _atk;
    public float CritRate => _critRate;
    public float RangeMultiplier => _rangeMultiplier;

    public float RangeMul => _rangeMultiplier;
    public float MainSkillCooldownRatio => _mainSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_mainSkillCooldown / _mainSkillCooldownDuration);
    public float SubSkillCooldownRatio => _subSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_subSkillCooldown / _subSkillCooldownDuration);
    public float SuperSkillCooldownRatio => _superSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_superSkillCooldown / _superSkillCooldownDuration);
    public float SpecialSkillCooldownRatio => _specialSkillCooldownDuration <= 0 ? 0 : Mathf.Clamp01(_specialSkillCooldown / _specialSkillCooldownDuration);
    
    // 외부 효과(아티팩트·버프)용 스탯 변경
    public void AddAtkMultiplier(float bonus) => _atkMultiplierBonus += bonus;
    public void RemoveAtkMultiplier(float bonus) => _atkMultiplierBonus -= bonus;
    public void AddSpecialDamageBonus(float bonus) => _specialDamageBonus += bonus;
    public void AddDashInvincibleTime(float time) => _extraDashInvincibleTime += time;
    public int GetBaseAtk() => (int)_atk;


    // EnemyHitbox 에서 플레이어 공격 적중 시 호출
    public void NotifyAttackHit(EnemyBase target, AttackType type) => OnAttackHit?.Invoke(target, type);
    public AttackType GetAttackType(Collider2D attackCollider) => _playerAttack.GetAttackType(attackCollider);

    // Q 스킬 쿨타임 감소 (외부 효과용)
    public void ReduceSuperSkillCooldown(float amount)
    {
        _superSkillCooldown = Mathf.Max(0, _superSkillCooldown - amount);
        _uiPlayerSkill.SetCoolDown(SKILL.SUPER, SuperSkillCooldownRatio);   // 0이 되면 Update가 갱신하지 않으므로 직접 갱신
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
    public void GameOver()
    {
        _isGameOver = true;
    }

    void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        _spriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _trailRenderer.enabled = false;
        _spriteRenderer.flipX = true;

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
            _subSkillCooldown -= Time.deltaTime;
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

        Vector2 mousePos = Utils.GetMousePos();
        Vector2 dir = (mousePos - (Vector2)_playerAttack.transform.position).normalized;

        if (!_isAttack)
            _playerAttack.transform.right = dir;

        if (!_isDashing && _isControllable)
            transform.Translate(_dir * _speed * Time.deltaTime);

        if (_boundaryEnabled)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, _mapMin.x, _mapMax.x),
                Mathf.Clamp(transform.position.y, _mapMin.y, _mapMax.y),
                transform.position.z
            );
        }

        if (mousePos.x < transform.position.x)
        {
            _uiPlayerSkill.transform.position = transform.position + (Vector3.right / 10 * 7f);
            _spriteRenderer.flipX = false;
        }
        else
        {
            _uiPlayerSkill.transform.position = transform.position - (Vector3.right / 10 * 7f);
            _spriteRenderer.flipX = true;
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
        _playerAttack.AttackStart(() => _isAttack = false);
    }

    void OnDash()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_isDashing) return;
        if (_isAttack) return;
        if (_subSkillCooldown > 0) return;

        _subSkillCooldown = _subSkillCooldownDuration;
        StartCoroutine(DashRoutine());
    }

    void OnSuperSkill()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_superSkillCooldown > 0) return;
        if (_isAttack) return;

        _superSkillCooldown = _superSkillCooldownDuration;
        StartCoroutine(SuperSkillRoutine());
    }

    void OnSpecialSkill()
    {
        if (_isGameOver) return;
        if (!_isControllable) return;
        if (_specialSkillCooldown > 0) return;
        if (_isAttack) return;

        _specialSkillCooldown = _specialSkillCooldownDuration;
        StartCoroutine(SpecialSkillRoutine());
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
        StopAllCoroutines(); // 슈퍼스킬, 대쉬 등 모든 코루틴 중단
        _playerAttack.ForceStop();

        _isAttack = false;
        _isDashing = false;
        _damageMultiplier = 1f;

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

        _playerHitBox.enabled = true;
        _spriteRenderer.enabled = true;
        _trailRenderer.enabled = false;

        // 쿨다운 초기화
        _mainSkillCooldown = 0f;
        _subSkillCooldown = 0f;
        _superSkillCooldown = 0f;
        _specialSkillCooldown = 0f;
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.MAIN, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUB, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SUPER, 0f);
        _uiPlayerSkill.SetCoolDown(Defines.SKILL.SPECIAL, 0f);

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
    }

    public void AddCritRate(float amount)
    {
        _critRate += amount;
    }

    // ── 필살기 ───────────────────────────────────────────────
    IEnumerator SpecialSkillRoutine()
    {
        _isAttack = true;
        _invincibleCount++;
        _playerHitBox.enabled = false;

        SoundManager.Instance.PlaySFX("SFX/SpecialCharge");
        VFXManager.Instance.PlayAttached("SpecialCharge", transform);
        yield return new WaitForSecondsRealtime(_specialInvincibleDelay);

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

        _isAttack = false;
    }

    private void DealSpecialDamage()
    {
        float totalMultiplier = _specialDamageMultiplier * (1f + _specialDamageBonus);
        int damage = Mathf.RoundToInt(DamageReturn() * totalMultiplier);

        // 적 데미지
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (!enemy.gameObject.activeSelf || enemy.IsDead) continue;
            enemy.DamageTaken(damage);
            DamageTextManager.Instance.ShowSpecial(damage, enemy.transform.position);
        }

        // 상자 데미지 (ChestObject는 EnemyBase 미상속)
        var chests = FindObjectsByType<ChestObject>(FindObjectsSortMode.None);
        foreach (var chest in chests)
        {
            if (!chest.gameObject.activeSelf) continue;
            chest.TakeDamage(damage);
        }
    }

    // ── Q 스킬 ───────────────────────────────────────────────
    IEnumerator SuperSkillRoutine()
    {
        _isAttack = true;

        // 1초동안 차징
        Coroutine blinkRoutine = StartCoroutine(BlinkCharge(1f));
        yield return new WaitForSeconds(1f);
        StopCoroutine(blinkRoutine);

        // 깜빡임 끝나면 색상 복구
        _spriteRenderer.color = Color.white;

        _damageMultiplier = 8f;
        bool attackDone = false;
        _playerAttack.SuperAttackStart(() => attackDone = true);

        yield return new WaitUntil(() => attackDone);
        _damageMultiplier = 1f;

        yield return new WaitForSeconds(0.5f);
        _isAttack = false;
    }

    IEnumerator BlinkCharge(float duration)
    {
        float elapsed = 0f;
        float blinkInterval = 0.08f; // 깜빡임 간격 (작을수록 빠름)
        bool isWhite = false;

        Color normalColor = Color.white;
        Color blinkColor = new Color(1f, 1f, 0.2f, 1f); // 노란빛 흰색으로 기 모으는 느낌

        while (elapsed < duration)
        {
            isWhite = !isWhite;
            _spriteRenderer.color = isWhite ? blinkColor : normalColor;

            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
    }

    // ── 데미지 계산 ──────────────────────────────────────────
    public int DamageReturn()
    {
        return DamageReturn(out _);
    }
    public int DamageReturn(out bool isCrit)
    {
        float base_ = Random.Range(_atk * 0.9f, _atk * 1.1f);
        isCrit = Random.Range(0, 100f) < _critRate;
        float damage = isCrit ? base_ * _critDamageRate : base_;
        damage *= (1f + _atkMultiplierBonus);
        return (int)(damage * _damageMultiplier);
    }

    public bool DamageTaken()
    {
        if (IsInvincible || _isGameOver) return false;

        _hp--;
        _hpTxt.text = _hp.ToString();

        SoundManager.Instance?.PlaySFX("SFX/Hit");
        FloatingTextManager.Instance.ShowDamage(transform.position);

        if (_hp <= 0)
        {
            _isGameOver = true;
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

    // ── 대쉬 ────────────────────────────────────────────────
    IEnumerator DashRoutine()
    {
        _isDashing = true;
        _invincibleCount++;
        _playerHitBox.enabled = false;
        _trailRenderer.enabled = true;

        OnDashStarted?.Invoke();

        Vector2 startPos = transform.position;
        Vector2 mouseWorldPos = Utils.GetMousePos();
        Vector2 dirToMouse = mouseWorldPos - startPos;
        Vector2 dashDirection = dirToMouse.normalized;
        float finalDashDistance = Mathf.Min(_dashDistance, dirToMouse.magnitude);
        Vector2 targetPos = startPos + dashDirection * finalDashDistance;

        SoundManager.Instance.PlaySFX("SFX/Dash");
        float elapsed = 0f;
        while (elapsed < _dashDuration)
        {
            elapsed += Time.deltaTime;
            float curveT = _dashCurve.Evaluate(elapsed / _dashDuration);
            transform.position = Vector2.Lerp(startPos, targetPos, curveT);

            if (_boundaryEnabled)
            {
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, _mapMin.x, _mapMax.x),
                    Mathf.Clamp(transform.position.y, _mapMin.y, _mapMax.y),
                    transform.position.z
                );
            }

            if (Vector2.Distance(startPos, transform.position) >= finalDashDistance * 0.95f)
                break;

            yield return null;
        }

        _trailRenderer.enabled = false;
        _isDashing = false;

        if (_extraDashInvincibleTime > 0)
            yield return new WaitForSeconds(_extraDashInvincibleTime);

        _invincibleCount--;
        if (_invincibleCount <= 0)
            _playerHitBox.enabled = true;
    }
}