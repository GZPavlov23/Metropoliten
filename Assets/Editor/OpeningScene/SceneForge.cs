using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// What every generated scene is made with: one mesh per material, baked lights, stains, the player, the bake
// and the on-screen text. A builder calls Begin, fills the kits, then commits them.
public static class SceneForge
{
    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const float PlayerWalkSpeed = 2.2f, PlayerSprintSpeed = 3.6f;

    public static readonly Vector2 PaverTile = new Vector2(4f, 4f), BrickTile = new Vector2(2.4f, 2.4f);
    public static readonly Vector2 PlasterTile = new Vector2(3f, 3f), OchreTile = new Vector2(3.4f, 3.4f);
    public static readonly Vector2 StoneTile = new Vector2(1.6f, 1.6f), WoodTile = new Vector2(1.2f, 1.2f);
    public static readonly Vector2 MetalTile = new Vector2(1f, 1f), PaperTile = new Vector2(0.5f, 0.5f);

    public static readonly Color TubeColor = new Color(1f, 0.98f, 0.93f);
    public static readonly Color LampColor = new Color(3.2f, 3.1f, 2.8f);

    // Trims are only centimetres wide: at the scene's density they fall under a texel and pick up stray colour.
    // The counter is simply looked at from close up for a whole conversation.
    static readonly Dictionary<string, float> FineLightmaps = new Dictionary<string, float> { { "Stone", 3f }, { "Counter", 3f } };

    public static string RootDir { get; private set; }
    public static string MeshDir => RootDir + "/Meshes";
    public static string MaterialDir => RootDir + "/Materials";

    public static MeshKit glow, faultyGlow;

    static Dictionary<string, (string texture, Color tint)> surfaces;
    static Dictionary<string, float> lightmapScales;
    static HashSet<string> realtimeLit;
    static Dictionary<string, MeshKit> kits;

    // Scene-wide lightmap scales are merged over the defaults. Baked Lit ignores real-time lights,
    // so any surface one has to reach is named in realtimeLitSurfaces and gets Simple Lit instead.
    public static void Begin(string rootDir, Dictionary<string, (string texture, Color tint)> sceneSurfaces,
        Dictionary<string, float> sceneLightmapScales = null, IEnumerable<string> realtimeLitSurfaces = null)
    {
        RootDir = rootDir;
        surfaces = sceneSurfaces;
        lightmapScales = new Dictionary<string, float>(FineLightmaps);
        foreach (KeyValuePair<string, float> scale in sceneLightmapScales ?? new Dictionary<string, float>()) lightmapScales[scale.Key] = scale.Value;
        realtimeLit = new HashSet<string>(realtimeLitSurfaces ?? new string[0]);
        kits = new Dictionary<string, MeshKit>();
        glow = new MeshKit();
        faultyGlow = new MeshKit();
        foreach (string directory in new[] { MeshDir, MaterialDir }) Directory.CreateDirectory(directory);
    }

    public static MeshKit Kit(string name)
    {
        if (!kits.TryGetValue(name, out MeshKit kit)) kits[name] = kit = new MeshKit();
        return kit;
    }

    // A flat wall running along Z at this x, seen from the side facing (+1 or -1 on X).
    // Holes are given in world Z and Y; reveal, if given, lines each hole back through the wall.
    public static void WallAlongZ(MeshKit kit, float x, float facing, float zStart, float zEnd, float yMin, float yMax, IEnumerable<Rect> holes, Vector2 tile, MeshKit reveal = null, float depth = 0f)
    {
        bool forward = facing < 0f;
        Rect[] local = holes.Select(h => new Rect(forward ? h.xMin - zStart : zEnd - h.xMax, h.yMin - yMin, h.width, h.height)).ToArray();
        var origin = new Vector3(x, yMin, forward ? zStart : zEnd);
        Vector3 along = forward ? Vector3.forward : Vector3.back;
        kit.WallWithHoles(origin, along, Vector3.up, zEnd - zStart, yMax - yMin, local, tile);
        if (reveal == null) return;
        foreach (Rect hole in local) reveal.Reveal(origin, along, Vector3.up, hole, Vector3.right * (-Mathf.Sign(facing) * depth), tile);
    }

    // The same for a wall running along X at this z, seen from the side facing (+1 or -1 on Z).
    public static void WallAlongX(MeshKit kit, float z, float facing, float xStart, float xEnd, float yMin, float yMax, IEnumerable<Rect> holes, Vector2 tile, MeshKit reveal = null, float depth = 0f)
    {
        bool right = facing > 0f;
        Rect[] local = holes.Select(h => new Rect(right ? h.xMin - xStart : xEnd - h.xMax, h.yMin - yMin, h.width, h.height)).ToArray();
        var origin = new Vector3(right ? xStart : xEnd, yMin, z);
        Vector3 along = right ? Vector3.right : Vector3.left;
        kit.WallWithHoles(origin, along, Vector3.up, xEnd - xStart, yMax - yMin, local, tile);
        if (reveal == null) return;
        foreach (Rect hole in local) reveal.Reveal(origin, along, Vector3.up, hole, Vector3.forward * (-Mathf.Sign(facing) * depth), tile);
    }

    public static void HangingTube(Transform lights, ArchProfile arch, Vector3 center, float intensity, MeshKit tube, float length = 1.3f)
    {
        Kit("Metal").Box(center + Vector3.up * 0.05f, new Vector3(0.2f, 0.08f, length + 0.06f), MetalTile);
        tube.Box(center, new Vector3(0.14f, 0.02f, length), Vector2.one);
        foreach (float offset in new[] { -0.45f, 0.45f })
        {
            Vector3 top = center + new Vector3(0f, 0.09f, offset);
            Kit("Steel").Tube(top, new Vector3(top.x, arch.HeightAt(top.x), top.z), 0.012f, 5, MetalTile);
        }

        AreaLight(lights, "Tube", center + Vector3.down * 0.015f, new Vector2(0.16f, length), TubeColor, intensity);

        // The glow a bare tube throws back onto the vault, soft enough that the housing casts no hard shadow.
        Light spill = PointLight(lights, "TubeSpill", center + Vector3.down * 0.25f, TubeColor, intensity * 0.1f, 9f);
        spill.shapeRadius = 0.3f;
    }

    public static Light BakedLight(Transform parent, string name, LightType type, Vector3 position, Quaternion rotation, Color color, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(position + MeshKit.Origin, rotation);
        var light = go.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.lightmapBakeType = LightmapBakeType.Baked;
        light.shadows = LightShadows.Soft;
        light.bounceIntensity = 1.8f;
        return light;
    }

    public static void AreaLight(Transform parent, string name, Vector3 position, Vector2 size, Color color, float intensity)
    {
        Light light = BakedLight(parent, name, LightType.Rectangle, position, Quaternion.Euler(90f, 0f, 0f), color, intensity);
        light.areaSize = size;
        light.range = 16f;
    }

    public static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        Light light = BakedLight(parent, name, LightType.Point, position, Quaternion.identity, color, intensity);
        light.range = range;
        light.shapeRadius = 0.08f;
        return light;
    }

    public static Material LoadOrCreateMaterial(string name, string shader)
    {
        string path = $"{MaterialDir}/M_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find(shader);
        return material;
    }

    public static Material SurfaceMaterial(string name)
    {
        (string texture, Color tint) surface = surfaces.TryGetValue(name, out var known) ? known : (name, Color.white);
        string shader = realtimeLit.Contains(name) ? "Universal Render Pipeline/Simple Lit" : "Universal Render Pipeline/Baked Lit";
        Material material = LoadOrCreateMaterial(name, shader);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(surface.texture)));
        material.SetColor("_BaseColor", surface.tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Lays a multiplying stain just off a surface. V runs from the base of the stain to its faded edge.
    public static void Stain(MeshKit kit, Vector3 corner, Vector3 along, Vector3 up, Vector3 facing, float repeat)
    {
        corner += facing * 0.003f;
        float uStart = Mathf.Abs(corner.x * 0.37f + corner.z * 0.61f) % 1f;
        if (Vector3.Dot(Vector3.Cross(along, up), facing) < 0f)
        {
            corner += along;
            along = -along;
            kit.Quad(corner, corner + along, corner + along + up, corner + up,
                new Vector2(uStart + repeat, 0f), new Vector2(uStart, 0f), new Vector2(uStart, 1f), new Vector2(uStart + repeat, 1f));
            return;
        }
        kit.Quad(corner, corner + along, corner + along + up, corner + up,
            new Vector2(uStart, 0f), new Vector2(uStart + repeat, 0f), new Vector2(uStart + repeat, 1f), new Vector2(uStart, 1f));
    }

    // A soft patch that follows the curve of a vault.
    public static void VaultStain(MeshKit kit, ArchProfile arch, float xMin, float xMax, float zMin, float zMax)
    {
        const int strips = 10;
        for (int i = 0; i < strips; i++)
        {
            float u0 = (float)i / strips, u1 = (i + 1f) / strips;
            float x0 = Mathf.Lerp(xMin, xMax, u0), x1 = Mathf.Lerp(xMin, xMax, u1);
            float y0 = arch.HeightAt(x0) - 0.004f, y1 = arch.HeightAt(x1) - 0.004f;
            kit.Quad(new Vector3(x0, y0, zMin), new Vector3(x1, y1, zMin), new Vector3(x1, y1, zMax), new Vector3(x0, y0, zMax),
                new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f));
        }
    }

    public static void CommitStains(string name, string texture, MeshKit kit, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        Mesh mesh = kit.ToMesh(name);
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{name}.asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = LoadOrCreateMaterial(texture, "Metropoliten/Grime");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(texture)));
        EditorUtility.SetDirty(material);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.lightProbeUsage = LightProbeUsage.Off;
    }

    public static Mesh SaveMesh(MeshKit kit, string name)
    {
        Mesh mesh = kit.ToMesh(name);
        Unwrapping.GenerateSecondaryUVSet(mesh);
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{name}.asset");
        return mesh;
    }

    // Kits named Train go on the moving parent, where they still get lightmaps but can't be batched into the scene.
    public static void CommitSurfaces(Transform parent, Transform moving = null)
    {
        foreach (KeyValuePair<string, MeshKit> kit in kits)
        {
            bool moves = moving != null && kit.Key.StartsWith("Train");
            CommitSurface(kit.Key, kit.Value, moves ? moving : parent, moves);
        }
    }

    static void CommitSurface(string name, MeshKit kit, Transform parent, bool moves)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        Mesh mesh = SaveMesh(kit, name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var surface = go.AddComponent<MeshRenderer>();
        surface.sharedMaterial = SurfaceMaterial(name);

        if (lightmapScales.TryGetValue(name, out float scale))
        {
            var serialized = new SerializedObject(surface);
            serialized.FindProperty("m_ScaleInLightmap").floatValue = scale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Kits named after a texture are signs, notices and paper: nothing to bump into.
        if (!name.StartsWith("T_")) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(go, moves ? StaticEditorFlags.ContributeGI : StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
    }

    public static GameObject CommitUnlit(string name, MeshKit kit, Color color, Transform parent)
    {
        Material material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);

        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit, name);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    public static void CommitGlow(Transform parent)
    {
        CommitUnlit("LampGlow", glow, LampColor, parent);
        CommitUnlit("LampGlow_Faulty", faultyGlow, LampColor, parent).AddComponent<LampFlicker>();
    }

    public static FirstPersonController BuildPlayer(Vector3 position)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;

        // Shorter than the person, because the controller lifts itself by the step offset on every move:
        // at full height it would scrape every door lintel and stall the player underneath.
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.6f;
        controller.center = Vector3.down * 0.1f;
        controller.radius = 0.28f;
        controller.stepOffset = 0.2f;

        Camera camera = NewCamera(player.transform);
        camera.transform.localPosition = new Vector3(0f, 0.68f, 0f);

        var controls = player.AddComponent<FirstPersonController>();
        controls.cameraTransform = camera.transform;
        controls.walkSpeed = PlayerWalkSpeed;
        controls.sprintSpeed = PlayerSprintSpeed;
        return controls;
    }

    public static Camera NewCamera(Transform parent)
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.SetParent(parent);
        var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 60f;
        camera.allowMSAA = false;
        go.AddComponent<AudioListener>();
        return camera;
    }

    public static AudioClip Sound(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundForge.SoundDir}/{name}.wav");
        if (clip == null) throw new FileNotFoundException($"{name} hasn't been forged yet: run Metropoliten > Forge Sounds.");
        return clip;
    }

    public static AudioClip[] Sounds(string name, int count)
    {
        var clips = new AudioClip[count];
        for (int i = 0; i < count; i++) clips[i] = Sound($"{name}{i}");
        return clips;
    }

    // A sound from one spot in the scene, at full strength within near metres and gone by far.
    public static AudioSource Emitter(Transform parent, string name, Vector3 position, AudioClip clip, float volume, float near, float far)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = near;
        source.maxDistance = far;
        return source;
    }

    // One that's always going.
    public static AudioSource Ambient(Transform parent, string name, Vector3 position, AudioClip clip, float volume, float near, float far)
    {
        AudioSource source = Emitter(parent, name, position, clip, volume, near, far);
        source.loop = true;
        source.gameObject.AddComponent<AmbientLoop>();
        return source;
    }

    // The sound of the air in a room, the same wherever the player stands in it.
    public static void RoomTone(Transform parent, float volume)
    {
        AudioSource source = Ambient(parent, "RoomTone", Vector3.zero, Sound("A_Room"), volume, 1f, 2f);
        source.spatialBlend = 0f;
        source.GetComponent<AmbientLoop>().pitchSpread = 0f;
    }

    public static void ReverbZone(Transform parent, string name, Vector3 center, AudioReverbPreset preset, float inner, float outer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = center;
        var zone = go.AddComponent<AudioReverbZone>();
        zone.reverbPreset = preset;
        zone.minDistance = inner;
        zone.maxDistance = outer;
    }

    // The player's feet, and the tube that stutters, once there's sound to give them.
    public static void BuildPlayerSound(FirstPersonController player, int walking, int running)
    {
        AudioSource feet = Emitter(player.transform, "Feet", player.transform.position + Vector3.down * 0.9f, null, 1f, 1f, 2f);
        feet.spatialBlend = 0f;

        var footsteps = player.gameObject.AddComponent<Footsteps>();
        footsteps.player = player;
        footsteps.source = feet;
        footsteps.walking = Sounds("F_Walk", walking);
        footsteps.running = Sounds("F_Run", running);
    }

    public static void BuildTubeSound(Transform parent, Vector3 tube, float near, float far)
    {
        AudioSource hum = Ambient(parent, "FaultyTube", tube, Sound("A_Tube"), 0.3f, near, far);
        var lamp = Object.FindAnyObjectByType<LampFlicker>();
        lamp.hum = hum.GetComponent<AmbientLoop>();
        lamp.buzzSource = Emitter(parent, "FaultyTubeBuzz", tube, null, 0.5f, near, far + 6f);
        lamp.buzzes = Sounds("A_Buzz", SoundForge.TubeBuzzes);
    }

    public static void ConfigureEnvironment(float fogDensity)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.008f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.012f, 0.011f, 0.008f);
        RenderSettings.fogDensity = fogDensity;
    }

    public static void AddToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static void BakeLighting(string settingsName)
    {
        string path = $"{RootDir}/{settingsName}.lighting";
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
        if (settings == null)
        {
            settings = new LightingSettings { name = settingsName };
            AssetDatabase.CreateAsset(settings, path);
        }

        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.directionalityMode = LightmapsMode.NonDirectional;
        settings.lightmapResolution = 24f;
        settings.lightmapPadding = 4;
        settings.lightmapMaxSize = 2048;
        settings.lightmapCompression = LightmapCompression.HighQuality;
        settings.directSampleCount = 64;
        settings.indirectSampleCount = 1024;
        settings.environmentSampleCount = 64;
        settings.maxBounces = 4;
        settings.ao = true;
        settings.aoMaxDistance = 1.2f;
        settings.aoExponentIndirect = 1.2f;
        settings.aoExponentDirect = 0.4f;
        settings.filteringMode = LightingSettings.FilterMode.Auto;
        EditorUtility.SetDirty(settings);

        Lightmapping.lightingSettings = settings;
        if (!Lightmapping.Bake()) Debug.LogError("Lightmap bake failed.");
    }

    public struct GameUi
    {
        public Transform Canvas;
        public DialogueBox Dialogue;
        public ScreenFade Fade;
    }

    public static RectTransform UiChild(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    // The default font with a soft dark halo, so text stays readable over pale walls.
    static Material TextMaterial()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        string path = $"{MaterialDir}/M_UiText.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }
        material.CopyPropertiesFromMaterial(font.material);
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.95f));
        material.SetFloat("_UnderlaySoftness", 0.55f);
        material.SetFloat("_UnderlayDilate", 0.35f);
        EditorUtility.SetDirty(material);
        return material;
    }

    public static TMP_Text UiText(string name, RectTransform rect, float fontSize, TextAlignmentOptions alignment)
    {
        rect.gameObject.name = name;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.fontSharedMaterial = TextMaterial();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    static TMP_Text LowerText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var bottomCentre = new Vector2(0.5f, 0f);
        return UiText(name, UiChild(name, parent, bottomCentre, bottomCentre, position, size), fontSize, alignment);
    }

    // The conversation box, the talk prompt and the fade: everything on screen in every scene.
    public static GameUi BuildGameUi(string scriptPath)
    {
        var canvasGo = new GameObject("GameUI");
        canvasGo.AddComponent<UnityEngine.Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panelRect = UiChild("Conversation", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var panel = panelRect.gameObject.AddComponent<CanvasGroup>();
        panel.blocksRaycasts = false;
        panel.interactable = false;

        var shade = UiChild("Shade", panelRect, Vector2.zero, Vector2.right, new Vector2(0f, 230f), new Vector2(0f, 460f)).gameObject.AddComponent<RawImage>();
        shade.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path("T_UiShade"));
        shade.raycastTarget = false;

        const float lineY = 262f, replyTop = 168f, replyPitch = 42f;
        TMP_Text speaker = LowerText("Speaker", panelRect, new Vector2(0f, lineY + 62f), new Vector2(1400f, 40f), 26f, TextAlignmentOptions.Center);
        speaker.characterSpacing = 12f;
        TMP_Text line = LowerText("Line", panelRect, new Vector2(0f, lineY), new Vector2(1400f, 92f), 38f, TextAlignmentOptions.Center);
        TMP_Text continueMark = LowerText("Continue", panelRect, new Vector2(0f, lineY - 62f), new Vector2(200f, 32f), 22f, TextAlignmentOptions.Center);
        continueMark.text = "E";
        continueMark.color = new Color(0.62f, 0.62f, 0.6f);

        var replies = new TMP_Text[4];
        for (int i = 0; i < replies.Length; i++)
            replies[i] = LowerText($"Reply{i + 1}", panelRect, new Vector2(0f, replyTop - i * replyPitch), new Vector2(1200f, 40f), 32f, TextAlignmentOptions.Center);

        TMP_Text hint = LowerText("Hint", panelRect, new Vector2(0f, 18f), new Vector2(800f, 28f), 20f, TextAlignmentOptions.Center);
        hint.text = "W / S   Choose        E   Confirm";
        hint.color = new Color(0.62f, 0.62f, 0.6f);

        TMP_Text prompt = LowerText("Prompt", canvasGo.transform, new Vector2(0f, 200f), new Vector2(500f, 44f), 28f, TextAlignmentOptions.Center);

        RectTransform fadeRect = UiChild("Fade", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var black = fadeRect.gameObject.AddComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = false;

        var dialogue = canvasGo.AddComponent<DialogueBox>();
        dialogue.scriptFile = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptPath);
        dialogue.panel = panel;
        dialogue.speakerText = speaker;
        dialogue.lineText = line;
        dialogue.continueText = continueMark;
        dialogue.replyTexts = replies;
        dialogue.hintText = hint;
        dialogue.promptText = prompt;

        return new GameUi { Canvas = canvasGo.transform, Dialogue = dialogue, Fade = fadeRect.gameObject.AddComponent<ScreenFade>() };
    }

    // A trigger that shows one line from the script when the player walks into it.
    public static RemarkZone Zone(string name, Transform parent, Vector3 center, Vector3 size, DialogueBox dialogue, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        var zone = go.AddComponent<RemarkZone>();
        zone.dialogue = dialogue;
        zone.label = label;
        return zone;
    }
}
