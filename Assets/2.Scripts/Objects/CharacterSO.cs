using UnityEngine;

/// <summary>
/// 고를 수 있는 캐릭터 하나의 데이터.
/// 새 캐릭터를 추가할 때는 이 에셋을 하나 만들고(우클릭 → Create → Character → Character),
/// IngameManager의 Characters 목록에 넣으면 연습방 캐릭터 선택 창에 나온다.
/// </summary>
[CreateAssetMenu(menuName = "Character/Character", fileName = "Character")]
public class CharacterSO : ScriptableObject
{
    [Tooltip("저장·멀티플레이에서 캐릭터를 구분하는 이름 (영문, 바꾸지 말 것)")]
    public string characterId = "rabbit_sword";
    public string displayName = "토끼 검사";
    [TextArea] public string description;
    public Sprite portrait;
    [Tooltip("이 캐릭터로 플레이할 때 생성할 플레이어 프리팹 (PlayerController가 붙어 있어야 한다)")]
    public GameObject playerPrefab;

    [Tooltip("스킬 설명 4개 (순서: 기본기, 회피기, 특수기, 필살기)")]
    [TextArea] public string[] skillDescriptions = new string[4];
}
