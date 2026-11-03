using System.Collections.Generic;
using UnityEngine;
using static SceneForge;

// The train. Two cars with doors that really open, a rounded roof, and a cab built up the way the real thing is:
// a painted steel shell with the corners rolled, a desk that wraps round the driver, and everything bolted on after.
public static partial class StationBuilder
{
    const float EavesY = 2.2f, CabWallTop = 2.03f, CabCove = 0.2f, CabCrown = 0.05f, SaloonWallTop = 2.02f, SaloonCove = 0.2f, SaloonCrown = 0.06f;
    const float CabNoseZ = 32.72f, NoseInset = 0.42f, CabFrontZ = TrainFront - WallDepth, SoffitY = 1.99f;
    const float ScreenBottom = 1.2f, ScreenTop = 1.93f, HeadlampX = 0.72f;
    const float DeskXMin = 1.54f, DeskXMax = 3.46f, WingBack = 0.13f;
    const float ControllerX = 1.8f, ControllerAlong = 0.5f, ControllerBodyHeight = 0.07f;
    const float SpeedDialRadius = 0.1f, SmallDialRadius = 0.07f;

    const float CabLeft = BodyXMin + WallDepth, CabRight = BodyXMax - WallDepth;

    // The monitor on the driver's right, out of the way of the glass.
    static readonly Vector3 MonitorAt = new Vector3(3.36f, 1.82f, 32.8f);
    static readonly Quaternion MonitorTilt = Quaternion.Euler(-13f, 39f, 0f);

    // The desk, seen from the side: up from the floor, round the lip, up the sloping panel and over the instrument hood.
    static readonly Vector2[] DeskSection =
    {
        new Vector2(32.6f, FloorY), new Vector2(32.57f, 0.7f), new Vector2(32.42f, 0.8f), new Vector2(32.36f, 0.83f), new Vector2(32.335f, 0.865f),
        new Vector2(32.34f, 0.9f), new Vector2(32.37f, 0.925f), new Vector2(32.4f, 0.932f), new Vector2(32.78f, 1.02f), new Vector2(32.8f, 1.03f),
        new Vector2(32.93f, 1.25f), new Vector2(32.955f, 1.275f), new Vector2(33f, 1.28f), new Vector2(33.15f, 1.205f),
    };

    static readonly Vector3 PanelNear = new Vector3(0f, 0.932f, 32.4f), PanelFar = new Vector3(0f, 1.02f, 32.78f);
    static readonly Vector3 HoodBottom = new Vector3(0f, 1.03f, 32.8f), HoodTop = new Vector3(0f, 1.25f, 32.93f);

    static Vector3 PanelUp => (PanelFar - PanelNear).normalized;
    static Vector3 PanelFacing => Vector3.Cross(PanelUp, Vector3.right);
    static Vector3 HoodUp => (HoodTop - HoodBottom).normalized;
    static Vector3 HoodFacing => Vector3.Cross(HoodUp, Vector3.right);

    static Vector3 OnPanel(float x, float along) => Vector3.Lerp(PanelNear, PanelFar, along) + Vector3.right * x;
    static Vector3 OnHood(float x, float along) => Vector3.Lerp(HoodBottom, HoodTop, along) + Vector3.right * x;

    static List<SlideOpen> doorLeaves;
    static List<float> doorways;

    static void BuildTrain(Transform lights, MeshKit tailLamps)
    {
        doorLeaves = new List<SlideOpen>();
        doorways = new List<float>();

        BuildCar(lights, RearCarStart, RearCarEnd, RearCarEnd, false);
        BuildCar(lights, FrontCarStart, CabNoseZ, CabBack, true);
        BuildCab(lights);
        BuildNose();
        BuildTrainEnds(lights, tailLamps);

        var doors = train.gameObject.AddComponent<TrainDoors>();
        doors.leaves = doorLeaves.ToArray();
        doors.doorways = doorways.ToArray();
        doors.doorX = BodyXMax;
    }

    // Left to right across a rolled roof, seen from below.
    static List<Vector3> Cove(float left, float right, float wallTop, float radius, float crown, float z, bool fromOutside)
    {
        var section = new List<Vector3>();
        const int steps = 6;
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.PI * (1f - 0.5f * i / steps);
            section.Add(new Vector3(left + radius + Mathf.Cos(a) * radius, wallTop + Mathf.Sin(a) * radius, z));
        }
        section.Add(new Vector3((left + right) / 2f, wallTop + radius + crown, z));
        for (int i = steps; i >= 0; i--)
        {
            float a = Mathf.PI * (1f - 0.5f * i / steps);
            section.Add(new Vector3(right - radius - Mathf.Cos(a) * radius, wallTop + Mathf.Sin(a) * radius, z));
        }
        if (fromOutside) section.Reverse();
        return section;
    }

    static void WindowRubber(float x, float facing, Rect hole)
    {
        Kit("TrainRubber").Gasket(new Vector3(x, hole.center.y, hole.center.x), Vector3.right * facing, Vector3.up, hole.width, hole.height, 0.022f, 0.07f, 0.008f, MetalTile);
    }

    // One carriage from zStart to zEnd, with passenger space up to saloonEnd.
    static void BuildCar(Transform lights, float zStart, float zEnd, float saloonEnd, bool hasCab)
    {
        float length = zEnd - zStart;
        float[] doors = { zStart + 2.3f, (zStart + saloonEnd) / 2f, saloonEnd - 2.3f };
        doorways.AddRange(doors);

        var windows = new List<Rect>();
        var doorGlass = new List<Rect>();
        void Window(float a, float b) => windows.Add(Rect.MinMaxRect(a, WindowBottom, b, WindowTop));
        Window(zStart + 0.45f, doors[0] - 0.95f);
        for (int i = 0; i < doors.Length; i++)
        {
            doorGlass.Add(Rect.MinMaxRect(doors[i] - 0.52f, DoorWindowBottom, doors[i] - 0.12f, WindowTop));
            doorGlass.Add(Rect.MinMaxRect(doors[i] + 0.12f, DoorWindowBottom, doors[i] + 0.52f, WindowTop));
            if (i == doors.Length - 1) break;

            float a = doors[i] + 0.95f, b = doors[i + 1] - 0.95f, middle = (a + b) / 2f;
            Window(a, middle - 0.07f);
            Window(middle + 0.07f, b);
        }
        Window(doors[2] + 0.95f, saloonEnd - 0.45f);

        // The platform side has real doorways; the far side's doors never open, so they're part of the wall.
        var platformSide = new List<Rect>(windows);
        foreach (float door in doors) platformSide.Add(Rect.MinMaxRect(door - 0.67f, FloorY, door + 0.67f, CarDoorTop));
        var farSide = new List<Rect>(windows);
        farSide.AddRange(doorGlass);
        if (hasCab)
        {
            platformSide.Add(CabDoorway);
            farSide.Add(Rect.MinMaxRect(CabBack + 0.35f, WindowBottom, CabNoseZ - 0.12f, WindowTop));
        }

        MeshKit skin = Kit("TrainBody"), lining = Kit("TrainInterior"), trim = Kit("TrainTrim");
        WallAlongZ(skin, BodyXMax, 1f, zStart, zEnd, SkirtY, EavesY, platformSide, TrainTile, trim, WallDepth);
        WallAlongZ(skin, BodyXMin, -1f, zStart, zEnd, SkirtY, EavesY, farSide, TrainTile, trim, WallDepth);
        WallAlongZ(lining, BodyXMax - WallDepth, -1f, zStart, saloonEnd, FloorY, SaloonWallTop, platformSide, LiningTile);
        WallAlongZ(lining, BodyXMin + WallDepth, 1f, zStart, saloonEnd, FloorY, SaloonWallTop, farSide, LiningTile);

        foreach (Rect window in windows)
        {
            foreach ((float x, float facing) in new[] { (BodyXMax, 1f), (BodyXMin, -1f), (BodyXMax - WallDepth, -1f), (BodyXMin + WallDepth, 1f) })
                WindowRubber(x, facing, window);
        }
        foreach (Rect window in doorGlass)
        {
            WindowRubber(BodyXMin, -1f, window);
            WindowRubber(BodyXMin + WallDepth, 1f, window);
        }

        Kit("TrainFloor").Panel(new Vector3(BodyXMin, FloorY, zStart), Vector3.forward * length, Vector3.right * BodyWidth, MetalTile);
        Kit("TrainCeiling").Sweep(Cove(BodyXMin + WallDepth, BodyXMax - WallDepth, SaloonWallTop, SaloonCove, SaloonCrown, zStart, false), Vector3.forward * (saloonEnd - zStart), PlasterTile);
        Kit("TrainRoof").Sweep(Cove(BodyXMin, BodyXMax, EavesY, 0.26f, 0.08f, zStart, true), Vector3.forward * length, MetalTile);

        Vector3[] arch = Cove(BodyXMin, BodyXMax, EavesY, 0.26f, 0.08f, zStart, false).ToArray();
        Kit("TrainRoof").Fan(arch, Vector3.back, Vector3.right, Vector3.up, MetalTile);
        WallAlongX(skin, zStart, -1f, BodyXMin, BodyXMax, SkirtY, EavesY, NoHoles, TrainTile);
        WallAlongX(lining, zStart + WallDepth, 1f, BodyXMin, BodyXMax, FloorY, SaloonWallTop, NoHoles, LiningTile);
        Kit("TrainInterior").Fan(Cove(BodyXMin + WallDepth, BodyXMax - WallDepth, SaloonWallTop, SaloonCove, SaloonCrown, zStart + WallDepth, false), Vector3.forward, Vector3.right, Vector3.up, LiningTile);
        if (!hasCab)
        {
            WallAlongX(skin, zEnd, 1f, BodyXMin, BodyXMax, SkirtY, EavesY, NoHoles, TrainTile);
            WallAlongX(lining, zEnd - WallDepth, -1f, BodyXMin, BodyXMax, FloorY, SaloonWallTop, NoHoles, LiningTile);
            Kit("TrainRoof").Fan(Cove(BodyXMin, BodyXMax, EavesY, 0.26f, 0.08f, zEnd, false), Vector3.forward, Vector3.right, Vector3.up, MetalTile);
            Kit("TrainInterior").Fan(Cove(BodyXMin + WallDepth, BodyXMax - WallDepth, SaloonWallTop, SaloonCove, SaloonCrown, zEnd - WallDepth, false), Vector3.back, Vector3.right, Vector3.up, LiningTile);
        }

        // Vents along the roof, and the panel seams that stop the sides looking like a sheet of card.
        for (float z = zStart + 1.6f; z < zEnd - 1f; z += 3.2f)
            Kit("TrainRoof").RoundedBox(new Vector3(TrackX, EavesY + 0.34f, z), new Vector3(0.7f, 0.1f, 0.5f), 0.04f, 2, MetalTile);
        var gaps = new List<float>();
        foreach (float door in doors) gaps.Add(door);
        foreach (float y in new[] { 0.76f, 1.94f })
        {
            Kit("TrainChrome").Box(new Vector3(BodyXMin - 0.006f, y, (zStart + zEnd) / 2f), new Vector3(0.012f, 0.016f, length - 0.1f), MetalTile);

            float from = zStart + 0.05f;
            foreach (float door in gaps)
            {
                Strip(from, door - 0.7f, y);
                from = door + 0.7f;
            }
            if (hasCab)
            {
                Strip(from, CabDoorMin - 0.02f, y);
                from = CabDoorMax + 0.02f;
            }
            Strip(from, zEnd - 0.05f, y);
        }

        foreach (float door in doors)
        {
            BuildDoorway(door);

            float height = (FloorY + CarDoorTop) / 2f, tall = CarDoorTop - FloorY;
            foreach (float z in new[] { door - 0.68f, door, door + 0.68f })
                trim.Box(new Vector3(BodyXMin - 0.004f, height, z), new Vector3(0.012f, tall, 0.03f), MetalTile);
            trim.Box(new Vector3(BodyXMin - 0.004f, CarDoorTop, door), new Vector3(0.012f, 0.03f, 1.39f), MetalTile);

            // A pole to hang on to, with its foot and its top collar.
            Kit("TrainChrome").Tube(new Vector3(TrackX, FloorY, door), new Vector3(TrackX, CeilingY, door), 0.018f, 8, MetalTile);
            foreach (float y in new[] { FloorY + 0.012f, CeilingY - 0.012f })
                Kit("TrainChrome").Tube(new Vector3(TrackX, y - 0.012f, door), new Vector3(TrackX, y + 0.012f, door), 0.034f, 10, MetalTile);
        }

        // Rails down both sides of the ceiling.
        foreach (float x in new[] { TrackX - 0.62f, TrackX + 0.62f })
        {
            Kit("TrainChrome").Tube(new Vector3(x, 1.96f, zStart + 0.5f), new Vector3(x, 1.96f, saloonEnd - 0.5f), 0.014f, 8, MetalTile);
            for (float z = zStart + 1f; z < saloonEnd; z += 2.6f)
                Kit("TrainChrome").Tube(new Vector3(x, 1.96f, z), new Vector3(x, CeilingY - 0.02f, z), 0.008f, 6, MetalTile);
        }

        Seats(zStart + 0.35f, doors[0] - 0.8f);
        Seats(doors[0] + 0.8f, doors[1] - 0.8f);
        Seats(doors[1] + 0.8f, doors[2] - 0.8f);
        Seats(doors[2] + 0.8f, saloonEnd - 0.2f);

        float saloonMiddle = (zStart + saloonEnd) / 2f, strip = saloonEnd - zStart - 1.4f;
        Kit("TrainTrim").RoundedBox(new Vector3(TrackX, CeilingY - 0.004f, saloonMiddle), new Vector3(0.32f, 0.03f, strip + 0.08f), 0.012f, 2, MetalTile);
        trainGlow.Box(new Vector3(TrackX, CeilingY - 0.022f, saloonMiddle), new Vector3(0.24f, 0.016f, strip), Vector2.one);
        AreaLight(lights, "CarLight", new Vector3(TrackX, CeilingY - 0.04f, saloonMiddle), new Vector2(0.24f, strip), CarLightColor, 7.5f);

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

    // A bright strip along the platform side, between one doorway and the next.
    static void Strip(float zFrom, float zTo, float y)
    {
        Kit("TrainChrome").Box(new Vector3(BodyXMax + 0.006f, y, (zFrom + zTo) / 2f), new Vector3(0.012f, 0.016f, zTo - zFrom), MetalTile);
    }

    // A pair of doors on the platform side, each leaf hollow so that it can slide between the skin and the lining.
    static void BuildDoorway(float door)
    {
        void Leaf(float zFrom, float zTo, float slide)
        {
            var kit = new MeshKit();
            Rect[] glass = { Rect.MinMaxRect(zFrom + 0.14f, DoorWindowBottom, zTo - 0.14f, WindowTop) };
            WallAlongZ(kit, BodyXMax - 0.014f, 1f, zFrom, zTo, FloorY, CarDoorTop, glass, TrainTile, kit, WallDepth - 0.028f);
            WallAlongZ(kit, BodyXMax - WallDepth + 0.014f, -1f, zFrom, zTo, FloorY, CarDoorTop, glass, TrainTile);

            float meetingEdge = slide > 0f ? zFrom : zTo;
            kit.PanelFacing(new Vector3(BodyXMax - 0.014f, FloorY, meetingEdge), Vector3.left * (WallDepth - 0.028f), Vector3.up * (CarDoorTop - FloorY), slide > 0f ? Vector3.back : Vector3.forward, TrainTile);

            GameObject leaf = Dynamic($"Door{doorLeaves.Count}", train, kit, "TrainBody");
            var box = leaf.AddComponent<BoxCollider>();
            box.center = new Vector3(BodyXMax - WallDepth / 2f, (FloorY + CarDoorTop) / 2f, (zFrom + zTo) / 2f);
            box.size = new Vector3(WallDepth, CarDoorTop - FloorY, zTo - zFrom);

            var opener = leaf.AddComponent<SlideOpen>();
            opener.offset = Vector3.forward * slide;
            opener.seconds = 1.1f;
            doorLeaves.Add(opener);

            var rubber = new MeshKit();
            rubber.Gasket(new Vector3(BodyXMax - 0.014f, glass[0].center.y, glass[0].center.x), Vector3.right, Vector3.up, glass[0].width, glass[0].height, 0.016f, 0.05f, 0.006f, MetalTile);
            rubber.Gasket(new Vector3(BodyXMax - WallDepth + 0.014f, glass[0].center.y, glass[0].center.x), Vector3.left, Vector3.up, glass[0].width, glass[0].height, 0.016f, 0.05f, 0.006f, MetalTile);
            Dynamic($"Door{doorLeaves.Count - 1}Rubber", leaf.transform, rubber, "TrainRubber");
        }

        Leaf(door - 0.67f, door, -0.66f);
        Leaf(door, door + 0.67f, 0.66f);
        Kit("TrainTrim").Box(new Vector3(BodyXMax + 0.002f, CarDoorTop + 0.02f, door), new Vector3(0.012f, 0.04f, 1.42f), MetalTile);
    }

    // A red bench down each wall, between a pair of doors.
    static void Seats(float zStart, float zEnd)
    {
        float middle = (zStart + zEnd) / 2f, length = zEnd - zStart;
        foreach (float inward in new[] { 1f, -1f })
        {
            float wall = inward > 0f ? BodyXMin + WallDepth : BodyXMax - WallDepth;
            Kit("TrainSeat").RoundedBox(new Vector3(wall + inward * 0.25f, 0.46f, middle), new Vector3(0.5f, 0.11f, length), 0.04f, 3, MetalTile);
            Kit("TrainSeat").RoundedBox(new Vector3(wall + inward * 0.05f, 0.78f, middle), new Vector3(0.09f, 0.5f, length), 0.035f, 3, MetalTile);
            Kit("TrainUnder").Box(new Vector3(wall + inward * 0.2f, 0.22f, middle), new Vector3(0.34f, 0.36f, length), MetalTile);

            // A divider every few places, and a screen at each end.
            foreach (float z in new[] { zStart + 0.02f, zEnd - 0.02f })
                Kit("TrainChrome").RoundedBox(new Vector3(wall + inward * 0.26f, 0.72f, z), new Vector3(0.5f, 0.62f, 0.028f), 0.012f, 2, MetalTile);
        }
    }

    // Something small on the sloping panel: a post with a cap, a lens in a ring.
    static void Knob(MeshKit kit, Vector3 at, Vector3 facing, Vector3 up, float radius, float height)
    {
        kit.Tube(at, at + facing * height, radius, 10, MetalTile);
        kit.Disc(at + facing * height, facing, up, radius, 10);
    }

    static void Lens(string kit, Vector3 at, Vector3 facing, Vector3 up, float radius)
    {
        Kit("TrainChrome").Annulus(at + facing * 0.006f, facing, up, radius * 0.8f, radius * 1.3f, 12, MetalTile);
        Kit("TrainChrome").Tube(at, at + facing * 0.006f, radius * 1.3f, 12, MetalTile);
        Knob(Kit(kit), at, facing, up, radius, 0.008f);
    }

    static void Dial(string kit, Vector3 center, Vector3 facing, Vector3 up, float radius)
    {
        Kit("TrainBakelite").Tube(center - facing * 0.005f, center + facing * 0.002f, radius + 0.02f, 28, MetalTile);
        Kit("TrainBakelite").Annulus(center + facing * 0.002f, facing, up, radius + 0.008f, radius + 0.02f, 28, MetalTile);
        Kit(kit).Disc(center + facing * 0.003f, facing, up, radius, 32);
        Kit("TrainChrome").Tube(center + facing * 0.002f, center + facing * 0.016f, radius + 0.008f, 28, MetalTile);
        Kit("TrainChrome").Annulus(center + facing * 0.016f, facing, up, radius, radius + 0.008f, 28, MetalTile);
        Kit("TrainChrome").Tube(center + facing * 0.003f, center + facing * 0.011f, 0.008f, 8, MetalTile);
        Kit("TrainChrome").Disc(center + facing * 0.011f, facing, up, 0.008f, 8);
    }

    static Vector3[] DeskAt(float x, float zShift)
    {
        var section = new Vector3[DeskSection.Length];
        for (int i = 0; i < section.Length; i++) section[i] = new Vector3(x, DeskSection[i].y, DeskSection[i].x + zShift);
        return section;
    }

    // The steel shell of the cab: walls, rolled ceiling, and the three panes across the nose.
    static void BuildCabShell()
    {
        MeshKit paint = Kit("TrainCabPaint"), roof = Kit("TrainCabRoof"), trim = Kit("TrainTrim");
        Rect farWindow = Rect.MinMaxRect(CabBack + 0.35f, WindowBottom, CabNoseZ - 0.12f, WindowTop);

        WallAlongZ(paint, CabLeft, 1f, CabBack, CabNoseZ + 0.02f, FloorY, CabWallTop, new[] { farWindow }, LiningTile);
        WallAlongZ(paint, CabRight, -1f, CabBack, CabNoseZ + 0.02f, FloorY, CabWallTop, new[] { CabDoorway }, LiningTile);
        WindowRubber(CabLeft, 1f, farWindow);
        WindowRubber(BodyXMin, -1f, farWindow);

        roof.Sweep(Cove(CabLeft, CabRight, CabWallTop, CabCove, CabCrown, CabBack, false), Vector3.forward * (CabNoseZ - CabBack + 0.02f), PlasterTile);

        // The back wall, with a look-through into the saloon.
        Rect look = Rect.MinMaxRect(TrackX - 0.17f, 1.14f, TrackX + 0.17f, 1.72f);
        Rect[] lookHole = { look };
        WallAlongX(paint, CabBack, 1f, CabLeft, CabRight, FloorY, CabWallTop, lookHole, LiningTile);
        paint.Fan(Cove(CabLeft, CabRight, CabWallTop, CabCove, CabCrown, CabBack, false), Vector3.forward, Vector3.right, Vector3.up, LiningTile);
        WallAlongX(Kit("TrainInterior"), CabBack - 0.05f, -1f, BodyXMin, BodyXMax, FloorY, SaloonWallTop, lookHole, LiningTile);
        Kit("TrainInterior").Fan(Cove(BodyXMin + WallDepth, BodyXMax - WallDepth, SaloonWallTop, SaloonCove, SaloonCrown, CabBack - 0.05f, false), Vector3.back, Vector3.right, Vector3.up, LiningTile);

        WallAlongX(trim, CabBack + 0.02f, 1f, TrackX - 0.38f, TrackX + 0.38f, FloorY, 1.98f, lookHole, MetalTile, Kit("TrainTrim"), 0.07f);
        foreach (float z in new[] { CabBack + 0.02f })
        {
            trim.Box(new Vector3(TrackX, 1f, z - 0.03f), new Vector3(0.78f, 0.001f, 0.001f), MetalTile);
            Kit("TrainRubber").Gasket(new Vector3(TrackX, look.center.y, z), Vector3.forward, Vector3.up, look.width, look.height, 0.02f, 0.06f, 0.008f, MetalTile);
        }
        Kit("TrainRubber").Gasket(new Vector3(TrackX, look.center.y, CabBack - 0.05f), Vector3.back, Vector3.up, look.width, look.height, 0.02f, 0.06f, 0.008f, MetalTile);
        Kit("TrainChrome").Tube(new Vector3(TrackX + 0.31f, 0.92f, CabBack + 0.08f), new Vector3(TrackX + 0.31f, 1.16f, CabBack + 0.08f), 0.012f, 8, MetalTile);
        foreach (float y in new[] { 0.92f, 1.16f })
            Kit("TrainChrome").Tube(new Vector3(TrackX + 0.31f, y, CabBack + 0.02f), new Vector3(TrackX + 0.31f, y, CabBack + 0.08f), 0.01f, 8, MetalTile);

        // Across the nose: the front pane, and a pane either side set back on the corner.
        float frontLeft = CabLeft + NoseInset, frontRight = CabRight - NoseInset;
        Rect screen = Rect.MinMaxRect(frontLeft + 0.1f, ScreenBottom, frontRight - 0.1f, ScreenTop);
        WallAlongX(paint, CabFrontZ, -1f, frontLeft, frontRight, FloorY, SoffitY + 0.03f, new[] { screen }, LiningTile);
        Kit("TrainRubber").Gasket(new Vector3((screen.xMin + screen.xMax) / 2f, screen.center.y, CabFrontZ), Vector3.back, Vector3.up, screen.width, screen.height, 0.028f, 0.09f, 0.01f, MetalTile);

        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 corner = new Vector3(side < 0f ? CabLeft : CabRight, FloorY, CabNoseZ), front = new Vector3(side < 0f ? frontLeft : frontRight, FloorY, CabFrontZ);
            Vector3 along = corner - front;
            float width = along.magnitude;
            Rect pane = new Rect(0.14f, ScreenBottom - FloorY, width - 0.28f, ScreenTop - ScreenBottom);

            MeshKit wall = paint;
            Vector3 u = along.normalized;
            Vector3 inward = Vector3.Cross(u, Vector3.up);
            if (Vector3.Dot(inward, Vector3.back) < 0f)
            {
                // Walked the other way round so that the wall shows on the inside.
                wall.WallWithHoles(corner, -u, Vector3.up, width, SoffitY + 0.03f - FloorY, new[] { new Rect(width - pane.xMax, pane.y, pane.width, pane.height) }, LiningTile);
                inward = -inward;
            }
            else
            {
                wall.WallWithHoles(front, u, Vector3.up, width, SoffitY + 0.03f - FloorY, new[] { pane }, LiningTile);
            }

            Vector3 paneMiddle = front + u * (pane.x + pane.width / 2f) + Vector3.up * (pane.y + pane.height / 2f);
            Kit("TrainRubber").Gasket(paneMiddle, inward.normalized, Vector3.up, pane.width, pane.height, 0.024f, 0.07f, 0.01f, MetalTile);
        }

        // The header over the glass, coming down from the ceiling and running forward as a soffit.
        var header = new List<Vector3>
        {
            new Vector3(CabLeft, SoffitY - 0.01f, CabFrontZ + 0.03f), new Vector3(CabLeft, SoffitY, 32.86f), new Vector3(CabLeft, SoffitY + 0.012f, 32.8f),
            new Vector3(CabLeft, SoffitY + 0.04f, 32.75f), new Vector3(CabLeft, SoffitY + 0.09f, CabNoseZ), new Vector3(CabLeft, 2.32f, CabNoseZ),
        };
        roof.Sweep(header, Vector3.right * (CabRight - CabLeft), PlasterTile, 60f);

        var floorLine = new[]
        {
            new Vector3(BodyXMin, FloorY, CabNoseZ), new Vector3(CabLeft + NoseInset, FloorY, TrainFront), new Vector3(CabRight - NoseInset, FloorY, TrainFront), new Vector3(BodyXMax, FloorY, CabNoseZ),
        };
        Kit("TrainFloor").Fan(floorLine, Vector3.up, Vector3.right, Vector3.forward, MetalTile);

        // Dirt and fingermarks on the glass, with the arcs the wipers keep clean.
        var dirt = new MeshKit();
        dirt.Plate(new Vector3((screen.xMin + screen.xMax) / 2f, screen.center.y, CabFrontZ + 0.03f), Vector3.back, Vector3.up, screen.width + 0.02f, screen.height + 0.02f, 0.001f);
        CommitStains("TrainWindscreenDirt", "T_WindscreenDirt", dirt, train);
    }

    static void BuildDesk()
    {
        MeshKit desk = Kit("TrainDesk"), chrome = Kit("TrainChrome");
        Vector2 tile = new Vector2(0.8f, 0.8f);
        desk.Sweep(DeskAt(CabLeft, -WingBack), new Vector3(DeskXMin - CabLeft, 0f, WingBack), tile, 55f);
        desk.Sweep(DeskAt(DeskXMin, 0f), Vector3.right * (DeskXMax - DeskXMin), tile, 55f);
        desk.Sweep(DeskAt(DeskXMax, 0f), new Vector3(CabRight - DeskXMax, 0f, -WingBack), tile, 55f);

        // A steel edging along the lip of the desk, where hands and elbows have worn it bright.
        var lip = new[] { new Vector3(0f, 0.902f, 32.338f), new Vector3(0f, 0.876f, 32.33f), new Vector3(0f, 0.85f, 32.34f) };
        foreach ((float from, float to, float back) in new[] { (DeskXMin, DeskXMax, 0f) })
        {
            var strip = new List<Vector3>();
            foreach (Vector3 point in lip) strip.Add(point + Vector3.right * from + Vector3.back * 0.006f);
            chrome.Sweep(strip, Vector3.right * (to - from), MetalTile);
        }

        // The panels let into the slope and into the hood.
        var near = new Vector3(0f, PanelNear.y, PanelNear.z) + PanelFacing * 0.002f;
        var far = new Vector3(0f, PanelFar.y, PanelFar.z) + PanelFacing * 0.002f;
        const float panelLeft = 2f, panelRight = 3.42f;
        Kit("TrainConsole").Quad(near + Vector3.right * panelRight, near + Vector3.right * panelLeft, far + Vector3.right * panelLeft, far + Vector3.right * panelRight,
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Vector3 speed = OnHood(TrackX, 0.5f), up = HoodUp, facing = HoodFacing;
        Dial("TrainSpeedDial", speed, facing, up, SpeedDialRadius);
        Dial("TrainPressureDial", OnHood(TrackX - 0.31f, 0.52f), facing, up, SmallDialRadius);
        Dial("TrainVoltDial", OnHood(TrackX + 0.31f, 0.52f), facing, up, SmallDialRadius);
        Dial("TrainClockDial", OnHood(TrackX + 0.58f, 0.52f), facing, up, 0.058f);

        // Warning lamps along the hood under the dials.
        for (int i = 0; i < 4; i++)
        {
            string lens = i == 0 ? "TrainRedLens" : i == 1 ? "TrainAmberLens" : "TrainGreenLens";
            Lens(lens, OnHood(TrackX - 0.72f + i * 0.09f, 0.3f), facing, up, 0.012f);
            Lens(i % 2 == 0 ? "TrainGreenLens" : "TrainAmberLens", OnHood(TrackX - 0.72f + i * 0.09f, 0.7f), facing, up, 0.012f);
        }

        // Rows of switches along the slope, and the two big buttons for the doors.
        Vector3 panelUp = PanelUp, panelFacing = PanelFacing;
        for (int i = 0; i < 8; i++)
        {
            Vector3 at = OnPanel(2.1f + i * 0.085f, 0.28f);
            chrome.Annulus(at + panelFacing * 0.004f, panelFacing, panelUp, 0.004f, 0.014f, 10, MetalTile);
            chrome.Tube(at, at + panelFacing * 0.004f, 0.014f, 10, MetalTile);
            Vector3 tip = at + panelFacing * 0.034f + panelUp * (i % 3 == 0 ? 0.012f : -0.012f);
            Kit("TrainChrome").Tube(at, tip, 0.0045f, 6, MetalTile);
            Knob(Kit("TrainBakelite"), tip, (tip - at).normalized, panelUp, 0.0065f, 0.01f);
        }
        for (int i = 0; i < 3; i++)
        {
            Vector3 at = OnPanel(2.12f + i * 0.13f, 0.72f);
            Knob(Kit("TrainBakelite"), at, panelFacing, panelUp, 0.026f, 0.022f);
            Kit("TrainChrome").Annulus(at + panelFacing * 0.0225f, panelFacing, panelUp, 0.004f, 0.018f, 12, MetalTile);
        }
        foreach ((float x, string lens) in new[] { (2.92f, "TrainGreenLens"), (3.14f, "TrainRedLens") })
        {
            Vector3 at = OnPanel(x, 0.66f);
            Kit("TrainChrome").Tube(at, at + panelFacing * 0.012f, 0.05f, 16, MetalTile);
            Kit("TrainChrome").Annulus(at + panelFacing * 0.012f, panelFacing, panelUp, 0.036f, 0.05f, 16, MetalTile);
            Knob(Kit(lens), at, panelFacing, panelUp, 0.037f, 0.024f);
        }

        BuildControllerBody();
    }

    // The driver's chair: a padded seat and back on a steel post, worn pale where people have sat in it.
    static void BuildDriverSeat()
    {
        var seat = new Vector3(TrackX, 0f, SeatZ);
        MeshKit cushion = Kit("TrainCushion"), steel = Kit("TrainSteel");
        Vector2 tile = new Vector2(0.5f, 0.5f);

        steel.Tube(seat + Vector3.up * (FloorY + 0.02f), seat + Vector3.up * (SeatTop - 0.1f), 0.042f, 12, MetalTile);
        steel.Tube(seat + Vector3.up * (FloorY + 0.18f), seat + Vector3.up * (FloorY + 0.4f), 0.056f, 12, MetalTile);
        Kit("TrainTrim").RoundedBox(seat + Vector3.up * (FloorY + 0.012f), new Vector3(0.44f, 0.024f, 0.44f), 0.012f, 2, MetalTile);
        Kit("TrainTrim").RoundedBox(seat + Vector3.up * (SeatTop - 0.105f), new Vector3(0.42f, 0.03f, 0.4f), 0.01f, 2, MetalTile);

        cushion.RoundedBox(seat + Vector3.up * (SeatTop - 0.045f), new Vector3(0.5f, 0.11f, 0.48f), 0.05f, 4, tile);

        Quaternion leaning = Quaternion.Euler(-9f, 0f, 0f);
        Vector3 back = seat + new Vector3(0f, SeatTop + 0.3f, -0.275f);
        cushion.RoundedBox(Matrix4x4.TRS(back, leaning, Vector3.one), new Vector3(0.46f, 0.6f, 0.1f), 0.045f, 4, tile);
        Kit("TrainTrim").RoundedBox(Matrix4x4.TRS(back + leaning * new Vector3(0f, 0f, -0.06f), leaning, Vector3.one), new Vector3(0.42f, 0.56f, 0.03f), 0.012f, 2, MetalTile);
        Vector3 rest = back + leaning * new Vector3(0f, 0.42f, 0.012f);
        cushion.RoundedBox(Matrix4x4.TRS(rest, leaning, Vector3.one), new Vector3(0.26f, 0.16f, 0.09f), 0.04f, 4, tile);
        foreach (float side in new[] { -0.07f, 0.07f })
            Kit("TrainChrome").Tube(back + leaning * new Vector3(side, 0.28f, 0f), rest + leaning * new Vector3(side, 0f, 0f), 0.007f, 6, MetalTile);

        foreach (float side in new[] { -1f, 1f })
        {
            Vector3 pivot = seat + new Vector3(side * 0.27f, SeatTop + 0.12f, -0.23f);
            Kit("TrainTrim").Tube(pivot, seat + new Vector3(side * 0.25f, SeatTop + 0.1f, -0.27f), 0.014f, 6, MetalTile);
            cushion.RoundedBox(pivot + new Vector3(0f, 0.02f, 0.14f), new Vector3(0.07f, 0.05f, 0.34f), 0.022f, 3, tile);
        }

        // The rubber mat and the foot switch that has to be kept down.
        Kit("TrainRubber").RoundedBox(new Vector3(TrackX, FloorY + 0.006f, 32.26f), new Vector3(1.1f, 0.012f, 0.72f), 0.005f, 1, MetalTile);
        Kit("TrainTrim").RoundedBox(Matrix4x4.TRS(new Vector3(TrackX + 0.2f, FloorY + 0.06f, 32.5f), Quaternion.Euler(-24f, 0f, 0f), Vector3.one), new Vector3(0.2f, 0.018f, 0.24f), 0.008f, 2, MetalTile);
    }

    // Everything that's been fitted to the cab over the years.
    static void BuildCabFittings(Transform lights)
    {
        MeshKit trim = Kit("TrainTrim"), chrome = Kit("TrainChrome");

        // Cable runs along the top of each wall, clipped every so often.
        foreach (float x in new[] { CabLeft + 0.03f, CabRight - 0.03f })
        {
            foreach (float y in new[] { 1.96f, 1.915f })
                Kit("TrainBakelite").Tube(new Vector3(x, y, CabBack), new Vector3(x, y, CabNoseZ), 0.012f, 6, MetalTile);
            for (float z = CabBack + 0.2f; z < CabNoseZ; z += 0.5f)
                chrome.Box(new Vector3(x, 1.94f, z), new Vector3(0.03f, 0.1f, 0.016f), MetalTile);
        }

        // A hooded lamp over the desk, so the driver can read the dials and find the controller.
        var deskLamp = new Vector3(1.96f, SoffitY - 0.02f, 32.88f);
        trim.RoundedBox(deskLamp + Vector3.up * 0.01f, new Vector3(0.38f, 0.05f, 0.15f), 0.02f, 2, MetalTile);
        trainGlow.Box(deskLamp + Vector3.down * 0.018f, new Vector3(0.3f, 0.012f, 0.08f), Vector2.one);
        PointLight(lights, "DeskLamp", deskLamp + new Vector3(0.3f, -0.3f, -0.26f), new Color(1f, 0.9f, 0.74f), 0.42f, 3f);

        // The dome light in the ceiling.
        var dome = new Vector3(TrackX, CabWallTop + CabCove + CabCrown - 0.012f, 31.95f);
        chrome.Tube(dome + Vector3.up * 0.01f, dome + Vector3.down * 0.02f, 0.135f, 20, MetalTile);
        chrome.Annulus(dome + Vector3.down * 0.02f, Vector3.down, Vector3.forward, 0.11f, 0.135f, 20, MetalTile);
        trainGlow.Disc(dome + Vector3.down * 0.022f, Vector3.down, Vector3.forward, 0.112f, 16);
        PointLight(lights, "CabLamp", dome + Vector3.down * 0.2f, new Color(1f, 0.88f, 0.7f), 0.34f, 5f);

        // A fan on the header, for the summer nobody down here will see.
        var fan = new Vector3(3.3f, SoffitY + 0.12f, CabNoseZ - 0.05f);
        trim.Tube(fan + Vector3.forward * 0.05f, fan, 0.03f, 10, MetalTile);
        chrome.Tube(fan + Vector3.back * 0.02f, fan + Vector3.back * 0.03f, 0.09f, 18, MetalTile);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f + 0.4f;
            Vector3 way = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            trim.Box(Matrix4x4.TRS(fan + Vector3.back * 0.025f + way * 0.045f, Quaternion.LookRotation(Vector3.forward, way), Vector3.one), new Vector3(0.05f, 0.08f, 0.004f), MetalTile);
        }

        // The sun visor nobody has needed since the day it was fitted, and the wiper motor's housing.
        trim.RoundedBox(Matrix4x4.TRS(new Vector3(1.84f, ScreenTop + 0.015f, CabFrontZ - 0.06f), Quaternion.Euler(-34f, 0f, 2f), Vector3.one), new Vector3(0.36f, 0.1f, 0.012f), 0.006f, 1, MetalTile);
        chrome.Tube(new Vector3(1.66f, SoffitY - 0.005f, CabFrontZ - 0.08f), new Vector3(2.02f, SoffitY - 0.005f, CabFrontZ - 0.08f), 0.006f, 6, MetalTile);
        trim.RoundedBox(new Vector3(TrackX, ScreenBottom + 0.05f, CabFrontZ - 0.03f), new Vector3(0.2f, 0.06f, 0.05f), 0.015f, 2, MetalTile);

        // The monitor, hung from the header on the driver's right. It's dead until a shift wires it up.
        trim.RoundedBox(Matrix4x4.TRS(MonitorAt, MonitorTilt, Vector3.one), new Vector3(0.25f, 0.2f, 0.17f), 0.024f, 3, MetalTile);
        trim.Tube(MonitorAt + Vector3.up * 0.08f, new Vector3(MonitorAt.x, SoffitY + 0.01f, MonitorAt.z + 0.02f), 0.016f, 8, MetalTile);
        Kit("TrainRubber").Gasket(MonitorAt + MonitorTilt * new Vector3(0f, 0f, -0.087f), MonitorTilt * Vector3.back, MonitorTilt * Vector3.up, 0.19f, 0.145f, 0.016f, 0.02f, 0.006f, MetalTile);
        Kit("TrainBakelite").Plate(MonitorAt + MonitorTilt * new Vector3(0f, 0f, -0.085f), MonitorTilt * Vector3.back, MonitorTilt * Vector3.up, 0.2f, 0.155f, 0.001f);

        // The radio, on a bracket to the right of the desk, with its handset hung on the side.
        var radio = new Vector3(3.56f, 1.09f, 32.3f);
        Quaternion radioTurn = Quaternion.Euler(0f, -26f, 0f);
        trim.RoundedBox(Matrix4x4.TRS(radio, radioTurn, Vector3.one), new Vector3(0.3f, 0.2f, 0.24f), 0.018f, 2, MetalTile);
        Kit("TrainRadio").Plate(radio + radioTurn * new Vector3(0f, 0f, -0.121f), radioTurn * Vector3.back, Vector3.up, 0.27f, 0.17f, 0.002f);
        Vector3 hook = radio + radioTurn * new Vector3(-0.17f, 0.04f, -0.05f);
        Quaternion hanging = radioTurn * Quaternion.Euler(0f, 0f, 6f);
        Kit("TrainBakelite").RoundedBox(Matrix4x4.TRS(hook, hanging, Vector3.one), new Vector3(0.04f, 0.2f, 0.046f), 0.016f, 2, MetalTile);
        foreach (float y in new[] { -0.1f, 0.1f })
            Kit("TrainBakelite").RoundedBox(Matrix4x4.TRS(hook + hanging * new Vector3(-0.012f, y, 0f), hanging, Vector3.one), new Vector3(0.05f, 0.05f, 0.056f), 0.02f, 2, MetalTile);

        // Its cord, in a slack coil down to the bracket.
        Vector3 last = hook + hanging * new Vector3(0f, -0.11f, 0f);
        for (int i = 1; i <= 26; i++)
        {
            float t = i / 26f;
            Vector3 next = Vector3.Lerp(hook + hanging * new Vector3(0f, -0.11f, 0f), radio + radioTurn * new Vector3(-0.14f, -0.11f, -0.02f), t)
                + new Vector3(Mathf.Sin(t * 42f) * 0.008f, -Mathf.Sin(t * Mathf.PI) * 0.09f, Mathf.Cos(t * 42f) * 0.008f);
            Kit("TrainBakelite").Tube(last, next, 0.0025f, 4, MetalTile);
            last = next;
        }
        foreach (float x in new[] { -0.1f, 0.1f })
            trim.Box(radio + radioTurn * new Vector3(x, -0.12f, 0.02f), new Vector3(0.03f, 0.04f, 0.16f), MetalTile);

        // The orders for the night, clipped to the left of the desk.
        Vector3 wingFacing = Vector3.Cross(PanelUp + Vector3.right * 0.05f, Vector3.right + Vector3.forward * 0.3f).normalized;
        Vector3 notes = new Vector3(1.38f, 0.975f, 32.34f);
        Quaternion lying = Quaternion.LookRotation(wingFacing, PanelUp) * Quaternion.Euler(0f, 0f, 5f);
        Kit("TrainNotes").Plate(notes, lying * Vector3.forward, lying * Vector3.up, 0.21f, 0.3f, 0.004f);
        chrome.Box(Matrix4x4.TRS(notes + lying * new Vector3(0f, 0.15f, 0.006f), lying, Vector3.one), new Vector3(0.08f, 0.018f, 0.012f), MetalTile);

        // A heater under the far window, and the extinguisher and timetable on the back wall.
        Kit("TrainDesk").RoundedBox(new Vector3(CabLeft + 0.07f, 0.34f, 31.95f), new Vector3(0.12f, 0.46f, 1.05f), 0.02f, 2, MetalTile);
        for (float z = 31.5f; z < 32.42f; z += 0.05f)
            Kit("TrainBakelite").Box(new Vector3(CabLeft + 0.131f, 0.42f, z), new Vector3(0.002f, 0.24f, 0.02f), MetalTile);

        Vector2[] bottle =
        {
            new Vector2(0f, 0f), new Vector2(0.065f, 0f), new Vector2(0.07f, 0.02f), new Vector2(0.07f, 0.4f), new Vector2(0.055f, 0.45f),
            new Vector2(0.02f, 0.47f), new Vector2(0.02f, 0.52f), new Vector2(0f, 0.53f),
        };
        Kit("TrainSeat").Lathe(new Vector3(3.6f, FloorY + 0.06f, CabBack + 0.1f), bottle, 12, MetalTile);
        chrome.Box(new Vector3(3.6f, 0.34f, CabBack + 0.03f), new Vector3(0.18f, 0.03f, 0.05f), MetalTile);

        Kit("TrainNotes").Plate(new Vector3(1.66f, 1.42f, CabBack + 0.012f), Vector3.forward, Vector3.up, 0.3f, 0.42f, 0.006f);
        Kit("TrainChrome").Tube(new Vector3(CabRight - 0.04f, 0.88f, 31.14f), new Vector3(CabRight - 0.04f, 1.62f, 31.14f), 0.013f, 8, MetalTile);
        foreach (float y in new[] { 0.9f, 1.6f })
            Kit("TrainChrome").Tube(new Vector3(CabRight, y, 31.14f), new Vector3(CabRight - 0.04f, y, 31.14f), 0.01f, 6, MetalTile);

        // A plate over the gap between the cab and the platform.
        trim.Box(new Vector3((BodyXMax + EdgeX) / 2f, 0.02f, CabDoorMiddle), new Vector3(EdgeX - BodyXMax + 0.2f, 0.05f, CabDoorMax - CabDoorMin), MetalTile);
    }

    static void BuildCab(Transform lights)
    {
        BuildCabShell();
        BuildDesk();
        BuildDriverSeat();
        BuildCabFittings(lights);
    }

    // The front of the train: the same three faces as the cab inside them, with a brow over the glass.
    static void BuildNose()
    {
        MeshKit skin = Kit("TrainBody"), roof = Kit("TrainRoof"), trim = Kit("TrainTrim");
        float frontLeft = BodyXMin + NoseInset + 0.02f, frontRight = BodyXMax - NoseInset - 0.02f;

        Rect screen = Rect.MinMaxRect(CabLeft + NoseInset + 0.1f, ScreenBottom, CabRight - NoseInset - 0.1f, ScreenTop);
        WallAlongX(skin, TrainFront, 1f, frontLeft, frontRight, SkirtY, EavesY, new[] { screen }, TrainTile, trim, WallDepth);
        Kit("TrainRubber").Gasket(new Vector3((screen.xMin + screen.xMax) / 2f, screen.center.y, TrainFront), Vector3.forward, Vector3.up, screen.width, screen.height, 0.04f, 0.09f, 0.012f, MetalTile);

        foreach (float side in new[] { -1f, 1f })
        {
            var corner = new Vector3(side < 0f ? BodyXMin : BodyXMax, SkirtY, CabNoseZ);
            var front = new Vector3(side < 0f ? frontLeft : frontRight, SkirtY, TrainFront);
            float width = Vector3.Distance(corner, front);
            Vector3 u = (front - corner).normalized;
            Vector3 outward = Vector3.Cross(u, Vector3.up);
            float height = ScreenBottom - SkirtY;
            var pane = new Rect(0.14f, height, width - 0.28f, ScreenTop - ScreenBottom);

            if (Vector3.Dot(outward, Vector3.forward) >= 0f)
            {
                skin.WallWithHoles(corner, u, Vector3.up, width, EavesY - SkirtY, new[] { pane }, TrainTile);
            }
            else
            {
                outward = -outward;
                skin.WallWithHoles(front, -u, Vector3.up, width, EavesY - SkirtY, new[] { new Rect(width - pane.xMax, pane.y, pane.width, pane.height) }, TrainTile);
            }

            Vector3 paneMiddle = corner + u * (pane.x + pane.width / 2f) + Vector3.up * (pane.y + pane.height / 2f);
            Kit("TrainRubber").Gasket(paneMiddle, outward.normalized, Vector3.up, pane.width, pane.height, 0.03f, 0.08f, 0.012f, MetalTile);
        }

        // The brow, rolling down from the roof to the top of the glass.
        var brow = new List<Vector3>
        {
            new Vector3(BodyXMin, 2.54f, CabNoseZ - 0.04f), new Vector3(BodyXMin, 2.49f, 32.92f), new Vector3(BodyXMin, 2.4f, 33.08f), new Vector3(BodyXMin, 2.28f, 33.2f), new Vector3(BodyXMin, EavesY, 33.24f),
        };
        roof.Sweep(brow, Vector3.right * BodyWidth, MetalTile, 50f);
        var browEnd = new List<Vector3>(brow) { new Vector3(BodyXMin, EavesY, CabNoseZ) };
        roof.Fan(browEnd, Vector3.left, Vector3.forward, Vector3.up, MetalTile);
        var otherEnd = new List<Vector3>();
        foreach (Vector3 point in browEnd) otherEnd.Add(point + Vector3.right * BodyWidth);
        roof.Fan(otherEnd, Vector3.right, Vector3.forward, Vector3.up, MetalTile);
        roof.Panel(new Vector3(BodyXMin, EavesY, TrainFront + 0.04f), Vector3.back * 0.52f, Vector3.right * BodyWidth, MetalTile);

        // Below the glass: a bumper, the coupler and the valance over the wheels.
        Kit("TrainTrim").RoundedBox(new Vector3(TrackX, 0.08f, TrainFront + 0.03f), new Vector3(2.5f, 0.2f, 0.09f), 0.03f, 3, MetalTile);
        Kit("TrainUnder").RoundedBox(new Vector3(TrackX, -0.36f, TrainFront + 0.14f), new Vector3(0.26f, 0.2f, 0.42f), 0.05f, 3, MetalTile);
        Kit("TrainTrim").RoundedBox(new Vector3(TrackX, 0.5f, TrainFront + 0.012f), new Vector3(1.7f, 0.022f, 0.024f), 0.008f, 1, MetalTile);

        // The wipers, parked at the bottom of the glass.
        foreach (float x in new[] { TrackX - 0.08f, TrackX + 0.56f })
        {
            Kit("TrainBakelite").Tube(new Vector3(x, ScreenBottom - 0.03f, TrainFront + 0.03f), new Vector3(x - 0.26f, ScreenBottom + 0.015f, TrainFront + 0.03f), 0.006f, 5, MetalTile);
            Kit("TrainBakelite").Tube(new Vector3(x - 0.48f, ScreenBottom + 0.012f, TrainFront + 0.038f), new Vector3(x - 0.04f, ScreenBottom + 0.018f, TrainFront + 0.038f), 0.006f, 5, MetalTile);
        }
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
            float front = TrackX + side * HeadlampX, x = TrackX + side * 0.95f;

            // Headlamps on the nose, each in a round housing.
            trim.Tube(new Vector3(front, 0.72f, TrainFront), new Vector3(front, 0.72f, TrainFront + 0.05f), 0.13f, 16, MetalTile);
            Kit("TrainChrome").Annulus(new Vector3(front, 0.72f, TrainFront + 0.05f), Vector3.forward, Vector3.up, 0.1f, 0.13f, 16, MetalTile);
            trainGlow.Disc(new Vector3(front, 0.72f, TrainFront + 0.046f), Vector3.forward, Vector3.up, 0.1f, 14);

            // Tail lamps on the back of the rear car.
            trim.Box(new Vector3(x, 0.62f, RearCarStart - 0.02f), new Vector3(0.3f, 0.2f, 0.04f), MetalTile);
            tailLamps.Disc(new Vector3(x, 0.62f, RearCarStart - 0.042f), Vector3.back, Vector3.up, 0.075f, 14);
        }

        Light beam = BakedLight(lights, "Headlights", LightType.Spot, new Vector3(TrackX, 0.75f, TrainFront + 0.3f), Quaternion.LookRotation(Vector3.forward), HeadlightColor, 36f);
        beam.range = 24f;
        beam.spotAngle = 64f;

        Kit("TrainSign").Plate(new Vector3(TrackX, 2.08f, TrainFront + 0.002f), Vector3.forward, Vector3.up, 0.9f, 0.2f, 0.01f);
    }

    // The speedometer's needle, pivoting on the dial on the instrument hood.
    static Transform BuildSpeedNeedle()
    {
        Vector3 up = HoodUp, facing = HoodFacing, pivot = OnHood(TrackX, 0.5f);

        var kit = new MeshKit();
        kit.Box(new Vector3(0f, 0.034f, -0.003f), new Vector3(0.006f, 0.098f, 0.002f), MetalTile);
        GameObject needle = Dynamic("SpeedNeedle", train, kit, "TrainSeat");
        needle.transform.SetPositionAndRotation(pivot + facing * 0.007f, Quaternion.LookRotation(-facing, up));
        return needle.transform;
    }

    // Where the controller stands on the sloping desk, and which ways are along and up off it.
    static void ControllerPlace(out Vector3 seat, out Vector3 up, out Vector3 facing)
    {
        up = PanelUp;
        facing = PanelFacing;
        seat = OnPanel(ControllerX, ControllerAlong);
    }

    // The controller's housing and marked scale, which belong to the desk. The arm is built with the drive.
    static void BuildControllerBody()
    {
        const float width = 0.2f, length = 0.36f;
        ControllerPlace(out Vector3 seat, out Vector3 up, out Vector3 facing);
        Quaternion onDesk = Quaternion.LookRotation(up, facing);
        Kit("TrainControllerBody").RoundedBox(Matrix4x4.TRS(seat + facing * (ControllerBodyHeight / 2f), onDesk, Vector3.one), new Vector3(width, ControllerBodyHeight, length), 0.018f, 3, MetalTile);
        Kit("TrainControllerScale").Plate(seat + facing * ControllerBodyHeight, facing, up, 0.15f, 0.31f, 0.002f);
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

    // The cab door: two skins and a window, sliding back along the body.
    static SlideOpen BuildCabDoor()
    {
        var doorKit = new MeshKit();
        Rect glass = Rect.MinMaxRect(CabDoorMin + 0.2f, WindowBottom, CabDoorMax - 0.2f, WindowTop);
        Rect[] doorWindow = { glass };
        WallAlongZ(doorKit, BodyXMax + 0.03f, 1f, CabDoorMin - 0.02f, CabDoorMax + 0.02f, FloorY, CabDoorTop + 0.02f, doorWindow, TrainTile);
        WallAlongZ(doorKit, BodyXMax + 0.005f, -1f, CabDoorMin - 0.02f, CabDoorMax + 0.02f, FloorY, CabDoorTop + 0.02f, doorWindow, TrainTile);
        GameObject cabDoor = Dynamic("CabDoor", train, doorKit, "TrainBody");
        var doorCollider = cabDoor.AddComponent<BoxCollider>();
        doorCollider.center = new Vector3(BodyXMax + 0.02f, 1f, CabDoorMiddle);
        doorCollider.size = new Vector3(0.08f, 2f, CabDoorWidth + 0.04f);

        var rubber = new MeshKit();
        var middle = new Vector3(BodyXMax + 0.03f, glass.center.y, glass.center.x);
        rubber.Gasket(middle, Vector3.right, Vector3.up, glass.width, glass.height, 0.02f, 0.06f, 0.008f, MetalTile);
        rubber.Gasket(new Vector3(BodyXMax + 0.005f, glass.center.y, glass.center.x), Vector3.left, Vector3.up, glass.width, glass.height, 0.02f, 0.06f, 0.008f, MetalTile);
        Dynamic("CabDoorRubber", cabDoor.transform, rubber, "TrainRubber");

        var slide = cabDoor.AddComponent<SlideOpen>();
        slide.offset = Vector3.back * (CabDoorWidth + 0.06f);
        slide.sound = Emitter(cabDoor.transform, "DoorSound", new Vector3(BodyXMax, 1f, CabDoorMiddle), Sound("I_CabDoor"), 0.7f, 2f, 24f);
        return slide;
    }
}
