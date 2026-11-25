using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 토끼 검사의 스킬.
///   기본기 : 앞을 벤다 (모양은 PlayerAttack, 숙련도 강화로 찌르기·3연타 등으로 바뀐다)
///   특수기 : 잠시 힘을 모아 앞을 크게 벤다 (800%)
///   필살기 : 화면이 유리처럼 깨지는 연출과 함께 화면의 모든 적을 벤다 (3000%)
/// 숙련도 강화(PlayerSkillUpgrades)를 쓰는 캐릭터.
/// </summary>
public class RabbitSwordSkills : CharacterSkills
{
    [Header("특수기")]
    [SerializeField] float _superChargeTime = 0.5f;
    [SerializeField] float _superDamageRate = 8f;

    [Header("필살기")]
    [SerializeField] float _specialDamageMultiplier = 30f;
    [SerializeField] float _specialInvincibleDelay = 1.5f;   // 차징 시간 (이후 연출 동안 무적)

    PlayerSkillUpgrades _upgrades;
    SpecialSkillEffect _specialSkillEffect;

    public override bool SupportsMastery => true;

    protected override void Awake()
    {
        base.Awake();
        _upgrades = GetComponent<PlayerSkillUpgrades>();
    }

    // 필살기 연출 캔버스는 씬에 있으므로 생성된 뒤에 찾는다
    public override void OnInit()
    {
        var go = GameObject.FindWithTag("SpecialEffect");
        if (go != null) _specialSkillEffect = go.GetComponent<SpecialSkillEffect>();
        if (_specialSkillEffect != null) _specialSkillEffect.Close();
    }

    // ── 기본기 ───────────────────────────────────────────────
    public override void StartBasic(Action onDone) => Player.Attack.AttackStart(onDone);

    // ── 특수기 ───────────────────────────────────────────────
    public override IEnumerator SuperRoutine()
    {
        // 특수기 강화(모아베기 등)를 골랐으면 그 동작으로
        // (StartCoroutine 대신 그대로 이어서 실행 → PlayerController가 노드 이동 때 한꺼번에 멈출 수 있게)
        if (_upgrades != null && _upgrades.HasSuperUpgrade)
        {
            yield return _upgrades.SuperRoutine();
            yield break;
        }

        Player.IsAttacking = true;
        Player.BeginEmpoweredAttack();
        Player.PlayAnim("isSuper");

        // 누른 순간부터 이펙트가 나가기 전까지 이동속도 감소
        Player.SetMoveSpeedRate(Player.SuperChargeSpeedRate);

        // 차징 (하얗게 깜빡임)
        float elapsed = 0f;
        bool white = false;
        while (elapsed < _superChargeTime)
        {
            white = !white;
            Player.SetFlash(white ? 1f : 0f);
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }
        Player.SetFlash(0f);
        Player.SetMoveSpeedRate(1f);

        Player.SetDamageMultiplier(_superDamageRate);
        bool attackDone = false;
        Player.Attack.SuperAttackStart(() => attackDone = true);
        yield return new WaitUntil(() => attackDone);
        Player.SetDamageMultiplier(1f);

        yield return new WaitForSeconds(0.5f);
        Player.EndAttack();
    }

    // ── 필살기 ───────────────────────────────────────────────
    public override IEnumerator SpecialRoutine()
    {
        Player.IsAttacking = true;
        Player.BeginEmpoweredAttack();

        Player.PlayAnim("isSpecial");
        SoundManager.Instance.PlaySFX("SFX/Player_SpecialCharge");
        VFXManager.Instance.PlayAttached("SpecialCharge", transform);
        yield return new WaitForSecondsRealtime(_specialInvincibleDelay);

        // 차징 중에 맞아서 죽었다면 연출을 발동하지 않음
        if (Player.IsGameOver)
        {
            Player.EndAttack();
            yield break;
        }

        // 차징이 끝나고 연출이 발동되는 순간부터 무적
        Player.PushInvincible();

        // 화면을 찍어서 유리처럼 깨뜨리는 연출
        yield return new WaitForEndOfFrame();
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        if (_specialSkillEffect != null)
            yield return _specialSkillEffect.PlayRoutine(screenshot, DealSpecialDamage);
        else
        {
            Destroy(screenshot);
            DealSpecialDamage();
        }

        Player.PopInvincible();
        Player.EndAttack();
    }

    void DealSpecialDamage()
    {
        float rate = _upgrades != null ? _upgrades.SpecialDamageRate(_specialDamageMultiplier) : _specialDamageMultiplier;
        DealSpecialHit(rate);
        if (_upgrades != null) _upgrades.OnSpecialDamageDealt(); // 필살기 강화별 추가 효과 (연속 일섬, 달의 가호, 탄막 반사)
    }

    /// <summary>화면의 모든 적·상자에 한 번 피해 (필살기 강화 "연속 일섬"이 여러 번 부른다)</summary>
    public void DealSpecialHit(float rate)
    {
        float totalMultiplier = rate * (1f + Player.SpecialDamageBonus);
        int damage = Mathf.RoundToInt(Player.DamageReturn() * totalMultiplier);
        bool hitAny = false; // 하나라도 맞았는지 (적중 사운드용)

        foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (!enemy.gameObject.activeSelf || enemy.IsDead) continue;
            enemy.DamageTaken(damage);
            IngameManager.Instance.Flash(enemy.CharSprite, 0.5f);
            DamageTextManager.Instance.ShowSpecial(damage, enemy.transform.position);
            VFXManager.Instance.Play("SpecialHit", enemy.transform.position);
            hitAny = true;
        }

        // 상자 데미지 (ChestObject는 EnemyBase 미상속)
        foreach (var chest in FindObjectsByType<ChestObject>(FindObjectsSortMode.None))
        {
            if (!chest.gameObject.activeSelf) continue;
            chest.TakeDamage(damage);
            hitAny = true;
        }

        // 적중 사운드는 맞은 대상 수와 상관없이 한 번만 (여러 번 겹쳐 재생되면 소리가 뭉개짐)
        if (hitAny)
            SoundManager.Instance.PlaySFX("SFX/Player_SwordHit");
    }
}
