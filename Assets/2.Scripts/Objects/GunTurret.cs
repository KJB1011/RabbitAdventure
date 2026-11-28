using DG.Tweening;
using UnityEngine;
using static Defines;

/// <summary>
/// 토끼 총잡이 특수기 강화 "포탑".
/// 정해진 시간 동안 그 자리에 서서, 가장 가까운 적에게 일정 간격으로 유도탄(HomingMissile)을 쏜다.
/// 한 번에 하나만 (새로 설치하면 예전 것은 사라진다). RabbitGunnerSkills가 Spawn으로 만든다.
/// </summary>
public class GunTurret : MonoBehaviour
{
    PlayerController _owner;
    SpriteRenderer _sprite;
    Sprite _missileSprite;
    Material _material;
    float _life, _interval, _damageRate, _fireTimer;

    public static GunTurret Spawn(Vector2 pos, PlayerController owner, Sprite turretSprite, Sprite missileSprite,
                                  Material material, int sortingLayerId, float duration, float interval, float damageRate)
    {
        var go = new GameObject("GunTurret");
        go.transform.position = pos;
        var t = go.AddComponent<GunTurret>();
        t._owner = owner;
        t._missileSprite = missileSprite;
        t._material = material;
        t._life = duration;
        t._interval = interval;
        t._damageRate = damageRate;
        t._fireTimer = 0.3f; // 설치하고 잠깐 뒤 첫 발

        t._sprite = go.AddComponent<SpriteRenderer>();
        t._sprite.sprite = turretSprite;
        if (material != null) t._sprite.sharedMaterial = material;
        t._sprite.sortingLayerID = sortingLayerId;
        t._sprite.sortingOrder = 3;

        // 뿅 하고 나타남
        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        VFXManager.Instance.Play("BlinkStartVFX", pos);
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Reload", 0.8f);
        return t;
    }

    void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f) { Despawn(); return; }

        // 끝나기 직전엔 깜빡임
        if (_life < 1f) _sprite.enabled = Mathf.Sin(_life * 30f) > -0.3f;

        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f) return;

        var target = HomingMissile.FindNearestEnemy(transform.position);
        if (target == null) { _fireTimer = 0.1f; return; } // 적이 없으면 기다린다
        _fireTimer = _interval;

        // 적 쪽을 바라보고, 옆으로 살짝 튀어나가듯 발사 → 휘어서 날아간다
        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
        _sprite.flipX = toTarget.x < 0f;
        float side = Random.value < 0.5f ? 1f : -1f;
        Vector2 launchDir = Quaternion.Euler(0f, 0f, side * Random.Range(40f, 70f)) * toTarget.normalized;
        Vector2 muzzle = (Vector2)transform.position + new Vector2(_sprite.flipX ? -0.25f : 0.25f, 0.12f);
        HomingMissile.Launch(muzzle, launchDir, target, _owner, _damageRate, _missileSprite, _material, _sprite.sortingLayerID);

        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.2f, -0.15f, 0f), 0.15f, 1);
        SoundManager.Instance.PlaySFXPitch("SFX/Gen_Gunshot", 1.5f, 0.35f);
    }

    /// <summary>시간이 다 됐거나 새 포탑으로 바꿀 때</summary>
    public void Despawn()
    {
        if (this == null) return;
        VFXManager.Instance.Play("BlinkStartVFX", transform.position);
        Destroy(gameObject);
    }

    void OnDestroy() => transform.DOKill();
}

/// <summary>
/// 포탑이 쏘는 유도탄. 꼬리(TrailRenderer)를 남기며 목표 쪽으로 휘어 날아가고, 닿은 적에게 피해를 준다.
/// 목표가 먼저 죽으면 가장 가까운 다른 적을 다시 노린다.
/// </summary>
public class HomingMissile : MonoBehaviour
{
    const float Speed = 9f;
    const float TurnRate = 540f;   // 초당 회전 각도
    const float HitRadius = 0.2f;
    const float LifeTime = 3f;

    EnemyBase _target;
    PlayerController _owner;
    float _damageRate, _life;
    Vector2 _dir;
    bool _done;
    SpriteRenderer _sprite;
    TrailRenderer _trail;

    public static void Launch(Vector2 pos, Vector2 dir, EnemyBase target, PlayerController owner, float damageRate,
                              Sprite sprite, Material material, int sortingLayerId)
    {
        var go = new GameObject("HomingMissile");
        go.transform.position = pos;
        var m = go.AddComponent<HomingMissile>();
        m._target = target;
        m._owner = owner;
        m._damageRate = damageRate;
        m._dir = dir.normalized;
        m._life = LifeTime;

        m._sprite = go.AddComponent<SpriteRenderer>();
        m._sprite.sprite = sprite;
        if (material != null) m._sprite.sharedMaterial = material;
        m._sprite.sortingLayerID = sortingLayerId;
        m._sprite.sortingOrder = 7;
        m._sprite.color = new Color(1f, 0.7f, 0.35f);

        // 꼬리: 노랑 → 주황으로 옅어지며 가늘어진다
        m._trail = go.AddComponent<TrailRenderer>();
        if (material != null) m._trail.sharedMaterial = material;
        m._trail.sortingLayerID = sortingLayerId;
        m._trail.sortingOrder = 6;
        m._trail.time = 0.25f;
        m._trail.minVertexDistance = 0.05f;
        m._trail.widthCurve = AnimationCurve.Linear(0f, 0.12f, 1f, 0f);
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.15f), 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        m._trail.colorGradient = g;
    }

    /// <summary>pos에서 가장 가까운 살아 있는 적</summary>
    public static EnemyBase FindNearestEnemy(Vector2 pos)
    {
        EnemyBase best = null;
        float bestDist = float.MaxValue;
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (e.IsDead || !e.gameObject.activeInHierarchy) continue;
            float d = ((Vector2)e.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    void Update()
    {
        if (_done) return;

        _life -= Time.deltaTime;
        if (_life <= 0f) { Finish(); return; }

        // 목표가 죽었으면 다른 적으로
        if (_target == null || _target.IsDead || !_target.gameObject.activeInHierarchy)
            _target = FindNearestEnemy(transform.position);

        // 목표 쪽으로 조금씩 꺾는다
        if (_target != null)
        {
            Vector2 want = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.SignedAngle(_dir, want);
            float step = Mathf.Clamp(angle, -TurnRate * Time.deltaTime, TurnRate * Time.deltaTime);
            _dir = Quaternion.Euler(0f, 0f, step) * _dir;
        }

        transform.position += (Vector3)(_dir * Speed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg);

        // 적의 몸(EnemyHitbox)에 닿으면 터진다 (목표가 아니어도 맞은 적에게)
        foreach (var col in Physics2D.OverlapCircleAll(transform.position, HitRadius))
        {
            if (!col.TryGetComponent(out EnemyHitbox _)) continue;
            var enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDead) continue;
            Hit(enemy);
            return;
        }
    }

    void Hit(EnemyBase enemy)
    {
        if (_owner != null)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(_owner.DamageReturn(out bool isCrit) * _damageRate));
            enemy.DamageTaken(damage);
            IngameManager.Instance.Flash(enemy.CharSprite, 0.05f);
            DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);
            _owner.NotifyAttackHit(enemy, AttackType.Super);
        }
        VFXManager.Instance.Play("HitSpark", transform.position, Quaternion.LookRotation(_dir, Vector3.back));
        Finish();
    }

    // 몸은 바로 숨기고, 꼬리가 사라질 때까지 기다렸다가 지운다
    void Finish()
    {
        _done = true;
        if (_sprite != null) _sprite.enabled = false;
        if (_trail != null) _trail.emitting = false;
        Destroy(gameObject, _trail != null ? _trail.time : 0f);
    }
}
