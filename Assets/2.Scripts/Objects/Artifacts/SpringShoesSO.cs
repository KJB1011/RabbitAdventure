using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Spring Shoes", fileName = "SpringShoes")]
public class SpringShoesSO : ArtifactSO
{
    [SerializeField] float _radius = 1.5f;           // 충격파 반경
    [SerializeField] float _damageRate = 1f;         // 공격력의 몇 배 (1 = 100%)
    [SerializeField] string _vfxId = "SpringShockwave"; // Resources/VFX 안의 이름
    [SerializeField] string _sfxPath = "SFX/AreaDamage";

    public override ArtifactEffect CreateEffect()
        => new Artifact_SpringShoes(_radius, _damageRate, _vfxId, _sfxPath);
}

/// <summary>
/// 대쉬가 끝난 자리에 충격파를 일으켜 주변 적에게 피해
/// (AreaDamageZone은 적 전용이라 플레이어용 판정을 따로 둔다)
/// </summary>
public class Artifact_SpringShoes : ArtifactEffect
{
    private readonly float _radius;
    private readonly float _damageRate;
    private readonly string _vfxId;
    private readonly string _sfxPath;

    // 한 적이 콜라이더를 여러 개 가져도 한 번만 맞도록 (매번 새로 만들지 않고 재사용)
    private readonly HashSet<EnemyBase> _hitEnemies = new();
    private readonly HashSet<ChestObject> _hitChests = new();

    public Artifact_SpringShoes(float radius, float damageRate, string vfxId, string sfxPath)
    {
        _radius = radius;
        _damageRate = damageRate;
        _vfxId = vfxId;
        _sfxPath = sfxPath;
    }

    protected override void OnActivate() => Player.OnDashEnded += HandleDashEnded;
    protected override void OnDeactivate() => Player.OnDashEnded -= HandleDashEnded;

    private void HandleDashEnded()
    {
        Vector2 center = Player.transform.position;
        _hitEnemies.Clear();
        _hitChests.Clear();

        foreach (var hit in Physics2D.OverlapCircleAll(center, _radius))
        {
            // 적의 몸(EnemyHitbox)에 닿았을 때만 — 적의 공격 콜라이더나 총알은 무시
            if (hit.TryGetComponent(out EnemyHitbox _))
            {
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null && !enemy.IsDead && _hitEnemies.Add(enemy))
                    HitEnemy(enemy);
                continue;
            }

            var chest = hit.GetComponentInParent<ChestObject>();
            if (chest != null && chest.gameObject.activeSelf && _hitChests.Add(chest))
                chest.TakeDamage(CalcDamage(out _));
        }

        if (VFXManager.Instance != null) VFXManager.Instance.Play(_vfxId, center);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(_sfxPath);
    }

    private void HitEnemy(EnemyBase enemy)
    {
        int damage = CalcDamage(out bool isCrit);
        enemy.DamageTaken(damage);
        IngameManager.Instance.Flash(enemy.CharSprite, 0.05f);
        DamageTextManager.Instance.Show(damage, isCrit, enemy.transform.position);
        // OnAttackHit 알림은 보내지 않는다 → 다른 아티팩트가 연쇄로 발동하지 않게
    }

    private int CalcDamage(out bool isCrit)
        => Mathf.Max(1, Mathf.RoundToInt(Player.DamageReturn(out isCrit) * _damageRate));
}
