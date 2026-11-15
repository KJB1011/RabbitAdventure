using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Golden Carrot Bank", fileName = "GoldenCarrotBank")]
public class GoldenCarrotBankSO : ArtifactSO
{
    [SerializeField] int _goldPerStep = 10;        // 이 골드마다
    [SerializeField] float _bonusPerStep = 0.02f;  // 공격력 +2%
    [SerializeField] float _maxBonus = 0.3f;       // 최대 +30%

    public override ArtifactEffect CreateEffect()
        => new Artifact_GoldenCarrotBank(_goldPerStep, _bonusPerStep, _maxBonus);
}

/// <summary>
/// 보유 골드에 비례해 공격력 증가 (골드가 바뀔 때마다 다시 계산)
/// </summary>
public class Artifact_GoldenCarrotBank : ArtifactEffect
{
    private readonly int _goldPerStep;
    private readonly float _bonusPerStep;
    private readonly float _maxBonus;
    private float _appliedBonus;   // 지금 플레이어에게 더해져 있는 값 — 바꿀 때 이만큼 빼고 새로 더한다

    public Artifact_GoldenCarrotBank(int goldPerStep, float bonusPerStep, float maxBonus)
    {
        _goldPerStep = Mathf.Max(1, goldPerStep);
        _bonusPerStep = bonusPerStep;
        _maxBonus = maxBonus;
    }

    protected override void OnActivate()
    {
        _appliedBonus = 0f;
        if (IngameManager.Instance == null) return;

        IngameManager.Instance.OnGoldChanged += HandleGoldChanged;
        HandleGoldChanged(IngameManager.Instance.Gold); // 얻은 순간의 골드로 바로 적용
    }

    protected override void OnDeactivate()
    {
        if (IngameManager.Instance != null)
            IngameManager.Instance.OnGoldChanged -= HandleGoldChanged;

        Player.RemoveAtkMultiplier(_appliedBonus);
        _appliedBonus = 0f;
    }

    private void HandleGoldChanged(int gold)
    {
        float bonus = Mathf.Min((gold / _goldPerStep) * _bonusPerStep, _maxBonus); // 정수 나눗셈 → 10골드 단위로 계단식
        if (Mathf.Approximately(bonus, _appliedBonus)) return;

        Player.RemoveAtkMultiplier(_appliedBonus);
        Player.AddAtkMultiplier(bonus);
        _appliedBonus = bonus;
    }
}
