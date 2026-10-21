public class Defines
{
    public enum SKILL
    {
        MAIN = 0, SUB, SUPER, SPECIAL
    }

    // 플레이어 공격 종류 (아티팩트 발동 조건 구분용)
    public enum AttackType
    {
        Basic,   // 좌클릭 기본 공격
        Super,   // Q 스킬
    }

    public enum ITEM
    {
        GOLD = 0,
        HP,
        ATKUP,
        RANGEUP,
        CRITRATEUP,
    }
}