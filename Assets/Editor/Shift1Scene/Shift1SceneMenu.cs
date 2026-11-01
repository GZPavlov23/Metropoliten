using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SceneForge;

// The title screen is a real place: the platform from Shift 1 with the train waiting at it, seen from the dark
// of the tunnel it's about to enter, lit by the station behind it and by its own headlight.
public static partial class Shift1SceneBuilder
{
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    const string MenuRootDir = "Assets/Art/Generated/Menu";
    const float MenuTunnelEnd = Length + 48f, MenuHeadlight = 280f, MenuEyeHeight = 1.35f, MenuViewStart = 60f, MenuViewEnd = 54.5f, MenuViewYaw = 180f, HaloSize = 2.6f;

    [MenuItem("Metropoliten/Build Main Menu Scene")]
    public static void BuildMenuFromMenu()
    {
        bool confirmed = !File.Exists(MenuScenePath) || EditorUtility.DisplayDialog("Rebuild main menu scene?",
            "This replaces the scene and rebakes its lighting. Hand edits to it will be lost.", "Rebuild", "Cancel");
        if (confirmed) BuildMenu();
    }

    public static void BuildMenu()
    {
        Begin(MenuRootDir, Surfaces, LightmapScales, Headlit);
        TextureForge.ForgeStation();
        TextureForge.ForgeMenu();
        RetroLookSetup.Apply();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform lights = new GameObject("Lights").transform;
        train = new GameObject("Train").transform;
        trainGlow = new MeshKit();
        var tailLamps = new MeshKit();
        var signalLamp = new MeshKit();

        BuildHall("TileBand");
        BuildEndWalls("TileBand", true);
        BuildStationWall("T_SignStation");
        BuildLamps(lights, (3.5f, 18f, glow), (10.5f, 14f, glow), (17.5f, 0f, null), (24.5f, 18f, glow), (31.5f, 22f, glow), (38.5f, 16f, faultyGlow));
        BuildPlatformProps(lights);
        BuildBooth(lights);
        BuildSignal(lights, signalLamp);
        BuildPassage(lights);

        BuildBore(-TunnelLength, 0f);
        BuildBore(Length, MenuTunnelEnd);
        BuildPortalBack(Length + 0.02f, 1f);
        BuildRunDressing(lights, signalLamp, MenuTunnelEnd, false);

        // The light of a lamp further along, on the side of the tunnel the menu doesn't need.
        PointLight(lights, "TunnelGlow", new Vector3(TrackX - 1.7f, 2.2f, 56f), BulbColor, 1.8f, 12f);
        BuildTrack(-TunnelLength, MenuTunnelEnd);
        BuildCar(lights, RearCarStart, RearCarEnd, RearCarEnd, false);
        BuildCar(lights, FrontCarStart, TrainFront, CabBack, true);
        BuildCab(lights);
        BuildTrainEnds(lights, tailLamps);

        Transform architecture = new GameObject("Architecture").transform;
        CommitSurfaces(architecture, train);
        CommitGlow(architecture);
        CommitUnlit("TrainGlow", trainGlow, LampColor, train);
        CommitUnlit("TailLamps", tailLamps, new Color(2.4f, 0.12f, 0.08f), train);
        CommitUnlit("SignalLamp", signalLamp, new Color(0.1f, 2.4f, 0.42f), architecture);
        BuildGrime(architecture);

        Light headlight = BuildHeadlight(MenuHeadlight);
        headlight.enabled = true;
        BuildHeadlampHalos();

        Camera view = NewCamera(null);
        view.farClipPlane = 140f;
        view.transform.SetPositionAndRotation(new Vector3(TrackX, MenuEyeHeight, MenuViewStart), Quaternion.Euler(0f, MenuViewYaw, 0f));

        var backdrop = view.gameObject.AddComponent<MenuBackdrop>();
        backdrop.startPosition = new Vector3(TrackX, MenuEyeHeight, MenuViewStart);
        backdrop.endPosition = new Vector3(TrackX, MenuEyeHeight, MenuViewEnd);
        backdrop.headlight = headlight;

        BuildMenuSound(view.transform);
        ConfigureEnvironment(0.028f);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
        BakeLighting("MenuLighting");
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built and baked {MenuScenePath}.");
    }

    // A bright light in the dark blooms in the eye, and the lamps on their own are only a pair of dots from here.
    static void BuildHeadlampHalos()
    {
        var kit = new MeshKit();
        foreach (float side in new[] { -1f, 1f })
            kit.Plate(new Vector3(TrackX + side * 0.95f, 0.62f, TrainFront + 0.1f), Vector3.forward, Vector3.up, HaloSize, HaloSize, 0.001f);

        var go = new GameObject("HeadlampHalos");
        go.transform.SetParent(train, false);
        go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit, "HeadlampHalos");
        go.AddComponent<MeshRenderer>().sharedMaterial = LoadOrCreateMaterial("HeadlampHalo", "Metropoliten/Halo");
    }

    static void BuildMenuSound(Transform listener)
    {
        Transform parent = new GameObject("Sound").transform;
        RoomTone(parent, 0.14f);

        Ambient(parent, "Wind", new Vector3(TrackX, 1f, MenuViewEnd + 4f), Sound("A_Wind"), 0.5f, 8f, 44f);
        Ambient(parent, "Train", new Vector3(TrackX, 0f, TrainFront), Sound("T_Idle"), 0.3f, 8f, 40f);
        BuildTubeSound(parent, new Vector3(LampX, LampY, 38.5f), 6f, 34f);

        ReverbZone(parent, "TunnelReverb", new Vector3(TrackX, 1f, MenuViewEnd + 8f), AudioReverbPreset.SewerPipe, 24f, 34f);

        var go = new GameObject("TunnelSounds");
        go.transform.SetParent(parent);
        var scatter = go.AddComponent<SoundScatter>();
        scatter.listener = listener;
        scatter.kinds = new[]
        {
            new SoundScatter.Kind { name = "Drips", clips = Sounds("A_Drip", SoundForge.Drips), everySeconds = new Vector2(4f, 13f), distance = new Vector2(5f, 16f), volume = new Vector2(0.15f, 0.35f) },
            new SoundScatter.Kind { name = "Groans", clips = Sounds("A_Groan", SoundForge.Groans), everySeconds = new Vector2(30f, 70f), distance = new Vector2(12f, 28f), volume = new Vector2(0.35f, 0.6f), reach = 8f, behind = 0.85f },
        };
    }
}
