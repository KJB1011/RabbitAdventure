using UnityEngine;
using static Defines;

/// <summary>
/// 스킬 강화 종류. 실제 동작은 PlayerController.ApplySkillUpgrade에서 종류별로 켠다.
/// (새 강화를 만들면 여기에 이름을 추가하고, PlayerController에 동작을 추가한다)
/// </summary>
public enum SkillUpgradeType
{
    None = 0,

    // ── 회피기 (Dash) ──
    DashGale = 1,          // 질풍: 빠르게 돌진하며 지나간 자리의 적에게 피해
    DashDoubleCharge = 2,  // 회피기 2회 충전
    DashEmpower = 3,       // 회피기 후 다음 공격 강화

    // ── 기본기 (BasicAttack) ──
    MainThrust = 4,        // 찌르기: 좁고 긴 찌르기, 공격 속도 증가
    MainTripleSlash = 5,   // 3연타: 스슥-삭!
    MainCrescent = 6,      // 초승달 베기: 크게 한 번
    MainSwordWave = 7,     // 검기 베기: 앞으로 날아가는 검기 (원거리)

    // ── 회피기 (추가) ──
    DashBlink = 8,         // 순간이동: 즉시 이동 + 도착 지점 주변 탄환 제거 (이동 중 무적 없음)

    // ── 특수기 (SuperAttack) ──
    SuperCharge = 9,       // 모아베기: 누르는 동안 3단계 차징
    SuperSpin = 10,        // 회전베기: 주변 3회 베기 + 탄환 제거
    SuperParry = 11,       // 받아치기: 자세 중 맞으면 반격
    SuperSwordPlant = 12,  // 검 꽂기: 설치형 지속 피해

    // ── 필살기 (SpecialAttack) ──
    SpecialMultiSlash = 13,   // 연속 일섬: 5번 베기
    SpecialMoonBlessing = 14, // 달의 가호: 피해↓ 쿨타임↓ + 공격력 버프
    SpecialReflect = 15,      // 탄막 반사: 적 탄환을 검기로 되돌림
    SpecialSpirit = 16,       // 투지 충전: 쿨타임 대신 적중 게이지

    // ══ 토끼 총잡이 (RabbitGunnerSkills에서 처리) ══
    // ── 기본기 ──
    GunExtendedMag = 17,      // 확장 탄창: 6발 → 10발, 재장전 2초 → 2.5초
    GunCritRefund = 18,       // 탄 회수: 치명타 확률 +10%, 치명타가 터지면 총알 1발 회수
    GunPrecision = 19,        // 정밀 사격: 피격 원 가운데를 맞히면 +30% 추가 피해
    GunChargeShot = 20,       // 차지샷: 누르고 있다가 놓으면 3발을 쓰는 관통 500% 저격탄

    // ── 회피기 ──
    GunIncendiary = 21,       // 특수탄 장전: 구르기로 장전하면 맨 위 3발이 소이탄 (2초 화상)
    GunBackstep = 22,         // 백스텝 산탄: 커서 반대쪽으로 구르며 커서 쪽으로 산탄 5발
    GunCasingBomb = 23,       // 탄피 폭탄: 구른 자리에 1초 뒤 터지는 폭탄
    GunQuickDraw = 24,        // 퀵 드로우: 구르기 후 3초 안의 첫 발은 확정 치명타 + 관통
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
