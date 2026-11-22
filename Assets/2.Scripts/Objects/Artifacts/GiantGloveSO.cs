using UnityEngine;

[CreateAssetMenu(menuName = "Artifact/Giant Glove", fileName = "GiantGlove")]
public class GiantGloveSO : ArtifactSO
{
    [SerializeField] float _rangeBonus = 0.4f;        // 공격범위 +40%
    [SerializeField] float _mainCooldownAdd = 0.1f;   // 대신 기본기 쿨타임 +0.1초

    public override ArtifactEffect CreateEffect() => new Artifact_GiantGlove(_rangeBonus, _mainCooldownAdd);
}

/// <summary>
/// 거인의 장갑: 공격범위 크게 증가, 대신 기본기가 조금 느려짐 (대가형)
/// </summary>
public class Artifact_GiantGlove : ArtifactEffect
{
    private readonly float _rangeBonus;
    private readonly float _mainCooldownAdd;

    public Artifact_GiantGlove(float rangeBonus, float mainCooldownAdd)
    {
        _rangeBonus = rangeBonus;
        _mainCooldownAdd = mainCooldownAdd;
    }

    protected override void OnActivate()
    {
        Player.AddRange(_rangeBonus);
        Player.ModifyMainCooldown(1f, _mainCooldownAdd);
    }

    protected override void OnDeactivate()
    {
        Player.AddRange(-_rangeBonus);
        Player.ModifyMainCooldown(1f, -_mainCooldownAdd);
    }
}
