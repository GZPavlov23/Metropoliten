using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Generates the Opening scene: the station vestibule with the registry windows. Every surface is
// lit by baked lightmaps through URP's Baked Lit shader, and the characters by the baked light probes.
public static partial class OpeningSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Opening.unity";
    const string RootDir = "Assets/Art/Generated/Opening";
    const string MeshDir = RootDir + "/Meshes";
    const string MaterialDir = RootDir + "/Materials";
    const string AnimatorDir = RootDir + "/Animators";

    const string ClerkModel = "Assets/Art/Mixamo_NPC/Clerk_OrdinaryMan/ordinaryMan.fbx";
    const string AdultModel = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/AdultMan___.fbx";
    const string YoungModel = "Assets/Art/Mixamo_NPC/Waiting2_YoungMan/YoungMan___.fbx";
    const string IdleClip = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/Idle.fbx";
    const string SittingClip = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/SittingIdle.fbx";

    // Hall size in metres. X runs east, Z north; the player starts at the south end facing north.
    const float Width = 7f, Length = 8.6f, Spring = 2.25f, Rise = 0.8f, DadoTop = 1.1f, WallThickness = 0.24f;
    const float ExitZMin = 3.55f, ExitZMax = 5.25f, ExitTop = 2.05f;
    const float WindowWidth = 0.72f, WindowSill = 1.05f, WindowTop = 1.95f, ClosedWindowX = 2.2f, OpenWindowX = 4.8f;
    const float SeatHeight = 0.44f, LampHeight = 2.72f;
    const float BoothXMin = 3.7f, BoothXMax = 5.9f, BoothDepth = 1.9f, BoothTop = 2.3f;
    const int ArchSegments = 28, StairSteps = 14;
    const float StairRise = 0.175f, StairRun = 0.3f, TunnelHeadroom = 2.3f;
    const float LandingEnd = Width + 1.4f, StairEnd = LandingEnd + StairSteps * StairRun, StairBottom = -StairSteps * StairRise;
    const float TunnelEnd = StairEnd + 9f;

    // Walls run this far past their corners and joins, so the edge of a lightmap chart never lands on a visible seam.
    const float WallOverlap = 0.1f;

    static readonly float[] RibZ = { 2.87f, 5.73f };
    static readonly ArchProfile Arch = new ArchProfile(0f, Width, Spring, Rise);

    static readonly Vector2 PaverTile = new Vector2(4f, 4f), BrickTile = new Vector2(2.4f, 2.4f);
    static readonly Vector2 PlasterTile = new Vector2(3f, 3f), OchreTile = new Vector2(3.4f, 3.4f);
    static readonly Vector2 StoneTile = new Vector2(1.6f, 1.6f), WoodTile = new Vector2(1.2f, 1.2f);
    static readonly Vector2 MetalTile = new Vector2(1f, 1f), PaperTile = new Vector2(0.5f, 0.5f);

    static readonly Color TubeColor = new Color(1f, 0.98f, 0.93f);

    static readonly Dictionary<string, (string texture, Color tint)> Surfaces = new Dictionary<string, (string, Color)>
    {
        { "Floor", ("T_Pavers", Color.white) },
        { "Plaster", ("T_Concrete", Color.white) },
        { "Vault", ("T_Concrete", new Color(0.86f, 0.86f, 0.83f)) },
        { "Dado", ("T_Brick", Color.white) },
        { "Ochre", ("T_Ochre", Color.white) },
        { "Stone", ("T_Stone", new Color(0.8f, 0.79f, 0.76f)) },
        { "Counter", ("T_Stone", Color.white) },
        { "Pier", ("T_Brick", new Color(0.85f, 0.82f, 0.78f)) },
        { "Wood", ("T_Wood", Color.white) },
        { "Metal", ("T_Metal", Color.white) },
        { "Steel", ("T_Stone", new Color(0.6f, 0.62f, 0.63f)) },
        { "BoothWall", ("T_Concrete", new Color(0.9f, 0.93f, 0.92f)) },
        { "BoothFloor", ("T_Linoleum", Color.white) },
        { "Paper", ("T_Paper", Color.white) },
        { "Folders", ("T_Folder", Color.white) },
        { "DoorGlass", ("T_DoorGlass", Color.white) },
        { "Bottle", ("T_Paper", new Color(0.2f, 0.12f, 0.05f)) },
        { "Spectacles", ("T_Metal", new Color(0.22f, 0.2f, 0.18f)) },
        { "RedPaint", ("T_RedPaint", Color.white) },
        { "GateSteel", ("T_Metal", new Color(0.5f, 0.5f, 0.5f)) },
    };

    static Dictionary<string, MeshKit> kits;
    static MeshKit glow, faultyGlow;

    static MeshKit Kit(string name)
    {
        if (!kits.TryGetValue(name, out MeshKit kit)) kits[name] = kit = new MeshKit();
        return kit;
    }

    [MenuItem("Metropoliten/Build Opening Scene")]
    public static void BuildFromMenu()
    {
        bool confirmed = !File.Exists(ScenePath) || EditorUtility.DisplayDialog("Rebuild Opening scene?",
            "This replaces the scene and rebakes its lighting. Hand edits to it will be lost.", "Rebuild", "Cancel");
        if (confirmed) Build();
    }

    public static void Build()
    {
        foreach (string directory in new[] { MeshDir, MaterialDir, AnimatorDir }) Directory.CreateDirectory(directory);

        ConfigureCharacter(ClerkModel);
        ConfigureCharacter(AdultModel);
        ConfigureCharacter(YoungModel);
        ConfigureClip(IdleClip);
        ConfigureClip(SittingClip);
        TextureForge.ForgeAll();
        RetroLookSetup.Apply();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        kits = new Dictionary<string, MeshKit>();
        glow = new MeshKit();
        faultyGlow = new MeshKit();
        Transform lights = new GameObject("Lights").transform;

        BuildHall();
        BuildRegistryWall();
        BuildBooth(lights);
        BuildStairwell(lights);
        BuildEntrance();
        BuildBench(0.7f, 2.45f);
        BuildBench(3.25f, 5.3f);
        BuildBench(6.15f, 7.7f);
        BuildProps();
        BuildServices();
        HangingTube(lights, 1.5f, 3.5f, faultyGlow);
        HangingTube(lights, 4.3f, 6f, glow);
        HangingTube(lights, 7.15f, 16f, glow);

        Transform architecture = new GameObject("Architecture").transform;
        foreach (KeyValuePair<string, MeshKit> kit in kits) CommitSurface(kit.Key, kit.Value, architecture);
        CommitGlow(architecture);
        BuildGrime(architecture);

        BuildProbes();
        Transform characters = BuildCharacters();
        BuildExitTrigger();
        FirstPersonController player = BuildPlayer();
        BuildIntro(characters, player, player.cameraTransform);
        ConfigureEnvironment();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        BakeLighting();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built and baked {ScenePath}.");
    }

    static void ConfigureCharacter(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        ConfigureRig(importer);
        importer.SaveAndReimport();
    }

    // These FBX files must keep their raw units: with file scale on, the skeleton comes in 100x the size of its mesh.
    static void ConfigureRig(ModelImporter importer)
    {
        importer.animationType = ModelImporterAnimationType.Human;
        importer.useFileScale = false;
        importer.globalScale = 1f;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }

    // Clip settings live in the model importer: anything set on the clip itself is lost on reimport.
    static void ConfigureClip(string path)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        ConfigureRig(importer);

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = true;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static void BuildHall()
    {
        Kit("Floor").Panel(new Vector3(-0.4f, 0f, -0.4f), Vector3.forward * (Length + 0.8f), Vector3.right * (Width + 0.8f), PaverTile);
        Kit("Vault").Vault(Arch, -0.1f, Length + 0.1f, ArchSegments, PlasterTile);
        Kit("Plaster").ArchFill(Arch, 0f, Spring, Vector3.forward, Vector3.right, ArchSegments, PlasterTile);

        var exit = new Rect(ExitZMin, 0f, ExitZMax - ExitZMin, ExitTop);
        DadoWall(new Vector3(0f, 0f, Length), Vector3.back, Length, new Rect[0], WallOverlap);
        DadoWall(new Vector3(Width, 0f, 0f), Vector3.forward, Length, new[] { exit }, WallOverlap);
        DadoWall(Vector3.zero, Vector3.right, Width, new Rect[0], 0f);

        foreach (float z in RibZ)
        {
            Kit("Vault").ArchBand(Arch, z - 0.24f, z + 0.24f, 0.13f, ArchSegments, PlasterTile);
            Kit("Plaster").Box(new Vector3(0.07f, Spring / 2f, z), new Vector3(0.14f, Spring, 0.48f), PlasterTile);
            Kit("Plaster").Box(new Vector3(Width - 0.07f, Spring / 2f, z), new Vector3(0.14f, Spring, 0.48f), PlasterTile);
        }

        var stone = new Vector3(0.08f, 0.06f, 0.1f);
        Kit("Stone").Box(new Vector3(0.02f, DadoTop, Length / 2f), new Vector3(stone.x, stone.y, Length), StoneTile);
        Kit("Stone").Box(new Vector3(Width / 2f, DadoTop, 0.02f), new Vector3(Width, stone.y, stone.x), StoneTile);
        Kit("Stone").Box(new Vector3(Width - 0.02f, DadoTop, ExitZMin / 2f), new Vector3(stone.x, stone.y, ExitZMin), StoneTile);
        float northRun = Length - ExitZMax;
        Kit("Stone").Box(new Vector3(Width - 0.02f, DadoTop, ExitZMax + northRun / 2f), new Vector3(stone.x, stone.y, northRun), StoneTile);

        Kit("Stone").Box(new Vector3(Width - 0.02f, ExitTop / 2f, ExitZMin - 0.07f), new Vector3(0.1f, ExitTop + 0.14f, 0.14f), StoneTile);
        Kit("Stone").Box(new Vector3(Width - 0.02f, ExitTop / 2f, ExitZMax + 0.07f), new Vector3(0.1f, ExitTop + 0.14f, 0.14f), StoneTile);
        Kit("Stone").Box(new Vector3(Width - 0.02f, ExitTop + 0.07f, (ExitZMin + ExitZMax) / 2f), new Vector3(0.1f, 0.14f, ExitZMax - ExitZMin + 0.28f), StoneTile);
    }

    static void DadoWall(Vector3 origin, Vector3 along, float length, Rect[] holes, float overTop)
    {
        origin -= along * WallOverlap;
        length += WallOverlap * 2f;
        var lowerHoles = new Rect[holes.Length];
        var upperHoles = new Rect[holes.Length];
        for (int i = 0; i < holes.Length; i++)
        {
            lowerHoles[i] = new Rect(holes[i].x + WallOverlap, holes[i].y, holes[i].width, holes[i].height);
            upperHoles[i] = new Rect(lowerHoles[i].x, holes[i].y - DadoTop, holes[i].width, holes[i].height);
        }

        Kit("Dado").WallWithHoles(origin, along, Vector3.up, length, DadoTop, lowerHoles, BrickTile);
        Kit("Plaster").WallWithHoles(origin + Vector3.up * DadoTop, along, Vector3.up, length, Spring - DadoTop + overTop, upperHoles, PlasterTile);
    }

    static Rect WindowRect(float centerX)
    {
        return new Rect(Width - centerX - WindowWidth / 2f, WindowSill, WindowWidth, WindowTop - WindowSill);
    }

    static void BuildRegistryWall()
    {
        Rect closed = WindowRect(ClosedWindowX), open = WindowRect(OpenWindowX);
        var origin = new Vector3(Width, 0f, Length);

        // Matching the arch's vertices along the top edge welds wall and half-moon into one lightmap chart.
        var archCuts = new List<float>();
        for (int i = 0; i <= ArchSegments; i++) archCuts.Add(Width - Arch.Point((float)i / ArchSegments).x);
        Kit("Ochre").WallWithHoles(origin, Vector3.left, Vector3.up, Width, Spring, new[] { closed, open }, OchreTile, archCuts);
        Kit("Ochre").ArchFill(Arch, Length, Spring, Vector3.back, Vector3.left, ArchSegments, OchreTile);
        Kit("Stone").Reveal(origin, Vector3.left, Vector3.up, closed, Vector3.forward * WallThickness, StoneTile);
        Kit("Stone").Reveal(origin, Vector3.left, Vector3.up, open, Vector3.forward * WallThickness, StoneTile);

        foreach (float x in new[] { ClosedWindowX, OpenWindowX })
        {
            float half = WindowWidth / 2f;
            float height = WindowTop - WindowSill, middle = (WindowTop + WindowSill) / 2f;
            Kit("Stone").Box(new Vector3(x - half - 0.02f, middle, Length - 0.008f), new Vector3(0.04f, height + 0.04f, 0.02f), StoneTile);
            Kit("Stone").Box(new Vector3(x + half + 0.02f, middle, Length - 0.008f), new Vector3(0.04f, height + 0.04f, 0.02f), StoneTile);
            Kit("Stone").Box(new Vector3(x, WindowTop + 0.02f, Length - 0.008f), new Vector3(WindowWidth + 0.08f, 0.04f, 0.02f), StoneTile);
        }

        const float ledgeDepth = 0.46f, ledgeThickness = 0.16f;
        float underLedge = WindowSill - ledgeThickness;
        Kit("Counter").Box(new Vector3(Width / 2f, WindowSill - ledgeThickness / 2f, Length - ledgeDepth / 2f + 0.02f), new Vector3(5.6f, ledgeThickness, ledgeDepth), StoneTile);
        Kit("Dado").Panel(new Vector3(Width + 0.1f, 0f, Length - 0.004f), Vector3.left * (Width + 0.2f), Vector3.up * underLedge, BrickTile);
        Kit("Pier").Box(new Vector3(0.93f, underLedge / 2f, Length - 0.22f), new Vector3(0.44f, underLedge, 0.44f), BrickTile);
        Kit("Pier").Box(new Vector3(Width - 0.93f, underLedge / 2f, Length - 0.22f), new Vector3(0.44f, underLedge, 0.44f), BrickTile);

        float shutterHeight = WindowTop - WindowSill;
        Kit("Metal").Box(new Vector3(ClosedWindowX, WindowSill + shutterHeight / 2f, Length + 0.1f), new Vector3(WindowWidth, shutterHeight, 0.03f), MetalTile);
        Kit("T_SignClosed").Plate(new Vector3(ClosedWindowX, 1.5f, Length + 0.08f), Vector3.back, Vector3.up, 0.42f, 0.21f, 0.003f);

        Kit("T_Plate1").Plate(new Vector3(ClosedWindowX, 2.22f, Length), Vector3.back, Vector3.up, 0.27f, 0.36f, 0.015f);
        Kit("T_Plate2").Plate(new Vector3(OpenWindowX, 2.22f, Length), Vector3.back, Vector3.up, 0.27f, 0.36f, 0.015f);
        Kit("T_SignRegistry").Plate(new Vector3(Width / 2f, 2.62f, Length), Vector3.back, Vector3.up, 3f, 0.375f, 0.03f);
        Kit("T_NoticeBoard").Plate(new Vector3(Width / 2f, 1.56f, Length), Vector3.back, Vector3.up, 1.14f, 0.8f, 0.02f);

        var clockCenter = new Vector3(Width / 2f, 2.22f, Length - 0.025f);
        Kit("T_Clock").Disc(clockCenter, Vector3.back, Vector3.up, 0.17f, 24);
        Kit("Metal").Tube(clockCenter, clockCenter + Vector3.forward * 0.025f, 0.175f, 24, MetalTile);

        Kit("Paper").Box(new Vector3(4.02f, WindowSill + 0.025f, Length - 0.2f), new Vector3(0.22f, 0.05f, 0.3f), PaperTile);
        Kit("T_Form").Plate(new Vector3(4.02f, WindowSill + 0.05f, Length - 0.2f), Vector3.up, Vector3.forward, 0.21f, 0.296f, 0.002f);
        Kit("Metal").Tube(new Vector3(4.96f, WindowSill + 0.006f, Length - 0.2f), new Vector3(5.1f, WindowSill + 0.006f, Length - 0.27f), 0.005f, 6, MetalTile);
    }

    static void BuildBooth(Transform lights)
    {
        float zMin = Length + WallThickness, zMax = zMin + BoothDepth, width = BoothXMax - BoothXMin;
        Vector2 wallTile = PlasterTile;

        Kit("BoothFloor").Panel(new Vector3(BoothXMin - 0.1f, 0f, zMin - 0.1f), Vector3.forward * (BoothDepth + 0.2f), Vector3.right * (width + 0.2f), new Vector2(2f, 2f));
        Kit("BoothWall").Panel(new Vector3(BoothXMin - 0.1f, BoothTop, zMin - 0.1f), Vector3.right * (width + 0.2f), Vector3.forward * (BoothDepth + 0.2f), wallTile);

        var hole = new Rect(OpenWindowX - BoothXMin - WindowWidth / 2f, WindowSill, WindowWidth, WindowTop - WindowSill);
        Kit("BoothWall").WallWithHoles(new Vector3(BoothXMin, 0f, zMin), Vector3.right, Vector3.up, width, BoothTop, new[] { hole }, wallTile);
        Kit("BoothWall").Panel(new Vector3(BoothXMax, 0f, zMax), Vector3.left * width, Vector3.up * BoothTop, wallTile);
        Kit("BoothWall").Panel(new Vector3(BoothXMin, 0f, zMax), Vector3.back * BoothDepth, Vector3.up * BoothTop, wallTile);
        Kit("BoothWall").Panel(new Vector3(BoothXMax, 0f, zMin), Vector3.forward * BoothDepth, Vector3.up * BoothTop, wallTile);

        Kit("Counter").Box(new Vector3(OpenWindowX, WindowSill - 0.08f, zMin + 0.1f), new Vector3(WindowWidth + 0.5f, 0.16f, 0.36f), StoneTile);

        foreach (float y in new[] { 0.72f, 1.26f, 1.8f })
        {
            Kit("Wood").Box(new Vector3(OpenWindowX, y, zMax - 0.16f), new Vector3(width - 0.1f, 0.03f, 0.3f), WoodTile);
            BinderRow(BoothXMin + 0.08f, BoothXMax - 0.08f, y + 0.015f, zMax - 0.3f);
        }

        foreach (float height in new[] { 1.02f, 0.62f })
            for (int i = 0; i < 2; i++)
                Kit("Paper").Box(new Vector3(BoothXMin + 0.42f + i * 0.36f, height / 2f, zMin + 0.34f), new Vector3(0.3f, height, 0.3f - i * 0.02f), PaperTile);

        var fixture = new Vector3(OpenWindowX, BoothTop - 0.03f, zMin + 0.7f);
        glow.Box(fixture, new Vector3(0.9f, 0.04f, 0.12f), Vector2.one);
        AreaLight(lights, "BoothLight", fixture + Vector3.down * 0.03f, new Vector2(0.9f, 0.12f), new Color(0.94f, 1f, 0.95f), 14f);
    }

    // One shelf of binders, each showing a different spine from the T_Folder atlas.
    static void BinderRow(float xStart, float xEnd, float y, float front)
    {
        const float depth = 0.24f;
        int row = Mathf.RoundToInt(y * 100f);
        float x = xStart;
        for (int i = 0; ; i++)
        {
            float binderWidth = Mathf.Lerp(0.055f, 0.095f, TextureForge.Hash(i, row, 1));
            float height = Mathf.Lerp(0.27f, 0.33f, TextureForge.Hash(i, row, 2));
            if (x + binderWidth > xEnd) return;

            // Now and then a binder is out, leaving a gap on the shelf.
            if (TextureForge.Hash(i, row, 3) < 0.08f)
            {
                x += binderWidth;
                continue;
            }

            MeshKit kit = Kit("Folders");
            kit.Box(new Vector3(x + binderWidth / 2f, y + height / 2f, front + depth / 2f), new Vector3(binderWidth, height, depth), new Vector2(0.5f, 0.5f));

            int spine = Mathf.FloorToInt(TextureForge.Hash(i, row, 4) * TextureForge.BinderSpines);
            float u0 = (float)spine / TextureForge.BinderSpines, u1 = (spine + 1f) / TextureForge.BinderSpines;
            float z = front - 0.001f;
            kit.Quad(new Vector3(x + binderWidth, y, z), new Vector3(x, y, z), new Vector3(x, y + height, z), new Vector3(x + binderWidth, y + height, z),
                new Vector2(u1, 0f), new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f));
            x += binderWidth + 0.002f;
        }
    }

    static void BuildStairwell(Transform lights)
    {
        float zMin = ExitZMin, zMax = ExitZMax, width = zMax - zMin;
        const float steps = StairSteps, rise = StairRise, run = StairRun, landingEnd = LandingEnd, stairEnd = StairEnd;
        const float bottom = StairBottom, tunnelEnd = TunnelEnd, headroom = TunnelHeadroom;
        float slope = rise / run;

        float Ceiling(float x) => Mathf.Max(bottom + headroom, Mathf.Min(ExitTop, ExitTop - (x - landingEnd - 0.5f) * slope));
        float Foot(float x) => x <= landingEnd ? -0.2f : Mathf.Max(bottom, -(x - landingEnd) * slope - 0.2f);

        Kit("Floor").Panel(new Vector3(Width - 0.1f, 0f, zMin - 0.1f), Vector3.forward * (width + 0.2f), Vector3.right * (landingEnd - Width + 0.1f), PaverTile);
        Kit("Floor").Panel(new Vector3(stairEnd, bottom, zMin - 0.1f), Vector3.forward * (width + 0.2f), Vector3.right * (tunnelEnd - stairEnd + 0.1f), PaverTile);
        for (int i = 0; i < StairSteps; i++)
        {
            float x = landingEnd + i * run, top = -i * rise, tread = top - rise;
            Kit("Stone").Panel(new Vector3(x, tread, zMin), Vector3.forward * width, Vector3.right * run, StoneTile);
            Kit("Stone").PanelFacing(new Vector3(x, tread, zMin), Vector3.forward * width, Vector3.up * rise, Vector3.right, StoneTile);
        }

        // The extra stop keeps the sloped ceiling's lightmap from spanning the whole dark tunnel.
        var breaks = new List<float>
        {
            Width, landingEnd, landingEnd + 0.5f, landingEnd + (-bottom - 0.2f) / slope,
            landingEnd + 0.5f + (ExitTop - bottom - headroom) / slope, stairEnd + 3.4f, tunnelEnd
        };
        breaks.Sort();
        for (int i = 0; i < breaks.Count - 1; i++)
        {
            float x0 = breaks[i], x1 = breaks[i + 1];
            if (x1 - x0 < 0.001f) continue;
            float foot0 = Foot(x0), foot1 = Foot(x1), line0 = foot0 + 1.3f, line1 = foot1 + 1.3f, top0 = Ceiling(x0), top1 = Ceiling(x1);

            foreach ((float z, Vector3 facing) in new[] { (zMin, Vector3.forward), (zMax, Vector3.back) })
            {
                Kit("Dado").QuadFacing(new Vector3(x0, foot0, z), new Vector3(x1, foot1, z), new Vector3(x1, line1, z), new Vector3(x0, line0, z), facing, Vector3.right, Vector3.up, BrickTile);
                Kit("Plaster").QuadFacing(new Vector3(x0, line0, z), new Vector3(x1, line1, z), new Vector3(x1, top1, z), new Vector3(x0, top0, z), facing, Vector3.right, Vector3.up, PlasterTile);
            }
            Kit("Vault").Panel(new Vector3(x0, top0, zMin), new Vector3(x1 - x0, top1 - top0, 0f), Vector3.forward * width, PlasterTile);
        }
        Kit("Dado").Panel(new Vector3(tunnelEnd, bottom, zMin), Vector3.forward * width, Vector3.up * headroom, BrickTile);

        foreach (float z in new[] { zMin + 0.07f, zMax - 0.07f })
        {
            var start = new Vector3(landingEnd - 0.2f, 0.92f, z);
            var end = new Vector3(stairEnd + 0.1f, bottom + 0.92f, z);
            Kit("Steel").Tube(start, end, 0.024f, 8, MetalTile);
            for (int i = 0; i <= 4; i++)
            {
                Vector3 top = Vector3.Lerp(start, end, i / 4f);
                float stepY = -Mathf.Clamp(Mathf.Ceil((top.x - landingEnd) / run), 0f, steps) * rise;
                Kit("Steel").Tube(top, new Vector3(top.x, stepY, top.z), 0.014f, 6, MetalTile);
            }
        }

        var bulkhead = new Vector3(landingEnd - 0.5f, ExitTop - 0.03f, (zMin + zMax) / 2f);
        Kit("Metal").Box(bulkhead + Vector3.up * 0.018f, new Vector3(0.4f, 0.03f, 0.18f), MetalTile);
        glow.Box(bulkhead + Vector3.down * 0.02f, new Vector3(0.34f, 0.05f, 0.12f), Vector2.one);
        PointLight(lights, "LandingLamp", bulkhead + Vector3.down * 0.2f, TubeColor, 0.55f, 9f);

        // A single bare lamp at the foot of the stairs; past it the tunnel runs off into the dark.
        float tunnelCeiling = bottom + headroom;
        var bulb = new Vector3(stairEnd + 1.5f, tunnelCeiling - 0.15f, (zMin + zMax) / 2f);
        Vector2[] shade = { new Vector2(0.17f, 0f), new Vector2(0.12f, 0.06f), new Vector2(0.05f, 0.11f), new Vector2(0.04f, 0.16f) };
        Kit("Metal").Lathe(bulb, shade, 12, MetalTile);
        Vector2[] shadeInside = { new Vector2(0.04f, 0.15f), new Vector2(0.05f, 0.1f), new Vector2(0.12f, 0.05f), new Vector2(0.165f, -0.002f) };
        Kit("Steel").Lathe(bulb, shadeInside, 12, StoneTile);
        glow.Lathe(bulb + Vector3.down * 0.06f, new[] { new Vector2(0f, 0f), new Vector2(0.035f, 0.02f), new Vector2(0.045f, 0.06f), new Vector2(0.03f, 0.1f), new Vector2(0f, 0.12f) }, 10, Vector2.one);
        PointLight(lights, "StairBulb", bulb + Vector3.down * 0.12f, new Color(1f, 0.86f, 0.68f), 0.7f, 10f);
    }

    static void BuildEntrance()
    {
        const float doorWidth = 0.97f, doorHeight = 2.14f, x0 = 2.5f;
        for (int i = 0; i < 2; i++)
        {
            float x = x0 + doorWidth * (i + 0.5f) + 0.03f * i;
            Kit("Metal").Box(new Vector3(x, doorHeight / 2f, 0.03f), new Vector3(doorWidth, doorHeight, 0.06f), MetalTile);
            Kit("DoorGlass").Box(new Vector3(x, 1.5f, 0.062f), new Vector3(0.34f, 0.5f, 0.006f), Vector2.one);
            float handleX = i == 0 ? x + doorWidth / 2f - 0.1f : x - doorWidth / 2f + 0.1f;
            Kit("Steel").Tube(new Vector3(handleX, 0.9f, 0.1f), new Vector3(handleX, 1.22f, 0.1f), 0.014f, 6, MetalTile);
        }
        Kit("Stone").Box(new Vector3(x0 - 0.06f, 1.1f, 0.03f), new Vector3(0.12f, 2.26f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3(x0 + 2f + 0.06f, 1.1f, 0.03f), new Vector3(0.12f, 2.26f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3(x0 + 1f, 2.2f, 0.03f), new Vector3(2.24f, 0.12f, 0.1f), StoneTile);
        Kit("Metal").Box(new Vector3(Width / 2f, 0.008f, 0.7f), new Vector3(1.8f, 0.016f, 1f), MetalTile);
    }

    static void BuildBench(float zStart, float zEnd)
    {
        float length = zEnd - zStart, zMiddle = (zStart + zEnd) / 2f;
        const float xBack = 0.2f, depth = 0.44f;

        for (int i = 0; i < 3; i++)
            Kit("Wood").Box(new Vector3(xBack + 0.07f + i * 0.15f, SeatHeight - 0.018f, zMiddle), new Vector3(0.135f, 0.036f, length), WoodTile);

        foreach (float y in new[] { SeatHeight + 0.22f, SeatHeight + 0.38f })
        {
            Matrix4x4 slat = Matrix4x4.TRS(new Vector3(xBack + 0.015f, y, zMiddle), Quaternion.Euler(0f, 0f, -8f), Vector3.one);
            Kit("Wood").Box(slat, new Vector3(0.024f, 0.12f, length), WoodTile);
        }

        foreach (float z in new[] { zStart + 0.1f, zMiddle, zEnd - 0.1f })
        {
            Kit("Metal").Box(new Vector3(xBack + depth - 0.05f, (SeatHeight - 0.04f) / 2f, z), new Vector3(0.05f, SeatHeight - 0.04f, 0.05f), MetalTile);
            Kit("Metal").Box(new Vector3(xBack - 0.015f, 0.46f, z), new Vector3(0.05f, 0.92f, 0.05f), MetalTile);
            Kit("Metal").Box(new Vector3(xBack + depth / 2f - 0.02f, SeatHeight - 0.06f, z), new Vector3(depth + 0.04f, 0.04f, 0.05f), MetalTile);
        }
    }

    static void BuildProps()
    {
        Kit("T_MetroMap").Plate(new Vector3(0.012f, 1.66f, 4.28f), Vector3.right, Vector3.up, 1.16f, 0.87f, 0.02f);

        Vector2[] bin =
        {
            new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.19f, 0.03f), new Vector2(0.21f, 0.62f),
            new Vector2(0.225f, 0.64f), new Vector2(0.225f, 0.67f), new Vector2(0.19f, 0.67f), new Vector2(0.17f, 0.56f), new Vector2(0f, 0.56f)
        };
        Kit("Metal").Lathe(new Vector3(0.42f, 0f, 2.87f), bin, 14, MetalTile);

        Vector2[] bottle =
        {
            new Vector2(0f, 0f), new Vector2(0.036f, 0f), new Vector2(0.038f, 0.02f), new Vector2(0.038f, 0.15f),
            new Vector2(0.03f, 0.18f), new Vector2(0.014f, 0.21f), new Vector2(0.014f, 0.28f), new Vector2(0f, 0.285f)
        };
        Kit("Bottle").Lathe(new Vector3(0.3f, SeatHeight, 2.25f), bottle, 8, PaperTile);
        Kit("Bottle").Lathe(new Vector3(0.78f, 0f, 5.46f), bottle, 8, PaperTile);

        Kit("T_Form").Plate(new Vector3(0.42f, SeatHeight + 0.002f, 1.2f), Vector3.up, Vector3.right, 0.24f, 0.33f, 0.003f);

        BuildRadiator(1.2f, 14);
        Kit("T_Timetable").Plate(new Vector3(Width - 0.012f, 1.7f, 1.9f), Vector3.left, Vector3.up, 1.1f, 0.78f, 0.02f);
        BuildFireCabinet(new Vector3(Width - 0.09f, 1.5f, 6.95f));

        const float pipeY = 2.1f;
        foreach (float x in new[] { 6.8f, 6.71f })
            Kit("Steel").Tube(new Vector3(x, pipeY, -0.05f), new Vector3(x, pipeY, Length + 0.05f), 0.034f, 8, MetalTile);
        foreach (float z in new[] { 1f, 2.2f, 3.4f, 4.8f, 6.4f, 7.6f })
            Kit("Metal").Box(new Vector3(6.84f, pipeY + 0.045f, z), new Vector3(0.32f, 0.02f, 0.04f), MetalTile);

        // Hangs across the hall just past the exit, so it reads from the entrance and on the way back.
        var signCenter = new Vector3(6.1f, 2.22f, ExitZMax + 0.22f);
        Kit("T_SignPlatform").Plate(signCenter, Vector3.back, Vector3.up, 1.15f, 0.36f, 0.02f);
        Kit("T_SignPlatformBack").Plate(signCenter, Vector3.forward, Vector3.up, 1.15f, 0.36f, 0.02f);
        foreach (float offset in new[] { -0.45f, 0.45f })
        {
            Vector3 hook = signCenter + new Vector3(offset, 0.18f, 0f);
            Kit("Steel").Tube(hook, new Vector3(hook.x, Arch.HeightAt(hook.x), hook.z), 0.012f, 5, MetalTile);
        }
    }

    static void BuildRadiator(float zStart, int fins)
    {
        const float finDepth = 0.05f, pitch = 0.085f, height = 0.6f, bottom = 0.16f, x = Width - 0.15f;
        for (int i = 0; i < fins; i++)
            Kit("Metal").Box(new Vector3(x, bottom + height / 2f, zStart + i * pitch), new Vector3(0.11f, height, finDepth), MetalTile);

        float zEnd = zStart + (fins - 1) * pitch;
        foreach (float y in new[] { bottom + 0.06f, bottom + height - 0.06f })
            Kit("Steel").Tube(new Vector3(x, y, zStart - 0.08f), new Vector3(x, y, zEnd + 0.08f), 0.024f, 8, MetalTile);
        Kit("Steel").Tube(new Vector3(x, bottom + 0.06f, zStart - 0.08f), new Vector3(x, 0f, zStart - 0.08f), 0.02f, 8, MetalTile);
        Kit("Steel").Tube(new Vector3(x, bottom + height - 0.06f, zEnd + 0.08f), new Vector3(Width, bottom + height - 0.06f, zEnd + 0.08f), 0.02f, 8, MetalTile);
    }

    static void BuildFireCabinet(Vector3 center)
    {
        var size = new Vector3(0.18f, 0.74f, 0.52f);
        Kit("RedPaint").Box(center, size, MetalTile);
        float front = center.x - size.x / 2f;
        Kit("DoorGlass").Box(new Vector3(front - 0.002f, center.y + 0.1f, center.z), new Vector3(0.006f, 0.34f, 0.36f), Vector2.one);
        Kit("T_SignFire").Plate(new Vector3(front, center.y - 0.22f, center.z), Vector3.left, Vector3.up, 0.34f, 0.12f, 0.004f);
    }

    // The tubes need feeding: one conduit along the crown of the vault, dropping into each fixture.
    static void BuildServices()
    {
        const float x = Width / 2f + 0.16f, y = 2.89f;
        Kit("Steel").Tube(new Vector3(x, y, -0.05f), new Vector3(x, y, Length + 0.05f), 0.014f, 6, MetalTile);
        for (float z = 0.7f; z < Length; z += 1.4f)
            Kit("Steel").Tube(new Vector3(x, y, z), new Vector3(x, Arch.HeightAt(x), z), 0.006f, 4, MetalTile);

        foreach (float z in new[] { 1.5f, 4.3f, 7.15f })
        {
            Kit("Metal").Box(new Vector3(x, y, z), new Vector3(0.1f, 0.05f, 0.1f), MetalTile);
            Kit("Steel").Tube(new Vector3(x, y - 0.02f, z), new Vector3(Width / 2f + 0.03f, LampHeight + 0.09f, z), 0.008f, 5, MetalTile);
        }
    }

    static void HangingTube(Transform lights, float z, float intensity, MeshKit tube)
    {
        const float length = 1.3f;
        var center = new Vector3(Width / 2f, LampHeight, z);
        Kit("Metal").Box(center + Vector3.up * 0.05f, new Vector3(0.2f, 0.08f, length + 0.06f), MetalTile);
        tube.Box(center, new Vector3(0.14f, 0.02f, length), Vector2.one);
        foreach (float offset in new[] { -0.45f, 0.45f })
        {
            Vector3 top = center + new Vector3(0f, 0.09f, offset);
            Kit("Steel").Tube(top, new Vector3(top.x, Arch.HeightAt(top.x), top.z), 0.012f, 5, MetalTile);
        }

        AreaLight(lights, "Tube", center + Vector3.down * 0.015f, new Vector2(0.16f, length), TubeColor, intensity);

        // The glow a bare tube throws back onto the vault, soft enough that the housing casts no hard shadow.
        Light spill = PointLight(lights, "TubeSpill", center + Vector3.down * 0.25f, TubeColor, intensity * 0.1f, 9f);
        spill.shapeRadius = 0.3f;
    }

    static Light BakedLight(Transform parent, string name, LightType type, Vector3 position, Quaternion rotation, Color color, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(position, rotation);
        var light = go.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.lightmapBakeType = LightmapBakeType.Baked;
        light.shadows = LightShadows.Soft;
        light.bounceIntensity = 1.8f;
        return light;
    }

    static void AreaLight(Transform parent, string name, Vector3 position, Vector2 size, Color color, float intensity)
    {
        Light light = BakedLight(parent, name, LightType.Rectangle, position, Quaternion.Euler(90f, 0f, 0f), color, intensity);
        light.areaSize = size;
        light.range = 16f;
    }

    static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        Light light = BakedLight(parent, name, LightType.Point, position, Quaternion.identity, color, intensity);
        light.range = range;
        light.shapeRadius = 0.08f;
        return light;
    }

    static Material LoadOrCreateMaterial(string name, string shader)
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

    static Material SurfaceMaterial(string name)
    {
        (string texture, Color tint) surface = Surfaces.TryGetValue(name, out var known) ? known : (name, Color.white);
        Material material = LoadOrCreateMaterial(name, "Universal Render Pipeline/Baked Lit");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(surface.texture)));
        material.SetColor("_BaseColor", surface.tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    // Lays a multiplying stain just off a surface. V runs from the base of the stain to its faded edge.
    static void Stain(MeshKit kit, Vector3 corner, Vector3 along, Vector3 up, Vector3 facing, float repeat)
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

    // A soft patch that follows the curve of the vault.
    static void VaultStain(MeshKit kit, float xMin, float xMax, float zMin, float zMax)
    {
        const int strips = 10;
        for (int i = 0; i < strips; i++)
        {
            float u0 = (float)i / strips, u1 = (i + 1f) / strips;
            float x0 = Mathf.Lerp(xMin, xMax, u0), x1 = Mathf.Lerp(xMin, xMax, u1);
            float y0 = Arch.HeightAt(x0) - 0.004f, y1 = Arch.HeightAt(x1) - 0.004f;
            kit.Quad(new Vector3(x0, y0, zMin), new Vector3(x1, y1, zMin), new Vector3(x1, y1, zMax), new Vector3(x0, y0, zMax),
                new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f));
        }
    }

    static void BuildGrime(Transform parent)
    {
        var rising = new MeshKit();
        var streaks = new MeshKit();
        var blotches = new MeshKit();

        // Dirt climbing the foot of every wall.
        const float dirt = 0.42f;
        Stain(rising, new Vector3(0f, 0f, 0f), Vector3.forward * Length, Vector3.up * dirt, Vector3.right, 3f);
        Stain(rising, new Vector3(0f, 0f, 0f), Vector3.right * 2.38f, Vector3.up * dirt, Vector3.forward, 1f);
        Stain(rising, new Vector3(4.62f, 0f, 0f), Vector3.right * 2.38f, Vector3.up * dirt, Vector3.forward, 1f);
        Stain(rising, new Vector3(Width, 0f, 0f), Vector3.forward * (ExitZMin - 0.1f), Vector3.up * dirt, Vector3.left, 1.2f);
        Stain(rising, new Vector3(Width, 0f, ExitZMax + 0.1f), Vector3.forward * (Length - ExitZMax - 0.1f), Vector3.up * dirt, Vector3.left, 1.2f);
        Stain(rising, new Vector3(0f, 0f, Length - 0.008f), Vector3.right * Width, Vector3.up * dirt, Vector3.back, 2.4f);

        // The floor darkens towards the walls, leaving a worn middle where people walk.
        const float edge = 1.05f;
        Stain(rising, new Vector3(0f, 0f, 0f), Vector3.forward * Length, Vector3.right * edge, Vector3.up, 3f);
        Stain(rising, new Vector3(Width, 0f, 0f), Vector3.forward * (ExitZMin - 0.1f), Vector3.left * edge, Vector3.up, 1.2f);
        Stain(rising, new Vector3(Width, 0f, ExitZMax + 0.1f), Vector3.forward * (Length - ExitZMax - 0.1f), Vector3.left * edge, Vector3.up, 1.2f);
        Stain(rising, new Vector3(0f, 0f, 0f), Vector3.right * Width, Vector3.forward * edge, Vector3.up, 2.4f);
        Stain(rising, new Vector3(0f, 0f, Length), Vector3.right * Width, Vector3.back * edge, Vector3.up, 2.4f);

        // Water and rust running down from the pipes, the stone capping and the counter.
        const float pipeStain = 1.04f;
        Stain(streaks, new Vector3(Width, 2.2f - pipeStain, 0.1f), Vector3.forward * 3.3f, Vector3.up * pipeStain, Vector3.left, 1.4f);
        Stain(streaks, new Vector3(Width, 2.2f - pipeStain, ExitZMax + 0.15f), Vector3.forward * 3.1f, Vector3.up * pipeStain, Vector3.left, 1.3f);
        Stain(streaks, new Vector3(0.6f, 0.34f, Length - 0.008f), Vector3.right * 5.8f, Vector3.up * 0.55f, Vector3.back, 2.2f);
        Stain(streaks, new Vector3(0f, 0.5f, 0f), Vector3.forward * Length, Vector3.up * 0.58f, Vector3.right, 3.4f);

        // Soot over each tube, shoulder wear where people lean back on the benches, and old marks on the floor.
        foreach (float z in new[] { 1.5f, 4.3f, 7.15f })
            VaultStain(blotches, Width / 2f - 1.15f, Width / 2f + 1.15f, z - 1.25f, z + 1.25f);
        foreach (float z in new[] { 1.55f, 4.0f, 6.85f })
            Stain(blotches, new Vector3(0f, 1.14f, z - 0.85f), Vector3.forward * 1.7f, Vector3.up * 0.42f, Vector3.right, 1f);
        Stain(blotches, new Vector3(3.9f, 1.05f, Length - 0.44f), Vector3.right * 1.8f, Vector3.forward * 0.46f, Vector3.up, 1f);
        Stain(blotches, new Vector3(1f, 0f, 1.6f), Vector3.right * 2.2f, Vector3.forward * 1.9f, Vector3.up, 1f);
        Stain(blotches, new Vector3(4.2f, 0f, 2.6f), Vector3.right * 2.7f, Vector3.forward * 2.3f, Vector3.up, 1f);
        Stain(blotches, new Vector3(1.8f, 0f, 5.5f), Vector3.right * 2.1f, Vector3.forward * 2.2f, Vector3.up, 1f);

        CommitStains("Stains_Rising", "T_GrimeRising", rising, parent);
        CommitStains("Stains_Streaks", "T_GrimeStreaks", streaks, parent);
        CommitStains("Stains_Blotches", "T_GrimeBlotch", blotches, parent);
    }

    static void CommitStains(string name, string texture, MeshKit kit, Transform parent)
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

    static Mesh SaveMesh(MeshKit kit, string name)
    {
        Mesh mesh = kit.ToMesh(name);
        Unwrapping.GenerateSecondaryUVSet(mesh);
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{name}.asset");
        return mesh;
    }

    static void CommitSurface(string name, MeshKit kit, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        Mesh mesh = SaveMesh(kit, name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var surface = go.AddComponent<MeshRenderer>();
        surface.sharedMaterial = SurfaceMaterial(name);

        // Trims are only centimetres wide: at the scene's density they fall under a texel and pick up stray colour.
        // The counter is simply looked at from close up for a whole conversation.
        if (name == "Stone" || name == "Counter")
        {
            var serialized = new SerializedObject(surface);
            serialized.FindProperty("m_ScaleInLightmap").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Kits named after a texture are signs, notices and paper: nothing to bump into.
        if (!name.StartsWith("T_")) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
    }

    static void CommitGlow(Transform parent)
    {
        Material material = LoadOrCreateMaterial("LampGlow", "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", new Color(3.2f, 3.1f, 2.8f));
        EditorUtility.SetDirty(material);

        GameObject Glow(string name, MeshKit kit)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit, name);
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        Glow("LampGlow", glow);
        Glow("LampGlow_Faulty", faultyGlow).AddComponent<LampFlicker>();
    }

    static void BuildProbes()
    {
        var positions = new List<Vector3>();
        foreach (float y in new[] { 0.35f, 1.25f, 2.05f })
        {
            for (float x = 0.5f; x < Width; x += 1.5f)
                for (float z = 0.5f; z < Length; z += 1.25f)
                    positions.Add(new Vector3(x, y, z));
            for (float x = BoothXMin + 0.3f; x < BoothXMax; x += 0.8f)
                for (float z = Length + WallThickness + 0.3f; z < Length + WallThickness + BoothDepth - 0.5f; z += 0.5f)
                    positions.Add(new Vector3(x, y, z));
        }
        foreach (float y in new[] { 0.35f, 1.25f, 1.9f })
            foreach (float x in new[] { Width + 0.4f, Width + 1.2f })
                foreach (float z in new[] { ExitZMin + 0.3f, ExitZMax - 0.3f })
                    positions.Add(new Vector3(x, y, z));

        var go = new GameObject("LightProbes");
        go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
    }

    static RuntimeAnimatorController LoopingController(string name, AnimationClip clip)
    {
        string path = $"{AnimatorDir}/AC_{name}.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddMotion(clip);

        // Lets HeadTurn steer the head on top of the animation.
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;
        return controller;
    }

    static AnimationClip LoadClip(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
        throw new System.InvalidOperationException($"No animation clip in {path}");
    }

    static Transform BuildCharacters()
    {
        Transform parent = new GameObject("Characters").transform;
        AnimationClip idle = LoadClip(IdleClip), sitting = LoadClip(SittingClip);
        RuntimeAnimatorController standing = LoopingController("Idle", idle), seated = LoopingController("Sitting", sitting);

        GameObject clerk = SpawnCharacter("Clerk", ClerkModel, "T_NpcClerk", standing, 1.78f, parent);
        clerk.transform.SetPositionAndRotation(new Vector3(OpenWindowX, 0f, Length + WallThickness + 0.42f), Quaternion.Euler(0f, 180f, 0f));
        Pose(clerk);
        clerk.transform.position += Vector3.down * MeasurePose(clerk).min.y;
        AddSpectacles(clerk);
        BakeShadowProxy(clerk, parent);

        GameObject adult = SpawnCharacter("Waiting_Adult", AdultModel, "T_NpcAdult", seated, 1.78f, parent);
        Seat(adult, 0.42f, 3.95f);
        BakeShadowProxy(adult, parent);

        GameObject young = SpawnCharacter("Waiting_Young", YoungModel, "T_NpcYoung", seated, 1.72f, parent);
        Seat(young, 0.42f, 6.75f);
        BakeShadowProxy(young, parent);
        return parent;
    }

    static GameObject SpawnCharacter(string name, string model, string texture, RuntimeAnimatorController controller, float height, Transform parent)
    {
        var character = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model), parent);
        character.name = name;

        Material material = LoadOrCreateMaterial(texture, "Universal Render Pipeline/Baked Lit");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(texture)));
        EditorUtility.SetDirty(material);

        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
        {
            // The clerk's file still carries the modeller's flat reference-image planes.
            if (!(renderer is SkinnedMeshRenderer))
            {
                renderer.enabled = false;
                continue;
            }
            var materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
        character.transform.localScale = Vector3.one * (height / MeasurePose(character).size.y);

        Animator animator = character.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Big heads and hands on thin bodies are half of what makes these people unsettling.
        animator.GetBoneTransform(HumanBodyBones.Head).localScale = Vector3.one * 1.28f;
        animator.GetBoneTransform(HumanBodyBones.LeftHand).localScale = Vector3.one * 1.3f;
        animator.GetBoneTransform(HumanBodyBones.RightHand).localScale = Vector3.one * 1.3f;
        return character;
    }

    // Skinned renderer bounds don't follow the pose or the skeleton's scale, so these read the real vertices.
    static List<Vector3> PosedVertices(GameObject character)
    {
        var points = new List<Vector3>();
        var baked = new Mesh();
        foreach (SkinnedMeshRenderer renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.BakeMesh(baked, true);
            foreach (Vector3 vertex in baked.vertices) points.Add(renderer.transform.TransformPoint(vertex));
        }
        Object.DestroyImmediate(baked);
        return points;
    }

    // Evaluates the real Animator, so what gets measured is what plays in the game.
    static void Pose(GameObject character)
    {
        Animator animator = character.GetComponent<Animator>();
        animator.Rebind();
        animator.Update(0f);
    }

    static Bounds MeasurePose(GameObject character)
    {
        List<Vector3> points = PosedVertices(character);
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (Vector3 point in points) bounds.Encapsulate(point);
        return bounds;
    }

    // The NPCs never leave their spot, so a copy of each posed body goes into the bake as a shadow caster:
    // real contact shadows on the bench and floor for no runtime cost.
    static void BakeShadowProxy(GameObject character, Transform parent)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var baked = new Mesh();
        foreach (SkinnedMeshRenderer renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.BakeMesh(baked, true);
            int offset = vertices.Count;
            foreach (Vector3 vertex in baked.vertices) vertices.Add(renderer.transform.TransformPoint(vertex));
            foreach (int index in baked.triangles) triangles.Add(offset + index);
        }
        Object.DestroyImmediate(baked);

        var mesh = new Mesh { name = character.name + "_Shadow", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{mesh.name}.asset");

        var go = new GameObject(mesh.name);
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var shadow = go.AddComponent<MeshRenderer>();
        shadow.sharedMaterial = SurfaceMaterial("Spectacles");
        shadow.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        shadow.receiveGI = ReceiveGI.LightProbes;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
    }

    // Where a point on the texture atlas sits on the posed body.
    static Vector3 PointOnBody(GameObject character, Vector2 uv)
    {
        var baked = new Mesh();
        foreach (SkinnedMeshRenderer renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderer.BakeMesh(baked, true);
            Vector2[] uvs = renderer.sharedMesh.uv;
            Vector3[] vertices = baked.vertices;
            int[] triangles = renderer.sharedMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Vector2 ab = uvs[b] - uvs[a], ac = uvs[c] - uvs[a], ap = uv - uvs[a];
                float area = ab.x * ac.y - ab.y * ac.x;
                if (Mathf.Abs(area) < 1e-9f) continue;
                float s = (ap.x * ac.y - ap.y * ac.x) / area, t = (ab.x * ap.y - ab.y * ap.x) / area;
                if (s < 0f || t < 0f || s + t > 1f) continue;
                Vector3 local = vertices[a] + (vertices[b] - vertices[a]) * s + (vertices[c] - vertices[a]) * t;
                Object.DestroyImmediate(baked);
                return renderer.transform.TransformPoint(local);
            }
        }
        Object.DestroyImmediate(baked);
        throw new System.InvalidOperationException($"Nothing on {character.name} maps to uv {uv}");
    }

    // Wire glasses, built around where the eyes actually are and carried by the head bone.
    static void AddSpectacles(GameObject clerk)
    {
        Vector3 eyeA = PointOnBody(clerk, TextureForge.ClerkEyeA), eyeB = PointOnBody(clerk, TextureForge.ClerkEyeB);
        Transform head = clerk.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        Vector3 middle = (eyeA + eyeB) / 2f;
        Vector3 across = (eyeB - eyeA).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(middle - head.position, across).normalized;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(across, forward).normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f) up = -up;

        float gap = Vector3.Distance(eyeA, eyeB) / 2f, radius = gap * 0.74f;
        const float wire = 0.0028f;
        Vector2 tile = MetalTile;
        var kit = new MeshKit();

        void Rim(float side)
        {
            Vector3 center = across * side * gap + forward * 0.016f;
            const int segments = 14;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = center + (across * Mathf.Cos(a0) + up * Mathf.Sin(a0) * 0.82f) * radius;
                Vector3 p1 = center + (across * Mathf.Cos(a1) + up * Mathf.Sin(a1) * 0.82f) * radius;
                kit.Tube(p0, p1, wire, 4, tile);
            }

            Vector3 hinge = center + across * side * radius;
            kit.Tube(hinge, hinge + across * side * 0.012f - forward * 0.1f, wire, 4, tile);
        }

        Rim(-1f);
        Rim(1f);
        kit.Tube(across * (radius - gap) + forward * 0.016f, across * (gap - radius) + forward * 0.016f, wire, 4, tile);

        Mesh mesh = kit.ToMesh("Spectacles");
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/Spectacles.asset");
        var go = new GameObject("Spectacles");
        go.transform.position = middle;
        go.transform.SetParent(head, true);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = SurfaceMaterial("Spectacles");
    }

    // Measures the seated pose, then rests the underside of the body under the pelvis on the bench.
    static void Seat(GameObject character, float seatCenterX, float z)
    {
        character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
        Pose(character);
        Animator animator = character.GetComponent<Animator>();
        Vector3 hips = animator.GetBoneTransform(HumanBodyBones.Hips).position;

        float underside = hips.y;
        foreach (Vector3 point in PosedVertices(character))
            if (new Vector2(point.x - hips.x, point.z - hips.z).magnitude < 0.16f) underside = Mathf.Min(underside, point.y);

        character.transform.position = new Vector3(seatCenterX - hips.x, SeatHeight - underside, z - hips.z);

        // Keeps the player out of the legs and off the lap.
        var body = new GameObject(character.name + "_Blocker").AddComponent<BoxCollider>();
        body.transform.SetParent(character.transform.parent);
        body.transform.position = new Vector3(seatCenterX + 0.22f, 0.6f, z);
        body.size = new Vector3(0.74f, 1.2f, 0.62f);

        Debug.Log($"{character.name}: scale {character.transform.localScale.x:F4}, hips {hips}, underside {underside:F3}, soles {MeasurePose(character).min.y:F3}, head scale {animator.GetBoneTransform(HumanBodyBones.Head).localScale.x:F2}");
    }

    static void BuildExitTrigger()
    {
        var go = new GameObject("ExitTrigger_ToPlatform");
        go.transform.position = new Vector3(StairEnd + 6.2f, StairBottom + 1.2f, (ExitZMin + ExitZMax) / 2f);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.8f, 3f, ExitZMax - ExitZMin);
        go.AddComponent<SceneTransitionTrigger>().targetSceneName = "Shift1";
    }

    static FirstPersonController BuildPlayer()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(Width / 2f, 0.92f, 1f);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.28f;
        controller.stepOffset = 0.3f;

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(player.transform);
        cameraGo.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 68f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 60f;
        camera.allowMSAA = false;
        cameraGo.AddComponent<AudioListener>();

        var controls = player.AddComponent<FirstPersonController>();
        controls.cameraTransform = cameraGo.transform;
        controls.walkSpeed = PlayerWalkSpeed;
        controls.sprintSpeed = PlayerSprintSpeed;
        return controls;
    }

    static void ConfigureEnvironment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.008f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.012f, 0.011f, 0.008f);
        RenderSettings.fogDensity = 0.05f;
    }

    static void AddToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void BakeLighting()
    {
        string path = $"{RootDir}/OpeningLighting.lighting";
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
        if (settings == null)
        {
            settings = new LightingSettings { name = "OpeningLighting" };
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
}
