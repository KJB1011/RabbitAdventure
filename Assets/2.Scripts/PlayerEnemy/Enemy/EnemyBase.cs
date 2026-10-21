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

    [Header("등장 연출")]
    [SerializeField] protected float _entranceDuration = 1.2f;
    [SerializeField] protected float _entranceOffsetX = 20f;

    [Header("사망 연출")]
    [SerializeField] float _bounceHeight = 3f;
    [SerializeField] float _bounceUpTime = 0.3f;
    [SerializeField] float _fallSpeed = 15f;
    [SerializeField] float _fallDistance = 10f;
    [SerializeField] float _spinSpeed = 720f;

    public bool IsDead { get; protected set; }

    public void DamageTaken(int damage) 
    {
        if (IsDead || damage <= 0) return;

        int before = _nowHp;
        _nowHp = ClampHp(_nowHp - damage);

        // 전투 통계 — 실제로 깎인 HP만 집계
        if (IngameManager.Instance._isBattle)
            IngameManager.Instance.AddDamage(before - _nowHp);

        IngameManager.Instance.SetHPBar(hpRate);

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

        // 탄환 전체 제거
        VFXManager.Instance.ClearAllBullets();

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.StartInvincible(5f);

        int reward = UnityEngine.Random.Range(8, 11);
        IngameManager.Instance.AddGold(reward);

        VFXManager.Instance.Play("FinishHit", transform.position);
        SoundManager.Instance.PlaySFX("SFX/Slain");

        OnDied?.Invoke();

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathRoutine());
    }

    protected IEnumerator EntranceFromRight(Vector2 targetPos)
    {
        Vector2 startPos = targetPos + Vector2.right * _entranceOffsetX;
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