using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Defines;

/// <summary>
/// 플레이어의 "스킬 강화(숙련도)"를 담당하는 컴포넌트. Player 프리팹에 PlayerController와 함께 붙는다.
///
/// - 어떤 강화를 골랐는지 기억하고(Apply), 강화별 수치를 인스펙터에서 조정할 수 있게 모아 둔다.
/// - PlayerController는 스킬을 쓸 때 "강화가 있으면 이렇게 해줘"하고 이 컴포넌트의 함수를 부른다.
///   (예: 대쉬 거리 배율, 특수기 동작, 필살기 피해 배율, 받아치기 판정 …)
/// - 기본기의 모양(찌르기·3연타 등)은 PlayerAttack이 담당하고, 여기서는 쿨타임만 바꾼다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerSkillUpgrades : MonoBehaviour
{
    enum SuperStyle { Normal, Charge, Spin, Parry, SwordPlant }
    enum SpecialStyle { Normal, MultiSlash, MoonBlessing, Reflect, Spirit }

    // ── 기본기 ──
    [Header("기본기 강화 (모양·데미지는 PlayerAttack)")]
    [SerializeField] float _thrustAttackSpeedRate = 1.5f;   // 찌르기: 공격 속도 배율 (쿨타임 ÷ 1.5)
    [SerializeField] float _tripleCooldownAdd = 0.5f;       // 3연타: 쿨타임 증가 (초)
    [SerializeField] float _crescentCooldownAdd = 0.5f;     // 초승달 베기: 쿨타임 증가 (초)

    // ── 회피기 ──
    [Header("회피기 강화: 질풍베기")]
    [SerializeField] float _galeDurationRate = 0.45f;   // 대쉬 시간 배율 (작을수록 빠름)
    [SerializeField] float _galeDistanceRate = 1.3f;    // 대쉬 거리 배율
    [SerializeField] float _galeHitRadius = 0.8f;       // 지나가며 베는 판정 반경
    [SerializeField] float _galeDamageRate = 1f;        // 기본 공격력의 몇 배 (1 = 100%)

    [Header("회피기 강화: 연속 도약")]
    [SerializeField] int _upgradedDashCharges = 2;

    [Header("회피기 강화: 도약의 일격")]
    [SerializeField] float _dashEmpowerBonus = 0.3f;    // 다음 공격 데미지 +30%
    [SerializeField] Color _dashEmpowerTint = new Color(1f, 0.8f, 0.55f); // 강화가 준비된 동안 캐릭터 색

    [Header("회피기 강화: 순간이동")]
    [SerializeField] float _blinkClearRadius = 1.3f;     // 도착 지점 주변 적 탄환을 지우는 반경

    // ── 특수기 ──
    [Header("특수기 강화: 모아베기")]
    [SerializeField] float _chargeStageTime = 0.5f;      // 한 단계 모으는 시간
    [SerializeField] float _chargeMaxHold = 1.6f;        // 이 시간이 지나면 떼지 않아도 자동으로 벤다
    [SerializeField] float[] _chargeDamage = { 5f, 9f, 15f };     // 단계별 데미지 배율 (500% / 900% / 1500%)
    [SerializeField] float[] _chargeRange = { 1f, 1.3f, 1.6f };   // 단계별 범위 배율

    [Header("특수기 강화: 회전베기")]
    [SerializeField] int _spinCount = 3;
    [SerializeField] float _spinInterval = 0.13f;
    [SerializeField] float _spinRadius = 2.5f;
    [SerializeField] float _spinDamageRate = 2.5f;        // 한 바퀴당 250%
    [SerializeField] float _spinBulletClearRadius = 1.6f; // 탄환을 지우는 반경 (공격 범위보다 작게)

    [Header("특수기 강화: 받아치기")]
    [SerializeField] float _parryWindow = 0.4f;          // 받아치기 자세 시간
    [SerializeField] float _parryMoveRate = 0.3f;        // 자세 중 이동속도 배율
    [SerializeField] float _parryCounterRate = 15f;      // 반격 1500%
    [SerializeField] float _parryInvincibleTime = 0.6f;  // 반격 후 무적
    [SerializeField, Range(0f, 1f)] float _parryFailRefund = 0.5f; // 실패하면 쿨타임 이만큼 돌려받음

    [Header("특수기 강화: 검 꽂기")]
    [SerializeField] float _plantRadius = 2f;
    [SerializeField] float _plantDuration = 4f;
    [SerializeField] float _plantTickInterval = 0.5f;
    [SerializeField] float _plantDamageRate = 1.2f;      // 한 번에 120%

    // ── 필살기 ──
    [Header("필살기 강화")]
    [SerializeField] float _multiSlashRate = 7f;         // 연속 일섬: 한 번에 700%
    [SerializeField] int _multiSlashCount = 5;
    [SerializeField] float _multiSlashInterval = 0.12f;
    [SerializeField] float _moonRate = 18f;              // 달의 가호: 1800%
    [SerializeField] float _moonCooldown = 9f;
    [SerializeField] float _moonAtkBonus = 0.3f;         // 공격력 +30%
    [SerializeField] float _moonBuffTime = 6f;
    [SerializeField] float _reflectRate = 20f;           // 탄막 반사: 본 피해 2000%
    [SerializeField] float _reflectWaveRate = 0.6f;      // 되돌린 검기 1개당 60%
    [SerializeField] int _reflectMax = 20;
    [SerializeField] float _spiritRate = 25f;            // 투지 충전: 2500%
    [SerializeField] int _spiritRequiredHits = 40;       // 필요한 적중 수

    PlayerController _player;

    // 고른 강화
    bool _hasGale, _hasDashEmpower, _hasBlink;
    SuperStyle _superStyle = SuperStyle.Normal;
    SpecialStyle _specialStyle = SpecialStyle.Normal;

    // 진행 상태
    bool _isDashEmpowerReady;
    bool _isParrying, _parrySucceeded;
    int _spiritGauge;
    Coroutine _moonBuffRoutine;
    bool _isMoonBuffActive;

    GameObject _moonAura;   // 달의 가호 동안 몸에 붙는 오라
    GameObject _chargeGlow; // 모아베기 중 몸에 붙는 빛

    readonly HashSet<EnemyBase> _galeHitEnemies = new();
    readonly HashSet<ChestObject> _galeHitChests = new();
    readonly List<PooledBullet> _bulletBuffer = new();
    static int _lastAreaHitSoundFrame = -1;

    void Awake() => _player = GetComponent<PlayerController>();

    // ══ PlayerController가 물어보는 값들 ═══════════════════════

    public bool HasBlink => _hasBlink;
    public float DashDistanceRate => _hasGale ? _galeDistanceRate : 1f;
    public float DashDurationRate => _hasGale ? _galeDurationRate : 1f;
    public bool HasSuperUpgrade => _superStyle != SuperStyle.Normal;
    public bool UsesSpiritGauge => _specialStyle == SpecialStyle.Spirit;
    public float SpiritShadow => _spiritRequiredHits <= 0 ? 0f : 1f - Mathf.Clamp01((float)_spiritGauge / _spiritRequiredHits);
    public float SpiritHitsLeft => Mathf.Max(0, _spiritRequiredHits - _spiritGauge);

    public float SpecialDamageRate(float normalRate) => _specialStyle switch
    {
        SpecialStyle.MultiSlash => _multiSlashRate,
        SpecialStyle.MoonBlessing => _moonRate,
        SpecialStyle.Reflect => _reflectRate,
        SpecialStyle.Spirit => _spiritRate,
        _ => normalRate,
    };

    // ══ 강화 적용 ═══════════════════════════════════════════════

    /// <summary>SkillMasteryManager에서 고른 강화를 켠다 (PlayerController.ApplySkillUpgrade가 넘겨준다)</summary>
    public void Apply(SkillUpgradeType type)
    {
        switch (type)
        {
            // 기본기: 모양은 PlayerAttack, 쿨타임은 PlayerController
            case SkillUpgradeType.MainThrust:
                _player.Attack.SetBasicStyle(PlayerAttack.BasicStyle.Thrust);
                _player.ModifyMainCooldown(1f / _thrustAttackSpeedRate, 0f);
                break;
            case SkillUpgradeType.MainTripleSlash:
                _player.Attack.SetBasicStyle(PlayerAttack.BasicStyle.TripleSlash);
                _player.ModifyMainCooldown(1f, _tripleCooldownAdd);
                break;
            case SkillUpgradeType.MainCrescent:
                _player.Attack.SetBasicStyle(PlayerAttack.BasicStyle.Crescent);
                _player.ModifyMainCooldown(1f, _crescentCooldownAdd);
                break;
            case SkillUpgradeType.MainSwordWave:
                _player.Attack.SetBasicStyle(PlayerAttack.BasicStyle.SwordWave);
                break;

            // 회피기
            case SkillUpgradeType.DashGale: _hasGale = true; break;
            case SkillUpgradeType.DashDoubleCharge: _player.SetMaxDashCharges(_upgradedDashCharges); break;
            case SkillUpgradeType.DashEmpower: _hasDashEmpower = true; break;
            case SkillUpgradeType.DashBlink: _hasBlink = true; break;

            // 특수기
            case SkillUpgradeType.SuperCharge: _superStyle = SuperStyle.Charge; break;
            case SkillUpgradeType.SuperSpin: _superStyle = SuperStyle.Spin; break;
            case SkillUpgradeType.SuperParry: _superStyle = SuperStyle.Parry; break;
            case SkillUpgradeType.SuperSwordPlant: _superStyle = SuperStyle.SwordPlant; break;

            // 필살기
            case SkillUpgradeType.SpecialMultiSlash: _specialStyle = SpecialStyle.MultiSlash; break;
            case SkillUpgradeType.SpecialMoonBlessing:
                _specialStyle = SpecialStyle.MoonBlessing;
                _player.SetSpecialCooldownDuration(_moonCooldown);
                break;
            case SkillUpgradeType.SpecialReflect: _specialStyle = SpecialStyle.Reflect; break;
            case SkillUpgradeType.SpecialSpirit:
                _specialStyle = SpecialStyle.Spirit;
                _spiritGauge = _spiritRequiredHits; // 고르자마자 한 번은 쓸 수 있게
                _player.SetSpecialCooldownDuration(0f);
                _player.RefreshSkillUI(SKILL.SPECIAL);
                break;
        }
    }

    // ══ 노드 이동·재시작 ═══════════════════════════════════════

    /// <summary>노드 이동 시작: 진행 중인 강화 코루틴을 멈추고 상태를 정리한다</summary>
    public void OnNodeMove()
    {
        StopAllCoroutines();
        _moonBuffRoutine = null;
        _isParrying = false;
        EndMoonBuff();
        ClearAttached(ref _chargeGlow);
    }

    // ══ 몸에 붙는 지속 이펙트 (오라 등) ════════════════════════
    // VFXManager 풀은 3초 뒤 자동으로 거둬 가므로, 오래 유지할 이펙트는 직접 만들고 지운다

    GameObject SpawnAttached(string id, float scale = 1f)
    {
        var prefab = Resources.Load<GameObject>($"VFX/{id}");
        if (prefab == null) return null;
        var go = Instantiate(prefab, transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static void ClearAttached(ref GameObject go)
    {
        if (go != null) Destroy(go);
        go = null;
    }

    /// <summary>노드 재시작 시 (PlayerController.ResetState)</summary>
    public void ResetStates()
    {
        OnNodeMove();
        _parrySucceeded = false;
        SetDashEmpowerReady(false);
        if (UsesSpiritGauge) _spiritGauge = _spiritRequiredHits;
        _player.RefreshSkillUI(SKILL.SPECIAL);
    }

    // ══ 공용: 원 안의 적에게 피해 ═══════════════════════════════

    /// <summary>
    /// center 반경 안의 적(과 상자)에게 한 번씩 피해를 준다. 맞힌 적 수를 돌려준다.
    /// </summary>
    /// <param name="damageRate">기본 공격력의 몇 배인지</param>
    /// <param name="notifyType">아티팩트에 "적중" 알림을 보낼 공격 종류 (null이면 알리지 않음 → 연쇄 발동 방지)</param>
    /// <param name="extraRate">추가 배율 (설치형처럼 나중에 맞히는 공격이 쓴 순간의 강화 배율을 유지할 때)</param>
    public int HitArea(Vector2 center, float radius, float damageRate, AttackType? notifyType, float extraRate = 1f)
    {
        int count = 0;
        var hitEnemies = new HashSet<EnemyBase>();
        bool hitChest = false;

        foreach (var hit in Physics2D.OverlapCircleAll(center, radius))
        {
            if (hit.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !hitEnemies.Add(enemy)) continue;
                HitEnemy(enemy, center, damageRate * extraRate);

                if (notifyType.HasValue) _player.NotifyAttackHit(enemy, notifyType.Value);
                else OnEnemyHit();
                count++;
                continue;
            }

            if (!hitChest)
            {
                var chest = hit.GetComponentInParent<ChestObject>();
                if (chest != null && chest.gameObject.activeSelf)
                {
                    chest.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_player.DamageReturn() * damageRate * extraRate)));
                    hitChest = true;
                }
            }
        }

        if (count > 0 || hitChest) PlayHitSound();
        return count;
    }

    // 적 하나에게 피해 + 맞는 연출
    void HitEnemy(EnemyBase enemy, Vector2 from, float rate)
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(_player.DamageReturn(out bool isCrit) * rate));
        enemy.DamageTaken(damage);
        IngameManager.Instance.Flash(enemy.CharSprite, 0.05f);
        DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);

        Vector2 dir = (Vector2)enemy.transform.position - from;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        VFXManager.Instance.Play("HitSpark", enemy.transform.position, Quaternion.LookRotation(dir, Vector3.back));
    }

    static void PlayHitSound()
    {
        if (_lastAreaHitSoundFrame == Time.frameCount) return; // 같은 프레임에 여러 번 울리지 않게
        _lastAreaHitSoundFrame = Time.frameCount;
        SoundManager.Instance.PlaySFX("SFX/Player_SwordHit");
    }

    // ══ 회피기 ═════════════════════════════════════════════════

    /// <summary>대쉬 시작 (질풍: 이번 대쉬에서 맞은 적 목록 초기화)</summary>
    public void OnDashBegin()
    {
        _galeHitEnemies.Clear();
        _galeHitChests.Clear();

        // 질풍베기: 바람 소리 + 굵고 밝은 꼬리(잔상)
        if (_hasGale)
        {
            SoundManager.Instance.PlaySFXVaried("SFX/Gen_Wind", 0.05f);
            _player.SetTrailBoost(true);
        }
    }

    /// <summary>대쉬로 움직이는 매 프레임 (질풍: 지나가는 자리의 적을 벤다)</summary>
    public void OnDashMoving()
    {
        if (!_hasGale) return;

        Vector2 pos = transform.position;
        bool hitAny = false;
        foreach (var hit in Physics2D.OverlapCircleAll(pos, _galeHitRadius))
        {
            if (hit.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !_galeHitEnemies.Add(enemy)) continue;
                HitEnemy(enemy, pos, _galeDamageRate);
                hitAny = true;
                continue;
            }

            var chest = hit.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeSelf && _galeHitChests.Add(chest))
            {
                chest.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_player.DamageReturn() * _galeDamageRate)));
                hitAny = true;
            }
        }
        if (hitAny) PlayHitSound();
    }

    /// <summary>대쉬가 끝난 순간 (도약의 일격: 다음 공격 강화 준비)</summary>
    public void OnDashFinished()
    {
        _player.SetTrailBoost(false);
        if (_hasDashEmpower) SetDashEmpowerReady(true);
    }

    /// <summary>공격을 시작할 때 호출. 다음 공격 강화가 준비돼 있으면 소모하고 배율을 돌려준다 (없으면 1)</summary>
    public float ConsumeAttackEmpower()
    {
        if (!_isDashEmpowerReady) return 1f;
        SetDashEmpowerReady(false);
        return 1f + _dashEmpowerBonus;
    }

    // 준비된 동안은 캐릭터 색을 살짝 바꿔서 알려준다
    void SetDashEmpowerReady(bool ready)
    {
        _isDashEmpowerReady = ready;
        if (_player != null && _player.CharSprite != null)
            _player.CharSprite.color = ready ? _dashEmpowerTint : Color.white;
    }

    /// <summary>순간이동: 미끄러지지 않고 바로 이동 + 도착 지점 주변 탄환 제거 (이동 중 무적 없음)</summary>
    public IEnumerator BlinkRoutine()
    {
        _player.RaiseDashStarted();

        Vector2 start = transform.position;
        Vector2 toMouse = Utils.GetMousePos() - start;
        float dist = Mathf.Min(_player.DashDistance, toMouse.magnitude);
        Vector2 target = _player.ClampToMap(start + (toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : Vector2.right) * dist);

        SoundManager.Instance.PlaySFXVaried("SFX/Gen_Blink", 0.05f);
        VFXManager.Instance.Play("BlinkStartVFX", start);   // 펑 하고 사라지고
        _player.TeleportTo(target);

        VFXManager.Instance.ClearBulletsInRadius(target, _blinkClearRadius);
        VFXManager.Instance.Play("LightHitVFX", target);    // 반짝 하고 나타난다
        OnDashFinished();
        _player.RaiseDashEnded();

        // 도착 순간 하얗게 번쩍
        _player.SetFlash(1f);
        yield return new WaitForSeconds(0.06f);
        _player.SetFlash(0f);

        // 이동 중 무적은 없지만, 아티팩트로 받은 "대쉬 후 무적"은 그대로 준다
        if (_player.ExtraDashInvincibleTime > 0)
            _player.StartInvincible(_player.ExtraDashInvincibleTime);
    }

    // ══ 특수기 ═════════════════════════════════════════════════

    /// <summary>고른 특수기 강화의 동작 (PlayerController가 코루틴으로 실행한다)</summary>
    public IEnumerator SuperRoutine()
    {
        switch (_superStyle)
        {
            case SuperStyle.Charge: return SuperChargeRoutine();
            case SuperStyle.Spin: return SuperSpinRoutine();
            case SuperStyle.Parry: return SuperParryRoutine();
            case SuperStyle.SwordPlant: return SuperPlantRoutine();
            default: return null;
        }
    }

    // 모아베기: 누르고 있는 동안 단계가 오르고, 떼면 벤다
    IEnumerator SuperChargeRoutine()
    {
        _player.IsAttacking = true;
        _player.BeginEmpoweredAttack();
        _player.PlayAnim("isSuper");
        _player.SetMoveSpeedRate(_player.SuperChargeSpeedRate);

        // 모으기 시작: "띵" + 몸에 작은 빛 (단계가 오를수록 소리는 높게, 빛은 크게)
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_ChargeStep", 0.9f, 0.7f);
        ClearAttached(ref _chargeGlow);
        _chargeGlow = SpawnAttached("ChargeGlow", 0.6f);

        float held = 0f, blinkTimer = 0f;
        int stage = 1;
        bool white = false;
        while (held < _chargeMaxHold && !_player.IsGameOver && _player.IsSuperKeyHeld)
        {
            held += Time.deltaTime;
            int next = 1 + Mathf.Min(_chargeDamage.Length - 1, (int)(held / _chargeStageTime));
            if (next != stage)
            {
                stage = next;
                SoundManager.Instance.PlaySFXPitch("SFX/Gen_ChargeStep", 0.9f + 0.3f * (stage - 1));
                if (_chargeGlow != null) _chargeGlow.transform.localScale = Vector3.one * (0.6f + 0.35f * (stage - 1));
                FloatingTextManager.Instance.ShowItemEffect($"{stage}단계", new Color(1f, 0.85f, 0.3f), transform.position);
            }

            // 단계가 오를수록 빠르게 깜빡임
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= 0.14f / stage)
            {
                blinkTimer = 0f;
                white = !white;
                _player.SetFlash(white ? 0.8f : 0f);
            }
            yield return null;
        }

        _player.SetFlash(0f);
        _player.SetMoveSpeedRate(1f);
        ClearAttached(ref _chargeGlow);
        if (_player.IsGameOver)
        {
            _player.EndAttack();
            yield break;
        }

        int i = Mathf.Clamp(stage - 1, 0, _chargeDamage.Length - 1);
        _player.SetDamageMultiplier(_chargeDamage[i]);
        bool done = false;
        _player.Attack.SuperAttackStart(() => done = true, _chargeRange[Mathf.Min(i, _chargeRange.Length - 1)]);
        yield return new WaitUntil(() => done);
        _player.SetDamageMultiplier(1f);

        yield return new WaitForSeconds(0.3f);
        _player.EndAttack();
    }

    // 회전베기: 주변을 여러 번 베고, 닿은 탄환을 지운다 (움직이면서 가능)
    IEnumerator SuperSpinRoutine()
    {
        _player.IsAttacking = true;
        _player.BeginEmpoweredAttack();

        float m = _player.RangeMultiplier;
        for (int i = 0; i < _spinCount; i++)
        {
            Vector2 c = transform.position;
            var vfx = VFXManager.Instance.Play("SpinVFX", c, Quaternion.Euler(0f, 0f, i * 120f));
            if (vfx != null) vfx.transform.localScale = Vector3.one * m;
            // 바퀴마다 조금씩 높아지는 칼 소리 (슝-슝-슝↑)
            SoundManager.Instance.PlaySFXPitch(i == 0 ? "SFX/Player_StrongSword" : "SFX/Player_Sword", 1.1f + 0.12f * i);

            VFXManager.Instance.ClearBulletsInRadius(c, _spinBulletClearRadius * m);
            HitArea(c, _spinRadius * m, _spinDamageRate, AttackType.Super);
            yield return new WaitForSeconds(_spinInterval);
        }

        _player.EndAttack();
    }

    // 받아치기: 자세 중에 맞으면 피해 대신 반격
    IEnumerator SuperParryRoutine()
    {
        _player.IsAttacking = true;
        _isParrying = true;
        _parrySucceeded = false;
        _player.SetMoveSpeedRate(_parryMoveRate);
        _player.SetFlash(0.6f);
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_ChargeStep", 0.75f, 0.7f); // 낮은 "띵" = 자세를 잡았다

        float t = 0f;
        while (t < _parryWindow && !_parrySucceeded && !_player.IsGameOver)
        {
            t += Time.deltaTime;
            yield return null;
        }

        _isParrying = false;
        _player.SetFlash(0f);
        _player.SetMoveSpeedRate(1f);

        if (_parrySucceeded && !_player.IsGameOver)
        {
            // 반격: 잠깐 무적 + 주변 탄환 정리 + 앞쪽 크게 베기
            _player.StartInvincible(_parryInvincibleTime);
            VFXManager.Instance.ClearBulletsInRadius(transform.position, 2f);
            SoundManager.Instance.PlaySFX("SFX/Gen_Parry");                 // "챙!"
            VFXManager.Instance.Play("ParryVFX", transform.position);       // 칼끼리 부딪히는 X자 섬광
            VFXManager.Instance.Play("FlashVFX", transform.position);
            FloatingTextManager.Instance.ShowItemEffect("받아치기!", new Color(1f, 0.85f, 0.3f), transform.position);
            IngameManager.Instance.ShakeCamera(0.12f, 0.15f);

            _player.BeginEmpoweredAttack();
            _player.SetDamageMultiplier(_parryCounterRate);
            bool done = false;
            _player.Attack.SuperAttackStart(() => done = true, 1.2f);
            yield return new WaitUntil(() => done);
            _player.SetDamageMultiplier(1f);
        }
        else
        {
            // 실패: 쿨타임 일부를 돌려받는다
            _player.RefundSuperCooldown(_parryFailRefund);
        }

        yield return new WaitForSeconds(0.15f);
        _player.EndAttack();
    }

    /// <summary>PlayerController.DamageTaken에서 호출: 받아치기 자세 중이면 성공 처리하고 true</summary>
    public bool TryParry()
    {
        if (!_isParrying || _player.IsGameOver) return false;
        _isParrying = false;
        _parrySucceeded = true;
        return true;
    }

    // 검 꽂기: 발밑에 검을 꽂아 일정 시간 주변 적에게 반복 피해
    IEnumerator SuperPlantRoutine()
    {
        _player.IsAttacking = true;
        _player.BeginEmpoweredAttack();
        _player.PlayAnim("isSuper");
        SoundManager.Instance.PlaySFX("SFX/Gen_SwordPlant");               // "쿵-징"
        VFXManager.Instance.Play("GroundHitVFX", transform.position);       // 땅이 갈라지는 먼지
        IngameManager.Instance.ShakeCamera(0.1f, 0.1f);

        var prefab = Resources.Load<GameObject>("VFX/PlantedSword");
        if (prefab != null)
        {
            // 쓴 순간의 "다음 공격 강화"를 검에 실어 보낸다 (검은 4초 동안 나중에 때리므로)
            var go = Instantiate(prefab, transform.position, Quaternion.identity);
            if (go.TryGetComponent(out PlayerSwordZone zone))
                zone.Begin(this, _plantRadius * _player.RangeMultiplier, _plantDuration, _plantTickInterval,
                           _plantDamageRate, _player.AttackEmpowerRate);
        }

        yield return new WaitForSeconds(0.25f);
        _player.EndAttack();
    }

    // ══ 필살기 ═════════════════════════════════════════════════

    /// <summary>필살기 피해가 들어간 순간 (연출 중, 시간이 멈춘 상태)</summary>
    public void OnSpecialDamageDealt()
    {
        switch (_specialStyle)
        {
            case SpecialStyle.MultiSlash:
                StartCoroutine(MultiSlashRoutine());
                break;

            case SpecialStyle.MoonBlessing:
                if (_moonBuffRoutine != null) StopCoroutine(_moonBuffRoutine);
                _moonBuffRoutine = StartCoroutine(MoonBuffRoutine());
                break;

            case SpecialStyle.Reflect:
                ReflectBullets();
                break;
        }
    }

    // 연속 일섬: 첫 번째 일섬 뒤에 나머지를 연달아 (시간이 멈춰 있어도 진행되게 Realtime)
    IEnumerator MultiSlashRoutine()
    {
        for (int i = 1; i < _multiSlashCount; i++)
        {
            yield return new WaitForSecondsRealtime(_multiSlashInterval);
            SoundManager.Instance.PlaySFXPitch("SFX/Player_SwordOneSlash", 1f + 0.08f * i); // 벨 때마다 조금씩 높게
            _player.DealSpecialHit(_multiSlashRate);
            IngameManager.Instance.ShakeCamera(0.06f, 0.1f);
        }
    }

    // 달의 가호: 일정 시간 공격력 증가 (다시 쓰면 시간만 새로)
    IEnumerator MoonBuffRoutine()
    {
        if (!_isMoonBuffActive)
        {
            _player.AddAtkMultiplier(_moonAtkBonus);
            _isMoonBuffActive = true;
        }
        FloatingTextManager.Instance.ShowItemEffect("달의 가호!", new Color(0.75f, 0.85f, 1f), transform.position);
        SoundManager.Instance.PlaySFX("SFX/Gen_Buff");
        if (_moonAura == null) _moonAura = SpawnAttached("MoonAura"); // 버프가 끝날 때까지 몸에 오라

        yield return new WaitForSeconds(_moonBuffTime);

        _moonBuffRoutine = null;
        EndMoonBuff();
    }

    // 달의 가호 버프를 즉시 끝낸다 (노드 이동처럼 코루틴이 한꺼번에 멈출 때 공격력이 남지 않게)
    void EndMoonBuff()
    {
        if (_moonBuffRoutine != null) StopCoroutine(_moonBuffRoutine);
        _moonBuffRoutine = null;
        ClearAttached(ref _moonAura);
        if (!_isMoonBuffActive) return;
        _player.RemoveAtkMultiplier(_moonAtkBonus);
        _isMoonBuffActive = false;
    }

    // 탄막 반사: 화면의 적 탄환을 검기로 바꿔 가장 가까운 적에게 날린다
    void ReflectBullets()
    {
        VFXManager.Instance.GetActiveBullets(_bulletBuffer);
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        if (_bulletBuffer.Count > 0) SoundManager.Instance.PlaySFX("SFX/Gen_Reflect");

        int sent = 0;
        foreach (var bullet in _bulletBuffer)
        {
            Vector2 from = bullet.transform.position;
            bullet.ReleaseSelf();
            if (sent < _reflectMax)
            {
                var poof = VFXManager.Instance.Play("BlinkStartVFX", from); // 탄환이 펑 하고 검기로 바뀐다
                if (poof != null) poof.transform.localScale = Vector3.one * 0.5f;
            }
            if (sent >= _reflectMax) continue;

            EnemyBase target = null;
            float best = float.MaxValue;
            foreach (var e in enemies)
            {
                if (e == null || e.IsDead || !e.gameObject.activeInHierarchy) continue;
                float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                if (d < best) { best = d; target = e; }
            }
            if (target == null) continue;

            Vector2 dir = (Vector2)target.transform.position - from;
            var wave = VFXManager.Instance.Play("SwordWaveVFX", from, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            if (wave != null && wave.TryGetComponent(out PlayerProjectile proj))
            {
                wave.transform.localScale = Vector3.one * 0.8f;
                proj.Launch(_player, dir, 14f, dir.magnitude + 1f, 0.5f, _reflectWaveRate, null);
                sent++;
            }
        }
        _bulletBuffer.Clear();
    }

    // ── 필살기: 투지 충전 ──

    /// <summary>적을 맞힐 때마다 호출 (투지 충전 게이지)</summary>
    public void OnEnemyHit()
    {
        if (!UsesSpiritGauge || _spiritGauge >= _spiritRequiredHits) return;
        _spiritGauge++;
        _player.RefreshSkillUI(SKILL.SPECIAL);

        // 가득 찬 순간 "딩-딩!" + 반짝
        if (_spiritGauge >= _spiritRequiredHits)
        {
            SoundManager.Instance.PlaySFX("SFX/Gen_GaugeReady");
            VFXManager.Instance.PlayAttached("GaugeReadyVFX", transform);
        }
    }

    /// <summary>게이지가 가득이면 비우고 true (필살기 사용)</summary>
    public bool TryUseSpiritGauge()
    {
        if (_spiritGauge < _spiritRequiredHits) return false;
        _spiritGauge = 0;
        _player.RefreshSkillUI(SKILL.SPECIAL);
        return true;
    }
}
