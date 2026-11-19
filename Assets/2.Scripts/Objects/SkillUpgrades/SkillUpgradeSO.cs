using UnityEngine;
using static Defines;

/// <summary>
/// 스킬 강화 종류. 실제 동작은 PlayerController.ApplySkillUpgrade에서 종류별로 켠다.
/// (새 강화를 만들면 여기에 이름을 추가하고, PlayerController에 동작을 추가한다)
/// </summary>
public enum SkillUpgradeType
{
    None = 0,

    // ── 대쉬 ──
    DashGale = 1,          // 질풍: 빠르게 돌진하며 지나간 자리의 적에게 피해
    DashDoubleCharge = 2,  // 대쉬 2회 충전
    DashEmpower = 3,       // 대쉬 후 다음 공격 강화
}

/// <summary>
/// 숙련도가 가득 찼을 때 고를 수 있는 스킬 강화 하나의 데이터 (이름·설명·아이콘)
/// </summary>
[CreateAssetMenu(menuName = "Skill/Skill Upgrade", fileName = "SkillUpgrade")]
public class SkillUpgradeSO : ScriptableObject
{
    public SKILL skill;                  // 어느 스킬의 강화인지
    public SkillUpgradeType type;        // 어떤 강화인지
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;
}
