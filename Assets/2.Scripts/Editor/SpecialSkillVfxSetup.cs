using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// [에디터 전용 도구] 필살기 연출용 파티클 3종을 한 번에 만들어 연결한다.
/// 메뉴: Tools > RabbitAdventure > 필살기 파티클 세팅
///
/// 하는 일
///   1. 필살기 연출 Canvas를 Screen Space - Camera로 변경 (파티클이 캡처 화면 위에 보이도록)
///   2. 파티클 3종 생성 (같은 이름이 있으면 지우고 새로 만든다 → 여러 번 실행해도 안전)
///      ① VFX_HeadSparks : 칼끝에서 뒤로 튀는 불꽃
///      ② VFX_LineGlitter: 베인 자국을 따라 피어오르는 빛
///      ③ VFX_GlassShards: 화면이 깨질 때 튀는 유리 파편
///   3. SpecialSkillEffect의 파티클 칸에 자동 연결
///
/// 크기·속도는 월드 단위 기준이다 (카메라 Orthographic Size 5.4 → 화면 높이 10.8).
/// 만든 뒤에는 각 파티클을 선택해서 Inspector에서 자유롭게 조정하면 된다.
/// </summary>
public static class SpecialSkillVfxSetup
{
    const string Title = "필살기 파티클 세팅";
    const int CanvasSortingOrder = 100;   // 연출 Canvas
    const int ParticleSortingOrder = 110; // 파티클은 Canvas(캡처 화면)보다 앞에

    [MenuItem("Tools/RabbitAdventure/필살기 파티클 세팅")]
    static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(Title, "플레이 모드를 끄고 실행해 주세요.", "확인");
            return;
        }

        var effect = Object.FindFirstObjectByType<SpecialSkillEffect>(FindObjectsInactive.Include);
        if (effect == null)
        {
            EditorUtility.DisplayDialog(Title, "열린 씬에서 SpecialSkillEffect를 찾지 못했습니다.\nIngameScene을 열고 다시 실행해 주세요.", "확인");
            return;
        }

        var canvas = effect.GetComponent<Canvas>();
        var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        if (canvas == null || cam == null)
        {
            EditorUtility.DisplayDialog(Title, "연출 Canvas 또는 카메라를 찾지 못했습니다.", "확인");
            return;
        }

        // 1. Canvas 모드 변경
        Undo.RecordObject(canvas, Title);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f; // 카메라 바로 앞 (월드보다 앞, Near Clip 0.3보다 뒤)
        canvas.sortingOrder = CanvasSortingOrder;

        // HitSpark와 같은 머티리얼을 써서 URP 2D에서 확실히 보이게 한다
        Material mat = LoadSparkMaterial();
        int layerId = canvas.sortingLayerID;

        // 2. 파티클 생성 (위치·각도·폭은 런타임에 SpecialSkillEffect가 선에 맞춰 잡는다)
        var root = effect.transform;
        var sparks = CreateOrReplace(root, "VFX_HeadSparks");
        ConfigureHeadSparks(sparks, mat, layerId);

        var glitter = CreateOrReplace(root, "VFX_LineGlitter");
        ConfigureLineGlitter(glitter, mat, layerId);

        var shards = CreateOrReplace(root, "VFX_GlassShards");
        ConfigureGlassShards(shards, mat, layerId);

        // 3. SpecialSkillEffect에 연결
        var so = new SerializedObject(effect);
        so.FindProperty("_headSparks").objectReferenceValue = sparks;
        so.FindProperty("_lineGlitter").objectReferenceValue = glitter;
        so.FindProperty("_glassShards").objectReferenceValue = shards;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(effect.gameObject.scene);
        Selection.activeGameObject = effect.gameObject;

        EditorUtility.DisplayDialog(Title,
            "완료했습니다.\n\n" +
            "- 연출 Canvas: Screen Space - Camera\n" +
            "- 파티클 3종 생성 및 연결\n\n" +
            "씬을 저장(Ctrl+S)한 뒤 필살기를 테스트해 보세요." +
            (mat == null ? "\n\n※ HitSpark 머티리얼을 찾지 못했습니다. 파티클 Renderer에 머티리얼을 직접 넣어 주세요." : ""),
            "확인");
    }

    // ── ① 칼끝 불꽃: 칼끝을 따라가며 뒤쪽으로 짧게 튀는 선 모양 불꽃 ─────────────
    static void ConfigureHeadSparks(ParticleSystem ps, Material mat, int layerId)
    {
        var main = Common(ps, mat, layerId, loop: true);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 350f;

        // Cone은 로컬 +Z로 뿜는다 → Y축 -90° 회전으로 진행 방향(+X)의 반대쪽을 향하게
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(0f, -90f, 0f);

        ShrinkAndFade(ps);

        // 속도 방향으로 늘어난 선 모양 → "튀는" 느낌
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.04f;
        r.lengthScale = 1f;
    }

    // ── ② 베인 자국 빛: 선 전체에서 작은 빛 입자가 천천히 피어오름 ──────────────
    static void ConfigureLineGlitter(ParticleSystem ps, Material mat, int layerId)
    {
        var main = Common(ps, mat, layerId, loop: true);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 80f;

        // Edge: 로컬 X축을 따라 선 모양으로 생성, 로컬 Y(선의 수직) 방향으로 나아간다
        // radius(선 길이의 절반)는 런타임에 선 길이에 맞춰 바뀐다
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 5f;
        shape.randomDirectionAmount = 0.3f;

        ShrinkAndFade(ps);
    }

    // ── ③ 유리 파편: 경계선에서 한 번에 터져 사방으로 날아가며 떨어짐 ─────────────
    static void ConfigureGlassShards(ParticleSystem ps, Material mat, int layerId)
    {
        var main = Common(ps, mat, layerId, loop: false);
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(0.85f, 0.95f, 1f, 1f); // 살짝 푸른 유리빛
        main.gravityModifier = 1.5f;
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)60) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 5f;
        shape.randomDirectionAmount = 1f; // 사방으로

        // 날아가며 빙글빙글 돈다
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2f, Mathf.PI * 2f);

        ShrinkAndFade(ps);
    }

    // ── 공통 ──────────────────────────────────────────────────
    static ParticleSystem.MainModule Common(ParticleSystem ps, Material mat, int layerId, bool loop)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;                                   // SpecialSkillEffect가 직접 재생
        main.loop = loop;
        main.duration = 1f;
        main.useUnscaledTime = true;                                // timeScale = 0인 연출 중에도 움직이게
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 이미 생긴 입자는 제자리에 남게
        main.scalingMode = ParticleSystemScalingMode.Local;         // 아주 작은 Canvas 스케일의 영향을 받지 않게
        main.startColor = Color.white;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) r.sharedMaterial = mat;
        r.sortingLayerID = layerId;
        r.sortingOrder = ParticleSortingOrder;

        return main;
    }

    // 수명 동안 작아지고 투명해진다
    static void ShrinkAndFade(ParticleSystem ps)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    static ParticleSystem CreateOrReplace(Transform parent, string name)
    {
        var old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, Title);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go.AddComponent<ParticleSystem>(); // ParticleSystemRenderer도 자동으로 붙는다
    }

    static Material LoadSparkMaterial()
    {
        var prefab = Resources.Load<GameObject>("VFX/HitSpark");
        if (prefab == null) return null;
        var r = prefab.GetComponentInChildren<ParticleSystemRenderer>(true);
        return r != null ? r.sharedMaterial : null;
    }
}
