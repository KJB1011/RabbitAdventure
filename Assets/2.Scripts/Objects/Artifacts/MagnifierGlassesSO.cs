using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Magnifier Glasses", fileName = "MagnifierGlasses")]
public class MagnifierGlassesSO : ArtifactSO
{
    [Tooltip("공격범위 +10%당 공격력 증가량 (0.06 = +6%)")]
    [SerializeField] float _atkPerTenPercentRange = 0.06f;

    public override ArtifactEffect CreateEffect() => new Artifact_MagnifierGlasses(_atkPerTenPercentRange);
}

/// <summary>
/// 돋보기 안경: 공격범위가 100%를 넘는 만큼 공격력 증가 (범위 +10%당 공격력 +6%)
/// 공격범위가 바뀔 때마다(OnRangeChanged) 다시 계산한다.
/// </summary>
public class Artifact_MagnifierGlasses : ArtifactEffect
{
    private readonly float _atkPerTenPercent;
    private float _appliedBonus; // 지금 적용 중인 공격력 보너스 (바뀌면 빼고 새로 더한다)

    public Artifact_MagnifierGlasses(float atkPerTenPercent) => _atkPerTenPercent = atkPerTenPercent;

    protected override void OnActivate()
    {
        Player.OnRangeChanged += Recalculate;
        Recalculate();
    }

    protected override void OnDeactivate()
    {
        Player.OnRangeChanged -= Recalculate;
        Player.RemoveAtkMultiplier(_appliedBonus);
        _appliedBonus = 0f;
    }

    private void Recalculate()
    {
        float extraRange = Mathf.Max(0f, Player.RangeMultiplier - 1f);   // 1.3이면 0.3 (= +30%)
        float bonus = extraRange * 10f * _atkPerTenPercent;               // +30% → 3 × 6% = +18%

        Player.RemoveAtkMultiplier(_appliedBonus);
        Player.AddAtkMultiplier(bonus);
        _appliedBonus = bonus;
    }
}
