using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Enemy들의 기초Base를 담당하는 스크립트
/// 기본 스탯, 생성, 연출, 피해, 죽음 등의 기능이 들어있습니다.
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    public int _totalHp = 100;
    public int _nowHp;
    public float hpRate => (float)_nowHp / _totalHp;

    public event Action OnDied;

    [Header("처치 보상")]
    [SerializeField] int _goldRewardMin = 5;   // 처치 시 골드 (최소~최대, 최대값 포함)
    [SerializeField] int _goldRewardMax = 7;

    [Header("등장 연출")]
    [SerializeField] protected float _entranceDuration = 1.2f;
    [SerializeField] protected float _entranceOffsetX = 20f;

    [Header("사망 연출")]
    [SerializeField] float _bounceHeight = 3f;
    [SerializeField] float _bounceUpTime = 0.3f;
    [SerializeField] float _fallSpeed = 15f;
    [SerializeField] float _fallDistance = 10f;
    [SerializeField] float _spinSpeed = 720f;

    [Header("전투 연출")]
    [SerializeField] SpriteRenderer _enemySprite;
    [Tooltip("스프라이트 원본이 오른쪽을 보고 있으면 체크 (플레이어 쪽으로 뒤집을 때 기준)")]
    [SerializeField] bool _spriteFacesRight = true;

    UIHPBar _hpBar; // 노드가 지정해 준 HP바 (없으면 기본 HP바 사용)

    public bool IsDead { get; protected set; }
    public SpriteRenderer CharSprite { get => _enemySprite; }

    /// <summary>이 적 전용 HP바 지정 (적이 여러 마리 나올 때 각자 다른 바를 쓰게)</summary>
    public void BindHPBar(UIHPBar bar) => _hpBar = bar;

    /// <summary>지정한 위치를 바라보도록 스프라이트를 좌우로 뒤집는다</summary>
    public void FaceTo(Vector2 targetPos)
    {
        if (_enemySprite == null) return;
        float dx = targetPos.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return; // 바로 위·아래에 있으면 그대로
        bool targetIsRight = dx > 0f;
        _enemySprite.flipX = targetIsRight != _spriteFacesRight;
    }

    // virtual: 허수아비(TrainingDummy)처럼 체력 대신 다른 처리를 하는 적이 재정의한다
    public virtual void DamageTaken(int damage)
    {
        if (IsDead || damage <= 0) return;

        int before = _nowHp;
        _nowHp = ClampHp(_nowHp - damage);

        // 전투 통계 — 실제로 깎인 HP만 집계
        if (IngameManager.Instance._isBattle)
            IngameManager.Instance.AddDamage(before - _nowHp);

        if (_hpBar != null) _hpBar.SetBarValue(hpRate);
        else IngameManager.Instance.SetHPBar(hpRate);

        if (_nowHp <= 0)
            Die();
    }

    // 데미지 적용 후 HP 보정. 보스 페이즈처럼 HP 하한이 다른 적은 이것만 재정의
    protected virtual int ClampHp(int hp) => Mathf.Max(hp, 0);

    protected virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 패턴 즉시 중단
        StopAllCoroutines();
        var attack = GetComponent<EnemyAttack>();
        if (attack != null)
            attack.ClearAll();

        // 마지막 적일 때만: 탄환 전체 제거 + 플레이어 무적
        // (듀오처럼 동료가 남아 있으면 동료의 탄과 전투는 그대로 이어진다)
        if (!AnyOtherEnemyAlive())
        {
            VFXManager.Instance.ClearAllBullets();

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                player.StartInvincible(5f);
        }

        int reward = UnityEngine.Random.Range(_goldRewardMin, _goldRewardMax + 1);
        IngameManager.Instance.AddGold(reward);

        VFXManager.Instance.Play("FinishHit", transform.position);
        SoundManager.Instance.PlaySFX("SFX/Slain");

        OnDied?.Invoke();

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathRoutine());
    }

    bool AnyOtherEnemyAlive()
    {
        foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (enemy != this && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                return true;
        return false;
    }

    protected IEnumerator EntranceFromRight(Vector2 targetPos)
    {
        Vector2 startPos = targetPos + Vector2.right * _entranceOffsetX;
        transform.position = startPos;
        FaceTo(targetPos); // 들어오는 방향(왼쪽)을 보면서 등장
        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < _entranceDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / _entranceDuration), 3f);
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.position = targetPos;
    }

    IEnumerator DeathRoutine()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * _bounceHeight;

        float elapsed = 0f;
        while (elapsed < _bounceUpTime)
        {
            elapsed += Time.deltaTime;
            float eased = 1f - Mathf.Pow(1f - elapsed / _bounceUpTime, 2f);
            transform.position = Vector3.Lerp(startPos, peakPos, eased);
            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            yield return null;
        }

        Vector3 fallTarget = peakPos + Vector3.down * _fallDistance;
        elapsed = 0f;
        float fallDuration = _fallDistance / _fallSpeed;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float eased = Mathf.Pow(elapsed / fallDuration, 2f);
            transform.position = Vector3.Lerp(peakPos, fallTarget, eased);
            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}