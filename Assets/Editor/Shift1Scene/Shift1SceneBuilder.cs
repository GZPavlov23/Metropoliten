using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SceneForge;

// Generates Shift 1's station: the platform the clerk sends the player to, with the train waiting at it.
// Built and lit the same way as the Opening: generated meshes and baked lightmaps through URP's Baked Lit.
public static partial class Shift1SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Shift1.unity";
    const string RootDir = "Assets/Art/Generated/Shift1";
    const string ScriptPath = "Assets/Dialogue/Shift1.txt";

    // Metres. Z runs along the platform towards the tunnel the train is facing;
    // X runs from the far wall, across the track, to the back of the platform.
    const float Width = 9.15f, Length = 42f, Spring = 2.9f, Rise = 1.7f, TileTop = 2.1f;
    const float EdgeX = 3.95f, TrackX = 2.5f, TrackY = -1.15f, RailY = -0.87f, Gauge = 1.52f;
    const float PortalXMin = 1f, PortalXMax = 4f, PortalTop = 2.75f, TunnelLength = 26f;

    // The running tunnels are round, wider than the portals that lead into them.
    const float BoreX = TrackX, BoreY = 0.95f, BoreRadius = 2.8f, WalkwayTop = -0.12f;
    const int BoreSegments = 30;
    const float DoorXMin = 5.7f, DoorXMax = 7.5f, DoorTop = 2.05f, PassageTop = 2.3f;

    // The passage runs on behind where the player starts, far past where any light reaches.
    const float SpawnZ = -6.5f, PassageBarrierZ = -15f, PassageEnd = -34f;

    const float LampX = 6.6f, LampY = 3.05f, SeatHeight = 0.44f;
    const int ArchSegments = 44;

    // The train. The cab is at the front car's north end, nose towards the tunnel.
    const float BodyXMin = TrackX - 1.35f, BodyXMax = TrackX + 1.35f, BodyWidth = BodyXMax - BodyXMin;
    const float FloorY = 0.04f, CeilingY = 2.28f, RoofY = 2.42f, SkirtY = -0.62f, WallDepth = 0.06f;
    const float WindowBottom = 0.98f, DoorWindowBottom = 1.02f, WindowTop = 1.86f, CarDoorTop = 2f, WheelRadius = 0.39f;
    const float RearCarStart = 4.2f, RearCarEnd = 18.4f, FrontCarStart = 18.9f, CabBack = 31f, TrainFront = 33.2f;
    const float CabDoorMin = 31.25f, CabDoorMax = 32.15f, CabDoorTop = 2.02f;
    const float ConsoleXMin = 1.3f, ConsoleXMax = 3.7f, ConsoleNearY = 0.93f, ConsoleFarY = 1.12f;
    const float ControllerX = 1.72f, ControllerAlong = 0.52f, ControllerBodyHeight = 0.08f;

    const float IntercomZ = 5f, KeyBoardZ = 15.6f, SignalZ = 41f;

    // The run: a straight tunnel north from the platform to Severna, a second hall built like the first.
    // Past Severna the track runs on far enough for even a full-speed overrun to stop in.
    const float RunLength = 540f, NextHallZ = Length + RunLength, NextHallEnd = NextHallZ + Length, RunOutLength = 70f, BoardZ = 37f;
    const float RunLampSpacing = 28f, RunRingSpacing = 7.5f, RunBracketSpacing = 3f;

    // A driver sits high, to see the track over the desk.
    const float SeatZ = 31.8f, SeatTop = 0.72f, SeatedEyeY = 1.52f;
    const float BoothDepth = 1.8f, BoothZMin = 8.6f, BoothZMax = 12f, BoothTop = 2.25f;

    static readonly Rect[] NoHoles = new Rect[0];
    static readonly float[] RibZ = { 7f, 14f, 21f, 28f, 35f };
    static readonly ArchProfile Arch = new ArchProfile(0f, Width, Spring, Rise);

    static readonly Vector2 TileTile = new Vector2(1.2f, 1.2f), BandTile = new Vector2(0.6f, 0.6f), BallastTile = new Vector2(2f, 2f);

    // Six segments to a turn of the tunnel, eight rings to a repeat of the texture.
    static readonly Vector2 TubingTile = new Vector2(BoreRadius * Mathf.PI * 2f, 12f);
    static readonly Vector2 TrainTile = new Vector2(7f, TextureForge.TrainPaintHeight);
    static readonly Vector2 LiningTile = new Vector2(2f, TextureForge.TrainPaintHeight);

    static readonly Color BulbColor = new Color(1f, 0.86f, 0.68f);
    static readonly Color CarLightColor = new Color(1f, 0.92f, 0.78f);
    static readonly Color HeadlightColor = new Color(1f, 0.94f, 0.82f);

    static readonly Rect CabDoorway = Rect.MinMaxRect(CabDoorMin, FloorY, CabDoorMax, CabDoorTop);

    // What the train's headlight has to reach, and the long surfaces that are only ever seen at speed.
    static readonly string[] Headlit = { "Tunnel", "TunnelConcrete", "TunnelSteel", "TunnelCable", "TunnelDoor", "Ballast", "Sleeper", "Rail" };
    static readonly Dictionary<string, float> LightmapScales = new Dictionary<string, float>
    {
        { "Tunnel", 0.2f }, { "TunnelConcrete", 0.2f }, { "TunnelSteel", 0.4f }, { "TunnelCable", 0.4f }, { "TunnelDoor", 0.5f }, { "Ballast", 0.2f }, { "Sleeper", 0.1f }, { "Rail", 0.1f },
    };

    static Transform train;
    static MeshKit trainGlow;

    static readonly Dictionary<string, (string texture, Color tint)> Surfaces = new Dictionary<string, (string, Color)>
    {
        { "Floor", ("T_Pavers", Color.white) },
        { "Tile", ("T_Tile", Color.white) },
        { "TileBand", ("T_TileBand", Color.white) },
        { "TileBandNext", ("T_TileBandNext", Color.white) },
        { "Plaster", ("T_Concrete", Color.white) },
        { "Vault", ("T_Concrete", new Color(0.86f, 0.86f, 0.83f)) },
        { "TrackWall", ("T_Concrete", new Color(0.46f, 0.46f, 0.44f)) },
        { "Tunnel", ("T_Tubing", Color.white) },
        { "TunnelConcrete", ("T_Concrete", new Color(0.5f, 0.5f, 0.47f)) },
        { "TunnelDoor", ("T_Metal", new Color(0.8f, 0.8f, 0.8f)) },
        { "Dado", ("T_Brick", Color.white) },
        { "Stone", ("T_Stone", new Color(0.8f, 0.79f, 0.76f)) },
        { "Wood", ("T_Wood", Color.white) },
        { "Metal", ("T_Metal", Color.white) },
        { "Steel", ("T_Stone", new Color(0.6f, 0.62f, 0.63f)) },
        { "SignalHead", ("T_Metal", new Color(0.42f, 0.42f, 0.42f)) },
        { "TunnelSteel", ("T_Metal", new Color(0.46f, 0.46f, 0.44f)) },
        { "TunnelCable", ("T_Metal", new Color(0.12f, 0.12f, 0.12f)) },
        { "Ballast", ("T_Ballast", Color.white) },
        { "Sleeper", ("T_Wood", new Color(0.42f, 0.38f, 0.34f)) },
        { "Rail", ("T_Stone", new Color(0.36f, 0.36f, 0.37f)) },
        { "Yellow", ("T_YellowLine", Color.white) },
        { "BoothWall", ("T_Concrete", new Color(0.9f, 0.93f, 0.92f)) },
        { "Paper", ("T_Paper", Color.white) },
        { "Bread", ("T_Wood", new Color(0.95f, 0.72f, 0.45f)) },
        { "Brass", ("T_Metal", new Color(1f, 0.84f, 0.42f)) },
        { "DoorGlass", ("T_DoorGlass", Color.white) },
        { "TrainBody", ("T_TrainSide", Color.white) },
        { "TrainTrim", ("T_Metal", new Color(0.42f, 0.42f, 0.42f)) },
        { "TrainSteel", ("T_Stone", new Color(0.6f, 0.62f, 0.63f)) },
        { "TrainConsole", ("T_CabConsole", Color.white) },
        { "TrainSign", ("T_SignTrain", Color.white) },
        { "TrainGrip", ("T_Metal", new Color(0.14f, 0.14f, 0.14f)) },
        { "TrainControllerBody", ("T_Metal", new Color(0.62f, 0.66f, 0.62f)) },
        { "TrainControllerScale", ("T_ControllerPlate", Color.white) },
        { "TrainInterior", ("T_TrainLining", Color.white) },
        { "TrainCeiling", ("T_Stone", new Color(0.74f, 0.74f, 0.68f)) },
        { "TrainFloor", ("T_RubberFloor", Color.white) },
        { "TrainSeat", ("T_RedPaint", Color.white) },
        { "TrainUnder", ("T_Metal", new Color(0.3f, 0.3f, 0.3f)) },
    };

    [MenuItem("Metropoliten/Build Shift 1 Scene")]
    public static void BuildFromMenu()
    {
        bool confirmed = !File.Exists(ScenePath) || EditorUtility.DisplayDialog("Rebuild Shift 1 scene?",
            "This replaces the scene and rebakes its lighting. Hand edits to it will be lost.", "Rebuild", "Cancel");
        if (confirmed) Build();
    }

    public static void Build()
    {
        Begin(RootDir, Surfaces, LightmapScales, Headlit);
        TextureForge.ForgeStation();
        RetroLookSetup.Apply();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform lights = new GameObject("Lights").transform;
        train = new GameObject("Train").transform;
        train.gameObject.AddComponent<Rigidbody>().isKinematic = true;
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

        MeshKit.Origin = Vector3.forward * NextHallZ;
        BuildHall("TileBandNext");
        BuildEndWalls("TileBandNext", false);
        BuildStationWall("T_SignNext");
        BuildLamps(lights, (3.5f, 0f, null), (10.5f, 12f, glow), (17.5f, 16f, glow), (24.5f, 0f, null), (31.5f, 20f, glow), (38.5f, 14f, glow));
        Poster("T_PosterLine", 30.6f);
        BuildStopBoard();
        MeshKit.Origin = Vector3.zero;

        BuildBore(-TunnelLength, 0f);
        BuildBore(Length, NextHallZ);
        BuildBore(NextHallEnd, NextHallEnd + RunOutLength);
        BuildPortalBack(Length + 0.02f, 1f);
        BuildPortalBack(NextHallZ - 0.02f, -1f);
        BuildRunDressing(lights, signalLamp);
        BuildTrack(-TunnelLength, NextHallEnd + RunOutLength);
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

        BuildProbes();
        FirstPersonController player = BuildPlayer(new Vector3((DoorXMin + DoorXMax) / 2f, 0.92f, SpawnZ));
        TrainDrive drive = BuildShift(player);
        BuildSound(player, drive);
        ConfigureEnvironment(0.028f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        BakeLighting("Shift1Lighting");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built and baked {ScenePath}.");
    }

    static void BuildHall(string band)
    {
        Kit("Vault").Vault(Arch, -0.1f, Length + 0.1f, ArchSegments, PlasterTile);

        float zLength = Length + 0.2f;
        Kit("Floor").Panel(new Vector3(EdgeX, 0f, -0.1f), Vector3.forward * zLength, Vector3.right * (Width - EdgeX + 0.1f), PaverTile);
        Kit("Stone").Box(new Vector3(EdgeX + 0.16f, -0.02f, Length / 2f), new Vector3(0.32f, 0.06f, Length), StoneTile);
        Kit("Yellow").Panel(new Vector3(EdgeX + 0.5f, 0.004f, 0f), Vector3.forward * Length, Vector3.right * 0.14f, TileTile);
        WallAlongZ(Kit("TrackWall"), EdgeX, -1f, -0.1f, Length + 0.1f, TrackY, 0.01f, NoHoles, PlasterTile);
        Kit("Ballast").Panel(new Vector3(-0.1f, TrackY, -0.1f), Vector3.forward * zLength, Vector3.right * (EdgeX + 0.2f), BallastTile);

        WallAlongZ(Kit("Tile"), Width, -1f, -0.1f, Length + 0.1f, 0f, TileTop, NoHoles, TileTile);
        WallAlongZ(Kit("Plaster"), Width, -1f, -0.1f, Length + 0.1f, TileTop, Spring, NoHoles, PlasterTile);
        WallAlongZ(Kit("Tile"), 0f, 1f, -0.1f, Length + 0.1f, TrackY, TileTop, NoHoles, TileTile);
        WallAlongZ(Kit("Plaster"), 0f, 1f, -0.1f, Length + 0.1f, TileTop, Spring, NoHoles, PlasterTile);

        WallAlongZ(Kit(band), Width - 0.004f, -1f, 0f, Length, 1.62f, 1.86f, NoHoles, BandTile);
        WallAlongZ(Kit(band), 0.004f, 1f, 0f, Length, 1.62f, 1.86f, NoHoles, BandTile);

        Kit("Stone").Box(new Vector3(Width - 0.02f, TileTop, Length / 2f), new Vector3(0.08f, 0.07f, Length), StoneTile);
        Kit("Stone").Box(new Vector3(0.02f, TileTop, Length / 2f), new Vector3(0.08f, 0.07f, Length), StoneTile);

        foreach (float z in RibZ)
        {
            Kit("Vault").ArchBand(Arch, z - 0.25f, z + 0.25f, 0.16f, ArchSegments, PlasterTile);
            Kit("Plaster").Box(new Vector3(Width - 0.08f, Spring / 2f, z), new Vector3(0.16f, Spring, 0.5f), PlasterTile);
            Kit("Plaster").Box(new Vector3(0.08f, (Spring + TrackY) / 2f, z), new Vector3(0.16f, Spring - TrackY, 0.5f), PlasterTile);
        }
    }

    // With no passage behind it, the way out is a shutter pulled down for the night.
    static void BuildEndWalls(string band, bool passage)
    {
        Rect portalLow = Rect.MinMaxRect(PortalXMin, TrackY, PortalXMax, TileTop);
        Rect portalHigh = Rect.MinMaxRect(PortalXMin, TileTop, PortalXMax, PortalTop);
        Rect door = Rect.MinMaxRect(DoorXMin, 0f, DoorXMax, DoorTop);

        WallAlongX(Kit("Tile"), 0f, 1f, -0.1f, Width + 0.1f, TrackY, TileTop, passage ? new[] { portalLow, door } : new[] { portalLow }, TileTile);
        WallAlongX(Kit("Plaster"), 0f, 1f, -0.1f, Width + 0.1f, TileTop, Spring, new[] { portalHigh }, PlasterTile);
        Kit("Plaster").ArchFill(Arch, 0f, Spring, Vector3.forward, Vector3.right, ArchSegments, PlasterTile);

        WallAlongX(Kit("Tile"), Length, -1f, -0.1f, Width + 0.1f, TrackY, TileTop, new[] { portalLow }, TileTile);
        WallAlongX(Kit("Plaster"), Length, -1f, -0.1f, Width + 0.1f, TileTop, Spring, new[] { portalHigh }, PlasterTile);
        Kit("Plaster").ArchFill(Arch, Length, Spring, Vector3.back, Vector3.left, ArchSegments, PlasterTile);

        foreach (float z in new[] { 0.03f, Length - 0.03f })
        {
            Kit("Stone").Box(new Vector3(PortalXMin - 0.08f, (TrackY + PortalTop) / 2f, z), new Vector3(0.16f, PortalTop - TrackY, 0.08f), StoneTile);
            Kit("Stone").Box(new Vector3(PortalXMax + 0.08f, (TrackY + PortalTop) / 2f, z), new Vector3(0.16f, PortalTop - TrackY, 0.08f), StoneTile);
            Kit("Stone").Box(new Vector3((PortalXMin + PortalXMax) / 2f, PortalTop + 0.08f, z), new Vector3(PortalXMax - PortalXMin + 0.32f, 0.16f, 0.08f), StoneTile);
        }
        WallAlongX(Kit(band), 0.004f, 1f, PortalXMax + 0.16f, DoorXMin - 0.13f, 1.62f, 1.86f, NoHoles, BandTile);
        WallAlongX(Kit(band), 0.004f, 1f, DoorXMax + 0.13f, Width, 1.62f, 1.86f, NoHoles, BandTile);
        WallAlongX(Kit(band), Length - 0.004f, -1f, PortalXMax + 0.16f, Width, 1.62f, 1.86f, NoHoles, BandTile);

        float doorWidth = DoorXMax - DoorXMin;
        Kit("Stone").Box(new Vector3(DoorXMin - 0.06f, DoorTop / 2f, 0.02f), new Vector3(0.14f, DoorTop + 0.1f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3(DoorXMax + 0.06f, DoorTop / 2f, 0.02f), new Vector3(0.14f, DoorTop + 0.1f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3((DoorXMin + DoorXMax) / 2f, DoorTop + 0.05f, 0.02f), new Vector3(doorWidth + 0.26f, 0.1f, 0.1f), StoneTile);
        if (!passage)
        {
            for (float y = 0.05f; y < DoorTop; y += 0.1f)
                Kit("Metal").Box(new Vector3((DoorXMin + DoorXMax) / 2f, y, 0.05f), new Vector3(doorWidth, 0.09f, 0.03f), MetalTile);
        }

        // The way out of the hall that the player has no key for.
        const float staffDoorX = 7.4f;
        Kit("Metal").Box(new Vector3(staffDoorX, 1f, Length - 0.03f), new Vector3(0.96f, 2f, 0.06f), MetalTile);
        Kit("Stone").Box(new Vector3(staffDoorX, 1.04f, Length - 0.015f), new Vector3(1.12f, 2.08f, 0.03f), StoneTile);
        Kit("Steel").Tube(new Vector3(staffDoorX - 0.34f, 0.9f, Length - 0.08f), new Vector3(staffDoorX - 0.34f, 1.2f, Length - 0.08f), 0.014f, 6, MetalTile);
        Kit("T_SignStaff").Plate(new Vector3(staffDoorX, 2.32f, Length - 0.012f), Vector3.back, Vector3.up, 1.3f, 0.25f, 0.02f);

        Kit("T_SignExit").Plate(new Vector3((DoorXMin + DoorXMax) / 2f, 2.34f, 0.012f), Vector3.forward, Vector3.up, 1.1f, 0.32f, 0.02f);
    }

    // Where the wall of a bore stands at a given height, on the left (-1) or the right (1) of the track.
    static float BoreWall(float y, float side)
    {
        return BoreX + side * Mathf.Sqrt(BoreRadius * BoreRadius - (y - BoreY) * (y - BoreY));
    }

    // A round tube: iron lining down to the trackbed, a raised walkway on the right and a rail along it.
    static void BuildBore(float zStart, float zEnd)
    {
        var center = new Vector2(BoreX, BoreY);
        float length = zEnd - zStart, middle = (zStart + zEnd) / 2f, floorAngle = Mathf.Acos((TrackY - BoreY) / BoreRadius);
        Kit("Tunnel").Bore(center, BoreRadius, -floorAngle, floorAngle, zStart, zEnd, BoreSegments, TubingTile);

        float floorLeft = BoreWall(TrackY, -1f), floorRight = BoreWall(TrackY, 1f);
        Kit("Ballast").Panel(new Vector3(floorLeft, TrackY, zStart), Vector3.forward * length, Vector3.right * (floorRight - floorLeft), BallastTile);

        float walkEdge = BodyXMax + 0.3f, walkWall = BoreWall(WalkwayTop, 1f) + 0.1f;
        Kit("TunnelConcrete").Box(new Vector3((walkEdge + walkWall) / 2f, (WalkwayTop + TrackY) / 2f, middle), new Vector3(walkWall - walkEdge, WalkwayTop - TrackY, length), PlasterTile);

        float railY = WalkwayTop + 1f, railX = BoreWall(railY, 1f) - 0.14f;
        Kit("TunnelSteel").Tube(new Vector3(railX, railY, zStart), new Vector3(railX, railY, zEnd), 0.022f, 6, MetalTile);
    }

    // The tube ends in a flat wall around the portal, which is all the driver sees of the station's end wall.
    static void BuildPortalBack(float z, float facing)
    {
        Rect portal = Rect.MinMaxRect(PortalXMin, TrackY, PortalXMax, PortalTop);
        WallAlongX(Kit("TunnelConcrete"), z, facing, BoreX - BoreRadius - 0.1f, BoreX + BoreRadius + 0.1f, TrackY, BoreY + BoreRadius + 0.1f, new[] { portal }, PlasterTile);
    }

    // What the driver has to look at on the run: iron rings going by, cables, pipes, doors and lamps.
    static void BuildRunDressing(Transform lights, MeshKit signalLamp)
    {
        var center = new Vector2(BoreX, BoreY);
        float floorAngle = Mathf.Acos((TrackY - BoreY) / BoreRadius);

        // Every fifth ring stands proud of the rest.
        for (float z = Length + RunRingSpacing; z < NextHallZ; z += RunRingSpacing)
            Kit("Tunnel").BoreRing(center, BoreRadius, 0.07f, -floorAngle, floorAngle, z - 0.09f, z + 0.09f, BoreSegments, TubingTile);

        // Power cables on the right, carried on brackets, and pipes along the left.
        float[] cables = { 1.3f, 1.42f, 1.54f, 1.66f, 1.78f, 1.9f };
        foreach (float y in cables)
        {
            float x = BoreWall(y, 1f) - 0.1f;
            Kit("TunnelCable").Tube(new Vector3(x, y, Length), new Vector3(x, y, NextHallZ), 0.024f, 5, MetalTile);
        }

        for (float z = Length + 2f; z < NextHallZ; z += RunBracketSpacing)
        {
            float x = BoreWall(1.6f, 1f) - 0.05f;
            Kit("TunnelSteel").Box(new Vector3(x, 1.6f, z), new Vector3(0.14f, 0.82f, 0.05f), MetalTile);
        }

        foreach ((float y, float radius) in new[] { (1.75f, 0.11f), (2.1f, 0.05f) })
        {
            float x = BoreWall(y, -1f) + radius + 0.08f;
            Kit("TunnelSteel").Tube(new Vector3(x, y, Length), new Vector3(x, y, NextHallZ), radius, 10, MetalTile);
            for (float z = Length + 3f; z < NextHallZ; z += 6f)
                Kit("TunnelSteel").Tube(new Vector3(x, y, z - 0.04f), new Vector3(x, y, z + 0.04f), radius + 0.025f, 10, MetalTile);
        }

        // Lamps along the right. Every fourth has gone, and nobody comes down here to change them.
        int lampNumber = 0;
        for (float z = Length + RunLampSpacing / 2f; z < NextHallZ - 10f; z += RunLampSpacing)
        {
            const float y = 2.5f;
            var lamp = new Vector3(BoreWall(y, 1f) - 0.17f, y, z);
            Kit("TunnelSteel").Box(lamp + Vector3.right * 0.07f, new Vector3(0.1f, 0.26f, 0.44f), MetalTile);
            foreach (float bar in new[] { -0.12f, 0f, 0.12f })
                Kit("TunnelSteel").Box(lamp + new Vector3(-0.05f, 0f, bar), new Vector3(0.02f, 0.2f, 0.012f), MetalTile);

            if (++lampNumber % 4 == 0)
            {
                Kit("DoorGlass").Box(lamp, new Vector3(0.07f, 0.13f, 0.3f), Vector2.one);
                continue;
            }
            glow.Box(lamp, new Vector3(0.07f, 0.13f, 0.3f), Vector2.one);
            PointLight(lights, "TunnelLamp", lamp + Vector3.left * 0.45f, BulbColor, 1.5f, 9f);
        }

        // Distance boards, facing the driver.
        for (int i = 1; i <= TextureForge.RunMarkers; i++)
        {
            const float y = 1.2f;
            var mark = new Vector3(BoreWall(y, 1f) - 0.5f, y, Length + i * 100f);
            Kit("TunnelSteel").Box(mark + Vector3.right * 0.25f, new Vector3(0.5f, 0.03f, 0.03f), MetalTile);
            Kit($"T_Mark{i}").Plate(mark, Vector3.back, Vector3.up, 0.5f, 0.25f, 0.015f);
        }

        // Steel doors in the left wall, to wherever they go.
        foreach (float z in new[] { 148f, 322f, 471f })
        {
            const float y = 0.55f;
            float x = BoreWall(y, -1f) + 0.04f;
            Kit("TunnelConcrete").Box(new Vector3(x, y, z), new Vector3(0.16f, 2.1f, 1.2f), PlasterTile);
            Kit("TunnelDoor").Box(new Vector3(x + 0.1f, y, z), new Vector3(0.05f, 1.9f, 0.9f), MetalTile);
            Kit("TunnelSteel").Tube(new Vector3(x + 0.15f, y - 0.1f, z - 0.34f), new Vector3(x + 0.15f, y + 0.12f, z - 0.34f), 0.012f, 5, MetalTile);
        }

        // A signal half way along, showing green.
        const float signalZ = 300f, signalY = 2.3f;
        var head = new Vector3(BoreWall(signalY, -1f) + 0.3f, signalY, signalZ);
        Kit("TunnelSteel").Box(head, new Vector3(0.22f, 0.5f, 0.12f), MetalTile);
        signalLamp.Disc(head + new Vector3(0f, -0.12f, -0.063f), Vector3.back, Vector3.up, 0.07f, 14);
        PointLight(lights, "RunSignalGlow", head + new Vector3(0.1f, -0.1f, -0.4f), new Color(0.25f, 1f, 0.5f), 0.3f, 4f);
    }

    static void BuildTrack(float zStart, float zEnd)
    {
        for (float z = zStart; z < zEnd; z += 0.72f)
            Kit("Sleeper").Box(new Vector3(TrackX, TrackY + 0.06f, z), new Vector3(2.5f, 0.12f, 0.24f), WoodTile);

        float middle = (zStart + zEnd) / 2f, length = zEnd - zStart;
        foreach (float side in new[] { -1f, 1f })
        {
            float x = TrackX + side * Gauge / 2f;
            Kit("Rail").Box(new Vector3(x, RailY - 0.08f, middle), new Vector3(0.05f, 0.16f, length), StoneTile);
            Kit("Rail").Box(new Vector3(x, TrackY + 0.135f, middle), new Vector3(0.14f, 0.03f, length), StoneTile);
        }

        // The live rail, covered in timber, on the far side of the track.
        const float liveX = PortalXMin + 0.28f;
        Kit("Rail").Box(new Vector3(liveX, -0.8f, middle), new Vector3(0.08f, 0.1f, length), StoneTile);
        Kit("Sleeper").Box(new Vector3(liveX, -0.72f, middle), new Vector3(0.26f, 0.03f, length), WoodTile);
        for (float z = zStart; z < zEnd; z += 3f)
            Kit("Rail").Box(new Vector3(liveX, -0.96f, z), new Vector3(0.06f, 0.34f, 0.08f), MetalTile);
    }

    // Carries on from the Opening's tunnel, in the same brick and plaster, and leads back into the dark the way the player came.
    static void BuildPassage(Transform lights)
    {
        float zStart = PassageEnd, length = -PassageEnd, width = DoorXMax - DoorXMin;
        // The floor meets the hall's, and the ceiling stops at the wall: past it, it would cut across the exit sign.
        Kit("Floor").Panel(new Vector3(DoorXMin - 0.1f, 0f, zStart - 0.1f), Vector3.forward * length, Vector3.right * (width + 0.2f), PaverTile);
        Kit("Vault").Panel(new Vector3(DoorXMin - 0.1f, PassageTop, zStart - 0.1f), Vector3.right * (width + 0.2f), Vector3.forward * (length + 0.1f), PlasterTile);

        foreach ((float x, float facing) in new[] { (DoorXMin, 1f), (DoorXMax, -1f) })
        {
            WallAlongZ(Kit("Dado"), x, facing, zStart, 0.02f, 0f, 1.3f, NoHoles, BrickTile);
            WallAlongZ(Kit("Plaster"), x, facing, zStart, 0.02f, 1.3f, PassageTop, NoHoles, PlasterTile);
        }
        WallAlongX(Kit("Dado"), zStart, 1f, DoorXMin, DoorXMax, 0f, PassageTop, NoHoles, BrickTile);
        WallAlongX(Kit("Plaster"), 0f, -1f, DoorXMin, DoorXMax, DoorTop, PassageTop, NoHoles, PlasterTile);

        // The second bulb is enough to show the way back, and little more. Past it the path goes black.
        foreach ((float z, float power) in new[] { (-3.2f, 0.8f), (-9f, 0.28f) })
        {
            var lamp = new Vector3((DoorXMin + DoorXMax) / 2f, PassageTop - 0.16f, z);
            ShadedLamp(lamp, PassageTop);
            PointLight(lights, "PassageBulb", lamp + Vector3.down * 0.14f, BulbColor, power, 8f);
        }
    }

    static void ShadedLamp(Vector3 center, float ceiling)
    {
        Vector2[] shade = { new Vector2(0.17f, 0f), new Vector2(0.12f, 0.06f), new Vector2(0.05f, 0.11f), new Vector2(0.04f, ceiling - center.y) };
        Kit("Metal").Lathe(center, shade, 12, MetalTile);
        Vector2[] inside = { new Vector2(0.04f, 0.15f), new Vector2(0.05f, 0.1f), new Vector2(0.12f, 0.05f), new Vector2(0.165f, -0.002f) };
        Kit("Steel").Lathe(center, inside, 12, StoneTile);
        Vector2[] bulb = { new Vector2(0f, 0f), new Vector2(0.035f, 0.02f), new Vector2(0.045f, 0.06f), new Vector2(0.03f, 0.1f), new Vector2(0f, 0.12f) };
        glow.Lathe(center + Vector3.down * 0.06f, bulb, 10, Vector2.one);
    }

    // A tube with no glow to give has died and nobody has replaced it.
    static void BuildLamps(Transform lights, params (float z, float power, MeshKit tube)[] tubes)
    {
        const float length = 1.6f;
        foreach ((float z, float power, MeshKit tube) in tubes)
        {
            var center = new Vector3(LampX, LampY, z);
            if (tube != null)
            {
                HangingTube(lights, Arch, center, power, tube, length);
                continue;
            }

            Kit("Metal").Box(center + Vector3.up * 0.05f, new Vector3(0.2f, 0.08f, length + 0.06f), MetalTile);
            Kit("DoorGlass").Box(center, new Vector3(0.14f, 0.02f, length), Vector2.one);
            foreach (float offset in new[] { -0.45f, 0.45f })
            {
                Vector3 top = center + new Vector3(0f, 0.09f, offset);
                Kit("Steel").Tube(top, new Vector3(top.x, Arch.HeightAt(top.x), top.z), 0.012f, 5, MetalTile);
            }
        }

        // Conduit feeding the fixtures along the crown of the vault.
        const float conduitX = LampX + 0.2f;
        float conduitY = Arch.HeightAt(conduitX) - 0.05f;
        Kit("Steel").Tube(new Vector3(conduitX, conduitY, -0.05f), new Vector3(conduitX, conduitY, Length + 0.05f), 0.018f, 6, MetalTile);
    }

    // What every platform has: its name on both walls, benches, a clock and bins.
    static void BuildStationWall(string sign)
    {
        for (int i = 0; i < 6; i++)
        {
            float z = 3.5f + i * 7f;
            Kit(sign).Plate(new Vector3(Width - 0.012f, 2.5f, z), Vector3.left, Vector3.up, 3.2f, 0.6f, 0.03f);
            Kit(sign).Plate(new Vector3(0.012f, 2.5f, z), Vector3.right, Vector3.up, 3.2f, 0.6f, 0.03f);
        }

        BuildBench(17f, 20.2f);
        BuildBench(22f, 25.5f);
        BuildBench(29f, 32.2f);
        BuildBench(36f, 39.4f);

        // A clock on a bracket, facing both ways down the platform.
        var clock = new Vector3(LampX, 2.72f, 21f);
        Kit("Metal").Tube(clock + Vector3.back * 0.05f, clock + Vector3.forward * 0.05f, 0.33f, 28, MetalTile);
        Kit("T_Clock").Disc(clock + Vector3.back * 0.052f, Vector3.back, Vector3.up, 0.32f, 28);
        Kit("T_Clock").Disc(clock + Vector3.forward * 0.052f, Vector3.forward, Vector3.up, 0.32f, 28);
        Kit("Steel").Tube(clock + Vector3.up * 0.33f, new Vector3(clock.x, Arch.HeightAt(clock.x) - 0.16f, clock.z), 0.02f, 6, MetalTile);

        Vector2[] bin =
        {
            new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.19f, 0.03f), new Vector2(0.21f, 0.62f),
            new Vector2(0.225f, 0.64f), new Vector2(0.225f, 0.67f), new Vector2(0.19f, 0.67f), new Vector2(0.17f, 0.56f), new Vector2(0f, 0.56f)
        };
        Kit("Metal").Lathe(new Vector3(Width - 0.5f, 0f, 21.5f), bin, 14, MetalTile);
        Kit("Metal").Lathe(new Vector3(Width - 0.5f, 0f, 35f - 0.6f), bin, 14, MetalTile);
    }

    // Everything particular to the platform the shift starts on.
    static void BuildPlatformProps(Transform lights)
    {
        Kit("T_MetroMapStation").Plate(new Vector3(Width - 0.012f, 1.46f, 24.6f), Vector3.left, Vector3.up, 1.5f, 1.13f, 0.02f);
        Poster("T_PosterDrivers", 18.6f);
        Poster("T_PosterLine", 30.6f);
        WallLamp(lights, 18.6f, 0.34f);

        // The intercom.
        const float height = 0.46f, depth = 0.1f;
        Kit("Metal").Box(new Vector3(Width - depth / 2f, 1.42f, IntercomZ), new Vector3(depth, height, 0.32f), MetalTile);
        Kit("T_Intercom").Plate(new Vector3(Width - depth, 1.4f, IntercomZ), Vector3.left, Vector3.up, 0.26f, 0.39f, 0.004f);
        Kit("Steel").Tube(new Vector3(Width - 0.025f, 1.65f, IntercomZ), new Vector3(Width - 0.025f, TileTop, IntercomZ), 0.016f, 6, MetalTile);

        // The key board.
        Kit("Metal").Box(new Vector3(Width - 0.015f, 1.42f, KeyBoardZ), new Vector3(0.03f, 0.74f, 1.04f), MetalTile);
        Kit("T_KeyBoard").Plate(new Vector3(Width - 0.03f, 1.42f, KeyBoardZ), Vector3.left, Vector3.up, 0.96f, 0.66f, 0.004f);

        // Its own little lamp, because the tube that should light this stretch has died.
        WallLamp(lights, KeyBoardZ, 0.42f);
    }

    static void WallLamp(Transform lights, float z, float power)
    {
        var lamp = new Vector3(Width - 0.09f, 1.97f, z);
        Kit("Metal").Box(lamp + Vector3.up * 0.03f, new Vector3(0.18f, 0.05f, 0.34f), MetalTile);
        glow.Box(lamp, new Vector3(0.13f, 0.03f, 0.28f), Vector2.one);
        PointLight(lights, "WallLamp", lamp + new Vector3(-0.2f, -0.1f, 0f), new Color(1f, 0.9f, 0.74f), power, 5f);
    }

    static void Poster(string texture, float z)
    {
        Kit("Metal").Box(new Vector3(Width - 0.01f, 1.46f, z), new Vector3(0.02f, 1.08f, 0.8f), MetalTile);
        Kit(texture).Plate(new Vector3(Width - 0.02f, 1.46f, z), Vector3.left, Vector3.up, 0.72f, 1f, 0.004f);
    }

    static void BuildBench(float zStart, float zEnd)
    {
        float length = zEnd - zStart, zMiddle = (zStart + zEnd) / 2f;
        const float xBack = Width - 0.2f, depth = 0.44f;

        for (int i = 0; i < 3; i++)
            Kit("Wood").Box(new Vector3(xBack - 0.07f - i * 0.15f, SeatHeight - 0.018f, zMiddle), new Vector3(0.135f, 0.036f, length), WoodTile);

        foreach (float y in new[] { SeatHeight + 0.22f, SeatHeight + 0.38f })
        {
            Matrix4x4 slat = Matrix4x4.TRS(new Vector3(xBack - 0.015f, y, zMiddle), Quaternion.Euler(0f, 0f, 8f), Vector3.one);
            Kit("Wood").Box(slat, new Vector3(0.024f, 0.12f, length), WoodTile);
        }

        foreach (float z in new[] { zStart + 0.1f, zMiddle, zEnd - 0.1f })
        {
            Kit("Metal").Box(new Vector3(xBack - depth + 0.05f, (SeatHeight - 0.04f) / 2f, z), new Vector3(0.05f, SeatHeight - 0.04f, 0.05f), MetalTile);
            Kit("Metal").Box(new Vector3(xBack + 0.015f, 0.46f, z), new Vector3(0.05f, 0.92f, 0.05f), MetalTile);
            Kit("Metal").Box(new Vector3(xBack - depth / 2f + 0.02f, SeatHeight - 0.06f, z), new Vector3(depth + 0.04f, 0.04f, 0.05f), MetalTile);
        }
    }

    // The attendant's booth. Nobody's in it, but the lamp's on and there's food on the shelf.
    static void BuildBooth(Transform lights)
    {
        float xFront = Width - BoothDepth, zMiddle = (BoothZMin + BoothZMax) / 2f;
        Rect[] window = { Rect.MinMaxRect(BoothZMin + 0.7f, 1.02f, BoothZMax - 0.7f, 1.95f) };
        const float thick = 0.08f;

        WallAlongZ(Kit("BoothWall"), xFront, -1f, BoothZMin, BoothZMax, 0f, BoothTop, window, PlasterTile, Kit("Stone"), thick);
        WallAlongZ(Kit("BoothWall"), xFront + thick, 1f, BoothZMin, BoothZMax, 0f, BoothTop, window, PlasterTile);
        WallAlongX(Kit("BoothWall"), BoothZMin, -1f, xFront, Width, 0f, BoothTop, NoHoles, PlasterTile);
        WallAlongX(Kit("BoothWall"), BoothZMax, 1f, xFront, Width, 0f, BoothTop, NoHoles, PlasterTile);
        WallAlongX(Kit("BoothWall"), BoothZMin + thick, 1f, xFront, Width, 0f, BoothTop, NoHoles, PlasterTile);
        WallAlongX(Kit("BoothWall"), BoothZMax - thick, -1f, xFront, Width, 0f, BoothTop, NoHoles, PlasterTile);
        Kit("BoothWall").Panel(new Vector3(xFront, BoothTop - 0.02f, BoothZMin), Vector3.right * BoothDepth, Vector3.forward * (BoothZMax - BoothZMin), PlasterTile);
        Kit("Metal").Panel(new Vector3(xFront - 0.04f, BoothTop, BoothZMin - 0.04f), Vector3.forward * (BoothZMax - BoothZMin + 0.08f), Vector3.right * (BoothDepth + 0.04f), MetalTile);

        Kit("T_SignDuty").Plate(new Vector3(xFront, 2.08f, zMiddle), Vector3.left, Vector3.up, 1f, 0.2f, 0.016f);

        // Its door, on the end facing the way the player comes in.
        Kit("Metal").Box(new Vector3(xFront + 0.75f, 1f, BoothZMin - 0.02f), new Vector3(0.84f, 2f, 0.04f), MetalTile);
        Kit("Stone").Box(new Vector3(xFront + 0.75f, 1.03f, BoothZMin - 0.008f), new Vector3(0.96f, 2.06f, 0.016f), StoneTile);
        Kit("Steel").Tube(new Vector3(xFront + 0.44f, 0.92f, BoothZMin - 0.07f), new Vector3(xFront + 0.44f, 1.12f, BoothZMin - 0.07f), 0.012f, 6, MetalTile);

        // Inside: a desk under the window, a chair, and the bread the dispatcher mentions.
        Kit("Wood").Box(new Vector3(xFront + 0.4f, 0.82f, zMiddle), new Vector3(0.6f, 0.04f, 2.2f), WoodTile);
        Kit("Metal").Box(new Vector3(xFront + 0.4f, 0.4f, BoothZMin + 0.5f), new Vector3(0.5f, 0.8f, 0.04f), MetalTile);
        Kit("Metal").Box(new Vector3(xFront + 0.4f, 0.4f, BoothZMax - 0.5f), new Vector3(0.5f, 0.8f, 0.04f), MetalTile);
        Kit("Wood").Box(new Vector3(xFront + 1.05f, 0.46f, zMiddle + 0.1f), new Vector3(0.42f, 0.05f, 0.42f), WoodTile);
        Kit("Wood").Box(new Vector3(xFront + 1.26f, 0.74f, zMiddle + 0.1f), new Vector3(0.04f, 0.56f, 0.42f), WoodTile);
        Kit("Paper").Box(new Vector3(xFront + 0.4f, 0.848f, zMiddle + 0.5f), new Vector3(0.34f, 0.012f, 0.42f), PaperTile);
        Vector2[] loaf = { new Vector2(0.13f, 0f), new Vector2(0.15f, 0.03f), new Vector2(0.14f, 0.07f), new Vector2(0.09f, 0.105f), new Vector2(0f, 0.12f) };
        Kit("Bread").Lathe(new Vector3(xFront + 0.4f, 0.854f, zMiddle + 0.5f), loaf, 12, PaperTile);

        PointLight(lights, "BoothLamp", new Vector3(xFront + 0.9f, 1.95f, zMiddle), new Color(1f, 0.84f, 0.62f), 0.9f, 6f);
        glow.Lathe(new Vector3(xFront + 0.9f, BoothTop - 0.1f, zMiddle), new[] { new Vector2(0f, 0f), new Vector2(0.05f, 0.03f), new Vector2(0.05f, 0.07f), new Vector2(0f, 0.08f) }, 10, Vector2.one);

        // Outside: the shelf with the shift log, on a chain so nobody walks off with it.
        const float shelfY = 1f;
        var book = new Vector3(xFront - 0.17f, shelfY + 0.02f, zMiddle);
        Kit("Wood").Box(new Vector3(xFront - 0.16f, shelfY, zMiddle), new Vector3(0.32f, 0.04f, 1.5f), WoodTile);
        foreach (float z in new[] { zMiddle - 0.6f, zMiddle + 0.6f })
            Kit("Metal").Box(new Vector3(xFront - 0.08f, shelfY - 0.12f, z), new Vector3(0.14f, 0.2f, 0.03f), MetalTile);
        Kit("Paper").Box(book + Vector3.up * 0.012f, new Vector3(0.26f, 0.024f, 0.4f), PaperTile);
        Kit("T_LogBook").Plate(book + Vector3.up * 0.024f, Vector3.up, Vector3.right, 0.4f, 0.26f, 0.002f);
        Kit("Steel").Tube(book + new Vector3(0.14f, 0.034f, 0.28f), book + new Vector3(0.02f, 0.034f, 0.34f), 0.005f, 6, MetalTile);
    }

    static void BuildSignal(Transform lights, MeshKit lamp)
    {
        const float x = 4.6f, headY = 2.05f, headDepth = 0.14f;
        Kit("Metal").Tube(new Vector3(x, 0f, SignalZ), new Vector3(x, headY, SignalZ), 0.04f, 8, MetalTile);
        Kit("SignalHead").Box(new Vector3(x, headY + 0.18f, SignalZ), new Vector3(0.24f, 0.56f, headDepth), MetalTile);

        // It faces back down the platform, towards the train.
        foreach (float y in new[] { headY + 0.03f, headY + 0.3f })
            Kit("DoorGlass").Disc(new Vector3(x, y, SignalZ - headDepth / 2f - 0.002f), Vector3.back, Vector3.up, 0.075f, 14);
        lamp.Disc(new Vector3(x, headY + 0.03f, SignalZ - headDepth / 2f - 0.004f), Vector3.back, Vector3.up, 0.07f, 14);
        PointLight(lights, "SignalGlow", new Vector3(x, headY + 0.03f, SignalZ - 0.5f), new Color(0.25f, 1f, 0.5f), 0.35f, 3.5f);
    }

    // The board a driver stops at, out on the edge of the platform where it shows through the windscreen.
    static void BuildStopBoard()
    {
        const float x = 4.45f, top = 2.3f;
        Kit("Metal").Tube(new Vector3(x, 0f, BoardZ), new Vector3(x, top, BoardZ), 0.03f, 8, MetalTile);
        Kit("T_SignStop").Plate(new Vector3(x, top - 0.1f, BoardZ - 0.03f), Vector3.back, Vector3.up, 0.8f, 0.4f, 0.02f);
    }

    // One carriage from zStart to zEnd, with passenger space up to saloonEnd.
    static void BuildCar(Transform lights, float zStart, float zEnd, float saloonEnd, bool hasCab)
    {
        float length = zEnd - zStart;
        float[] doors = { zStart + 2.3f, (zStart + saloonEnd) / 2f, saloonEnd - 2.3f };

        var holes = new List<Rect>();
        void Window(float a, float b) => holes.Add(Rect.MinMaxRect(a, WindowBottom, b, WindowTop));
        Window(zStart + 0.45f, doors[0] - 0.95f);
        for (int i = 0; i < doors.Length; i++)
        {
            holes.Add(Rect.MinMaxRect(doors[i] - 0.52f, DoorWindowBottom, doors[i] - 0.12f, WindowTop));
            holes.Add(Rect.MinMaxRect(doors[i] + 0.12f, DoorWindowBottom, doors[i] + 0.52f, WindowTop));
            if (i == doors.Length - 1) break;

            float a = doors[i] + 0.95f, b = doors[i + 1] - 0.95f, middle = (a + b) / 2f;
            Window(a, middle - 0.07f);
            Window(middle + 0.07f, b);
        }
        Window(doors[2] + 0.95f, saloonEnd - 0.45f);

        var platformSide = new List<Rect>(holes);
        var farSide = new List<Rect>(holes);
        if (hasCab)
        {
            platformSide.Add(CabDoorway);
            farSide.Add(Rect.MinMaxRect(CabBack + 0.35f, WindowBottom, TrainFront - 0.4f, WindowTop));
        }

        MeshKit skin = Kit("TrainBody"), lining = Kit("TrainInterior"), trim = Kit("TrainTrim");
        WallAlongZ(skin, BodyXMax, 1f, zStart, zEnd, SkirtY, RoofY, platformSide, TrainTile, trim, WallDepth);
        WallAlongZ(skin, BodyXMin, -1f, zStart, zEnd, SkirtY, RoofY, farSide, TrainTile, trim, WallDepth);
        WallAlongZ(lining, BodyXMax - WallDepth, -1f, zStart, zEnd, FloorY, CeilingY, platformSide, LiningTile);
        WallAlongZ(lining, BodyXMin + WallDepth, 1f, zStart, zEnd, FloorY, CeilingY, farSide, LiningTile);

        Kit("TrainFloor").Panel(new Vector3(BodyXMin, FloorY, zStart), Vector3.forward * length, Vector3.right * BodyWidth, MetalTile);
        Kit("TrainCeiling").Panel(new Vector3(BodyXMin, CeilingY, zStart), Vector3.right * BodyWidth, Vector3.forward * length, PlasterTile);
        Kit("TrainUnder").Panel(new Vector3(BodyXMin, RoofY, zStart), Vector3.forward * length, Vector3.right * BodyWidth, MetalTile);

        WallAlongX(skin, zStart, -1f, BodyXMin, BodyXMax, SkirtY, RoofY, NoHoles, TrainTile);
        WallAlongX(lining, zStart + WallDepth, 1f, BodyXMin, BodyXMax, FloorY, CeilingY, NoHoles, LiningTile);
        if (!hasCab)
        {
            WallAlongX(skin, zEnd, 1f, BodyXMin, BodyXMax, SkirtY, RoofY, NoHoles, TrainTile);
            WallAlongX(lining, zEnd - WallDepth, -1f, BodyXMin, BodyXMax, FloorY, CeilingY, NoHoles, LiningTile);
        }

        foreach (float door in doors)
        {
            foreach (float x in new[] { BodyXMax + 0.004f, BodyXMin - 0.004f })
            {
                float height = (FloorY + CarDoorTop) / 2f, tall = CarDoorTop - FloorY;
                foreach (float z in new[] { door - 0.68f, door, door + 0.68f })
                    trim.Box(new Vector3(x, height, z), new Vector3(0.012f, tall, 0.03f), MetalTile);
                trim.Box(new Vector3(x, CarDoorTop, door), new Vector3(0.012f, 0.03f, 1.39f), MetalTile);
            }
            Kit("TrainSteel").Tube(new Vector3(TrackX, FloorY, door), new Vector3(TrackX, CeilingY, door), 0.018f, 6, MetalTile);
        }

        Seats(zStart + 0.35f, doors[0] - 0.8f);
        Seats(doors[0] + 0.8f, doors[1] - 0.8f);
        Seats(doors[1] + 0.8f, doors[2] - 0.8f);
        Seats(doors[2] + 0.8f, saloonEnd - 0.2f);

        float saloonMiddle = (zStart + saloonEnd) / 2f, strip = saloonEnd - zStart - 1.4f;
        trainGlow.Box(new Vector3(TrackX, CeilingY - 0.012f, saloonMiddle), new Vector3(0.24f, 0.02f, strip), Vector2.one);
        AreaLight(lights, "CarLight", new Vector3(TrackX, CeilingY - 0.03f, saloonMiddle), new Vector2(0.24f, strip), CarLightColor, 12f);

        // Under the floor: the frame, then a bogie at each end.
        Kit("TrainUnder").Box(new Vector3(TrackX, (SkirtY + FloorY) / 2f - 0.01f, (zStart + zEnd) / 2f), new Vector3(BodyWidth - 0.04f, FloorY - SkirtY - 0.02f, length - 0.04f), MetalTile);
        float wheelY = RailY + WheelRadius;
        foreach (float z in new[] { zStart + 2.6f, zEnd - 2.6f })
        {
            Kit("TrainUnder").Box(new Vector3(TrackX, wheelY, z), new Vector3(1.9f, 0.3f, 2.3f), MetalTile);
            foreach (float axle in new[] { z - 0.85f, z + 0.85f })
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = TrackX + side * Gauge / 2f;
                    Kit("TrainUnder").Tube(new Vector3(x - 0.05f, wheelY, axle), new Vector3(x + 0.05f, wheelY, axle), WheelRadius, 12, MetalTile);
                    Kit("TrainUnder").Disc(new Vector3(x + side * 0.05f, wheelY, axle), Vector3.right * side, Vector3.up, WheelRadius, 12);
                }
            }
        }
    }

    // A red bench down each wall, between a pair of doors.
    static void Seats(float zStart, float zEnd)
    {
        float middle = (zStart + zEnd) / 2f, length = zEnd - zStart;
        foreach (float inward in new[] { 1f, -1f })
        {
            float wall = inward > 0f ? BodyXMin + WallDepth : BodyXMax - WallDepth;
            Kit("TrainSeat").Box(new Vector3(wall + inward * 0.25f, 0.46f, middle), new Vector3(0.48f, 0.1f, length), MetalTile);
            Kit("TrainSeat").Box(new Vector3(wall + inward * 0.05f, 0.76f, middle), new Vector3(0.08f, 0.5f, length), MetalTile);
            Kit("TrainUnder").Box(new Vector3(wall + inward * 0.2f, 0.22f, middle), new Vector3(0.34f, 0.36f, length), MetalTile);
        }
    }

    static void BuildCab(Transform lights)
    {
        MeshKit lining = Kit("TrainInterior"), trim = Kit("TrainTrim");
        WallAlongX(lining, CabBack, 1f, BodyXMin, BodyXMax, FloorY, CeilingY, NoHoles, LiningTile);
        WallAlongX(lining, CabBack - 0.05f, -1f, BodyXMin, BodyXMax, FloorY, CeilingY, NoHoles, LiningTile);
        trim.Box(new Vector3(TrackX, 1.02f, CabBack + 0.01f), new Vector3(0.72f, 1.96f, 0.02f), MetalTile);

        Rect[] windscreen = { Rect.MinMaxRect(1.5f, 1.15f, 3.5f, 1.98f) };
        WallAlongX(Kit("TrainBody"), TrainFront, 1f, BodyXMin, BodyXMax, SkirtY, RoofY, windscreen, TrainTile, trim, WallDepth);
        WallAlongX(lining, TrainFront - WallDepth, -1f, BodyXMin, BodyXMax, FloorY, CeilingY, windscreen, LiningTile);

        // The desk: a steel base and a sloped instrument panel facing the seat.
        const float deskBack = TrainFront - 0.74f, deskFront = TrainFront - WallDepth;
        float deskMiddle = (deskBack + deskFront) / 2f;
        trim.Box(new Vector3(TrackX, 0.48f, deskMiddle), new Vector3(2.4f, 0.88f, deskFront - deskBack), MetalTile);
        var near = new Vector3(0f, ConsoleNearY, deskBack);
        var far = new Vector3(0f, ConsoleFarY, deskFront - 0.01f);
        Kit("TrainConsole").Quad(near + Vector3.right * ConsoleXMax, near + Vector3.right * ConsoleXMin, far + Vector3.right * ConsoleXMin, far + Vector3.right * ConsoleXMax,
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        BuildControllerBody();

        // The radio on the right of the desk.
        trim.Box(new Vector3(3.6f, 1.03f, deskBack + 0.2f), new Vector3(0.14f, 0.1f, 0.22f), MetalTile);

        // The driver's seat, on a post.
        var seat = new Vector3(TrackX, 0f, SeatZ);
        Kit("TrainSteel").Tube(seat + Vector3.up * FloorY, seat + Vector3.up * (SeatTop - 0.06f), 0.04f, 8, MetalTile);
        Kit("TrainUnder").Box(seat + Vector3.up * (SeatTop - 0.04f), new Vector3(0.5f, 0.08f, 0.46f), MetalTile);
        Kit("TrainUnder").Box(seat + new Vector3(0f, SeatTop + 0.33f, -0.24f), new Vector3(0.46f, 0.66f, 0.07f), MetalTile);

        // A plate over the gap between the cab and the platform.
        trim.Box(new Vector3((BodyXMax + EdgeX) / 2f, 0.02f, (CabDoorMin + CabDoorMax) / 2f), new Vector3(EdgeX - BodyXMax + 0.2f, 0.05f, CabDoorMax - CabDoorMin), MetalTile);

        // A hooded lamp over the desk, so the driver can read the dials and find the controller.
        var deskLamp = new Vector3(2f, 1.98f, TrainFront - 0.2f);
        trim.Box(deskLamp + Vector3.up * 0.06f, new Vector3(0.36f, 0.06f, 0.14f), MetalTile);
        trainGlow.Box(deskLamp + Vector3.up * 0.02f, new Vector3(0.3f, 0.02f, 0.07f), Vector2.one);
        PointLight(lights, "DeskLamp", deskLamp + new Vector3(0f, -0.25f, -0.2f), new Color(1f, 0.9f, 0.74f), 0.22f, 3f);

        var dome = new Vector3(TrackX, CeilingY - 0.01f, 32f);
        trainGlow.Disc(dome, Vector3.down, Vector3.forward, 0.12f, 12);
        PointLight(lights, "CabLamp", dome + Vector3.down * 0.2f, new Color(1f, 0.88f, 0.7f), 0.32f, 5f);
    }

    static void BuildTrainEnds(Transform lights, MeshKit tailLamps)
    {
        MeshKit trim = Kit("TrainTrim");

        // Between the cars: a dark bellows and the coupling under it.
        float gapMiddle = (RearCarEnd + FrontCarStart) / 2f, gap = FrontCarStart - RearCarEnd;
        Kit("TrainUnder").Box(new Vector3(TrackX, 1.2f, gapMiddle), new Vector3(1.9f, 2.2f, gap + 0.02f), MetalTile);
        Kit("TrainUnder").Box(new Vector3(TrackX, -0.3f, gapMiddle), new Vector3(0.3f, 0.2f, gap + 0.02f), MetalTile);

        foreach (float side in new[] { -1f, 1f })
        {
            float x = TrackX + side * 0.95f;

            // Headlamps on the nose.
            trim.Box(new Vector3(x, 0.62f, TrainFront + 0.02f), new Vector3(0.34f, 0.26f, 0.04f), MetalTile);
            trainGlow.Disc(new Vector3(x, 0.62f, TrainFront + 0.042f), Vector3.forward, Vector3.up, 0.1f, 14);

            // Tail lamps on the back of the rear car.
            trim.Box(new Vector3(x, 0.62f, RearCarStart - 0.02f), new Vector3(0.3f, 0.2f, 0.04f), MetalTile);
            tailLamps.Disc(new Vector3(x, 0.62f, RearCarStart - 0.042f), Vector3.back, Vector3.up, 0.075f, 14);
        }

        Light beam = BakedLight(lights, "Headlights", LightType.Spot, new Vector3(TrackX, 0.75f, TrainFront + 0.3f), Quaternion.LookRotation(Vector3.forward), HeadlightColor, 36f);
        beam.range = 24f;
        beam.spotAngle = 64f;

        Kit("TrainSign").Plate(new Vector3(TrackX, 2.14f, TrainFront + 0.002f), Vector3.forward, Vector3.up, 0.9f, 0.22f, 0.01f);
    }

    static void BuildGrime(Transform parent)
    {
        var rising = new MeshKit();
        var streaks = new MeshKit();
        var blotches = new MeshKit();

        StainHall(rising, streaks, blotches);
        MeshKit.Origin = Vector3.forward * NextHallZ;
        StainHall(rising, streaks, blotches);
        MeshKit.Origin = Vector3.zero;

        CommitStains("Stains_Rising", "T_GrimeRising", rising, parent);
        CommitStains("Stains_Streaks", "T_GrimeStreaks", streaks, parent);
        CommitStains("Stains_Blotches", "T_GrimeBlotch", blotches, parent);
    }

    static void StainHall(MeshKit rising, MeshKit streaks, MeshKit blotches)
    {
        // Dirt climbing the foot of the walls, and the floor darkening towards them.
        Stain(rising, new Vector3(Width, 0f, 0f), Vector3.forward * Length, Vector3.up * 0.46f, Vector3.left, 9f);
        Stain(rising, new Vector3(PortalXMax + 0.16f, 0f, 0f), Vector3.right * (DoorXMin - PortalXMax - 0.3f), Vector3.up * 0.46f, Vector3.forward, 1f);
        Stain(rising, new Vector3(DoorXMax + 0.14f, 0f, 0f), Vector3.right * (Width - DoorXMax - 0.14f), Vector3.up * 0.46f, Vector3.forward, 1f);
        Stain(rising, new Vector3(PortalXMax + 0.16f, 0f, Length), Vector3.right * (Width - PortalXMax - 0.16f), Vector3.up * 0.46f, Vector3.back, 2f);
        Stain(rising, new Vector3(Width, 0f, 0f), Vector3.forward * Length, Vector3.left * 1.3f, Vector3.up, 9f);
        Stain(rising, new Vector3(EdgeX, 0f, 0f), Vector3.right * (Width - EdgeX), Vector3.forward * 1.2f, Vector3.up, 2f);
        Stain(rising, new Vector3(EdgeX, 0f, Length), Vector3.right * (Width - EdgeX), Vector3.back * 1.2f, Vector3.up, 2f);

        // Water running down the tiles from the capping, on both sides of the track.
        Stain(streaks, new Vector3(Width, 1f, 0f), Vector3.forward * Length, Vector3.up * 1.06f, Vector3.left, 8f);
        Stain(streaks, new Vector3(Width, TileTop, 0f), Vector3.forward * Length, Vector3.up * (Spring - TileTop), Vector3.left, 6f);
        Stain(streaks, new Vector3(0f, 0.7f, 0f), Vector3.forward * Length, Vector3.up * 1.36f, Vector3.right, 8f);

        // Soot over each tube, and worn patches where people wait.
        foreach (float z in new[] { 3.5f, 10.5f, 17.5f, 24.5f, 31.5f, 38.5f })
            VaultStain(blotches, Arch, LampX - 1.5f, LampX + 1.5f, z - 1.8f, z + 1.8f);
        Stain(blotches, new Vector3(4.7f, 0f, 4f), Vector3.right * 2.6f, Vector3.forward * 3f, Vector3.up, 1f);
        Stain(blotches, new Vector3(5f, 0f, 14f), Vector3.right * 3f, Vector3.forward * 3.4f, Vector3.up, 1f);
        Stain(blotches, new Vector3(4.6f, 0f, 22f), Vector3.right * 2.4f, Vector3.forward * 3.2f, Vector3.up, 1f);
        Stain(blotches, new Vector3(5.4f, 0f, 33f), Vector3.right * 2.8f, Vector3.forward * 3.6f, Vector3.up, 1f);
    }

    static void BuildProbes()
    {
        var positions = new List<Vector3>();
        foreach (float y in new[] { 0.4f, 1.4f, 2.4f })
        {
            foreach (float x in new[] { 4.5f, 6.3f, 8.3f })
                for (float z = 0.6f; z < Length; z += 5f)
                    positions.Add(new Vector3(x, y, z));
            foreach (float z in new[] { 31.2f, 32.8f })
                foreach (float x in new[] { 1.6f, 3.4f })
                    positions.Add(new Vector3(x, Mathf.Min(y, 2.1f), z));
        }
        var go = new GameObject("LightProbes");
        go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
    }

    static TalkSpot Spot(string name, Transform parent, Vector3 position, Vector3 lookAt, GameUi ui, FirstPersonController player, string prompt, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;

        var focus = new GameObject("Focus").transform;
        focus.SetParent(go.transform);
        focus.position = lookAt;

        var spot = go.AddComponent<TalkSpot>();
        spot.dialogue = ui.Dialogue;
        spot.player = player;
        spot.person = focus;
        spot.prompt = prompt;
        spot.firstLabel = label;
        spot.laterLabel = label + "_again";
        return spot;
    }

    static void Barrier(string name, Transform parent, Vector3 center, Vector3 size)
    {
        var box = new GameObject(name).AddComponent<BoxCollider>();
        box.transform.SetParent(parent);
        box.transform.position = center;
        box.size = size;
    }

    // Baked with everything else so that it matches, but free to move because it isn't batched.
    static GameObject Dynamic(string name, Transform parent, MeshKit kit, string surface)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit, name);
        go.AddComponent<MeshRenderer>().sharedMaterial = SurfaceMaterial(surface);
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
        return go;
    }

    // The speedometer's needle, pivoting over the painted dial on the sloped console.
    static Transform BuildSpeedNeedle()
    {
        const float deskBack = TrainFront - 0.74f, deskFront = TrainFront - WallDepth;
        var near = new Vector3(0f, ConsoleNearY, deskBack);
        var far = new Vector3(0f, ConsoleFarY, deskFront - 0.01f);
        Vector3 up = (far - near).normalized, facing = Vector3.Cross(up, Vector3.right);

        float x = Mathf.Lerp(ConsoleXMin, ConsoleXMax, TextureForge.SpeedDialX / TextureForge.ConsoleWidth);
        Vector3 pivot = Vector3.Lerp(near, far, TextureForge.SpeedDialY / TextureForge.ConsoleHeight) + Vector3.right * x;

        var kit = new MeshKit();
        kit.Box(new Vector3(0f, 0.08f, -0.004f), new Vector3(0.012f, 0.19f, 0.004f), MetalTile);
        GameObject needle = Dynamic("SpeedNeedle", train, kit, "TrainSeat");
        needle.transform.SetPositionAndRotation(pivot + facing * 0.004f, Quaternion.LookRotation(-facing, up));
        return needle.transform;
    }

    // Where the controller stands on the sloping desk, and which ways are along and up off it.
    static void ControllerPlace(out Vector3 seat, out Vector3 up, out Vector3 facing)
    {
        const float deskBack = TrainFront - 0.74f, deskFront = TrainFront - WallDepth;
        var near = new Vector3(ControllerX, ConsoleNearY, deskBack);
        var far = new Vector3(ControllerX, ConsoleFarY, deskFront - 0.01f);
        up = (far - near).normalized;
        facing = Vector3.Cross(up, Vector3.right);
        seat = Vector3.Lerp(near, far, ControllerAlong);
    }

    // The controller's housing and marked scale, which belong to the desk. The arm is built with the drive.
    static void BuildControllerBody()
    {
        const float width = 0.24f, length = 0.46f;
        ControllerPlace(out Vector3 seat, out Vector3 up, out Vector3 facing);
        Quaternion onDesk = Quaternion.LookRotation(up, facing);
        Kit("TrainControllerBody").Box(Matrix4x4.TRS(seat + facing * (ControllerBodyHeight / 2f), onDesk, Vector3.one), new Vector3(width, ControllerBodyHeight, length), MetalTile);
        Kit("TrainControllerScale").Plate(seat + facing * ControllerBodyHeight, facing, up, 0.2f, 0.4f, 0.002f);
    }

    // The arm of the controller, with a T-grip, tipping forward for power and back for the brake.
    static Transform BuildPowerLever()
    {
        ControllerPlace(out Vector3 seat, out Vector3 up, out Vector3 facing);
        var pivot = new GameObject("PowerLever").transform;
        pivot.SetParent(train, false);
        pivot.SetPositionAndRotation(seat + facing * ControllerBodyHeight, Quaternion.LookRotation(up, facing));

        const float armLength = 0.25f, gripWidth = 0.17f;
        var arm = new MeshKit();
        arm.Tube(Vector3.down * 0.02f, Vector3.up * armLength, 0.017f, 8, MetalTile);
        Dynamic("Arm", pivot, arm, "TrainSteel");

        var grip = new MeshKit();
        grip.Lathe(Vector3.down * 0.002f, new[] { new Vector2(0.04f, 0f), new Vector2(0.036f, 0.018f), new Vector2(0.022f, 0.05f), new Vector2(0.018f, 0.08f) }, 10, MetalTile);
        grip.Tube(new Vector3(-gripWidth / 2f, armLength, 0f), new Vector3(gripWidth / 2f, armLength, 0f), 0.026f, 10, MetalTile);
        foreach (float side in new[] { -1f, 1f })
            grip.Disc(new Vector3(side * gripWidth / 2f, armLength, 0f), Vector3.right * side, Vector3.up, 0.026f, 10);
        Dynamic("Grip", pivot, grip, "TrainGrip");
        return pivot;
    }

    // Everything the player acts on: the intercom, the jobs, the cab door and the seat.
    static TrainDrive BuildShift(FirstPersonController player)
    {
        Transform parent = new GameObject("Shift").transform;
        GameUi ui = BuildGameUi(ScriptPath);
        TrainDrive drive = BuildDrive(player, ui);

        // The player's not to walk off the edge: the only way through it is the cab doorway.
        // It's all one object, so a task that needs the track can simply switch it off.
        Transform edge = new GameObject("PlatformEdge").transform;
        edge.SetParent(parent);
        const float barrierX = EdgeX - 0.1f;
        Barrier("South", edge, new Vector3(barrierX, 1f, (CabDoorMin - 0.5f) / 2f), new Vector3(0.2f, 3f, CabDoorMin + 0.5f));
        Barrier("North", edge, new Vector3(barrierX, 1f, (CabDoorMax + Length + 0.5f) / 2f), new Vector3(0.2f, 3f, Length + 0.5f - CabDoorMax));
        float doorMiddle = (CabDoorMin + CabDoorMax) / 2f, doorWidth = CabDoorMax - CabDoorMin;

        // There's no going back the way the player came.
        Barrier("PassageBarrier", parent, new Vector3((DoorXMin + DoorXMax) / 2f, 1f, PassageBarrierZ), new Vector3(DoorXMax - DoorXMin + 0.4f, 3f, 0.2f));

        // The call lamp on the intercom.
        var lampKit = new MeshKit();
        lampKit.Lathe(new Vector3(Width - 0.07f, 1.65f, IntercomZ), new[] { new Vector2(0.03f, 0f), new Vector2(0.03f, 0.02f), new Vector2(0.015f, 0.05f), new Vector2(0f, 0.055f) }, 10, Vector2.one);
        Material lampMaterial = LoadOrCreateMaterial("CallLamp", "Universal Render Pipeline/Unlit");
        var callLampGo = new GameObject("CallLamp");
        callLampGo.transform.SetParent(parent);
        callLampGo.AddComponent<MeshFilter>().sharedMesh = SaveMesh(lampKit, "CallLamp");
        callLampGo.AddComponent<MeshRenderer>().sharedMaterial = lampMaterial;
        var callLamp = callLampGo.AddComponent<CallLamp>();

        // The key on hook two.
        var keyHook = new Vector3(Width - 0.05f, 1.42f + (138f / 176f - 0.5f) * 0.66f, KeyBoardZ + (0.5f - 96f / 256f) * 0.96f);
        var keyKit = new MeshKit();
        Vector3 keyRing = keyHook + Vector3.down * 0.03f;
        const int ringSegments = 12;
        for (int i = 0; i < ringSegments; i++)
        {
            float a0 = i * Mathf.PI * 2f / ringSegments, a1 = (i + 1) * Mathf.PI * 2f / ringSegments;
            keyKit.Tube(keyRing + new Vector3(0f, Mathf.Sin(a0), Mathf.Cos(a0)) * 0.02f, keyRing + new Vector3(0f, Mathf.Sin(a1), Mathf.Cos(a1)) * 0.02f, 0.005f, 4, MetalTile);
        }
        keyKit.Box(keyHook + new Vector3(0f, -0.11f, 0f), new Vector3(0.006f, 0.12f, 0.01f), MetalTile);
        keyKit.Box(keyHook + new Vector3(0f, -0.155f, 0.014f), new Vector3(0.006f, 0.014f, 0.02f), MetalTile);
        GameObject key = Dynamic("CabKey", parent, keyKit, "Brass");

        // The cab door: two skins and a window, sliding back along the body.
        var doorKit = new MeshKit();
        Rect[] doorWindow = { Rect.MinMaxRect(CabDoorMin + 0.2f, WindowBottom, CabDoorMax - 0.2f, WindowTop) };
        WallAlongZ(doorKit, BodyXMax + 0.03f, 1f, CabDoorMin - 0.02f, CabDoorMax + 0.02f, FloorY, CabDoorTop + 0.02f, doorWindow, TrainTile);
        WallAlongZ(doorKit, BodyXMax + 0.005f, -1f, CabDoorMin - 0.02f, CabDoorMax + 0.02f, FloorY, CabDoorTop + 0.02f, doorWindow, TrainTile);
        GameObject cabDoor = Dynamic("CabDoor", train, doorKit, "TrainBody");
        var doorCollider = cabDoor.AddComponent<BoxCollider>();
        doorCollider.center = new Vector3(BodyXMax + 0.02f, 1f, doorMiddle);
        doorCollider.size = new Vector3(0.08f, 2f, doorWidth + 0.04f);
        var slide = cabDoor.AddComponent<SlideOpen>();
        slide.offset = Vector3.back * (doorWidth + 0.06f);
        slide.sound = Emitter(cabDoor.transform, "DoorSound", new Vector3(BodyXMax, 1f, doorMiddle), Sound("I_CabDoor"), 0.7f, 2f, 24f);

        RemarkZone locked = Zone("Remark_CabDoor", parent, new Vector3(EdgeX + 0.22f, 1f, doorMiddle), new Vector3(0.44f, 2f, doorWidth), ui.Dialogue, "cab_locked");

        float xBooth = Width - BoothDepth - 0.17f, zBooth = (BoothZMin + BoothZMax) / 2f;
        TalkSpot intercom = Spot("Intercom", parent, new Vector3(Width - 0.5f, 1.2f, IntercomZ), new Vector3(Width - 0.1f, 1.46f, IntercomZ), ui, player, "E   Answer", "briefing");
        intercom.laterLabel = "intercom_again";
        TalkSpot log = Spot("ShiftLog", parent, new Vector3(xBooth - 0.3f, 1.2f, zBooth), new Vector3(xBooth, 1.04f, zBooth), ui, player, "E   Sign", "log");
        TalkSpot board = Spot("KeyBoard", parent, new Vector3(Width - 0.5f, 1.2f, KeyBoardZ), new Vector3(Width - 0.05f, 1.42f, KeyBoardZ), ui, player, "E   Take the key", "key");
        TalkSpot signal = Spot("Signal", parent, new Vector3(4.9f, 1.2f, SignalZ - 0.6f), new Vector3(4.6f, 2.1f, SignalZ), ui, player, "E   Check", "signal");
        TalkSpot seat = Spot("DriverSeat", parent, new Vector3(TrackX + 0.4f, 1.2f, 31.7f), new Vector3(TrackX, 1.5f, TrainFront), ui, player, "E   Sit down", "seat");
        seat.lookAccuracy = 0.5f;
        seat.reach = 1.5f;

        UnityEventTools.AddBoolPersistentListener(board.onFirstConversationEnded, key.SetActive, false);
        UnityEventTools.AddPersistentListener(seat.onTalkStarted, drive.TakeSeat);

        var exit = parent.gameObject.AddComponent<SceneTransitionTrigger>();
        exit.targetSceneName = "MainMenu";
        exit.fade = ui.Fade;

        var topLeft = new Vector2(0f, 1f);
        RectTransform listRect = UiChild("Checklist", ui.Canvas, topLeft, topLeft, new Vector2(370f, -130f), new Vector2(640f, 180f));
        TMP_Text list = UiText("Checklist", listRect, 24f, TextAlignmentOptions.TopLeft);
        list.color = new Color(0.82f, 0.8f, 0.74f);

        var checklist = ui.Canvas.gameObject.AddComponent<ShiftChecklist>();
        checklist.listText = list;
        checklist.steps = new[]
        {
            new ShiftChecklist.Step { tasks = new[] { "Speak to the dispatcher" }, spots = new[] { intercom } },
            new ShiftChecklist.Step { tasks = new[] { "Sign the shift log", "Take the cab key", "Check the signal by the tunnel" }, spots = new[] { log, board, signal } },
            new ShiftChecklist.Step { tasks = new[] { "Take your seat in the cab" }, spots = new[] { seat } },
            new ShiftChecklist.Step { tasks = new[] { "Drive to Severna", "Stop at the board on the platform" }, spots = new TalkSpot[0] },
        };
        UnityEventTools.AddPersistentListener(intercom.onTalkStarted, callLamp.Answer);
        UnityEventTools.AddPersistentListener(checklist.steps[1].onFinished, slide.Open);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[1].onFinished, locked.gameObject.SetActive, false);
        UnityEventTools.AddPersistentListener(checklist.steps[2].onFinished, slide.Close);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[2].onFinished, seat.gameObject.SetActive, false);
        UnityEventTools.AddPersistentListener(checklist.steps[2].onFinished, drive.BeginRun);
        UnityEventTools.AddPersistentListener(drive.onRunFinished, checklist.FinishStep);
        UnityEventTools.AddPersistentListener(checklist.steps[3].onFinished, exit.Leave);
        return drive;
    }

    static TrainDrive BuildDrive(FirstPersonController player, GameUi ui)
    {
        var seatPoint = new GameObject("SeatPoint").transform;
        seatPoint.SetParent(train);
        seatPoint.position = new Vector3(TrackX, SeatedEyeY - player.cameraTransform.localPosition.y, SeatZ);

        var beam = new GameObject("Headlight").AddComponent<Light>();
        beam.transform.SetParent(train);
        beam.transform.SetPositionAndRotation(new Vector3(TrackX, 0.8f, TrainFront + 0.2f), Quaternion.LookRotation(new Vector3(0f, -0.02f, 1f)));
        beam.type = LightType.Spot;
        beam.lightmapBakeType = LightmapBakeType.Realtime;
        beam.shadows = LightShadows.None;
        beam.color = HeadlightColor;
        beam.intensity = 26f;
        beam.range = 90f;
        beam.spotAngle = 130f;
        beam.innerSpotAngle = 20f;
        beam.enabled = false;

        var drive = train.gameObject.AddComponent<TrainDrive>();
        drive.player = player;
        drive.dialogue = ui.Dialogue;
        drive.seat = seatPoint;
        drive.headlight = beam;
        drive.speedNeedle = BuildSpeedNeedle();
        drive.powerLever = BuildPowerLever();
        drive.stopAt = NextHallZ + BoardZ - TrainFront;
        return drive;
    }
}
