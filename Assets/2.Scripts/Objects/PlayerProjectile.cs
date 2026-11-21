using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Defines;

/// <summary>
/// 플레이어가 날리는 투사체 (기본기 강화: 검기 베기).
/// 정해진 거리만큼 날아가며 닿은 적을 관통해서 한 번씩 벤다.
/// VFXManager의 풀에서 꺼내 쓰고(Play), 꺼낸 직후 Launch로 출발시킨다.
/// (적의 EnemyHitbox는 플레이어 몸에 붙은 공격 판정만 알아보므로, 판정은 여기서 직접 한다)
/// </summary>
public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] SpriteRenderer _sprite;
    [SerializeField] float _fadeTime = 0.1f;   // 다 날아간 뒤 사라지는 시간

    PlayerController _owner;
    Vector2 _dir;
    float _speed, _range, _radius, _damageRate, _launchEmpower;
    float _traveled;
    bool _isFlying;
    AttackType? _notifyType;

    readonly HashSet<EnemyBase> _hitEnemies = new();
    readonly HashSet<ChestObject> _hitChests = new();
    static int _lastHitSoundFrame = -1; // 같은 프레임에 여러 적을 맞혀도 소리는 한 번

    /// <param name="damageRate">기본 공격력의 몇 배인지 (0.7 = 70%)</param>
    /// <param name="notifyType">적중 알림을 보낼 공격 종류 (null이면 알리지 않음 — 탄막 반사처럼 한꺼번에 많이 날릴 때)</param>
    public void Launch(PlayerController owner, Vector2 dir, float speed, float range, float radius, float damageRate,
                       AttackType? notifyType = AttackType.Basic)
    {
        _notifyType = notifyType;
        _owner = owner;
        _dir = dir.normalized;
        _speed = speed;
        _range = range;
        _radius = radius;
        _damageRate = damageRate;
        _launchEmpower = owner != null ? owner.AttackEmpowerRate : 1f; // 쏜 순간의 "다음 공격 강화"를 기억
        _traveled = 0f;
        _isFlying = true;
        _hitEnemies.Clear();
        _hitChests.Clear();

        if (_sprite != null)
        {
            _sprite.DOKill();
            var c = _sprite.color; c.a = 1f; _sprite.color = c;
        }
    }

    void Update()
    {
        if (!_isFlying) return;

        float step = _speed * Time.deltaTime;
        transform.position += (Vector3)(_dir * step);
        _traveled += step;
        HitAround();

        if (_traveled >= _range)
        {
            _isFlying = false;
            if (_sprite != null) _sprite.DOFade(0f, _fadeTime);
        }
    }

    void HitAround()
    {
        if (_owner == null) return;

        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, _radius))
        {
            // 적의 몸(EnemyHitbox)에 닿았을 때만 — 적의 공격 콜라이더나 총알은 무시
            if (hit.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead || !_hitEnemies.Add(enemy)) continue;

                int damage = CalcDamage(out bool isCrit);
                enemy.DamageTaken(damage);
                IngameManager.Instance.Flash(enemy.CharSprite, 0.05f);
                DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);
                VFXManager.Instance.Play("HitSpark", enemy.transform.position, Quaternion.LookRotation(_dir, Vector3.back));
                if (_notifyType.HasValue)
                    _owner.NotifyAttackHit(enemy, _notifyType.Value); // 기본기 적중으로 치는 아티팩트도 발동

                if (_lastHitSoundFrame != Time.frameCount)
                {
                    _lastHitSoundFrame = Time.frameCount;
                    SoundManager.Instance.PlaySFX("SFX/Player_SwordHit");
                }
                continue;
            }

            var chest = hit.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeSelf && _hitChests.Add(chest))
                chest.TakeDamage(CalcDamage(out _));
        }
    }

    // 날아가는 동안 기본기가 끝나서 "다음 공격 강화"가 사라져도, 쏜 순간의 배율로 계산한다
    int CalcDamage(out bool isCrit)
    {
        float empowerFix = _launchEmpower / Mathf.Max(0.01f, _owner.AttackEmpowerRate);
        return Mathf.Max(1, Mathf.RoundToInt(_owner.DamageReturn(out isCrit) * _damageRate * empowerFix));
    }

    void OnDisable()
    {
        _isFlying = false;
        if (_sprite != null) _sprite.DOKill();
    }
}
