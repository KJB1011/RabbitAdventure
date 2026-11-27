using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Defines;

/// <summary>
/// 토끼 총잡이의 스킬.
///   기본기 : 커서 방향으로 일직선으로 쏜다. 처음 맞은 적 1명만 (사거리 무제한, 100%). 탄창 6발, 다 쓰면 2초 재장전
///   회피기 : 공통 구르기 + 끝나면 즉시 재장전
///   특수기 : 남은 총알을 0.15초 간격으로 빠르게 전부 쏜다 (기본기와 같은 방식, 쌍권총 중에는 2배 빠르게)
///   필살기 : 쌍권총 — 즉시 재장전 + 10초 동안 탄창 2배, 기본기 쿨타임 절반
/// 남은 총알은 스킬칸 옆 탄환 줄(UIAmmoBar)로 보여준다.
/// 공격범위 증가(아티팩트)는 총잡이에게 연사속도 증가로 적용된다 (FireRateScale).
///
/// 기본기 숙련도 강화 (SkillMasteryManager에서 골라서 ApplyUpgrade로 켠다)
///   확장 탄창 : 6발 → 10발, 재장전 2초 → 2.5초
///   탄 회수   : 치명타 확률 +10%, 치명타가 터지면 총알 1발이 돌아온다
///   정밀 사격 : 쏜 순간 커서가 맞은 적 피격 원의 가운데에 있으면 +30% 추가 피해 (노란 타격 이펙트 + "칭!")
///   차지샷    : 누르고 있으면 모으고, 다 모은 뒤 놓으면 총알 3발을 쓰는 관통 500% 저격탄
/// 기본기 강화는 기본기를 연달아 쏘는 특수기(속사)에도 그대로 적용된다 (차지샷 제외).
///
/// 회피기 숙련도 강화
///   특수탄 장전 : 구르기로 장전하면 맨 위 3발이 소이탄 (맞으면 2초 화상, 초당 20%). 탄환 줄에 주황색
///   백스텝 산탄 : 커서 반대쪽으로 최대 거리만큼 구르며, 출발할 때 커서 쪽으로 산탄 5발 (각 40%, 총알 안 씀)
///   탄피 폭탄   : 구르기 출발 자리에 탄피를 떨어뜨리고 1초 뒤 폭발 (150%)
///   퀵 드로우   : 구르기가 끝나고 3초 안에 쏘는 첫 발은 확정 치명타 + 관통
/// </summary>
public class RabbitGunnerSkills : CharacterSkills
{
    [Header("기본기 (권총)")]
    [SerializeField] int _magazineSize = 6;
    [SerializeField] float _reloadTime = 2f;
    [SerializeField] float _damageRate = 1f;           // 100%
    [SerializeField] float _maxRange = 40f;            // 사실상 무제한 (화면 끝까지)

    [Header("특수기 (속사)")]
    [SerializeField] float _rapidInterval = 0.15f;
    [SerializeField] float _dualRapidScale = 0.5f;     // 쌍권총 중에는 2배 빠르게

    [Header("필살기 (쌍권총)")]
    [SerializeField] float _dualDuration = 10f;
    [SerializeField] int _dualMagazineScale = 2;       // 탄창 2배
    [SerializeField] float _dualCooldownScale = 0.5f;  // 기본기 쿨타임 0.5배

    [Header("기본기 강화: 확장 탄창")]
    [SerializeField] int _extendedMagazineSize = 10;
    [SerializeField] float _extendedReloadTime = 2.5f;

    [Header("기본기 강화: 탄 회수")]
    [SerializeField] float _refundCritBonus = 10f;     // 치명타 확률 +10% (CritRate는 % 단위)

    [Header("기본기 강화: 정밀 사격")]
    [SerializeField] float _precisionBonus = 0.3f;     // +30%
    [Tooltip("커서가 피격 원 반지름의 몇 배 안쪽에 있으면 '가운데'로 칠지 (0.4 = 가운데 40%)")]
    [SerializeField] float _precisionRadiusRate = 0.4f;

    [Header("기본기 강화: 차지샷")]
    [SerializeField] float _chargeStartDelay = 0.2f;   // 이보다 짧게 누르면 그냥 한 발 (탭 사격)
    [SerializeField] float _chargeTime = 1f;           // 모으기 시작한 뒤 다 모이기까지
    [SerializeField] float _chargeMoveRate = 0.6f;     // 모으는 동안 이동속도
    [SerializeField] float _chargeDamageRate = 5f;     // 500%
    [SerializeField] int _chargeBulletCost = 3;

    [Header("회피기 강화: 특수탄 장전 (소이탄)")]
    [SerializeField] int _incendiaryCount = 3;         // 구르기로 장전하면 맨 위 3발
    [SerializeField] float _burnDuration = 2f;
    [SerializeField] float _burnTickInterval = 0.5f;
    [SerializeField] float _burnRatePerSecond = 0.2f;  // 초당 20%

    [Header("회피기 강화: 백스텝 산탄")]
    [SerializeField] int _pelletCount = 5;
    [SerializeField] float _pelletSpread = 40f;        // 부채꼴 전체 각도
    [SerializeField] float _pelletRange = 7f;
    [SerializeField] float _pelletDamageRate = 0.4f;   // 한 발당 40%

    [Header("회피기 강화: 탄피 폭탄")]
    [SerializeField] float _bombDelay = 1f;
    [SerializeField] float _bombRadius = 1.6f;
    [SerializeField] float _bombDamageRate = 1.5f;     // 150%

    [Header("회피기 강화: 퀵 드로우")]
    [SerializeField] float _quickDrawWindow = 3f;      // 구르기가 끝나고 이 시간 안의 첫 발

    [Header("연결")]
    [SerializeField] SpriteRenderer _gun;              // 손에 든 총 (커서 쪽을 향함, AttackArea의 자식)
    [SerializeField] SpriteRenderer _offhandGun;       // 쌍권총일 때만 보이는 두 번째 총
    [SerializeField] TextMeshPro _ammoText;            // 플레이어 아래 탄창 표시
    [SerializeField] float _muzzleOffset = 0.35f;      // 총 위치에서 총구까지 거리
    [SerializeField] ParticleSystem _dualAura;         // 쌍권총 지속 중 몸에서 나오는 파티클

    static readonly Color ChargedTracerColor = new Color(1f, 0.78f, 0.3f, 1f);
    static readonly Color DefaultTracerColor = new Color(1f, 0.95f, 0.7f, 1f); // TracerVFX 기본 색과 같게
    static readonly Color IncendiaryTracerColor = new Color(1f, 0.45f, 0.2f, 1f);
    static readonly Color QuickDrawTracerColor = new Color(0.85f, 0.95f, 1f, 1f);

    int _bullets;
    bool _isReloading;
    bool _isDual;
    Coroutine _reloadRoutine;
    Coroutine _dualRoutine;

    // 고른 기본기 강화
    bool _hasCritRefund;
    bool _hasPrecision;
    bool _hasChargeShot;

    // 고른 회피기 강화
    bool _hasIncendiary;
    bool _hasBackstep;
    bool _hasCasingBomb;
    bool _hasQuickDraw;

    int _incendiaryRounds;    // 남은 탄 중 맨 위 몇 발이 소이탄인지
    float _quickDrawUntil;    // 이 시각까지 쏘는 첫 발이 퀵 드로우
    readonly Dictionary<EnemyBase, Coroutine> _burns = new();

    int MagazineSize => _magazineSize * (_isDual ? _dualMagazineScale : 1);

    UIAmmoBar _ammoBar;

    protected override void Awake()
    {
        base.Awake();
        _bullets = _magazineSize;
        if (_gun != null) _gunRestPos = _gun.transform.localPosition;
        if (_offhandGun != null)
        {
            _offhandRestPos = _offhandGun.transform.localPosition;
            _offhandGun.enabled = false;
        }

        // 예전 "6 / 6" 글자 대신 스킬칸 옆 탄환 줄로 보여준다
        if (_ammoText != null) _ammoText.gameObject.SetActive(false);
        var skillColumn = GetComponentInChildren<UIPlayerSkill>(true);
        if (skillColumn != null) _ammoBar = UIAmmoBar.Create(skillColumn.transform, transform);
        UpdateAmmoBar();
    }

    // ── 숙련도 강화 ──────────────────────────────────────────
    public override bool ApplyUpgrade(SkillUpgradeType type)
    {
        switch (type)
        {
            case SkillUpgradeType.GunExtendedMag:
                _magazineSize = _extendedMagazineSize;
                _reloadTime = _extendedReloadTime;
                FinishReload(); // 고르자마자 늘어난 탄창으로 가득
                return true;
            case SkillUpgradeType.GunCritRefund:
                _hasCritRefund = true;
                Player.AddCritRate(_refundCritBonus);
                return true;
            case SkillUpgradeType.GunPrecision:
                _hasPrecision = true;
                return true;
            case SkillUpgradeType.GunChargeShot:
                _hasChargeShot = true;
                return true;

            case SkillUpgradeType.GunIncendiary: _hasIncendiary = true; return true;
            case SkillUpgradeType.GunBackstep: _hasBackstep = true; return true;
            case SkillUpgradeType.GunCasingBomb: _hasCasingBomb = true; return true;
            case SkillUpgradeType.GunQuickDraw: _hasQuickDraw = true; return true;
        }
        return false;
    }

    // ── 기본기 ───────────────────────────────────────────────
    public override bool CanUseBasic
    {
        get
        {
            if (_bullets > 0 && !_isReloading) return true;
            if (!_isReloading) StartReload(); // 비었는데 재장전을 안 하고 있으면 시작
            return false;
        }
    }

    // 총잡이에게 공격범위는 의미가 없으므로, 공격범위 증가(아티팩트)는 연사속도 증가로 바꿔 적용한다
    // (공격범위 +40% → 기본기 쿨타임·특수기 발사 간격 ÷1.4)
    float FireRateScale => 1f / Mathf.Max(0.1f, Player.RangeMultiplier);

    public override float BasicCooldownScale => (_isDual ? _dualCooldownScale : 1f) * FireRateScale;

    public override void StartBasic(Action onDone)
    {
        if (_hasChargeShot)
        {
            _chargeRoutine = StartCoroutine(ChargeRoutine(onDone)); // 놓는 순간 쏜다
            return;
        }
        Fire(false);
        onDone?.Invoke(); // 총은 쏘는 순간 끝 (회피기를 바로 쓸 수 있게)
    }

    // 한 발의 성질 (일반 탄·차지샷·소이탄·퀵 드로우·산탄이 같은 판정 코드를 쓴다)
    struct Shot
    {
        public float rate;         // 공격력의 몇 배
        public bool pierce;        // 길 위의 모든 적을 뚫는지 (아니면 처음 맞은 1명만)
        public bool forceCrit;     // 확정 치명타
        public bool incendiary;    // 맞으면 화상
        public AttackType type;    // 적중 알림 종류 (아티팩트용)
    }

    // 몸 → 커서 방향 (총구가 커서보다 앞에 있어도 방향이 뒤집히지 않게)
    Vector2 AimDirection()
    {
        Vector2 aim = Utils.GetMousePos() - (Vector2)transform.position;
        if (aim.sqrMagnitude < 0.0001f) aim = Player.Attack.transform.right;
        return aim.normalized;
    }

    /// <summary>
    /// 커서 방향으로 일직선으로 한 발.
    /// 일반 탄: 처음 맞은 적(또는 상자) 1명에게만 / 차지샷·퀵 드로우: 길 위의 모든 적을 관통
    /// </summary>
    void Fire(bool charged)
    {
        if (_bullets <= 0) return;
        int used = charged ? Mathf.Min(_chargeBulletCost, _bullets) : 1;
        _bullets -= used;

        // 맨 위 탄이 소이탄이면 이번 발은 소이탄 (위에서부터 쓰므로 쓴 만큼 줄어든다)
        bool incendiary = _incendiaryRounds > 0;
        _incendiaryRounds = Mathf.Max(0, _incendiaryRounds - used);

        // 퀵 드로우: 구르기 후 3초 안의 첫 발
        bool quick = _hasQuickDraw && Time.time <= _quickDrawUntil;
        if (quick)
        {
            _quickDrawUntil = 0f;
            ReturnFx(ref _quickDrawFx); // 준비 반짝임 끄기
        }
        UpdateAmmoBar();

        var gun = NextGun();
        Vector2 muzzle = MuzzleOf(gun);
        Vector2 aim = AimDirection();

        var shot = new Shot
        {
            rate = _damageRate * (charged ? _chargeDamageRate : 1f),
            pierce = charged || quick,
            forceCrit = quick,
            incendiary = incendiary,
            type = AttackType.Basic,
        };
        Vector2 end = CastShot(muzzle, aim, _maxRange, shot);

        // 총알 궤적 + 총구 불꽃 + 소리 + 반동
        if (charged) ShowTracer(muzzle, end, 2.5f, ChargedTracerColor, 2.5f); // 굵고 오래 남는 금빛
        else if (quick) ShowTracer(muzzle, end, 1.8f, QuickDrawTracerColor, 1.3f); // 하얗고 굵게
        else if (incendiary) ShowTracer(muzzle, end, 1.3f, IncendiaryTracerColor, 1f); // 주황
        else ShowTracer(muzzle, end, 1f, null, 1f);

        if (charged)
        {
            SoundManager.Instance.PlaySFXPitch("SFX/Gen_Gunshot", 0.7f);
            SoundManager.Instance.PlaySFX("SFX/HeavyHit", 0.5f);
        }
        else
        {
            SoundManager.Instance.PlaySFXVaried("SFX/Gen_Gunshot", 0.06f);
            if (quick) SoundManager.Instance.PlaySFXPitch("SFX/Gen_TipPing", 1.4f, 0.7f); // "팅!"
        }
        if (gun != null) StartCoroutine(RecoilRoutine(gun.transform, gun == _offhandGun ? _offhandRestPos : _gunRestPos, charged ? 0.18f : 0.08f));

        if (_bullets <= 0) StartReload();
    }

    /// <summary>한 방향으로 판정만 한다 (궤적·소리는 부르는 쪽에서). 총알이 멈춘 지점을 돌려준다</summary>
    Vector2 CastShot(Vector2 muzzle, Vector2 aim, float range, Shot shot)
    {
        // 가는 길에 닿는 대상 (RaycastAll은 가까운 순서로 돌려준다)
        Vector2 end = muzzle + aim * range; // 아무것도 안 맞으면 사거리 끝까지
        var hitEnemies = new HashSet<EnemyBase>();
        var hitChests = new HashSet<ChestObject>();
        foreach (var hit in Physics2D.RaycastAll(muzzle, aim, range))
        {
            // 적의 몸(EnemyHitbox)에 닿았을 때만 — 적의 공격 콜라이더나 총알은 무시
            if (hit.collider.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !hitEnemies.Add(enemy)) continue;
                HitEnemy(enemy, hit, muzzle, aim, shot);
                if (!shot.pierce) { end = hit.point; break; }
                continue;
            }
            var chest = hit.collider.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeInHierarchy && hitChests.Add(chest))
            {
                chest.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn() * shot.rate)));
                if (!shot.pierce) { end = hit.point; break; }
            }
        }
        return end;
    }

    void HitEnemy(EnemyBase enemy, RaycastHit2D hit, Vector2 muzzle, Vector2 aim, Shot shot)
    {
        float rate = shot.rate;
        bool precise = _hasPrecision && IsPreciseHit(hit.collider, Utils.GetMousePos());
        if (precise) rate *= 1f + _precisionBonus;

        int damage = Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn(shot.forceCrit, out bool isCrit) * rate));
        enemy.DamageTaken(damage);
        IngameManager.Instance.Flash(enemy.CharSprite, 0.05f);
        DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);
        VFXManager.Instance.Play("HitSpark", hit.point, Quaternion.LookRotation(aim, Vector3.back));

        // 정밀 사격: 검사의 "도약의 일격"처럼 노란 타격 이펙트 + "칭!"
        if (precise)
        {
            VFXManager.Instance.Play("EmpowerHitVFX", enemy.transform.position);
            SoundManager.Instance.PlaySFXPitch("SFX/Gen_TipPing", 1.15f, 0.6f);
        }

        // 탄 회수: 치명타가 터지면 1발 돌려받기
        if (isCrit && _hasCritRefund) RefundBullet();

        // 소이탄: 화상
        if (shot.incendiary) StartBurn(enemy);

        Player.NotifyAttackHit(enemy, shot.type);
    }

    // ── 회피기 강화: 특수탄 장전 (소이탄 화상) ───────────────
    void StartBurn(EnemyBase enemy)
    {
        // 이미 타고 있으면 처음부터 다시 (겹치지 않고 시간만 새로)
        if (_burns.TryGetValue(enemy, out var running) && running != null) StopCoroutine(running);
        _burns[enemy] = StartCoroutine(BurnRoutine(enemy));
    }

    IEnumerator BurnRoutine(EnemyBase enemy)
    {
        int ticks = Mathf.Max(1, Mathf.RoundToInt(_burnDuration / _burnTickInterval));
        float tickRate = _burnRatePerSecond * _burnTickInterval; // 한 번에 10%
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(_burnTickInterval);
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy) break;

            int damage = Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn() * tickRate));
            enemy.DamageTaken(damage);
            DamageTextManager.Instance.Show(damage, false, enemy.transform.position);

            // 작은 불꽃이 위로 화르륵 (총구 섬광 이펙트를 위쪽으로 재활용)
            Vector2 p = (Vector2)enemy.transform.position + UnityEngine.Random.insideUnitCircle * 0.3f;
            var flame = VFXManager.Instance.Play("MuzzleFlashVFX", p);
            if (flame != null && flame.TryGetComponent(out MuzzleFlashVFX fx))
            {
                fx.Show(p, Vector2.up);
                fx.transform.localScale = Vector3.one * 1.3f;
            }
        }
        if (enemy != null) _burns.Remove(enemy);
    }

    // ── 회피기 강화: 백스텝 산탄 · 탄피 폭탄 · 퀵 드로우 ─────
    public override bool DashAwayFromCursor => _hasBackstep;

    public override void OnDashStarted(Vector2 startPos)
    {
        if (_hasBackstep) FireBuckshot();
        if (_hasCasingBomb) StartCoroutine(CasingBombRoutine(startPos));
    }

    // 커서 쪽으로 부채꼴 산탄 (총알은 쓰지 않는다)
    void FireBuckshot()
    {
        Vector2 muzzle = MuzzleOf(_gun);
        Vector2 aim = AimDirection();
        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        var shot = new Shot { rate = _pelletDamageRate, type = AttackType.Dash };

        for (int i = 0; i < _pelletCount; i++)
        {
            float t = _pelletCount > 1 ? (float)i / (_pelletCount - 1) : 0.5f;
            float angle = (baseAngle + Mathf.Lerp(-_pelletSpread * 0.5f, _pelletSpread * 0.5f, t)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 end = CastShot(muzzle, dir, _pelletRange, shot);
            ShowTracer(muzzle, end, 0.8f, null, 1f, i == _pelletCount / 2); // 섬광은 가운데 한 번만
        }
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Gunshot", 0.6f);
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Gunshot", 0.8f, 0.7f);
        if (_gun != null) StartCoroutine(RecoilRoutine(_gun.transform, _gunRestPos, 0.2f));
    }

    // 출발 자리에 탄피 꾸러미를 떨어뜨리고, 1초 뒤 터진다
    IEnumerator CasingBombRoutine(Vector2 pos)
    {
        var go = new GameObject("CasingBomb");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("UI/AmmoBullet");
        if (_gun != null)
        {
            sr.sharedMaterial = _gun.sharedMaterial;
            sr.sortingLayerID = _gun.sortingLayerID;
        }
        sr.sortingOrder = 2;
        sr.color = new Color(0.95f, 0.75f, 0.35f);
        go.transform.localScale = Vector3.one * 1.6f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_TipPing", 0.8f, 0.5f); // 탄피 떨어지는 "팅"

        // 깜빡이며 점점 빨라진다
        float t = 0f;
        while (t < _bombDelay)
        {
            t += Time.deltaTime;
            float blink = Mathf.Lerp(6f, 22f, t / _bombDelay);
            sr.color = Mathf.Sin(t * blink) > 0f ? new Color(1f, 0.4f, 0.2f) : new Color(0.95f, 0.75f, 0.35f);
            yield return null;
        }
        Destroy(go);

        // 펑!
        VFXManager.Instance.Play("GroundHitVFX", pos);
        SoundManager.Instance.PlaySFX("SFX/AreaDamage", 0.7f);
        var hitEnemies = new HashSet<EnemyBase>();
        foreach (var col in Physics2D.OverlapCircleAll(pos, _bombRadius))
        {
            if (col.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = col.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !hitEnemies.Add(enemy)) continue;
                int damage = Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn(out bool isCrit) * _bombDamageRate));
                enemy.DamageTaken(damage);
                IngameManager.Instance.Flash(enemy.CharSprite, 0.08f);
                DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);
                Player.NotifyAttackHit(enemy, AttackType.Dash);
                continue;
            }
            var chest = col.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeInHierarchy)
                chest.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn() * _bombDamageRate)));
        }
    }

    // 쏜 순간 커서가 맞은 적 피격 원의 가운데 쪽에 있었는지 (적을 정확히 조준했는지)
    bool IsPreciseHit(Collider2D col, Vector2 cursor)
    {
        if (col is not CircleCollider2D circle) return false;
        Vector2 center = circle.transform.TransformPoint(circle.offset);
        Vector3 s = circle.transform.lossyScale;
        float radius = circle.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        return Vector2.Distance(cursor, center) <= radius * _precisionRadiusRate;
    }

    void RefundBullet()
    {
        if (_bullets >= MagazineSize) return;
        _bullets++;
        UpdateAmmoBar();
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 1.8f, 0.45f); // 짧고 높은 "찰칵"
    }

    // 총의 원래 자리 (반동 후 돌아올 위치, Awake에서 저장)
    Vector3 _gunRestPos;
    Vector3 _offhandRestPos;
    bool _offhandTurn; // 쌍권총일 때 이번 발은 두 번째 총 차례인지

    // 이번에 쏠 총: 평소엔 손에 든 총, 쌍권총일 때는 두 총이 번갈아 쏜다
    SpriteRenderer NextGun()
    {
        if (!_isDual || _offhandGun == null) return _gun;
        _offhandTurn = !_offhandTurn;
        return _offhandTurn ? _offhandGun : _gun;
    }

    // 뒤로 살짝 밀렸다가 돌아오는 반동
    IEnumerator RecoilRoutine(Transform gun, Vector3 rest, float kick)
    {
        gun.localPosition = rest - Vector3.right * kick;
        yield return new WaitForSeconds(0.06f);
        if (gun != null) gun.localPosition = rest;
    }

    Vector2 MuzzleOf(SpriteRenderer gun) => gun != null
        ? (Vector2)gun.transform.position + (Vector2)Player.Attack.transform.right * _muzzleOffset
        : (Vector2)transform.position;

    /// <param name="thickness">궤적 굵기 배율 (2 이상이면 총구 섬광도 크게)</param>
    /// <param name="color">궤적 색 (null이면 기본 색)</param>
    /// <param name="fade">궤적이 남는 시간 배율</param>
    /// <param name="withFlash">총구 섬광을 낼지 (산탄은 가운데 한 발만)</param>
    void ShowTracer(Vector2 from, Vector2 to, float thickness, Color? color, float fade, bool withFlash = true)
    {
        var vfx = VFXManager.Instance.Play("BulletTracerVFX", from);
        if (vfx != null && vfx.TryGetComponent(out TracerVFX tracer))
        {
            if (color.HasValue || !Mathf.Approximately(thickness, 1f) || !Mathf.Approximately(fade, 1f))
                tracer.Show(from, to, thickness, color ?? DefaultTracerColor, fade);
            else
                tracer.Show(from, to);
        }
        if (!withFlash) return;

        // 총구 섬광 (짧게 번쩍하고 사라지는 총 전용 이펙트)
        var flash = VFXManager.Instance.Play("MuzzleFlashVFX", from);
        if (flash != null && flash.TryGetComponent(out MuzzleFlashVFX muzzleFlash))
        {
            muzzleFlash.Show(from, to - from);
            muzzleFlash.transform.localScale = Vector3.one * (thickness >= 2f ? 1.8f : 1f);
        }
    }

    // ── 기본기 강화: 차지샷 ──────────────────────────────────
    Coroutine _chargeRoutine;
    PooledVFX _chargeGlow;     // 모으는 동안 총에서 빛남
    PooledVFX _chargeReadyFx;  // 다 모였을 때 총구에서 반짝임 (반복 재생 이펙트 → 쏘는 순간 직접 지운다)

    // 총구에 붙어서 따라다니는 반짝임 (다 모임·퀵 드로우 준비)
    PooledVFX PlayMuzzleShine() => _gun != null
        ? VFXManager.Instance.PlayAttached("GaugeReadyVFX", _gun.transform, Vector3.right * _muzzleOffset)
        : null;

    // 반복 재생 이펙트는 풀로 돌아가기 전까지(3초) 계속 보이므로 끝낼 때 바로 돌려보낸다
    static void ReturnFx(ref PooledVFX fx)
    {
        if (fx != null && fx.gameObject.activeSelf) fx.ReturnNow();
        fx = null;
    }

    // 누르고 있는 동안 모으고, 놓는 순간 쏜다.
    // 짧게 누르면(탭) 그냥 한 발, 다 모은 뒤 놓으면 관통 저격탄. 모으는 중 구르기를 누르면 취소.
    IEnumerator ChargeRoutine(Action onDone)
    {
        float held = 0f;
        bool charging = false, full = false;

        while (Player.IsAttackKeyHeld && !Player.IsGameOver)
        {
            if (Player.IsDashKeyHeld) // 구르기로 취소 (총알은 쓰지 않는다)
            {
                StopChargeEffects();
                _chargeRoutine = null;
                onDone?.Invoke();
                yield break;
            }

            held += Time.deltaTime;
            if (!charging && held >= _chargeStartDelay)
            {
                charging = true;
                Player.SetMoveSpeedRate(_chargeMoveRate);
                if (_gun != null) _chargeGlow = VFXManager.Instance.PlayAttached("ChargeGlow", _gun.transform);
                SoundManager.Instance.PlaySFXPitch("SFX/Gen_ChargeStep", 0.9f, 0.6f);
            }
            if (charging && !full && held >= _chargeStartDelay + _chargeTime)
            {
                full = true;
                _chargeReadyFx = PlayMuzzleShine();
                SoundManager.Instance.PlaySFXPitch("SFX/Gen_ChargeStep", 1.4f);
                StartCoroutine(FullChargeFlash());
            }
            yield return null;
        }

        StopChargeEffects();
        _chargeRoutine = null;
        if (!Player.IsGameOver)
            Fire(full && _bullets >= _chargeBulletCost); // 덜 모였거나 총알이 모자라면 일반 탄
        onDone?.Invoke();
    }

    // 다 모였을 때 몸이 한 번 하얗게 번쩍
    IEnumerator FullChargeFlash()
    {
        Player.SetFlash(0.6f);
        yield return new WaitForSeconds(0.08f);
        Player.SetFlash(0f);
    }

    void StopChargeEffects()
    {
        Player.SetMoveSpeedRate(1f);
        Player.SetFlash(0f);
        ReturnFx(ref _chargeGlow);
        ReturnFx(ref _chargeReadyFx);
    }

    void CancelCharge()
    {
        if (_chargeRoutine == null) return;
        StopCoroutine(_chargeRoutine);
        _chargeRoutine = null;
        StopChargeEffects();
    }

    // ── 재장전 ───────────────────────────────────────────────
    void StartReload()
    {
        if (_isReloading) return;
        _reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 1f, 0.8f);
        float t = 0f;
        while (t < _reloadTime)
        {
            t += Time.deltaTime;
            if (_ammoBar != null) _ammoBar.Set(_bullets, MagazineSize, t / _reloadTime); // 아래에서부터 차오름
            yield return null;
        }
        FinishReload();
    }

    // 즉시 가득 채움 (회피기, 쌍권총 시작, 노드 재시작)
    void FinishReload()
    {
        if (_reloadRoutine != null) StopCoroutine(_reloadRoutine);
        _reloadRoutine = null;
        _isReloading = false;
        _bullets = MagazineSize;
        _incendiaryRounds = 0; // 새 탄창 (소이탄은 구르기로 장전할 때만 다시 들어간다)
        UpdateAmmoBar();
    }

    public override void OnDashFinished()
    {
        bool wasNotFull = _bullets < MagazineSize || _isReloading;
        FinishReload();
        if (wasNotFull) SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 1.25f, 0.8f); // 구르며 철컥

        // 특수탄 장전: 맨 위 3발이 소이탄
        if (_hasIncendiary)
        {
            _incendiaryRounds = Mathf.Min(_incendiaryCount, _bullets);
            UpdateAmmoBar();
        }

        // 퀵 드로우: 3초 안의 첫 발 준비 (총구에서 반짝)
        if (_hasQuickDraw)
        {
            _quickDrawUntil = Time.time + _quickDrawWindow;
            // 준비된 동안 총구가 반짝이며 따라다님 → 쏘거나 시간이 지나면 꺼진다
            ReturnFx(ref _quickDrawFx);
            _quickDrawFx = PlayMuzzleShine();
            if (_quickDrawExpire != null) StopCoroutine(_quickDrawExpire);
            _quickDrawExpire = StartCoroutine(QuickDrawExpireRoutine());
        }
    }

    PooledVFX _quickDrawFx;
    Coroutine _quickDrawExpire;

    // 퀵 드로우 시간이 지나도록 쏘지 않았으면 반짝임 끄기
    IEnumerator QuickDrawExpireRoutine()
    {
        yield return new WaitForSeconds(_quickDrawWindow);
        _quickDrawExpire = null;
        if (Time.time >= _quickDrawUntil - 0.05f) ReturnFx(ref _quickDrawFx);
    }

    // 스킬칸 옆 탄환 줄 갱신 (재장전 중 진행도는 ReloadRoutine이 매 프레임 갱신)
    void UpdateAmmoBar()
    {
        if (_ammoBar != null) _ammoBar.Set(_bullets, MagazineSize, _isReloading ? 0f : -1f, _incendiaryRounds);
    }

    // ── 특수기: 속사 ─────────────────────────────────────────
    public override bool CanUseSuper => _bullets > 0 && !_isReloading;

    public override IEnumerator SuperRoutine()
    {
        Player.IsAttacking = true;
        Player.BeginEmpoweredAttack();
        while (_bullets > 0 && !Player.IsGameOver)
        {
            Fire(false);
            yield return new WaitForSeconds(_rapidInterval * (_isDual ? _dualRapidScale : 1f) * FireRateScale);
        }
        Player.EndAttack();
    }

    // ── 필살기: 쌍권총 ───────────────────────────────────────
    public override IEnumerator SpecialRoutine()
    {
        if (_dualRoutine != null) StopCoroutine(_dualRoutine);
        // 버프는 이 컴포넌트에서 따로 돌린다 (노드 이동으로 PlayerController의 코루틴이 멈춰도 10초가 지나면 끝나게)
        _dualRoutine = StartCoroutine(DualRoutine());
        yield break;
    }

    IEnumerator DualRoutine()
    {
        _isDual = true;
        FinishReload();                                  // 재장전 중이었어도 즉시 12발로 가득
        if (_offhandGun != null) _offhandGun.enabled = true;
        if (_dualAura != null) _dualAura.Play();         // 지속되는 동안 몸에서 금빛 불씨가 피어오름
        SoundManager.Instance.PlaySFX("SFX/Gen_Buff");
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 0.9f);
        FloatingTextManager.Instance.ShowItemEffect("쌍권총!", new Color(1f, 0.85f, 0.35f), transform.position);

        yield return new WaitForSeconds(_dualDuration);
        EndDual(true);
    }

    /// <param name="natural">시간이 다 돼서 끝났는지 (노드 재시작 등으로 정리할 때는 소리를 내지 않는다)</param>
    void EndDual(bool natural)
    {
        bool wasDual = _isDual;
        _isDual = false;
        _dualRoutine = null;
        _offhandTurn = false;
        if (_offhandGun != null)
        {
            _offhandGun.enabled = false;
            _offhandGun.transform.localPosition = _offhandRestPos;
        }
        if (_dualAura != null) _dualAura.Stop(true, ParticleSystemStopBehavior.StopEmitting); // 남은 불씨는 자연스럽게 사라짐
        _bullets = Mathf.Min(_bullets, MagazineSize); // 12발 → 6발로
        UpdateAmmoBar();

        if (natural && wasDual)
            SoundManager.Instance.PlaySFX("SFX/Gen_BuffEnd"); // 힘이 빠지는 소리
    }

    // ── 상태 정리 ────────────────────────────────────────────
    public override void OnNodeMove() => CancelCharge();

    public override void OnResetState()
    {
        CancelCharge();
        if (_dualRoutine != null) StopCoroutine(_dualRoutine);
        EndDual(false);
        FinishReload();
    }

    // 총이 커서 쪽을 향할 때 왼쪽을 보면 위아래가 뒤집히지 않게
    void LateUpdate()
    {
        if (Player == null || Player.Attack == null) return;
        bool left = Player.Attack.transform.right.x < 0f;
        if (_gun != null) _gun.flipY = left;
        if (_offhandGun != null) _offhandGun.flipY = left;
    }
}
