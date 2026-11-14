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
