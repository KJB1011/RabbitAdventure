using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// IngameManager Inspector 맨 아래에 디버그 버튼을 추가한다.
/// "Editor" 폴더 안의 스크립트는 에디터에서만 컴파일되고 빌드에는 들어가지 않는다.
/// </summary>
[CustomEditor(typeof(IngameManager))]
public class IngameManagerEditor : Editor
{
    const int GoldAmount = 50;
    const int MasteryAmount = 5;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // 원래 Inspector(SerializeField들)는 그대로 그린다

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("디버그", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 중에만 사용할 수 있습니다.", MessageType.Info);
            return;
        }

        var manager = (IngameManager)target;

        if (GUILayout.Button("플레이어 공격력 2배"))
            manager.DebugDoubleAtk();

        if (GUILayout.Button($"골드 +{GoldAmount}"))
            manager.DebugAddGold(GoldAmount);

        if (GUILayout.Button("무작위 아티팩트 얻기"))
            manager.DebugGiveRandomArtifact(FindAllArtifacts());

        // 스킬 숙련도 (전투 중에 가득 차면 전투가 끝난 뒤 강화 선택 창이 열린다)
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("스킬 숙련도", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("모든 숙련도 +25%"))
            manager.DebugAddMasteryAll(0.25f);
        if (GUILayout.Button("모든 숙련도 가득"))
            manager.DebugAddMasteryAll(1f);
        EditorGUILayout.EndHorizontal();
        MasteryButtons(manager, Defines.SKILL.MAIN, 20);
        MasteryButtons(manager, Defines.SKILL.SUB, MasteryAmount);
        MasteryButtons(manager, Defines.SKILL.SUPER, 2);
        MasteryButtons(manager, Defines.SKILL.SPECIAL, 1);
    }

    // 한 줄에 [○○ 숙련도 +n] [○○ 숙련도 가득]
    static void MasteryButtons(IngameManager manager, Defines.SKILL skill, int amount)
    {
        string name = SkillMasteryManager.SkillName(skill);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"{name} 숙련도 +{amount}"))
            manager.DebugAddMastery(skill, amount);
        if (GUILayout.Button($"{name} 숙련도 가득"))
            manager.DebugAddMastery(skill, 9999);
        EditorGUILayout.EndHorizontal();
    }

    // 프로젝트 안의 모든 ArtifactSO 에셋을 찾는다 → 새 아티팩트를 만들어도 따로 등록할 필요 없음
    static List<ArtifactSO> FindAllArtifacts()
    {
        var result = new List<ArtifactSO>();
        foreach (string guid in AssetDatabase.FindAssets("t:ArtifactSO"))
        {
            var artifact = AssetDatabase.LoadAssetAtPath<ArtifactSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (artifact != null) result.Add(artifact);
        }
        return result;
    }
}
