using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildOpeningScene
{
    const string ScenePath = "Assets/Scenes/Opening.unity";
    const string ArtMatFolder = "Assets/Materials/Art";

    const string WallDiffusePath = "Assets/Art/Textures/ConcreteWall006/concrete_wall_006_diff_2k.jpg";
    const string FloorDiffusePath = "Assets/Art/Textures/OldLinoleumFlooring01/old_linoleum_flooring_01_diff_2k.jpg";

    // Real PS1 hardware had no bump mapping, so our shader below has no normal map input —
    // the downloaded normal maps are unused by design, not an oversight.
    const string Ps1ShaderPath = "Assets/Shaders/PS1Lit.shader";

    const string DeskModelPath = "Assets/Art/Kenney_FurnitureKit/Models/desk.fbx";
    const string ChairModelPath = "Assets/Art/Kenney_FurnitureKit/Models/chair.fbx";
    const string LampModelPath = "Assets/Art/Kenney_FurnitureKit/Models/lampRoundTable.fbx";

    const string ClerkModelPath = "Assets/Art/Mixamo_NPC/Clerk_OrdinaryMan/ordinaryMan.fbx";
    const string ClerkTexturePath = "Assets/Art/Mixamo_NPC/Clerk_OrdinaryMan/OrdinaryManTexture_1024x1024.png";
    const string Waiting1ModelPath = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/AdultMan___.fbx";
    const string Waiting1TexturePath = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/ADULT_Texture_HIGHRES.jpg";
    const string Waiting2ModelPath = "Assets/Art/Mixamo_NPC/Waiting2_YoungMan/YoungMan___.fbx";
    const string Waiting2TexturePath = "Assets/Art/Mixamo_NPC/Waiting2_YoungMan/Texture_HIGHRES.png";
    const string IdleAnimPath = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/Idle.fbx";
    const string SittingAnimPath = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/SittingIdle.fbx";

    // These FBX files import at a raw scale far larger than real furniture; not fixable via import
    // unit settings (measured identical with useFileScale on/off). Correction factors are measured
    // bounds vs. plausible real-world sizes: desk ~1.2m wide, chair ~0.5m wide.
    const float DeskScaleCorrection = 1.2f / 7.34f;
    const float ChairScaleCorrection = 0.5f / 2.00f;
    const float LampScaleCorrection = DeskScaleCorrection;

    const float RoomWidth = 7f;  // X
    const float RoomDepth = 5f;  // Z
    const float WallHeight = 3f;
    const float WallThickness = 0.2f;
    const float DoorWidth = 1.2f;

    [MenuItem("Metropoliten/Build Opening Scene (Registry Desk)")]
    public static void Build()
    {
        if (File.Exists(ScenePath) && !Application.isBatchMode)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Opening scene already exists",
                $"{ScenePath} already exists. Re-running this will replace it and any hand edits will be lost.",
                "Overwrite", "Cancel");
            if (!overwrite) return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ConfigureModelImport(DeskModelPath);
        ConfigureModelImport(ChairModelPath);
        ConfigureModelImport(LampModelPath);
        ConfigureHumanoidRig(ClerkModelPath);
        ConfigureHumanoidRig(Waiting1ModelPath);
        ConfigureHumanoidRig(Waiting2ModelPath);
        ConfigureHumanoidRig(IdleAnimPath);
        ConfigureHumanoidRig(SittingAnimPath);

        Material wallMat = CreatePs1Mat("Wall", Color.white, WallDiffusePath, new Vector2(2f, 1.5f));
        Material ceilingMat = CreatePs1Mat("Ceiling", Color.white, WallDiffusePath, new Vector2(3.5f, 2.5f));
        Material floorMat = CreatePs1Mat("Floor", Color.white, FloorDiffusePath, new Vector2(3.5f, 2.5f));
        Material counterMat = CreatePs1Mat("Counter", new Color(0.40f, 0.32f, 0.22f), null, Vector2.one);
        Material chairMat = CreatePs1Mat("Chair", new Color(0.25f, 0.30f, 0.28f), null, Vector2.one);
        Material lampMat = CreatePs1Mat("Lamp", new Color(0.75f, 0.65f, 0.35f), null, Vector2.one);
        Material clerkMat = CreatePs1Mat("Clerk", Color.white, ClerkTexturePath, Vector2.one);
        Material waiting1Mat = CreatePs1Mat("Waiting1", Color.white, Waiting1TexturePath, Vector2.one);
        Material waiting2Mat = CreatePs1Mat("Waiting2", Color.white, Waiting2TexturePath, Vector2.one);

        AnimationClip idleClip = ExtractAnimationClip(IdleAnimPath);
        AnimationClip sittingClip = ExtractAnimationClip(SittingAnimPath);
        RuntimeAnimatorController idleController = CreateLoopingAnimatorController("Idle", idleClip);
        RuntimeAnimatorController sittingController = CreateLoopingAnimatorController("Sitting", sittingClip);

        Transform structure = new GameObject("Structure").transform;
        Transform furniture = new GameObject("Furniture").transform;
        Transform npcs = new GameObject("NPCs").transform;
        Transform lighting = new GameObject("Lighting").transform;
        Transform triggers = new GameObject("Triggers").transform;

        BuildShell(structure, wallMat, floorMat, ceilingMat);
        BuildCounter(furniture, counterMat, lampMat);
        GameObject[] chairs = BuildWaitingChairs(furniture, chairMat);
        BuildNpcs(npcs, chairs, clerkMat, waiting1Mat, waiting2Mat, idleController, sittingController);
        BuildLighting(lighting);
        BuildExitTrigger(triggers);
        BuildPlayer();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.24f);

        // Native URP fog — the plugin's custom Fog render feature only implements the old
        // (now hidden/unsupported in 6.3) rendering API, so it's inert under Render Graph.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.35f, 0.40f, 0.28f);
        RenderSettings.fogDensity = 0.04f;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddToBuildSettings(ScenePath);

        Debug.Log($"Built {ScenePath} and added it to Build Settings.");
    }

    [MenuItem("Metropoliten/Add Opening Scene To Build Settings")]
    public static void AddOpeningToBuildSettingsMenu()
    {
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Added Opening scene to Build Settings.");
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void BuildShell(Transform parent, Material wallMat, Material floorMat, Material ceilingMat)
    {
        CreateBox("Floor", new Vector3(RoomWidth / 2, -0.05f, RoomDepth / 2),
            new Vector3(RoomWidth, 0.1f, RoomDepth), floorMat, parent);

        CreateBox("Ceiling", new Vector3(RoomWidth / 2, WallHeight + 0.05f, RoomDepth / 2),
            new Vector3(RoomWidth, 0.1f, RoomDepth), ceilingMat, parent);

        // South wall: entrance gap, centered on X, player already came through it
        float doorHalf = DoorWidth / 2;
        float southSeg1Width = RoomWidth / 2 - doorHalf;
        CreateBox("Wall_South_A", new Vector3(southSeg1Width / 2, WallHeight / 2, 0),
            new Vector3(southSeg1Width, WallHeight, WallThickness), wallMat, parent);
        CreateBox("Wall_South_B", new Vector3(RoomWidth - southSeg1Width / 2, WallHeight / 2, 0),
            new Vector3(southSeg1Width, WallHeight, WallThickness), wallMat, parent);

        CreateBox("Wall_North", new Vector3(RoomWidth / 2, WallHeight / 2, RoomDepth),
            new Vector3(RoomWidth, WallHeight, WallThickness), wallMat, parent);

        CreateBox("Wall_West", new Vector3(0, WallHeight / 2, RoomDepth / 2),
            new Vector3(WallThickness, WallHeight, RoomDepth), wallMat, parent);

        // East wall: exit gap, centered on Z, leads to the station
        float eastSeg1Depth = RoomDepth / 2 - doorHalf;
        CreateBox("Wall_East_A", new Vector3(RoomWidth, WallHeight / 2, eastSeg1Depth / 2),
            new Vector3(WallThickness, WallHeight, eastSeg1Depth), wallMat, parent);
        CreateBox("Wall_East_B", new Vector3(RoomWidth, WallHeight / 2, RoomDepth - eastSeg1Depth / 2),
            new Vector3(WallThickness, WallHeight, eastSeg1Depth), wallMat, parent);
    }

    static void BuildCounter(Transform parent, Material counterMat, Material lampMat)
    {
        float z = RoomDepth - 1f;

        GameObject desk = InstantiateModel(DeskModelPath, parent, "Counter_Desk");
        ApplyMaterial(desk, counterMat);
        desk.transform.localScale = Vector3.one * DeskScaleCorrection;
        PlaceOnSurface(desk, RoomWidth / 2, z, 0f, 180f);
        Bounds deskBounds = GetRendererBounds(desk);

        GameObject lamp = InstantiateModel(LampModelPath, parent, "Counter_Lamp");
        ApplyMaterial(lamp, lampMat);
        lamp.transform.localScale = Vector3.one * LampScaleCorrection;
        PlaceOnSurface(lamp, RoomWidth / 2 - 0.4f, z - 0.15f, deskBounds.max.y, 0f);
    }

    static GameObject[] BuildWaitingChairs(Transform furnitureParent, Material chairMat)
    {
        float x = 0.7f;
        float[] chairZ = { 1f, 2f, 3f, 4f };
        GameObject[] chairs = new GameObject[chairZ.Length];
        for (int i = 0; i < chairZ.Length; i++)
        {
            GameObject chair = InstantiateModel(ChairModelPath, furnitureParent, $"Chair_{i + 1}");
            ApplyMaterial(chair, chairMat);
            chair.transform.localScale = Vector3.one * ChairScaleCorrection;
            PlaceOnSurface(chair, x, chairZ[i], 0f, 270f);
            chairs[i] = chair;
        }
        return chairs;
    }

    static void BuildNpcs(Transform parent, GameObject[] chairs, Material clerkMat, Material waiting1Mat, Material waiting2Mat,
        RuntimeAnimatorController idleController, RuntimeAnimatorController sittingController)
    {
        GameObject clerk = InstantiateModel(ClerkModelPath, parent, "NPC_Clerk");
        ApplyMaterial(clerk, clerkMat);
        SetAnimator(clerk, idleController);
        PlaceCharacter(clerk, RoomWidth / 2, RoomDepth - 0.4f, 0f, 180f, 1.8f);

        // Anchored to each chair's actual rendered bounds (x/z), not the nominal grid position —
        // same pivot-offset issue we already fixed once for the capsule placeholders (the chair
        // mesh's pivot isn't centered, so rotating it shifts its visual position away from its
        // transform). Seat height is a fixed real-world estimate (~0.45m), not derived from the
        // chair bounds — chair1Bounds.min.y is the floor contact point, not the seat surface,
        // which is what put the previous attempt at floor level instead of seated on the chair.
        const float seatHeight = 0.45f;

        Bounds chair1Bounds = GetRendererBounds(chairs[0]);
        GameObject waiting1 = InstantiateModel(Waiting1ModelPath, parent, "NPC_Waiting_1");
        ApplyMaterial(waiting1, waiting1Mat);
        SetAnimator(waiting1, sittingController);
        PlaceCharacter(waiting1, chair1Bounds.center.x, chair1Bounds.center.z, seatHeight, 90f, 1.85f);

        Bounds chair2Bounds = GetRendererBounds(chairs[2]);
        GameObject waiting2 = InstantiateModel(Waiting2ModelPath, parent, "NPC_Waiting_2");
        ApplyMaterial(waiting2, waiting2Mat);
        SetAnimator(waiting2, sittingController);
        PlaceCharacter(waiting2, chair2Bounds.center.x, chair2Bounds.center.z, seatHeight, 90f, 1.75f);
    }

    static void SetAnimator(GameObject character, RuntimeAnimatorController controller)
    {
        Animator animator = character.GetComponent<Animator>();
        if (animator == null) animator = character.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
    }

    // Scales to targetHeight (measured in bind pose) and grounds the character at world Y = groundY
    // (floor level for standing, the chair's own seat height for seated NPCs).
    static void PlaceCharacter(GameObject character, float x, float z, float groundY, float rotationY, float targetHeight)
    {
        character.transform.position = Vector3.zero;
        character.transform.rotation = Quaternion.identity;
        character.transform.localScale = Vector3.one;
        Bounds natural = GetRendererBounds(character);
        float scale = targetHeight / Mathf.Max(natural.size.y, 0.01f);
        character.transform.localScale = Vector3.one * scale;

        Bounds scaled = GetRendererBounds(character);
        float offset = groundY - scaled.min.y;
        character.transform.position = new Vector3(x, offset, z);
        character.transform.rotation = Quaternion.Euler(0, rotationY, 0);
    }

    static void BuildLighting(Transform parent)
    {
        Color sickly = new Color(0.75f, 0.85f, 0.55f);
        CreatePointLight("CeilingLight_1", new Vector3(2f, WallHeight - 0.2f, 2.5f), sickly, parent);
        CreatePointLight("CeilingLight_2", new Vector3(5f, WallHeight - 0.2f, 2.5f), sickly, parent);

        // Dim fill — not a diegetic sun, just a safety net so the custom PSX lighting subgraph
        // (which appears not to read ambient/light-probe contribution) doesn't leave anything
        // fully black outside the point lights' falloff.
        GameObject sunGo = new GameObject("DimFillDirectional");
        sunGo.transform.SetParent(parent);
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.5f, 0.55f, 0.45f);
        sun.intensity = 0.4f;
    }

    static void CreatePointLight(string name, Vector3 position, Color color, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 26f;
        light.range = 10f;
    }

    static void BuildExitTrigger(Transform parent)
    {
        GameObject go = new GameObject("ExitTrigger_ToStation");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(RoomWidth, WallHeight / 2, RoomDepth / 2);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.6f, WallHeight, DoorWidth);
        var trigger = go.AddComponent<SceneTransitionTrigger>();
        trigger.targetSceneName = "Shift1";
    }

    static void BuildPlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(RoomWidth / 2, 1f, 1f);

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.5f;
        capsule.enabled = false; // matches SampleScene setup: CharacterController handles movement collisions

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;

        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(player.transform);
        cameraGo.transform.localPosition = new Vector3(0, 0.6f, 0);
        Camera cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.02f);
        cameraGo.AddComponent<AudioListener>();
        cameraGo.AddComponent<PixelatedRenderer>().targetHeight = 240;

        FirstPersonController fpc = player.AddComponent<FirstPersonController>();
        fpc.cameraTransform = cameraGo.transform;
    }

    static void ConfigureModelImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        if (importer.useFileScale)
        {
            importer.useFileScale = false;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
        }
    }

    static void ConfigureHumanoidRig(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }
        if (importer.useFileScale)
        {
            importer.useFileScale = false;
            importer.globalScale = 1f;
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    static AnimationClip ExtractAnimationClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        Debug.LogError($"No animation clip found in {fbxPath}");
        return null;
    }

    static RuntimeAnimatorController CreateLoopingAnimatorController(string name, AnimationClip clip)
    {
        string path = $"{ArtMatFolder}/AC_{name}.controller";
        if (clip != null)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        Directory.CreateDirectory(ArtMatFolder);
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddMotion(clip);
        return controller;
    }

    static Material CreatePs1Mat(string name, Color tint, string diffusePath, Vector2 tiling)
    {
        Directory.CreateDirectory(ArtMatFolder);
        string path = $"{ArtMatFolder}/M_{name}.mat";
        Texture2D diffuse = diffusePath != null
            ? AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath)
            : GetOrCreateSolidTexture(name, tint);

        Shader ps1Shader = AssetDatabase.LoadAssetAtPath<Shader>(Ps1ShaderPath);
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        Material mat = existing != null ? existing : new Material(ps1Shader);
        mat.shader = ps1Shader; // guards against stale materials left on an old shader from a previous run

        mat.SetTexture("_BaseMap", diffuse);
        mat.SetTextureScale("_BaseMap", tiling);
        mat.SetColor("_BaseColor", Color.white); // color always comes from the texture (real or solid-generated)
        mat.SetFloat("_VertexGridSize", 256f);
        mat.SetFloat("_ColorLevels", 24f);
        // Off for now: vertex jitter only looks right on dense geometry (many small triangles).
        // Our room is built from single flat boxes (2 tris/face), so snapping the sparse corner
        // vertices independently warps the whole face instead of a subtle wobble. Revisit once
        // the level geometry is properly tessellated, not a tuning problem.
        mat.SetFloat("_JitterEnabled", 0f);
        mat.SetFloat("_PosterizeEnabled", 1f);

        if (existing == null) AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Texture2D GetOrCreateSolidTexture(string name, Color color)
    {
        Directory.CreateDirectory(ArtMatFolder);
        string path = $"{ArtMatFolder}/T_{name}.asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply(true);
        AssetDatabase.CreateAsset(tex, path);
        return tex;
    }

    static GameObject InstantiateModel(string assetPath, Transform parent, string name)
    {
        GameObject prefabSource = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, parent);
        instance.name = name;
        return instance;
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
    }

    static Bounds GetRendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // Positions go so the bottom of its renderer bounds sits at world Y = surfaceY
    static void PlaceOnSurface(GameObject go, float x, float z, float surfaceY, float rotationY)
    {
        go.transform.position = new Vector3(x, surfaceY, z);
        go.transform.rotation = Quaternion.Euler(0, rotationY, 0);
        Bounds b = GetRendererBounds(go);
        float offset = surfaceY - b.min.y;
        go.transform.position += new Vector3(0, offset, 0);
    }

    static void CreateBox(string name, Vector3 center, Vector3 size, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
