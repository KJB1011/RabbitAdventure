using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 상단 메뉴 Tools → RabbitAdventure → 스킬 이펙트 프리팹 만들기
/// Cartoon FX Remaster(JMO Assets)의 이펙트를 복사해서 Resources/VFX에 게임용 프리팹을 만든다.
///
/// 게임용으로 바꾸는 것:
///  - 맨 위에 빈 오브젝트(PooledVFX)를 하나 두고 그 아래에 원본을 넣는다
///    → VFXManager가 회전을 덮어써도 원본 이펙트의 기울기는 그대로 유지된다
///  - CFXR_Effect의 "끝나면 스스로 삭제(Destroy)"를 끈다 → 풀(재사용)에 돌아갈 수 있게
///  - CFXR_Effect의 카메라 흔들림을 끈다 → 우리 게임의 흔들림(IngameManager.ShakeCamera)만 쓰도록
/// 이미 있는 프리팹은 덮어쓴다. 이 메뉴를 여러 번 눌러도 괜찮다.
/// </summary>
public static class SkillVfxBuilder
{
    // (게임에서 부르는 이름, 원본 이펙트 이름, 크기, 카메라 쪽으로 세우기, X축 기울이기, 재생 속도)
    // faceCamera: 바닥에 붙어 그려지는 파티클(땅 충격)을 화면 정면을 보게 바꾼다
    // tiltX: 바닥에 눕힌 마법진처럼 "모양 자체가 바닥에 있는" 이펙트는 오브젝트를 -90° 세워서 정면으로 보이게 한다
    // speed: 이펙트 전체 재생 속도 (2 = 두 배 빠르게 퍼지고 두 배 빨리 사라진다)
    // ※ 3D 바닥용이라 2D에서 거의 안 보이던 "Wind Trails", "Sword Trail (360 Spiral)"은 뺐다
    static readonly (string id, string source, float scale, bool faceCamera, float tiltX, float speed)[] Entries =
    {
        ("BlinkStartVFX", "CFXR Magic Poof", 0.4f, false, 0f, 2.2f),                 // 순간이동 출발, 탄막 반사 (연기가 빨리 사라지게)
        ("LightHitVFX",   "CFXR3 Hit Light B (Air)", 0.8f, false, 0f, 1f),           // 순간이동 도착, 칼끝 일격
        ("ParryVFX",      "CFXR4 Sword Hit PLAIN (Cross)", 1.6f, false, 0f, 1f),     // 받아치기 성공
        ("FlashVFX",      "CFXR Flash", 0.3f, false, 0f, 1f),                        // 받아치기 성공
        ("GroundHitVFX",  "CFXR2 Ground Hit", 0.8f, true, 0f, 1f),                   // 검 꽂기
        ("EmpowerHitVFX", "CFXR Hit D 3D (Yellow)", 0.55f, false, 0f, 1f),           // 도약의 일격 적중
        ("GaugeReadyVFX", "CFXR2 Shiny Item (Loop)", 1f, false, 0f, 1f),             // 투지 충전 가득
        ("MoonAura",      "CFXR3 Magic Aura A (Runic)", 0.7f, false, -90f, 1f),      // 달의 가호: 몸 주위 룬 마법진 (직접 만들고 지움)
        ("ChargeGlow",    "CFXR3 LightGlow A (Loop)", 1f, false, 0f, 1f),            // 모아베기 (직접 만들고 지움)
    };

    [MenuItem("Tools/RabbitAdventure/스킬 이펙트 프리팹 만들기")]
    public static void Build()
    {
        var all = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/JMO Assets" })
                               .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        int made = 0;
        foreach (var (id, source, scale, faceCamera, tiltX, speed) in Entries)
        {
            string srcPath = all.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == source);
            if (srcPath == null) { Debug.LogWarning($"[SkillVfxBuilder] 원본을 찾지 못했습니다: {source}"); continue; }

            var root = new GameObject(id);
            root.AddComponent<PooledVFX>();

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(srcPath));
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localScale *= scale;
            if (faceCamera) FaceCamera(inst);
            if (tiltX != 0f) inst.transform.localRotation = Quaternion.Euler(tiltX, 0f, 0f);
            if (!Mathf.Approximately(speed, 1f))
                foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.simulationSpeed *= speed;
                }

            MakePoolFriendly(root, disableShake: true);
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/VFX/{id}.prefab");
            Object.DestroyImmediate(root);
            made++;
        }

        // 기존 이펙트 중 CFXR에서 가져온 것(SpecialHit 등)도 풀에 돌아갈 수 있게 고친다
        // (카메라 흔들림은 원래 느낌 그대로 둔다)
        int fixedCount = 0;
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/VFX" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            if (MakePoolFriendly(contents, disableShake: false))
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                fixedCount++;
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillVfxBuilder] 이펙트 {made}개 생성, 기존 이펙트 {fixedCount}개 수정 완료");
    }

    // 바닥에 눕혀 그리는 이펙트를 화면 정면으로 세운다
    // - 오브젝트를 바닥(XZ)으로 눕혀 둔 회전을 없애고
    // - 파티클이 "바닥에 붙어서(Horizontal Billboard)" 그려지던 것을 "카메라를 보고(Billboard)" 그리게 바꾼다
    static void FaceCamera(GameObject go)
    {
        go.transform.localRotation = Quaternion.identity;
        foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (r.renderMode == ParticleSystemRenderMode.HorizontalBillboard)
                r.renderMode = ParticleSystemRenderMode.Billboard;
    }

    // CFXR_Effect의 "스스로 삭제"와 카메라 흔들림을 끈다. 하나라도 바꿨으면 true
    // (CFXR는 별도 어셈블리라 타입을 직접 쓰지 않고 SerializedObject로 이름을 찾아 바꾼다)
    static bool MakePoolFriendly(GameObject root, bool disableShake)
    {
        bool changed = false;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb.GetType().Name != "CFXR_Effect") continue;
            var so = new SerializedObject(mb);

            var clear = so.FindProperty("clearBehavior");
            if (clear != null && clear.enumValueIndex != 0) { clear.enumValueIndex = 0; changed = true; } // None

            var shake = so.FindProperty("cameraShake.enabled");
            if (disableShake && shake != null && shake.boolValue) { shake.boolValue = false; changed = true; }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return changed;
    }
}
