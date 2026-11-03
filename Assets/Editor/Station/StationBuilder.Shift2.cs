using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SceneForge;

// Shift 2: the last service of the night, with people on it. It starts at Severna, stops at the hospital, gets held at
// the station that isn't on the registry's map, and comes up at the depot into the morning, where there's a bus home.
// Every surface can take a real-time light here, because the player spends part of the shift carrying a lamp.
public static partial class StationBuilder
{
    const string Shift2Path = "Assets/Scenes/Shift2.unity";
    const string Shift2RootDir = "Assets/Art/Generated/Shift2";
    const string Shift2Script = "Assets/Dialogue/Shift2.txt";
    const string PersonControllerDir = "Assets/Art/Generated/Characters/Animators";

    // Metres along the line. The closed station is built like the others, then left.
    const float ClosedHallZ = NextHallEnd + 380f, ClosedHallEnd = ClosedHallZ + Length;

    const float StaffRoomXMin = SevernaStaffDoorX - 1.65f, StaffRoomXMax = StaffRoomXMin + 3.4f, StaffRoomDepth = 6.4f, StaffRoomTop = 2.5f, StaffWallThickness = 0.14f;
    const float LockerWidth = 0.44f, LockerDepth = 0.46f, LockerHeight = 1.86f;

    // From the start of the closed station's hall.
    const float OldRoomDoorZ = 26.6f, OldRoomXMax = Width + 4.2f, OldRoomZMin = 23.4f, OldRoomZMax = 30f, RelayPassageEnd = -14f;

    // On Severna's bench, and the way out of the hospital's platform.
    const float MitkoZ = 30.7f, HospitalExitZ = 24.6f, HospitalExitLength = 7f;

    // The seats in the front car, in the train's own space.
    const float PlatformSeatX = BodyXMax - WallDepth - 0.25f, FarSeatX = BodyXMin + WallDepth + 0.25f, TrainSeatHeight = 0.515f;

    // How far the train's own space is along the line at each stop, for placing people on the platforms.
    const float AtHospital = NextHallZ + BoardZ - TrainFront, AtDepot = DepotBoardZ - TrainFront;

    [MenuItem("Metropoliten/Build Shift 2 Scene")]
    public static void BuildShift2FromMenu()
    {
        bool confirmed = !File.Exists(Shift2Path) || EditorUtility.DisplayDialog("Rebuild Shift 2 scene?",
            "This replaces the scene and rebakes its lighting. Hand edits to it will be lost.", "Rebuild", "Cancel");
        if (confirmed) BuildShift2();
    }

    public static void BuildShift2()
    {
        Begin(Shift2RootDir, Surfaces, LightmapScales, null, true);
        TextureForge.ForgeStation();
        TextureForge.ForgeShift2();
        TextureForge.ForgeCast();
        GaitForge.ForgeAll();
        RetroLookSetup.Apply();
        PhotoStudio.Shoot();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Transform lights = new GameObject("Lights").transform;
        train = new GameObject("Train").transform;
        train.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        trainGlow = new MeshKit();
        amberGlow = new MeshKit();
        var tailLamps = new MeshKit();
        var signalLamp = new MeshKit();
        var redLamp = new MeshKit();
        var clearLamp = new MeshKit();

        // Severna.
        backWall = SevernaWidth;
        BuildSevernaHall(lights, "T_SignSeverna", true);
        Kit("T_MetroMapStation").Plate(new Vector3(SevernaWidth - 0.012f, 1.46f, 24.6f), Vector3.left, Vector3.up, 1.5f, 1.13f, 0.02f);
        Poster("T_PosterDrivers", 15.6f);
        Poster("T_PosterLine", 33.6f);
        BuildIntercom();
        BuildBooth(lights);
        BuildSignal(lights, signalLamp);
        BuildPassage(lights, PassageEnd, true, SevernaShift);
        BuildStaffRoom(lights);
        backWall = Width;

        // Bolnichna, the stop for the hospital.
        MeshKit.Origin = Vector3.forward * NextHallZ;
        BuildHall("TileBandNext", "Tile", HospitalExitZ);
        BuildEndWalls("TileBandNext", false);
        BuildStationWall("T_SignHospital");
        BuildLamps(lights, (3.5f, 0f, null), (10.5f, 12f, glow), (17.5f, 16f, glow), (24.5f, 18f, glow), (31.5f, 20f, glow), (38.5f, 14f, faultyGlow));
        Poster("T_PosterLine", 30.6f);
        BuildStopBoard();
        BuildHospitalExit();

        MeshKit.Origin = Vector3.forward * ClosedHallZ;
        BuildClosedStation(lights, redLamp, clearLamp);

        MeshKit.Origin = Vector3.forward * TerminalZ;
        BuildStopBoard();
        MeshKit.Origin = Vector3.zero;
        BuildTerminal(lights);

        BuildBore(-TunnelLength, 0f);
        BuildBore(Length, NextHallZ);
        BuildBore(NextHallEnd, ClosedHallZ);
        BuildBore(ClosedHallEnd, PortalZ);
        foreach ((float z, float facing) in new[] { (Length + 0.02f, 1f), (NextHallZ - 0.02f, -1f), (NextHallEnd + 0.02f, 1f), (ClosedHallZ - 0.02f, -1f), (ClosedHallEnd + 0.02f, 1f) })
            BuildPortalBack(z, facing);
        BuildRunDressing(lights, Length, NextHallZ, true);
        BuildRunLandmarks(lights, signalLamp);
        BuildRunDressing(lights, NextHallEnd, ClosedHallZ, true);
        BuildRunDressing(lights, ClosedHallEnd, PortalZ, false);
        BuildTrack(-TunnelLength, LineEnd);
        BuildTrain(lights, tailLamps);

        Transform architecture = new GameObject("Architecture").transform;
        CommitSurfaces(architecture, train);
        CommitGlow(architecture);
        CommitUnlit("GlobeGlow", amberGlow, new Color(3.1f, 2.1f, 1.2f), architecture);
        CommitUnlit("WindowGlow", windowGlow, new Color(0.96f, 0.68f, 0.36f), architecture);
        CommitUnlit("TrainGlow", trainGlow, LampColor, train);
        CommitUnlit("TailLamps", tailLamps, new Color(2.4f, 0.12f, 0.08f), train);
        CommitUnlit("SignalLamp", signalLamp, new Color(0.1f, 2.4f, 0.42f), architecture);
        GameObject red = CommitUnlit("ClosedSignalRed", redLamp, new Color(2.6f, 0.12f, 0.06f), architecture);
        GameObject clear = CommitUnlit("ClosedSignalClear", clearLamp, new Color(0.1f, 2.4f, 0.42f), architecture);
        clear.SetActive(false);
        BuildShift2Grime(architecture);

        BuildShift2Probes();
        FirstPersonController player = BuildPlayer(new Vector3((DoorXMin + DoorXMax) / 2f + SevernaShift, 0.92f, SpawnZ));
        TrainDrive drive = BuildShift2Jobs(player, red, clear);
        BuildShift2Sound(player, drive);
        ConfigureEnvironment(0.028f);

        EditorSceneManager.SaveScene(scene, Shift2Path);
        AddToBuildSettings(Shift2Path);
        BakeLighting("Shift2Lighting");
        EditorSceneManager.SaveScene(scene, Shift2Path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Built and baked {Shift2Path}.");
    }

    static void BuildShift2Grime(Transform parent)
    {
        var rising = new MeshKit();
        var streaks = new MeshKit();
        var blotches = new MeshKit();

        StainSeverna(rising, streaks, blotches);
        foreach (float hall in new[] { NextHallZ, ClosedHallZ, ClosedHallZ })
        {
            MeshKit.Origin = Vector3.forward * hall;
            StainHall(rising, streaks, blotches);
        }
        MeshKit.Origin = Vector3.zero;

        CommitStains("Stains_Rising", "T_GrimeRising", rising, parent);
        CommitStains("Stains_Streaks", "T_GrimeStreaks", streaks, parent);
        CommitStains("Stains_Blotches", "T_GrimeBlotch", blotches, parent);
        CommitStains("DirtyGlass", "T_GlassDirt", dirtyGlass, parent);
    }

    static void BuildShift2Probes()
    {
        var positions = new List<Vector3>();
        var heights = new[] { 0.4f, 1.4f, 2.4f };
        ProbeGrid(positions, new[] { 4.5f, 6.3f, 8.8f, 11.4f }, heights, 0.6f, Length, 5f);
        ProbeGrid(positions, new[] { 4.5f, 6.3f, 8.3f }, heights, NextHallZ + 0.6f, NextHallEnd, 5f);
        ProbeGrid(positions, new[] { 4.5f, 6.3f, 9f }, heights, TerminalZ - 6f, TerminalZ + 50f, 6f);
        ProbeGrid(positions, new[] { 11.2f, 14f, 17f }, new[] { 0.5f, 2.2f }, TerminalZ + 20f, TerminalZ + 120f, 12f);

        // The light inside the cars, where the train stands at the start. Anyone aboard takes their light from here.
        ProbeGrid(positions, new[] { 1.8f, 3.2f }, new[] { 0.7f, 1.6f }, 6f, 30f, 6f);
        foreach (float y in heights)
            foreach (float z in new[] { 31.2f, 32.8f })
                foreach (float x in new[] { 1.6f, 3.4f })
                    positions.Add(new Vector3(x, Mathf.Min(y, 2.1f), z));

        foreach (float y in new[] { 0.3f, 1.3f, 2.3f })
            foreach (float x in new[] { StaffRoomXMin + 0.3f, (StaffRoomXMin + StaffRoomXMax) / 2f, StaffRoomXMax - 0.8f })
                foreach (float z in new[] { Length + 0.5f, Length + 3f, Length + StaffRoomDepth - 0.7f })
                    positions.Add(new Vector3(x, y, z));

        var go = new GameObject("LightProbes");
        go.AddComponent<LightProbeGroup>().probePositions = positions.ToArray();
    }

    // Behind the door at the end of the platform: a low green room with a row of lockers.
    static void BuildStaffRoom(Transform lights)
    {
        float zFront = Length + StaffWallThickness, zBack = Length + StaffRoomDepth;
        float width = StaffRoomXMax - StaffRoomXMin, depth = zBack - zFront, middleX = (StaffRoomXMin + StaffRoomXMax) / 2f;
        Rect door = Rect.MinMaxRect(SevernaStaffDoorX - StaffDoorWidth / 2f, 0f, SevernaStaffDoorX + StaffDoorWidth / 2f, StaffDoorTop);
        var noHoles = new Rect[0];

        Kit("Floor").Panel(new Vector3(StaffRoomXMin, 0f, Length - 0.02f), Vector3.forward * (StaffRoomDepth + 0.02f), Vector3.right * width, PaverTile);
        Kit("StaffWall").Panel(new Vector3(StaffRoomXMin, StaffRoomTop, zFront), Vector3.right * width, Vector3.forward * depth, PlasterTile);
        WallAlongX(Kit("StaffWall"), zFront, 1f, StaffRoomXMin, StaffRoomXMax, 0f, StaffRoomTop, new[] { door }, PlasterTile, Kit("Stone"), StaffWallThickness);
        WallAlongX(Kit("StaffWall"), zBack, -1f, StaffRoomXMin, StaffRoomXMax, 0f, StaffRoomTop, noHoles, PlasterTile);
        WallAlongZ(Kit("StaffWall"), StaffRoomXMin, 1f, zFront, zBack, 0f, StaffRoomTop, noHoles, PlasterTile);
        WallAlongZ(Kit("StaffWall"), StaffRoomXMax, -1f, zFront, zBack, 0f, StaffRoomTop, noHoles, PlasterTile);

        foreach (float x in new[] { door.xMin - 0.04f, door.xMax + 0.04f })
            Kit("MarbleDark").Box(new Vector3(x, 1.04f, Length - 0.015f), new Vector3(0.08f, 2.08f, 0.03f), MarbleTile);
        Kit("MarbleDark").Box(new Vector3(SevernaStaffDoorX, 2.04f, Length - 0.015f), new Vector3(StaffDoorWidth + 0.16f, 0.08f, 0.03f), MarbleTile);
        Kit("T_SignStaff").Plate(new Vector3(SevernaStaffDoorX, 2.32f, Length - 0.012f), Vector3.back, Vector3.up, 1.3f, 0.25f, 0.02f);

        // The lockers along the back wall, with the player's among them, and more down the side.
        LockerRow("T_LockerDoor", "Locker", new Vector3(StaffRoomXMin + 0.1f, 0f, zBack), Vector3.right, Vector3.back, 7, PlayerLocker);
        LockerRow("T_LockerDoor", "Locker", new Vector3(StaffRoomXMax, 0f, zFront + 1.1f), Vector3.forward, Vector3.left, 7, -1);

        // A bench down the middle of the room, and the rota on the wall by the door.
        float benchX = StaffRoomXMin + 0.7f;
        Kit("Wood").Box(new Vector3(benchX, SeatHeight, zFront + 2.8f), new Vector3(0.36f, 0.04f, 2f), WoodTile);
        foreach (float z in new[] { zFront + 2f, zFront + 3.6f })
            Kit("Metal").Box(new Vector3(benchX, SeatHeight / 2f, z), new Vector3(0.3f, SeatHeight, 0.04f), MetalTile);
        Kit("T_Rota").Plate(new Vector3(StaffRoomXMin + 0.012f, 1.5f, zFront + 2.5f), Vector3.right, Vector3.up, 0.5f, 0.7f, 0.006f);

        // Enough light gets in at the player's locker to see what's on the shelf.
        PointLight(lights, "LockerFill", PlayerLockerCorner + new Vector3(LockerWidth / 2f, 1.5f, 0.12f), new Color(1f, 0.94f, 0.84f), 0.3f, 1.7f);

        var tube = new Vector3(middleX, StaffRoomTop - 0.05f, zFront + 3f);
        Kit("Metal").Box(tube + Vector3.up * 0.03f, new Vector3(0.2f, 0.04f, 1.3f), MetalTile);
        glow.Box(tube, new Vector3(0.14f, 0.02f, 1.2f), Vector2.one);
        AreaLight(lights, "StaffTube", tube + Vector3.down * 0.02f, new Vector2(0.16f, 1.2f), TubeColor, 14f);
        PointLight(lights, "StaffTubeSpill", tube + Vector3.down * 0.3f, TubeColor, 1.4f, 8f);
    }

    // Lit from the probes and not the lightmap, and left out of the bake so the locker behind it fills with light.
    static GameObject Unbaked(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go, 0);
        return go;
    }

    // Which locker along the back wall is the player's.
    const int PlayerLocker = 3;

    static Vector3 PlayerLockerCorner => new Vector3(StaffRoomXMin + 0.1f + PlayerLocker * LockerWidth, 0f, Length + StaffRoomDepth - LockerDepth);

    // Steel lockers side by side against a wall. A locker that's left out of the row is built hollow,
    // for a door that really opens.
    static void LockerRow(string door, string body, Vector3 start, Vector3 along, Vector3 facing, int count, int hollow)
    {
        var size = new Vector3(LockerWidth - 0.01f, LockerHeight, LockerDepth);
        Quaternion turn = Quaternion.LookRotation(facing, Vector3.up);
        for (int i = 0; i < count; i++)
        {
            Vector3 center = start + along * ((i + 0.5f) * LockerWidth) + facing * (LockerDepth / 2f) + Vector3.up * (LockerHeight / 2f);
            if (i == hollow)
            {
                HollowLocker(body, center, turn, size);
                continue;
            }
            Kit(body).Box(Matrix4x4.TRS(center, turn, Vector3.one), size, MetalTile);
            Kit(door).Plate(center + facing * (LockerDepth / 2f), facing, Vector3.up, LockerWidth - 0.02f, LockerHeight - 0.02f, 0.004f);
        }
    }

    static void HollowLocker(string body, Vector3 center, Quaternion turn, Vector3 size)
    {
        const float skin = 0.012f;
        void Part(Vector3 offset, Vector3 partSize) => Kit(body).Box(Matrix4x4.TRS(center + turn * offset, turn, Vector3.one), partSize, MetalTile);

        Part(new Vector3(0f, 0f, -size.z / 2f + skin), new Vector3(size.x, size.y, skin * 2f));
        Part(new Vector3(-size.x / 2f + skin, 0f, 0f), new Vector3(skin * 2f, size.y, size.z));
        Part(new Vector3(size.x / 2f - skin, 0f, 0f), new Vector3(skin * 2f, size.y, size.z));
        Part(new Vector3(0f, size.y / 2f - skin, 0f), new Vector3(size.x, skin * 2f, size.z));
        Part(new Vector3(0f, -size.y / 2f + skin, 0f), new Vector3(size.x, skin * 2f, size.z));
        Part(new Vector3(0f, 0.32f, 0f), new Vector3(size.x, skin * 2f, size.z));
    }

    // The way off the hospital's platform: a doorway and a tiled corridor running off into the dark.
    static void BuildHospitalExit()
    {
        float zMin = HospitalExitZ - HallDoorWidth / 2f, zMax = HospitalExitZ + HallDoorWidth / 2f, xEnd = Width + HospitalExitLength;
        Kit("Floor").Panel(new Vector3(Width, 0f, zMin), Vector3.forward * HallDoorWidth, Vector3.right * HospitalExitLength, PaverTile);
        Kit("Plaster").Panel(new Vector3(Width, HallDoorTop + 0.2f, zMin), Vector3.right * HospitalExitLength, Vector3.forward * HallDoorWidth, PlasterTile);
        WallAlongX(Kit("Tile"), zMin, 1f, Width, xEnd, 0f, HallDoorTop + 0.2f, NoHoles, TileTile);
        WallAlongX(Kit("Tile"), zMax, -1f, Width, xEnd, 0f, HallDoorTop + 0.2f, NoHoles, TileTile);
        WallAlongZ(Kit("Tile"), xEnd, -1f, zMin, zMax, 0f, HallDoorTop + 0.2f, NoHoles, TileTile);

        foreach (float z in new[] { zMin - 0.05f, zMax + 0.05f })
            Kit("Stone").Box(new Vector3(Width - 0.01f, HallDoorTop / 2f, z), new Vector3(0.1f, HallDoorTop + 0.1f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3(Width - 0.01f, HallDoorTop + 0.05f, HospitalExitZ), new Vector3(0.1f, 0.1f, HallDoorWidth + 0.2f), StoneTile);
        Kit("T_SignExit").Plate(new Vector3(Width - 0.012f, 2.34f, HospitalExitZ), Vector3.left, Vector3.up, 1.1f, 0.32f, 0.02f);
    }

    // Zavodska: everything a platform has, shut off and left for thirty years.
    static void BuildClosedStation(Transform lights, MeshKit redLamp, MeshKit clearLamp)
    {
        BuildHall("TileBandClosed", "TileOld", OldRoomDoorZ);
        BuildEndWalls("TileBandClosed", true);
        BuildStationWall("T_SignZavodska");
        BuildLamps(lights, (3.5f, 0f, null), (10.5f, 0f, null), (17.5f, 0f, null), (24.5f, 0f, null), (31.5f, 0f, null), (38.5f, 0f, null));
        BuildPassage(lights, RelayPassageEnd, false);
        BuildSignal(lights, clearLamp, redLamp);
        BuildOldLockerRoom(lights);
        BuildRelayCabinet(redLamp, clearLamp);

        // Things that were being done when it all stopped.
        Vector2 planks = WoodTile;
        Kit("Wood").Box(Matrix4x4.TRS(new Vector3(7.8f, 0.06f, 12f), Quaternion.Euler(0f, 24f, 0f), Vector3.one), new Vector3(0.22f, 0.04f, 2.6f), planks);
        Kit("Wood").Box(Matrix4x4.TRS(new Vector3(7.6f, 0.1f, 12.3f), Quaternion.Euler(0f, -12f, 0f), Vector3.one), new Vector3(0.22f, 0.04f, 2.3f), planks);
        Kit("Wood").Box(Matrix4x4.TRS(new Vector3(8.2f, 0.6f, 15f), Quaternion.Euler(0f, 0f, 74f), Vector3.one), new Vector3(1.3f, 0.03f, 0.9f), planks);
        Kit("Metal").Box(Matrix4x4.TRS(new Vector3(5.6f, 0.3f, 33.4f), Quaternion.Euler(0f, 18f, 0f), Vector3.one), new Vector3(0.7f, 0.5f, 1.1f), MetalTile);

        var rubble = new[] { new Vector2(0.9f, 0f), new Vector2(0.72f, 0.1f), new Vector2(0.4f, 0.22f), new Vector2(0.12f, 0.28f), new Vector2(0f, 0.3f) };
        foreach (Vector3 heap in new[] { new Vector3(8.5f, 0f, 6.2f), new Vector3(8.3f, 0f, 37.5f), new Vector3(5.1f, 0f, 22.5f) })
            Kit("Rubble").Lathe(heap, rubble, 9, PlasterTile);
        Kit("Rubble").Box(new Vector3(7.2f, 0.025f, 8f), new Vector3(1.6f, 0.05f, 1.2f), PlasterTile);

        // One bulb on the same circuit as the signal has somehow never gone out.
        var bulb = new Vector3(Width - 0.5f, 2.2f, OldRoomDoorZ + 1.5f);
        Kit("Metal").Tube(bulb + Vector3.up * 0.1f, new Vector3(Width - 0.02f, 2.3f, OldRoomDoorZ + 1.5f), 0.012f, 5, MetalTile);
        ShadedLamp(bulb, bulb.y + 0.12f);
        PointLight(lights, "ClosedBulb", bulb + Vector3.down * 0.14f, new Color(1f, 0.74f, 0.5f), 0.34f, 7f);
    }

    // What the last person left behind, in a room off the platform.
    static void BuildOldLockerRoom(Transform lights)
    {
        float zMiddle = (OldRoomZMin + OldRoomZMax) / 2f, depth = OldRoomZMax - OldRoomZMin, width = OldRoomXMax - Width;
        const float top = 2.3f;
        Rect door = Rect.MinMaxRect(OldRoomDoorZ - HallDoorWidth / 2f, 0f, OldRoomDoorZ + HallDoorWidth / 2f, HallDoorTop);
        var noHoles = new Rect[0];

        Kit("Floor").Panel(new Vector3(Width, 0f, OldRoomZMin), Vector3.forward * depth, Vector3.right * width, PaverTile);
        Kit("StaffWall").Panel(new Vector3(Width, top, OldRoomZMin), Vector3.right * width, Vector3.forward * depth, PlasterTile);
        WallAlongZ(Kit("StaffWall"), Width + 0.02f, 1f, OldRoomZMin, OldRoomZMax, 0f, top, new[] { door }, PlasterTile);
        WallAlongZ(Kit("StaffWall"), OldRoomXMax, -1f, OldRoomZMin, OldRoomZMax, 0f, top, noHoles, PlasterTile);
        WallAlongX(Kit("StaffWall"), OldRoomZMin, 1f, Width, OldRoomXMax, 0f, top, noHoles, PlasterTile);
        WallAlongX(Kit("StaffWall"), OldRoomZMax, -1f, Width, OldRoomXMax, 0f, top, noHoles, PlasterTile);

        foreach (float z in new[] { OldRoomDoorZ - HallDoorWidth / 2f - 0.05f, OldRoomDoorZ + HallDoorWidth / 2f + 0.05f })
            Kit("Stone").Box(new Vector3(Width - 0.01f, HallDoorTop / 2f, z), new Vector3(0.1f, HallDoorTop + 0.1f, 0.1f), StoneTile);
        Kit("Stone").Box(new Vector3(Width - 0.01f, HallDoorTop + 0.05f, OldRoomDoorZ), new Vector3(0.1f, 0.1f, HallDoorWidth + 0.2f), StoneTile);

        // The door hangs open into the room.
        Quaternion swung = Quaternion.Euler(0f, -112f, 0f);
        var hinge = new Vector3(Width + 0.04f, 1f, OldRoomDoorZ + HallDoorWidth / 2f);
        Kit("LockerOld").Box(Matrix4x4.TRS(hinge + swung * (Vector3.back * HallDoorWidth / 2f), swung, Vector3.one), new Vector3(0.05f, 1.96f, HallDoorWidth - 0.04f), MetalTile);

        LockerRow("T_LockerDoorOld", "LockerOld", new Vector3(OldRoomXMax, 0f, OldRoomZMin + 0.3f), Vector3.forward, Vector3.left, 13, OldLocker);

        // One has gone over, and nobody has been in to pick it up.
        var fallen = new Vector3(Width + 2.4f, 0.24f, OldRoomZMin + 1.3f);
        Quaternion lying = Quaternion.Euler(0f, 62f, 90f);
        Kit("LockerOld").Box(Matrix4x4.TRS(fallen, lying, Vector3.one), new Vector3(LockerWidth, LockerHeight, LockerDepth), MetalTile);
        Kit("Paper").Box(Matrix4x4.TRS(new Vector3(Width + 2f, 0.01f, OldRoomZMax - 2f), Quaternion.Euler(0f, 31f, 0f), Vector3.one), new Vector3(0.21f, 0.004f, 0.3f), PaperTile);
        Kit("Paper").Box(Matrix4x4.TRS(new Vector3(Width + 2.5f, 0.01f, OldRoomZMax - 2.4f), Quaternion.Euler(0f, -14f, 0f), Vector3.one), new Vector3(0.21f, 0.004f, 0.3f), PaperTile);

        PointLight(lights, "OldRoomGlow", new Vector3(OldRoomXMax - 0.8f, 2.1f, zMiddle), new Color(1f, 0.74f, 0.5f), 0.08f, 5f);
    }

    // Which locker along the wall of the old room was forced.
    const int OldLocker = 9;

    static Vector3 OldLockerCenter => new Vector3(OldRoomXMax - LockerDepth / 2f, LockerHeight / 2f, ClosedHallZ + OldRoomZMin + 0.3f + (OldLocker + 0.5f) * LockerWidth);

    // The railway lamp on the shelf: a battery box, a lens in a bright ring, and a hoop over the top to carry it by.
    static GameObject BuildLampProp(Transform parent, Vector3 seat)
    {
        const float high = 0.17f, wide = 0.12f, deep = 0.1f;
        var bodyKit = new MeshKit();
        bodyKit.RoundedBox(seat + Vector3.up * (high / 2f), new Vector3(wide, high, deep), 0.018f, 3, MetalTile);
        bodyKit.RoundedBox(seat + Vector3.up * (high + 0.006f), new Vector3(wide - 0.03f, 0.014f, deep - 0.03f), 0.005f, 1, MetalTile);

        Vector3 Hoop(float a) => seat + new Vector3(Mathf.Cos(a) * (wide / 2f - 0.012f), high + Mathf.Sin(a) * 0.075f, 0f);
        for (int i = 0; i < 10; i++)
            bodyKit.Tube(Hoop(i / 10f * Mathf.PI), Hoop((i + 1) / 10f * Mathf.PI), 0.0055f, 5, MetalTile);

        GameObject lamp = Unbaked(Dynamic("LampOnTheShelf", parent, bodyKit, "LampBody"));

        var lens = seat + new Vector3(0f, high * 0.56f, -deep / 2f);
        var ringKit = new MeshKit();
        ringKit.Tube(lens, lens + Vector3.back * 0.022f, 0.046f, 18, MetalTile);
        ringKit.Annulus(lens + Vector3.back * 0.022f, Vector3.back, Vector3.up, 0.036f, 0.046f, 18, MetalTile);
        Unbaked(Dynamic("LampRing", lamp.transform, ringKit, "Brass"));

        var glassKit = new MeshKit();
        glassKit.Disc(lens + Vector3.back * 0.016f, Vector3.back, Vector3.up, 0.037f, 18);
        CommitUnlit("LampGlass", glassKit, new Color(0.34f, 0.31f, 0.24f), lamp.transform);
        return lamp;
    }

    // Stoyan Petkov, or what's left of the photograph of him.
    static Apparition BuildApparition(Transform parent, FirstPersonController player, HandLamp lamp)
    {
        GameObject figure = CharacterForge.Spawn(Cast.Stoyan, "Stoyan", OpeningSceneBuilder.Controller("Idle"), parent, true);
        figure.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        OpeningSceneBuilder.Pose(figure);
        float standHeight = -OpeningSceneBuilder.MeasurePose(figure).min.y;

        var turn = figure.AddComponent<HeadTurn>();
        turn.target = player.cameraTransform;
        turn.weight = 1f;
        turn.turnSpeed = 6f;
        figure.SetActive(false);

        var scare = new GameObject("Apparition").AddComponent<Apparition>();
        scare.transform.SetParent(parent);
        scare.figure = figure;
        scare.head = figure.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        scare.player = player;
        scare.lamp = lamp;
        scare.standHeight = standHeight;
        scare.passageX = RelayX;
        scare.footstep = Sound("F_Walk0");
        scare.breath = Sound("S_Breath");
        scare.nearby = Emitter(scare.transform, "Nearby", Vector3.zero, null, 1f, 0.6f, 6f);

        var spark = new GameObject("SwitchSpark").AddComponent<Light>();
        spark.transform.SetParent(scare.transform);
        spark.transform.position = new Vector3(RelayX + SwitchAcross, SwitchHinge + BladeLength, ClosedHallZ + RelayFront + 0.14f);
        spark.type = LightType.Point;
        spark.lightmapBakeType = LightmapBakeType.Realtime;
        spark.shadows = LightShadows.None;
        spark.color = new Color(0.62f, 0.76f, 1f);
        spark.intensity = 5f;
        spark.range = 7f;
        spark.enabled = false;
        scare.spark = spark;
        return scare;
    }

    // Three states for a person: standing, walking the way they walk, and sitting down.
    static RuntimeAnimatorController PersonController(GaitForge.Gait gait)
    {
        Directory.CreateDirectory(PersonControllerDir);
        string path = $"{PersonControllerDir}/AC_{gait.Name}.controller";
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path) is RuntimeAnimatorController made && personControllers.Contains(path)) return made;
        personControllers.Add(path);

        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Pose", AnimatorControllerParameterType.Int);

        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimationClip[] clips = { OpeningSceneBuilder.StandingClip, GaitForge.Load(gait), OpeningSceneBuilder.SittingClipAsset };
        string[] names = { "Standing", "Walking", "Sitting" };
        for (int i = 0; i < clips.Length; i++)
        {
            AnimatorState state = machine.AddState(names[i]);
            state.motion = clips[i];
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.Equals, i, "Pose");
            transition.duration = 0.25f;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            if (i == 0) machine.defaultState = state;
        }
        return controller;
    }

    static readonly HashSet<string> personControllers = new HashSet<string>();

    // Someone at a stop, waiting. Their seat on the train is measured here, with the train where it starts.
    static Passenger BuildPassenger(Look look, GaitForge.Gait gait, Transform parent, Transform view, Vector3 startsAt, float facing, float seatZ, bool farSide, int doorway, params Passenger.Trip[] trips)
    {
        float seatX = farSide ? FarSeatX : PlatformSeatX, yaw = farSide ? 90f : -90f;
        GameObject person = CharacterForge.Spawn(look, look.Name, OpeningSceneBuilder.Controller("Sitting"), parent, true);
        OpeningSceneBuilder.SitDown(person, seatX, seatZ, yaw, TrainSeatHeight, false);

        var passenger = person.AddComponent<Passenger>();
        passenger.train = train;
        passenger.doors = train.GetComponent<TrainDoors>();
        passenger.trips = trips;
        passenger.doorway = doorway;
        passenger.speed = gait.Speed * look.Height / 1.75f;
        passenger.stepSeconds = GaitForge.CycleSeconds(gait) / 2f;
        passenger.seatPosition = person.transform.position;
        passenger.seatRotation = person.transform.rotation;
        passenger.aisleSpot = new Vector3(farSide ? TrackX - 0.5f : TrackX + 0.5f, 0f, seatZ);

        person.GetComponent<Animator>().runtimeAnimatorController = OpeningSceneBuilder.Controller("Idle");
        person.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        OpeningSceneBuilder.Pose(person);
        passenger.standHeight = -OpeningSceneBuilder.MeasurePose(person).min.y;

        person.GetComponent<Animator>().runtimeAnimatorController = PersonController(gait);
        person.transform.SetPositionAndRotation(startsAt + Vector3.up * passenger.standHeight, Quaternion.Euler(0f, facing, 0f));

        var turn = person.AddComponent<HeadTurn>();
        turn.target = view;
        turn.weight = 0f;
        turn.turnSpeed = 1.1f;

        passenger.feet = Emitter(person.transform, "Feet", startsAt, null, 0.32f, 1.5f, 12f);
        passenger.steps = Sounds("F_Walk", SoundForge.WalkSteps);
        return passenger;
    }

    static Vector3 OnHospitalPlatform(float x, float z) => new Vector3(x, 0f, NextHallZ + z);

    // Everyone on the last train of the night.
    static Crowd BuildPassengers(Transform view)
    {
        var parent = new GameObject("Passengers").transform;

        // Out through the gate, over the road and away up the street into the fog, some way apart.
        Vector3[] Away(float farEnd, float side) => new[]
        {
            new Vector3(6.4f, 0f, GateZ - 0.8f), new Vector3(9.6f, 0f, GateZ + side * 0.2f), new Vector3(11.6f, 0f, GateZ + side * 0.3f), new Vector3(RoadXMax + 1.4f, 0f, GateZ + side * 1.2f), new Vector3(farEnd, 0f, GateZ + side * 2.4f),
        };

        Vector3[] hospitalExit = { OnHospitalPlatform(7.8f, HospitalExitZ), OnHospitalPlatform(Width + 5f, HospitalExitZ) };

        var everyone = new List<Passenger>
        {
            BuildPassenger(Cast.Fitter, GaitForge.Everyday, parent, view, new Vector3(5.1f, 0f, 21.8f), -90f, 23.3f, true, 3,
                new Passenger.Trip { cue = "severna_first", delay = 0.5f, boards = true },
                new Passenger.Trip { cue = "depot", delay = 1.5f, alights = true, path = Away(84f, -1f), leaves = true }),

            BuildPassenger(Cast.Nurse, GaitForge.Brisk, parent, view, new Vector3(5.2f, 0f, 26.2f), -110f, 23.4f, false, 4,
                new Passenger.Trip { cue = "severna_second", delay = 0.5f, boards = true },
                new Passenger.Trip { cue = "bolnichna", delay = 1.2f, alights = true, path = hospitalExit, leaves = true }),

            BuildPassenger(Cast.Drunk, GaitForge.Weary, parent, view, new Vector3(6.6f, 0f, 18f), 40f, 26.3f, true, 3,
                new Passenger.Trip { cue = "severna_second", delay = 3f, path = new[] { new Vector3(5f, 0f, 20.4f) }, boards = true },
                new Passenger.Trip { cue = "depot", delay = 5f, alights = true, path = Away(76f, 0.5f), leaves = true }),

            BuildPassenger(Cast.LongCoat, GaitForge.Everyday, parent, view, new Vector3(4.9f, 0f, 30f), -90f, 30.2f, false, 5,
                new Passenger.Trip { cue = "severna_last", delay = 0.2f, boards = true }),

            BuildPassenger(Cast.OldWoman, GaitForge.Shuffle, parent, view, OnHospitalPlatform(5f, 33.4f), -100f, 26.6f, false, 5,
                new Passenger.Trip { cue = "bolnichna", delay = 4f, boards = true },
                new Passenger.Trip { cue = "depot", delay = 8f, alights = true, path = Away(66f, 1f), leaves = true }),

            BuildPassenger(Cast.Student, GaitForge.Everyday, parent, view, OnHospitalPlatform(5.3f, 30f), -80f, 29.9f, true, 5,
                new Passenger.Trip { cue = "bolnichna", delay = 3f, boards = true },
                new Passenger.Trip { cue = "depot", delay = 2.5f, alights = true, path = Away(92f, -0.4f), leaves = true }),

            BuildPassenger(Cast.RedCoat, GaitForge.Brisk, parent, view, OnHospitalPlatform(5.6f, 24f), -90f, 26.9f, true, 4,
                new Passenger.Trip { cue = "bolnichna", delay = 2.4f, boards = true },
                new Passenger.Trip { cue = "depot", delay = 3.6f, alights = true, path = Away(96f, 0.2f), leaves = true }),

            BuildPassenger(Cast.OldMan, GaitForge.Shuffle, parent, view, OnHospitalPlatform(6.4f, 27.6f), -60f, 22.8f, true, 4,
                new Passenger.Trip { cue = "bolnichna", delay = 2f, boards = true },
                new Passenger.Trip { cue = "depot", delay = 10f, alights = true, path = Away(62f, -1.2f), leaves = true }),
        };

        // Everyone aboard takes their light from inside the car, wherever the car has got to.
        var inside = new GameObject("InsideTheCar").transform;
        inside.position = new Vector3(TrackX, 1.2f, 24f);
        var crowd = parent.gameObject.AddComponent<Crowd>();
        crowd.people = everyone.ToArray();
        foreach (Passenger person in crowd.people) person.aboardLight = inside;
        return crowd;
    }

    // The young man from the bench at the registry, who says he's never been there.
    static GameObject BuildMitko(Transform parent, Transform view, GameUi ui, out Transform head)
    {
        Transform group = new GameObject("Mitko").transform;
        group.SetParent(parent);

        GameObject body = OpeningSceneBuilder.SpawnSeated(Cast.Mitko, "MitkoBody", OpeningSceneBuilder.Controller("Sitting"), SevernaWidth - 0.42f, MitkoZ, -90f, group, true);
        var turn = body.AddComponent<HeadTurn>();
        turn.target = view;
        turn.weight = 0.8f;
        turn.dialogue = ui.Dialogue;
        turn.speakerName = "MITKO";

        head = body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        return group.gameObject;
    }

    // Vera in the duty booth, behind the glass, with the log on the shelf outside.
    static GameObject BuildVera(Transform parent, Transform view, GameUi ui, out Transform head)
    {
        Transform group = new GameObject("Vera").transform;
        group.SetParent(parent);

        float xFront = SevernaWidth - BoothDepth, zMiddle = (BoothZMin + BoothZMax) / 2f;
        GameObject body = OpeningSceneBuilder.SpawnSeated(Cast.Vera, "VeraBody", OpeningSceneBuilder.Controller("Sitting"), xFront + BoothChairX, zMiddle + 0.1f, -90f, group, true, 0.485f);
        var turn = body.AddComponent<HeadTurn>();
        turn.target = view;
        turn.weight = 0.9f;
        turn.dialogue = ui.Dialogue;
        turn.speakerName = "VERA";

        head = body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
        return group.gameObject;
    }

    // The clerk from the registry, on a platform in the middle of the night, watching the train come in.
    static void BuildWatcher(Transform view)
    {
        Transform parent = new GameObject("Watcher").transform;
        GameObject clerk = CharacterForge.Spawn(Cast.Clerk, "Clerk", OpeningSceneBuilder.Controller("Idle"), parent, true);
        clerk.transform.SetPositionAndRotation(new Vector3(4.9f, 0f, NextHallZ + 13f), Quaternion.Euler(0f, -90f, 0f));
        OpeningSceneBuilder.Pose(clerk);
        clerk.transform.position += Vector3.down * OpeningSceneBuilder.MeasurePose(clerk).min.y;
        OpeningSceneBuilder.BakeShadowProxy(clerk, parent);

        var turn = clerk.AddComponent<HeadTurn>();
        turn.target = view;
        turn.weight = 1f;
        turn.turnSpeed = 3f;
    }

    // A camera on a short arm off a pillar. The body it turns is its own object.
    static void BuildCamera(string name, Transform parent, Vector3 pivot, float wall, Transform view)
    {
        var arm = new MeshKit();
        arm.Tube(pivot, new Vector3(wall, pivot.y + 0.1f, pivot.z), 0.018f, 6, MetalTile);
        Dynamic($"{name}Arm", parent, arm, "Metal");

        var body = new MeshKit();
        body.RoundedBox(new Vector3(0f, 0f, 0.05f), new Vector3(0.11f, 0.1f, 0.28f), 0.018f, 2, MetalTile);
        body.Tube(new Vector3(0f, 0f, 0.19f), new Vector3(0f, 0f, 0.25f), 0.034f, 10, MetalTile);
        GameObject go = Dynamic(name, parent, body, "CameraBody");
        go.transform.position = pivot;

        var led = new MeshKit();
        led.Box(new Vector3(0.03f, 0.03f, 0.195f), new Vector3(0.012f, 0.012f, 0.008f), Vector2.one);
        GameObject tally = CommitUnlit($"{name}Tally", led, new Color(2.8f, 0.1f, 0.06f), go.transform);
        tally.transform.localPosition = Vector3.zero;

        var watcher = go.AddComponent<WatchingCamera>();
        watcher.view = view;
        watcher.motor = Emitter(go.transform, $"{name}Motor", pivot, Sound("A_Servo"), 0f, 0.8f, 11f);
        watcher.motor.loop = true;
        watcher.motor.playOnAwake = true;

        // They start out looking back down the platform, so that it's possible to catch one having moved.
        go.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, -0.5f, 0.2f));
    }

    // The cab's little screen, and the two cameras it can show.
    static void BuildMonitor(FirstPersonController player, TrainDoors doors)
    {
        var kit = new MeshKit();
        kit.Plate(MonitorAt + MonitorTilt * new Vector3(0f, 0f, -0.0875f), MonitorTilt * Vector3.back, MonitorTilt * Vector3.up, 0.19f, 0.145f, 0.001f);
        var screen = new GameObject("MonitorScreen");
        screen.transform.SetParent(train);
        screen.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit, "MonitorScreen");
        var surface = screen.AddComponent<MeshRenderer>();
        surface.sharedMaterial = LoadOrCreateMaterial("MonitorScreen", "Metropoliten/Monitor");
        surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var feed = new GameObject("MonitorFeed").AddComponent<Camera>();
        feed.transform.SetParent(train);
        feed.clearFlags = CameraClearFlags.SolidColor;
        feed.backgroundColor = Color.black;
        feed.fieldOfView = 70f;
        feed.nearClipPlane = 0.05f;
        feed.farClipPlane = 34f;
        feed.allowMSAA = false;
        feed.depth = -2f;

        Transform Spot(string name, Vector3 at, Vector3 looking)
        {
            var spot = new GameObject(name).transform;
            spot.SetParent(train);
            spot.SetPositionAndRotation(at, Quaternion.LookRotation(looking));
            return spot;
        }

        var monitor = feed.gameObject.AddComponent<CabMonitor>();
        monitor.feed = feed;
        monitor.saloonSpot = Spot("SaloonCamera", new Vector3(TrackX + 0.3f, 2.08f, CabBack - 0.2f), new Vector3(-0.12f, -0.34f, -1f));
        monitor.platformSpot = Spot("PlatformCamera", new Vector3(BodyXMax + 0.22f, 2.5f, CabBack + 0.6f), new Vector3(0.2f, -0.3f, -1f));
        monitor.doors = doors;
        monitor.player = player;
        monitor.screen = surface;
    }

    static TrainDrive BuildShift2Jobs(FirstPersonController player, GameObject redSignal, GameObject clearSignal)
    {
        Transform parent = new GameObject("Shift").transform;
        Transform view = player.cameraTransform;
        GameUi ui = BuildGameUi(Shift2Script, "SECOND SHIFT");

        TrainDrive drive = BuildDrive(player, ui);
        drive.runs = new[]
        {
            new TrainDrive.Run { stopAt = AtHospital, labels = "bolnichna", arrivalBed = "M_Dread", arrivalBedLevel = 0.4f },
            new TrainDrive.Run { stopAt = ClosedHallZ + BoardZ - TrainFront, stopsAtBoard = true, arrivalBed = "M_Hollow", arrivalBedLevel = 0.55f },
            new TrainDrive.Run { stopAt = AtDepot, labels = "depot", arrivalBed = "M_Hollow", arrivalBedLevel = 0.35f },
        };

        BuildPlatformBarriers(parent, SevernaShift);

        // Away from the first platform there's nothing at the edge but the train, which carries its own guards.
        Barrier("NoseGuard", train, new Vector3(EdgeX - 0.1f, 1f, TrainFront + 6f), new Vector3(0.2f, 3f, 12f));
        Barrier("TailGuard", train, new Vector3(EdgeX - 0.1f, 1f, RearCarStart - 6f), new Vector3(0.2f, 3f, 12f));
        Barrier("StreetBarrier", parent, new Vector3(SevernaWidth + ExitDoorSet, 1f, SevernaExitZ), new Vector3(0.2f, 3f, SevernaExitWidth));

        backWall = SevernaWidth;
        CallLamp callLamp = BuildCallLamp(parent);
        backWall = Width;
        SlideOpen cabDoor = BuildCabDoor();
        TrainDoors doors = train.GetComponent<TrainDoors>();
        doors.startOpen = true;

        RemarkZone cabLocked = Zone("Remark_CabDoor", parent, new Vector3(EdgeX + 0.22f, 1f, CabDoorMiddle), new Vector3(0.44f, 2f, CabDoorWidth), ui.Dialogue, "cab_locked");
        RemarkZone staffLocked = Zone("Remark_StaffDoor", parent, new Vector3(SevernaStaffDoorX, 1f, Length - 0.2f), new Vector3(StaffDoorWidth + 0.4f, 2f, 0.4f), ui.Dialogue, "staff_locked");
        Zone("Remark_Street", parent, new Vector3(SevernaWidth + 0.05f, 1f, SevernaExitZ), new Vector3(0.9f, 2f, SevernaExitWidth), ui.Dialogue, "street_closed");

        GameObject vera = BuildVera(parent, view, ui, out Transform veraHead);
        GameObject mitko = BuildMitko(parent, view, ui, out Transform mitkoHead);
        BuildWatcher(view);
        Crowd crowd = BuildPassengers(view);
        BuildMonitor(player, doors);

        // Glass in the booth window, as dirty as everything else.
        float xFront = SevernaWidth - BoothDepth, zBooth = (BoothZMin + BoothZMax) / 2f;
        var boothGlass = new MeshKit();
        boothGlass.Plate(new Vector3(xFront + 0.03f, 1.485f, zBooth), Vector3.left, Vector3.up, BoothZMax - BoothZMin - 1.4f, 0.93f, 0.002f);
        CommitStains("BoothGlass", "T_WindscreenDirt", boothGlass, parent);

        // The staff door, on its hinge at the left as you face it.
        var doorKit = new MeshKit();
        doorKit.Box(new Vector3(StaffDoorWidth / 2f, 1f, 0f), new Vector3(StaffDoorWidth - 0.02f, 2f, 0.05f), MetalTile);
        doorKit.Tube(new Vector3(StaffDoorWidth - 0.12f, 0.9f, -0.05f), new Vector3(StaffDoorWidth - 0.12f, 1.2f, -0.05f), 0.014f, 6, MetalTile);
        GameObject staffDoorGo = Dynamic("StaffDoor", parent, doorKit, "Metal");
        staffDoorGo.transform.position = new Vector3(SevernaStaffDoorX - StaffDoorWidth / 2f, 0f, Length + 0.04f);
        var staffCollider = staffDoorGo.AddComponent<BoxCollider>();
        staffCollider.center = new Vector3(StaffDoorWidth / 2f, 1f, 0f);
        staffCollider.size = new Vector3(StaffDoorWidth, 2f, 0.06f);
        var staffDoor = staffDoorGo.AddComponent<SwingOpen>();
        staffDoor.sound = Emitter(staffDoorGo.transform, "StaffDoorSound", new Vector3(SevernaStaffDoorX, 1f, Length), Sound("I_Door"), 0.7f, 2f, 18f);

        // The player's locker: a door with their name on it, and what's inside.
        Vector3 corner = PlayerLockerCorner;
        var lockerKit = new MeshKit();
        lockerKit.Plate(new Vector3(LockerWidth / 2f, LockerHeight / 2f, 0f), Vector3.back, Vector3.up, LockerWidth - 0.02f, LockerHeight - 0.02f, 0.012f);
        GameObject lockerGo = Unbaked(Dynamic("PlayerLockerDoor", parent, lockerKit, "T_LockerDoor"));
        lockerGo.transform.position = corner;

        var labelKit = new MeshKit();
        labelKit.Plate(new Vector3(LockerWidth / 2f, 1.5f, -0.014f), Vector3.back, Vector3.up, 0.25f, 0.0625f, 0.002f);
        Unbaked(Dynamic("PlayerLockerLabel", lockerGo.transform, labelKit, "T_LabelMine"));

        var insideKit = new MeshKit();
        insideKit.Plate(new Vector3(LockerWidth / 2f, LockerHeight / 2f, 0f), Vector3.forward, Vector3.up, LockerWidth - 0.02f, LockerHeight - 0.02f, 0.001f);
        Unbaked(Dynamic("PlayerLockerInside", lockerGo.transform, insideKit, "T_LockerInside"));

        var photoKit = new MeshKit();
        photoKit.Plate(new Vector3(LockerWidth / 2f, 1.34f, 0.002f), Vector3.forward, Vector3.up, 0.16f, 0.12f, 0.002f);
        Unbaked(Dynamic("Photograph", lockerGo.transform, photoKit, "T_Photo"));

        // A thin door end-on to the lamp would throw a shadow like a pencil line across the room.
        foreach (MeshRenderer part in lockerGo.GetComponentsInChildren<MeshRenderer>())
            part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var lockerDoor = lockerGo.AddComponent<SwingOpen>();
        lockerDoor.swing = new Vector3(0f, 104f, 0f);
        lockerDoor.seconds = 0.6f;

        GameObject lampProp = BuildLampProp(parent, corner + new Vector3(LockerWidth / 2f, 1.262f, LockerDepth / 2f));

        // The hand lamp itself.
        var beam = new GameObject("HandLamp").AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.lightmapBakeType = LightmapBakeType.Realtime;
        beam.color = new Color(1f, 0.9f, 0.72f);
        beam.intensity = 5f;
        beam.range = 17f;
        beam.spotAngle = 88f;
        beam.innerSpotAngle = 16f;
        beam.shadows = LightShadows.Soft;
        beam.enabled = false;
        var handLamp = player.gameObject.AddComponent<HandLamp>();
        handLamp.beam = beam;
        handLamp.view = view;

        // The old trainee's locker, and what's still on the shelf.
        Vector3 old = OldLockerCenter;
        var oldDoorKit = new MeshKit();
        oldDoorKit.Plate(new Vector3(0f, LockerHeight / 2f, LockerWidth / 2f), Vector3.left, Vector3.up, LockerWidth - 0.02f, LockerHeight - 0.02f, 0.012f);
        GameObject oldDoor = Dynamic("OldLockerDoor", parent, oldDoorKit, "T_LockerDoorOld");
        oldDoor.transform.SetPositionAndRotation(new Vector3(old.x - LockerDepth / 2f, 0f, old.z - LockerWidth / 2f), Quaternion.Euler(0f, -68f, 0f));
        var oldInsideKit = new MeshKit();
        oldInsideKit.Plate(new Vector3(0f, LockerHeight / 2f, LockerWidth / 2f), Vector3.right, Vector3.up, LockerWidth - 0.02f, LockerHeight - 0.02f, 0.001f);
        Dynamic("OldLockerInside", oldDoor.transform, oldInsideKit, "T_LockerInsideOld");
        var scratchedKit = new MeshKit();
        scratchedKit.Plate(new Vector3(-0.014f, 1.5f, LockerWidth / 2f), Vector3.left, Vector3.up, 0.25f, 0.0625f, 0.002f);
        Dynamic("OldLockerLabel", oldDoor.transform, scratchedKit, "T_LabelScratched");

        var badgeKit = new MeshKit();
        Vector3 shelf = new Vector3(old.x - 0.04f, LockerHeight / 2f + 0.34f, old.z);
        badgeKit.Plate(shelf, Vector3.up, Vector3.left, 0.14f, 0.09f, 0.004f);
        Dynamic("Badge", parent, badgeKit, "T_Badge");

        // The release: two copper blades on a hinge, and a bakelite grip worn pale where it's been held.
        var handlePivot = new Vector3(RelayX + SwitchAcross, SwitchHinge, ClosedHallZ + RelayFront + 0.05f);
        var bladeKit = new MeshKit();
        foreach (float side in new[] { -SwitchPole, SwitchPole })
            bladeKit.Box(new Vector3(side, BladeLength / 2f, 0f), new Vector3(0.012f, BladeLength + 0.03f, 0.034f), MetalTile);
        GameObject handleGo = Dynamic("RelayHandle", parent, bladeKit, "Copper");
        handleGo.transform.position = handlePivot;

        float gripAt = BladeLength * 0.82f;
        var gripKit = new MeshKit();
        gripKit.RoundedBox(new Vector3(0f, gripAt, 0.006f), new Vector3(SwitchPole * 2f + 0.05f, 0.05f, 0.03f), 0.01f, 2, MetalTile);
        gripKit.Tube(new Vector3(0f, gripAt, 0.02f), new Vector3(0f, gripAt, 0.1f), 0.014f, 10, MetalTile);
        gripKit.Tube(new Vector3(-0.07f, gripAt, 0.11f), new Vector3(0.07f, gripAt, 0.11f), 0.021f, 10, MetalTile);
        foreach (float side in new[] { -1f, 1f })
            gripKit.Disc(new Vector3(side * 0.07f, gripAt, 0.11f), Vector3.right * side, Vector3.up, 0.021f, 10);
        Dynamic("RelayGrip", handleGo.transform, gripKit, "Bakelite");

        var handle = handleGo.AddComponent<SwingOpen>();
        handle.swing = new Vector3(166f, 0f, 0f);
        handle.seconds = 0.55f;
        handle.sound = Emitter(handleGo.transform, "HandleSound", handlePivot, Sound("I_Lever"), 0.9f, 3f, 40f);

        var relayGlow = new GameObject("RelayLampGlow").AddComponent<Light>();
        relayGlow.transform.SetParent(parent);
        relayGlow.transform.position = new Vector3(RelayX + VoltDialAcross, RelayLampY, ClosedHallZ + RelayFront + 0.26f);
        relayGlow.type = LightType.Point;
        relayGlow.lightmapBakeType = LightmapBakeType.Realtime;
        relayGlow.shadows = LightShadows.None;
        relayGlow.color = new Color(1f, 0.16f, 0.1f);
        relayGlow.intensity = 0.05f;
        relayGlow.range = 2.2f;

        // The signal at the end of the closed platform.
        var glowLight = new GameObject("ClosedSignalGlow").AddComponent<Light>();
        glowLight.transform.SetParent(parent);
        glowLight.transform.position = new Vector3(4.6f, 2.08f, ClosedHallZ + SignalZ - 0.5f);
        glowLight.type = LightType.Point;
        glowLight.lightmapBakeType = LightmapBakeType.Realtime;
        glowLight.shadows = LightShadows.None;
        glowLight.color = new Color(1f, 0.16f, 0.1f);
        glowLight.intensity = 0.7f;
        glowLight.range = 9f;
        var signal = glowLight.gameObject.AddComponent<SignalLight>();
        signal.red = redSignal;
        signal.green = clearSignal;
        signal.glow = glowLight;
        signal.otherGlow = relayGlow;

        // Cameras, on the pillars here and on the walls at the closed station.
        BuildCamera("CameraNorth", parent, new Vector3(PillarX - PillarSize / 2f - 0.34f, 2.5f, 28f), PillarX - PillarSize / 2f, view);
        BuildCamera("CameraSouth", parent, new Vector3(PillarX - PillarSize / 2f - 0.34f, 2.5f, 7f), PillarX - PillarSize / 2f, view);
        BuildCamera("CameraClosed", parent, new Vector3(Width - 0.42f, 2.62f, ClosedHallZ + 14f), Width, view);
        BuildCamera("CameraClosedDoor", parent, new Vector3(Width - 0.42f, 2.62f, ClosedHallZ + 35f), Width, view);

        float xBooth = xFront - 0.17f;
        TalkSpot intercom = Spot("Intercom", parent, new Vector3(SevernaWidth - 0.5f, 1.2f, IntercomZ + 2f), new Vector3(SevernaWidth - 0.1f, 1.46f, IntercomZ + 2f), ui, player, "E   Answer", "briefing");
        intercom.laterLabel = "intercom_again";
        TalkSpot booth = Spot("VeraSpot", parent, new Vector3(xBooth - 0.3f, 1.2f, zBooth), new Vector3(xBooth, 1.3f, zBooth), ui, player, "E   Speak", "vera");
        booth.person = veraHead;
        booth.lookAccuracy = 0.7f;
        booth.reach = 2.2f;
        TalkSpot bench = Spot("MitkoSpot", parent, new Vector3(SevernaWidth - 1.3f, 1.2f, MitkoZ), new Vector3(SevernaWidth - 0.5f, 1.1f, MitkoZ), ui, player, "E   Talk", "mitko");
        bench.person = mitkoHead;
        bench.reach = 2.1f;
        TalkSpot door = Spot("StaffDoorSpot", parent, new Vector3(SevernaStaffDoorX, 1.2f, Length - 0.7f), new Vector3(SevernaStaffDoorX, 1.2f, Length), ui, player, "E   Unlock", "staff_door");
        TalkSpot locker = Spot("PlayerLocker", parent, corner + new Vector3(LockerWidth / 2f, 1.2f, -0.8f), corner + new Vector3(LockerWidth / 2f, 1.3f, 0f), ui, player, "E   Open", "locker_mine");
        locker.reach = 1.4f;
        locker.laterPrompt = "E   Look";

        TalkSpot seat = SeatSpot("DriverSeat", ui, player, "seat");
        TalkSpot backSeat = SeatSpot("DriverSeatAgain", ui, player, "back");
        TalkSpot closeFirst = CabJob("CloseDoorsSeverna", ui, player, "E   Close the doors");
        TalkSpot openHospital = CabJob("OpenDoorsBolnichna", ui, player, "E   Open the doors");
        TalkSpot closeHospital = CabJob("CloseDoorsBolnichna", ui, player, "E   Close the doors");
        TalkSpot openDepot = CabJob("OpenDoorsDepot", ui, player, "E   Open the doors");
        TalkSpot getUp = GetUpSpot("GetUp", ui, player, "get_up");
        TalkSpot getUpAgain = GetUpSpot("GetUpDepot", ui, player, "get_up_depot");
        crowd.waitFor = new[] { closeFirst, closeHospital };

        TalkSpot relay = Spot("Relay", parent, new Vector3(RelayX, 1.2f, ClosedHallZ + RelayFront + 0.85f), new Vector3(RelayX + SwitchAcross, SwitchHinge + BladeLength * 0.82f, ClosedHallZ + RelayFront + 0.1f), ui, player, "E   Pull", "relay");
        relay.reach = 1.3f;

        TalkSpot oldSpot = Spot("OldLocker", parent, old + new Vector3(-1f, -0.3f, 0f), old + Vector3.left * 0.2f, ui, player, "E   Look", "old_locker");
        oldSpot.laterLabel = "old_locker";
        Spot("CameraLook", parent, new Vector3(PillarX - 1.4f, 1.2f, 7f), new Vector3(PillarX - PillarSize / 2f - 0.34f, 2.5f, 7f), ui, player, "E   Look", "camera").reach = 3f;
        Spot("MapLook", parent, new Vector3(SevernaWidth - 0.9f, 1.2f, 24.6f), new Vector3(SevernaWidth - 0.02f, 1.46f, 24.6f), ui, player, "E   Look", "map");

        // The way out at the end of the line.
        TalkSpot timeClock = Spot("TimeClock", parent, new Vector3(ClockX, 1.2f, HutZ - 0.8f), new Vector3(ClockX, 1.32f, HutZ - 0.1f), ui, player, "E   Clock out", "clock_out");
        timeClock.laterLabel = "clock_again";
        timeClock.laterPrompt = "E   Look";

        // There's nothing for the player over the road, whatever everyone else has found there.
        Barrier("Kerb", parent, new Vector3(RoadXMin - 0.12f, 1f, (PavementSouth + PavementNorth) / 2f), new Vector3(0.2f, 3f, PavementNorth - PavementSouth));
        Zone("Remark_Crossing", parent, new Vector3(RoadXMin - 0.7f, 1f, GateZ), new Vector3(0.8f, 2f, CrossingWidth), ui.Dialogue, "crossing");

        CallLamp ringing = BuildPhoneLamp(parent);
        TalkSpot phone = Spot("Payphone", parent, new Vector3(FenceX + 1.3f, 1.2f, PhoneZ), new Vector3(FenceX + 0.2f, 1.5f, PhoneZ), ui, player, "E   Answer", "phone");
        phone.reach = 1.2f;

        // The stop. Getting there is the end of the night.
        var stop = new GameObject("BusStop").AddComponent<BoxCollider>();
        stop.transform.SetParent(parent);
        stop.transform.position = new Vector3((FenceX + RoadXMin) / 2f, 1f, StopZ);
        stop.size = new Vector3(RoadXMin - FenceX - 0.3f, 2f, 6.4f);
        stop.isTrigger = true;
        Arrival reached = stop.gameObject.AddComponent<Arrival>();
        stop.gameObject.SetActive(false);

        var exit = parent.gameObject.AddComponent<SceneTransitionTrigger>();
        exit.targetSceneName = "MainMenu";
        exit.fade = ui.Fade;
        exit.delay = 1.8f;

        ShiftChecklist checklist = BuildChecklist(ui, "SHIFT 2", new[]
        {
            new ShiftChecklist.Step { tasks = new[] { "Speak to the dispatcher" }, spots = new[] { intercom } },
            new ShiftChecklist.Step { tasks = new[] { "Sign the shift log", "Find Mitko" }, spots = new[] { booth, bench } },
            new ShiftChecklist.Step { tasks = new[] { "Open the staff door", "Get the hand lamp from your locker" }, spots = new[] { door, locker } },
            new ShiftChecklist.Step { tasks = new[] { "Take your seat in the cab" }, spots = new[] { seat } },
            new ShiftChecklist.Step { tasks = new[] { "Close the doors when everyone's aboard" }, spots = new[] { closeFirst } },
            new ShiftChecklist.Step { tasks = new[] { "Drive to Bolnichna", "Stop at the board" }, spots = new TalkSpot[0] },
            new ShiftChecklist.Step { tasks = new[] { "Open the doors" }, spots = new[] { openHospital } },
            new ShiftChecklist.Step { tasks = new[] { "Close the doors when everyone's aboard" }, spots = new[] { closeHospital } },
            new ShiftChecklist.Step { tasks = new[] { "Drive on to the depot" }, spots = new TalkSpot[0] },
            new ShiftChecklist.Step { tasks = new[] { "Get out of the cab" }, spots = new[] { getUp } },
            new ShiftChecklist.Step { tasks = new[] { "Release the signal in the relay room" }, spots = new[] { relay } },
            new ShiftChecklist.Step { tasks = new[] { "Get back to the cab" }, spots = new[] { backSeat } },
            new ShiftChecklist.Step { tasks = new[] { "Drive to the depot", "Stop at the board" }, spots = new TalkSpot[0] },
            new ShiftChecklist.Step { tasks = new[] { "Open the doors" }, spots = new[] { openDepot } },
            new ShiftChecklist.Step { tasks = new[] { "Get out of the cab" }, spots = new[] { getUpAgain } },
            new ShiftChecklist.Step { tasks = new[] { "Clock out at the hut" }, spots = new[] { timeClock } },
            new ShiftChecklist.Step { tasks = new[] { "Answer the telephone" }, spots = new[] { phone } },
            new ShiftChecklist.Step { tasks = new[] { "Wait for the bus at the stop" }, spots = new TalkSpot[0] },
        });

        void Cue(int step, string cue) => UnityEventTools.AddStringPersistentListener(checklist.steps[step].onFinished, crowd.Cue, cue);
        void Done(TalkSpot spot) => UnityEventTools.AddBoolPersistentListener(spot.onFirstConversationEnded, spot.gameObject.SetActive, false);

        UnityEventTools.AddPersistentListener(intercom.onTalkStarted, callLamp.Answer);
        Cue(0, "severna_first");
        UnityEventTools.AddBoolPersistentListener(checklist.steps[1].onFinished, staffLocked.gameObject.SetActive, false);
        Cue(1, "severna_second");

        UnityEventTools.AddPersistentListener(door.onTalkStarted, staffDoor.Open);
        Done(door);

        UnityEventTools.AddPersistentListener(locker.onTalkStarted, lockerDoor.Open);
        UnityEventTools.AddPersistentListener(locker.onFirstConversationEnded, handLamp.Carry);
        UnityEventTools.AddBoolPersistentListener(locker.onFirstConversationEnded, lampProp.SetActive, false);
        UnityEventTools.AddStringPersistentListener(locker.onFirstConversationEnded, checklist.ShowHint, "F   Hand lamp");

        // By the time the player comes out of the staff room there's nobody left on the platform.
        UnityEventTools.AddBoolPersistentListener(checklist.steps[2].onFinished, mitko.SetActive, false);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[2].onFinished, vera.SetActive, false);
        UnityEventTools.AddPersistentListener(checklist.steps[2].onFinished, cabDoor.Open);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[2].onFinished, cabLocked.gameObject.SetActive, false);
        Cue(2, "severna_last");

        UnityEventTools.AddPersistentListener(seat.onTalkStarted, drive.TakeSeat);
        UnityEventTools.AddPersistentListener(seat.onTalkStarted, handLamp.PutAway);
        UnityEventTools.AddPersistentListener(checklist.steps[3].onFinished, cabDoor.Close);
        Done(seat);

        Done(closeFirst);
        UnityEventTools.AddPersistentListener(checklist.steps[4].onFinished, doors.Close);
        UnityEventTools.AddPersistentListener(checklist.steps[4].onFinished, drive.BeginRun);
        UnityEventTools.AddPersistentListener(drive.onRunFinished, checklist.FinishStep);

        // The hospital.
        Done(openHospital);
        UnityEventTools.AddPersistentListener(checklist.steps[6].onFinished, doors.Open);
        Cue(6, "bolnichna");
        Done(closeHospital);
        UnityEventTools.AddPersistentListener(checklist.steps[7].onFinished, doors.Close);
        UnityEventTools.AddPersistentListener(checklist.steps[7].onFinished, drive.BeginRun);

        // Held at the red. Everyone aboard takes an interest.
        UnityEventTools.AddPersistentListener(getUp.onTalkStarted, drive.LeaveSeat);
        UnityEventTools.AddPersistentListener(getUp.onTalkStarted, cabDoor.Open);
        Done(getUp);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[9].onFinished, crowd.Stare, true);

        Done(relay);
        UnityEventTools.AddPersistentListener(checklist.steps[10].onFinished, handle.Open);
        UnityEventTools.AddPersistentListener(checklist.steps[10].onFinished, signal.Clear);
        UnityEventTools.AddPersistentListener(checklist.steps[10].onFinished, BuildApparition(parent, player, handLamp).Arm);
        Passenger longCoat = crowd.people[3];
        longCoat.windowPosition = new Vector3(TrackX, longCoat.standHeight + FloorY, CabBack - 0.42f);
        longCoat.windowRotation = Quaternion.identity;
        UnityEventTools.AddPersistentListener(checklist.steps[10].onFinished, longCoat.GoToWindow);

        UnityEventTools.AddPersistentListener(backSeat.onTalkStarted, drive.TakeSeat);
        UnityEventTools.AddPersistentListener(backSeat.onTalkStarted, handLamp.PutAway);
        UnityEventTools.AddPersistentListener(checklist.steps[11].onFinished, cabDoor.Close);
        Done(backSeat);
        UnityEventTools.AddPersistentListener(checklist.steps[11].onFinished, drive.BeginRun);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[11].onFinished, crowd.Stare, false);

        // The end of the line.
        Done(openDepot);
        UnityEventTools.AddPersistentListener(checklist.steps[13].onFinished, doors.Open);
        Cue(13, "depot");

        UnityEventTools.AddPersistentListener(getUpAgain.onTalkStarted, drive.LeaveSeat);
        UnityEventTools.AddPersistentListener(getUpAgain.onTalkStarted, cabDoor.Open);
        Done(getUpAgain);

        UnityEventTools.AddPersistentListener(phone.onTalkStarted, ringing.Answer);
        Done(phone);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[16].onFinished, stop.gameObject.SetActive, true);
        UnityEventTools.AddPersistentListener(reached.onArrived, checklist.FinishStep);
        UnityEventTools.AddPersistentListener(checklist.steps[17].onFinished, exit.Leave);
        return drive;
    }

    static TalkSpot SeatSpot(string name, GameUi ui, FirstPersonController player, string label)
    {
        TalkSpot spot = Spot(name, train, new Vector3(TrackX + 0.4f, 1.2f, 31.7f), new Vector3(TrackX, 1.5f, TrainFront), ui, player, "E   Sit down", label);
        spot.lookAccuracy = 0.5f;
        spot.reach = 1.5f;
        return spot;
    }

    static TalkSpot GetUpSpot(string name, GameUi ui, FirstPersonController player, string label)
    {
        TalkSpot spot = Spot(name, train, new Vector3(TrackX, 1.2f, SeatZ), new Vector3(BodyXMax, 1.2f, CabDoorMiddle), ui, player, "E   Get up", label);
        spot.lookAccuracy = -1f;
        spot.reach = 2f;
        return spot;
    }

    // Something the driver does from the seat, with nothing to say about it.
    static TalkSpot CabJob(string name, GameUi ui, FirstPersonController player, string prompt)
    {
        TalkSpot spot = Spot(name, train, new Vector3(TrackX, 1.2f, SeatZ), new Vector3(TrackX, 1.2f, TrainFront), ui, player, prompt, "");
        spot.lookAccuracy = -1f;
        spot.reach = 2f;
        return spot;
    }

    // A small lamp on the telephone's hood that goes with the bell.
    static CallLamp BuildPhoneLamp(Transform parent)
    {
        var lampKit = new MeshKit();
        lampKit.Lathe(new Vector3(PhoneX - 0.1f, 2.05f, PhoneZ), new[] { new Vector2(0.03f, 0f), new Vector2(0.03f, 0.02f), new Vector2(0.015f, 0.05f), new Vector2(0f, 0.055f) }, 10, Vector2.one);
        var go = new GameObject("PhoneLamp");
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(lampKit, "PhoneLamp");
        go.AddComponent<MeshRenderer>().sharedMaterial = LoadOrCreateMaterial("PhoneLamp", "Universal Render Pipeline/Unlit");
        var lamp = go.AddComponent<CallLamp>();
        lamp.period = 2.4f;
        lamp.blinksPerBuzz = 1;
        return lamp;
    }

    // Everything that Shift 2's stations sound like that Shift 1's don't.
    static void BuildShift2Sound(FirstPersonController player, TrainDrive drive)
    {
        Transform parent = new GameObject("Sound").transform;
        AudioClip wind = Sound("A_Wind"), tube = Sound("A_Tube");

        RoomTone(parent, 0.3f);
        BuildPlayerSound(player, SoundForge.WalkSteps, SoundForge.RunSteps);

        foreach (float z in new[] { 10.5f, 17.5f, 24.5f, 31.5f })
            Ambient(parent, "Tube", new Vector3(LampX, LampY, NextHallZ + z), tube, 0.16f, 1.5f, 8f);
        BuildTubeSound(parent, new Vector3(LampX, LampY, NextHallZ + 38.5f), 1.5f, 10f);
        foreach (float z in new[] { -3.2f, -9f })
            Ambient(parent, "Bulb", new Vector3(LampX + SevernaShift, PassageTop - 0.3f, z), Sound("A_Bulb"), 0.14f, 1f, 6f);
        foreach (float z in new[] { 3.5f, 10.5f, 24.5f, 31.5f, 38.5f })
            Ambient(parent, "Globes", new Vector3(5.5f, 2.5f, z), Sound("A_Bulb"), 0.1f, 1f, 6f);

        Ambient(parent, "WindNorth", new Vector3(TrackX, 1f, Length + 5f), wind, 0.45f, 4f, 34f);
        Ambient(parent, "WindSouth", new Vector3(TrackX, 1f, -5f), wind, 0.3f, 4f, 26f);
        Ambient(parent, "StreetAir", new Vector3(SevernaWidth + 3f, 1.4f, SevernaExitZ), wind, 0.25f, 2f, 16f);
        AudioSource dark = Ambient(parent, "DarkPath", new Vector3(LampX + SevernaShift, 1.2f, PassageBarrierZ - 8f), Sound("A_Dark"), 0.5f, 3f, 30f);
        dark.GetComponent<AmbientLoop>().heardThroughHush = true;

        var intercom = new Vector3(SevernaWidth - 0.1f, 1.46f, IntercomZ + 2f);
        Ambient(parent, "IntercomLine", intercom, Sound("A_Intercom"), 0.22f, 0.6f, 4.5f);
        foreach (CallLamp lamp in Object.FindObjectsByType<CallLamp>(FindObjectsSortMode.None))
        {
            if (lamp.name == "PhoneLamp") lamp.buzzer = Emitter(parent, "PhoneBell", new Vector3(PhoneX, 1.7f, PhoneZ), Sound("I_PhoneRing"), 0.55f, 3f, 34f);
            else lamp.buzzer = Emitter(parent, "IntercomCall", intercom, Sound("I_Call"), 0.4f, 1.5f, 24f);
        }

        Vector3 closed = Vector3.forward * ClosedHallZ;
        Ambient(parent, "ClosedWind", new Vector3(TrackX, 1f, ClosedHallEnd + 5f), wind, 0.5f, 5f, 40f);
        Ambient(parent, "ClosedDrone", new Vector3(LampX, 1.2f, ClosedHallZ + RelayPassageEnd + 3f), Sound("A_Dark"), 0.4f, 3f, 34f);
        Ambient(parent, "StaffRoomTube", new Vector3((StaffRoomXMin + StaffRoomXMax) / 2f, StaffRoomTop, Length + 3f), tube, 0.13f, 1f, 6f);
        Ambient(parent, "ClosedBulb", closed + new Vector3(Width - 0.5f, 2.2f, OldRoomDoorZ + 1.5f), Sound("A_Bulb"), 0.16f, 1f, 7f);

        // The open air: wind, and the depot working a long way off.
        Ambient(parent, "Morning", new Vector3(10f, 2f, TerminalZ + 30f), Sound("A_Morning"), 0.7f, 30f, 95f);

        ReverbZone(parent, "HallReverb", new Vector3(SevernaWidth / 2f, 2f, Length / 2f), AudioReverbPreset.StoneCorridor, 26f, 34f);
        ReverbZone(parent, "PassageReverb", new Vector3(LampX + SevernaShift, 1.5f, PassageBarrierZ + 4f), AudioReverbPreset.StoneCorridor, 9f, 16f);
        ReverbZone(parent, "NextHallReverb", new Vector3(Width / 2f, 2f, NextHallZ + Length / 2f), AudioReverbPreset.StoneCorridor, 24f, 32f);
        ReverbZone(parent, "RunReverb", new Vector3(TrackX, 1f, Length + RunLength / 2f), AudioReverbPreset.SewerPipe, RunLength / 2f - 8f, RunLength / 2f + 4f);
        ReverbZone(parent, "SecondRunReverb", new Vector3(TrackX, 1f, (NextHallEnd + ClosedHallZ) / 2f), AudioReverbPreset.SewerPipe, 180f, 192f);
        ReverbZone(parent, "ThirdRunReverb", new Vector3(TrackX, 1f, (ClosedHallEnd + PortalZ) / 2f), AudioReverbPreset.SewerPipe, 80f, 92f);
        ReverbZone(parent, "ClosedHallReverb", closed + new Vector3(Width / 2f, 2f, Length / 2f), AudioReverbPreset.StoneCorridor, 24f, 32f);
        ReverbZone(parent, "ClosedPassageReverb", closed + new Vector3(LampX, 1.5f, RelayPassageEnd / 2f), AudioReverbPreset.StoneCorridor, 8f, 14f);

        SoundScatter stationSounds = BuildStationSounds(parent, player);
        BuildTrainSound(drive, stationSounds);
        BuildClosedStationSounds(parent, player);

        // The doors, and the fog and distance that change as the line comes up into the day.
        TrainDoors doors = train.GetComponent<TrainDoors>();
        doors.motors = Emitter(train, "DoorMotors", new Vector3(BodyXMax, 1f, 25f), null, 0.8f, 6f, 40f);
        doors.openSound = Sound("T_DoorOpen");
        doors.closeSound = Sound("T_DoorClose");

        var air = player.gameObject.AddComponent<OpenAir>();
        air.view = player.cameraTransform.GetComponent<Camera>();
        air.startZ = PortalZ - 40f;
        air.fullZ = PortalZ + 30f;
        air.seeOutZ = PortalZ - 170f;
        air.mouthZ = PortalZ;
    }

    static void BuildClosedStationSounds(Transform parent, FirstPersonController player)
    {
        var go = new GameObject("ClosedStationSounds");
        go.transform.SetParent(parent);
        go.SetActive(false);

        var scatter = go.AddComponent<SoundScatter>();
        scatter.listener = player.cameraTransform;
        scatter.kinds = new[]
        {
            new SoundScatter.Kind { name = "Drips", clips = Sounds("A_Drip", SoundForge.Drips), everySeconds = new Vector2(2.5f, 8f), distance = new Vector2(4f, 14f), volume = new Vector2(0.2f, 0.45f) },
            new SoundScatter.Kind { name = "Ticks", clips = Sounds("A_Tick", SoundForge.Ticks), everySeconds = new Vector2(7f, 18f), distance = new Vector2(5f, 16f), volume = new Vector2(0.2f, 0.4f), behind = 0.7f },
            new SoundScatter.Kind { name = "Groans", clips = Sounds("A_Groan", SoundForge.Groans), everySeconds = new Vector2(24f, 50f), distance = new Vector2(10f, 22f), volume = new Vector2(0.4f, 0.7f), reach = 8f, behind = 0.9f },
        };

        var feet = go.AddComponent<FollowingSteps>();
        feet.player = player;
        feet.clips = Sounds("F_Walk", SoundForge.WalkSteps);
        feet.source = Emitter(go.transform, "SomeoneElse", Vector3.zero, null, 0.5f, 3f, 34f);

        // It all starts when the train comes to a stand, and ends when the player is back in the seat.
        var checklist = Object.FindAnyObjectByType<ShiftChecklist>();
        UnityEventTools.AddBoolPersistentListener(checklist.steps[8].onFinished, go.SetActive, true);
        UnityEventTools.AddBoolPersistentListener(checklist.steps[11].onFinished, go.SetActive, false);

        // The closed station is emptier than anywhere else, and that's most of what's wrong with it.
        var roomTone = parent.Find("RoomTone").GetComponent<AmbientLoop>();
        UnityEventTools.AddFloatPersistentListener(checklist.steps[8].onFinished, roomTone.FadeTo, 0.45f);
        UnityEventTools.AddFloatPersistentListener(checklist.steps[11].onFinished, roomTone.FadeTo, 1f);

        // And the room's own air doesn't follow anyone up into the morning.
        UnityEventTools.AddFloatPersistentListener(checklist.steps[12].onFinished, roomTone.FadeTo, 0.15f);
    }
}
