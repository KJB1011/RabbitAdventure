using System;
using System.Collections;
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

    [Header("연결")]
    [SerializeField] SpriteRenderer _gun;              // 손에 든 총 (커서 쪽을 향함, AttackArea의 자식)
    [SerializeField] SpriteRenderer _offhandGun;       // 쌍권총일 때만 보이는 두 번째 총
    [SerializeField] TextMeshPro _ammoText;            // 플레이어 아래 탄창 표시
    [SerializeField] float _muzzleOffset = 0.35f;      // 총 위치에서 총구까지 거리
    [SerializeField] ParticleSystem _dualAura;         // 쌍권총 지속 중 몸에서 나오는 파티클

    int _bullets;
    bool _isReloading;
    bool _isDual;
    Coroutine _reloadRoutine;
    Coroutine _dualRoutine;

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

    public override float BasicCooldownScale => _isDual ? _dualCooldownScale : 1f;

    public override void StartBasic(Action onDone)
    {
        FireOnce();
        onDone?.Invoke(); // 총은 쏘는 순간 끝 (회피기를 바로 쓸 수 있게)
    }

    // 커서 방향으로 일직선으로 날아가서, 처음 맞은 적(또는 상자) 1명에게만 명중
    void FireOnce()
    {
        if (_bullets <= 0) return;
        _bullets--;
        UpdateAmmoBar();

        Vector2 cursor = Utils.GetMousePos();
        var gun = NextGun();
        Vector2 muzzle = MuzzleOf(gun);
        Vector2 aim = cursor - (Vector2)transform.position; // 몸 → 커서 방향 (총구가 커서보다 앞에 있어도 방향이 뒤집히지 않게)
        if (aim.sqrMagnitude < 0.0001f) aim = Player.Attack.transform.right;
        aim.Normalize();

        // 가는 길에 처음 닿는 대상 찾기 (RaycastAll은 가까운 순서로 돌려준다)
        EnemyBase target = null;
        ChestObject chestTarget = null;
        Vector2 hitPos = muzzle + aim * _maxRange; // 아무것도 안 맞으면 화면 밖까지
        foreach (var hit in Physics2D.RaycastAll(muzzle, aim, _maxRange))
        {
            // 적의 몸(EnemyHitbox)에 닿았을 때만 — 적의 공격 콜라이더나 총알은 무시
            if (hit.collider.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                target = enemy;
                hitPos = hit.point;
                break;
            }
            var chest = hit.collider.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeInHierarchy)
            {
                chestTarget = chest;
                hitPos = hit.point;
                break;
            }
        }

        // 총알 궤적 + 총구 불꽃 + 소리
        ShowTracer(muzzle, hitPos);
        SoundManager.Instance.PlaySFXVaried("SFX/Gen_Gunshot", 0.06f);
        if (gun != null) StartCoroutine(RecoilRoutine(gun.transform, gun == _offhandGun ? _offhandRestPos : _gunRestPos));

        if (target != null)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn(out bool isCrit) * _damageRate));
            target.DamageTaken(damage);
            IngameManager.Instance.Flash(target.CharSprite, 0.05f);
            DamageTextManager.Instance.Show(damage, isCrit, target.transform.position);
            VFXManager.Instance.Play("HitSpark", hitPos, Quaternion.LookRotation(aim, Vector3.back));
            Player.NotifyAttackHit(target, AttackType.Basic);
        }
        else if (chestTarget != null)
        {
            chestTarget.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn() * _damageRate)));
        }

        if (_bullets <= 0) StartReload();
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
    IEnumerator RecoilRoutine(Transform gun, Vector3 rest)
    {
        gun.localPosition = rest - Vector3.right * 0.08f;
        yield return new WaitForSeconds(0.06f);
        if (gun != null) gun.localPosition = rest;
    }

    Vector2 MuzzleOf(SpriteRenderer gun) => gun != null
        ? (Vector2)gun.transform.position + (Vector2)Player.Attack.transform.right * _muzzleOffset
        : (Vector2)transform.position;

    void ShowTracer(Vector2 from, Vector2 to)
    {
        var vfx = VFXManager.Instance.Play("BulletTracerVFX", from);
        if (vfx != null && vfx.TryGetComponent(out TracerVFX tracer)) tracer.Show(from, to);
        // 총구 섬광 (짧게 번쩍하고 사라지는 총 전용 이펙트)
        var flash = VFXManager.Instance.Play("MuzzleFlashVFX", from);
        if (flash != null && flash.TryGetComponent(out MuzzleFlashVFX muzzleFlash)) muzzleFlash.Show(from, to - from);
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
        UpdateAmmoBar();
    }

    public override void OnDashFinished()
    {
        bool wasNotFull = _bullets < MagazineSize || _isReloading;
        FinishReload();
        if (wasNotFull) SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 1.25f, 0.8f); // 구르며 철컥
    }

    // 스킬칸 옆 탄환 줄 갱신 (재장전 중 진행도는 ReloadRoutine이 매 프레임 갱신)
    void UpdateAmmoBar()
    {
        if (_ammoBar != null) _ammoBar.Set(_bullets, MagazineSize, _isReloading ? 0f : -1f);
    }

    // ── 특수기: 속사 ─────────────────────────────────────────
    public override bool CanUseSuper => _bullets > 0 && !_isReloading;

    public override IEnumerator SuperRoutine()
    {
        Player.IsAttacking = true;
        Player.BeginEmpoweredAttack();
        while (_bullets > 0 && !Player.IsGameOver)
        {
            FireOnce();
            yield return new WaitForSeconds(_rapidInterval * (_isDual ? _dualRapidScale : 1f));
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
    public override void OnResetState()
    {
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
