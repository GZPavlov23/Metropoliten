using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static SceneForge;

// The end of the line, where the tunnel gives out at last into a grey morning on the edge of town: a concrete platform
// under a tin roof, the depot sheds over the fence, a hut with a time clock, and a road with a bus stop on it.
public static partial class StationBuilder
{
    const float PortalZ = ClosedHallEnd + 180f, TerminalZ = PortalZ + 96f, TerminalLength = 46f, DepotBoardZ = TerminalZ + BoardZ, LineEnd = TerminalZ + 70f;
    const float FenceX = 10.4f, GateZ = TerminalZ + 39.2f, GateWidth = 2f, HutZ = TerminalZ + 40.6f, CanopyY = 3.1f;
    const float RoadXMin = 13.4f, RoadXMax = 20.2f, StopZ = TerminalZ + 49f, PhoneZ = TerminalZ + 43.2f;

    // Where the pavement is closed off at each end, and the two gaps in the rail along the kerb.
    const float PavementSouth = GateZ - 5.6f, PavementNorth = StopZ + 8.6f, CrossingWidth = 3f;

    static readonly Vector2 GravelTile = new Vector2(3f, 3f), RoadTile = new Vector2(RoadXMax - RoadXMin, 28f), SlabTile = new Vector2(3f, 3f), KerbTile = new Vector2(2f, 2f);
    static readonly Color SodiumColor = new Color(1f, 0.66f, 0.34f), DawnColor = new Color(0.74f, 0.77f, 0.83f);

    // The wire is laid over whatever's behind it, like a stain: it only darkens, so the morning shows through.
    static MeshKit chainlink;

    // Rooms with the light on, seen through the glass.
    static MeshKit windowGlow;

    static void BuildTerminal(Transform lights)
    {
        chainlink = new MeshKit();
        windowGlow = new MeshKit();
        sceneryKits = new Dictionary<string, MeshKit>();
        fogGlows = new List<Vector3>();
        BuildCutting();
        BuildTerminalPlatform(lights);
        BuildHut(lights);
        BuildRoad(lights);
        BuildShelter();
        BuildPayphone();
        BuildDepotYard();
        BuildTown(new GameObject("Town").transform);
        CommitStains("Fence", "T_Chainlink", chainlink, lights);

        // The sky itself is a big overcast lamp, kept well away from the tunnels so that none of it gets underground.
        foreach ((Vector3 at, Vector2 size) in new[]
        {
            (new Vector3(14f, 34f, PortalZ + 40f), new Vector2(110f, 70f)),
            (new Vector3(14f, 34f, TerminalZ + 20f), new Vector2(110f, 70f)),
            (new Vector3(14f, 34f, TerminalZ + 90f), new Vector2(110f, 70f)),
        })
        {
            Light sky = BakedLight(lights, "SkyGlow", LightType.Rectangle, at, Quaternion.Euler(90f, 0f, 0f), DawnColor, 4.4f);
            sky.areaSize = size;
            sky.range = 100f;
            sky.bounceIntensity = 1.4f;
        }
    }

    // Out of the tube and up between concrete walls, with the ground coming down to meet the rails.
    static void BuildCutting()
    {
        var center = new Vector2(BoreX, BoreY);
        Rect portal = Rect.MinMaxRect(PortalXMin - 0.2f, TrackY, PortalXMax + 0.2f, PortalTop + 0.3f);
        WallAlongX(Kit("Concrete"), PortalZ, 1f, -14f, 11f, TrackY, 7f, new[] { portal }, PlasterTile);
        Kit("Concrete").Box(new Vector3(TrackX, PortalTop + 0.5f, PortalZ + 0.15f), new Vector3(PortalXMax - PortalXMin + 1.2f, 0.4f, 0.3f), PlasterTile);

        float zEnd = LineEnd + 12f, length = zEnd - PortalZ;
        Kit("Ballast").Panel(new Vector3(-30f, TrackY, PortalZ), Vector3.forward * length, Vector3.right * (EdgeX + 30f), BallastTile);
        WallAlongZ(Kit("TrackWall"), EdgeX, -1f, PortalZ, zEnd, TrackY, 0.01f, NoHoles, PlasterTile);
        Kit("Gravel").Panel(new Vector3(EdgeX, 0f, PortalZ), Vector3.forward * (length + 150f), Vector3.right * (FenceX - EdgeX + 0.1f), GravelTile);

        // The walls of the cutting, stepping down as the land falls away.
        for (int i = 0; i < 8; i++)
        {
            float z0 = PortalZ + i * 8f, height = Mathf.Lerp(6f, 0.6f, i / 7f);
            Kit("Concrete").Box(new Vector3(-1.6f, TrackY + height / 2f, z0 + 4f), new Vector3(0.5f, height, 8f), PlasterTile);
            Kit("Concrete").Box(new Vector3(EdgeX + 0.25f, height / 2f, z0 + 4f), new Vector3(0.5f, height - TrackY, 8f), PlasterTile);
        }
    }

    static void BuildTerminalPlatform(Transform lights)
    {
        float zEnd = TerminalZ + TerminalLength, middle = TerminalZ + TerminalLength / 2f;
        Kit("Concrete").Panel(new Vector3(EdgeX, 0.012f, TerminalZ), Vector3.forward * TerminalLength, Vector3.right * (FenceX - EdgeX), PaverTile);
        Kit("Stone").Box(new Vector3(EdgeX + 0.16f, -0.02f + 0.03f, middle), new Vector3(0.32f, 0.06f, TerminalLength), StoneTile);
        Kit("Yellow").Panel(new Vector3(EdgeX + 0.5f, 0.018f, TerminalZ), Vector3.forward * TerminalLength, Vector3.right * 0.14f, TileTile);

        // The tin roof, on a row of bent posts.
        for (float z = TerminalZ + 3f; z < zEnd; z += 7f)
        {
            Kit("Steelwork").Tube(new Vector3(8.2f, 0f, z), new Vector3(8.2f, CanopyY - 0.1f, z), 0.07f, 8, MetalTile);
            Kit("Steelwork").Box(Matrix4x4.TRS(new Vector3(6.6f, CanopyY - 0.02f, z), Quaternion.Euler(0f, 0f, -3f), Vector3.one), new Vector3(4.6f, 0.12f, 0.08f), MetalTile);
        }
        var ridge = new List<Vector3> { new Vector3(9.4f, CanopyY + 0.18f, TerminalZ), new Vector3(4.3f, CanopyY - 0.08f, TerminalZ) };
        Kit("Corrugated").Sweep(ridge, Vector3.forward * TerminalLength, new Vector2(0.6f, 0.6f));
        var under = new List<Vector3> { new Vector3(4.3f, CanopyY - 0.1f, TerminalZ), new Vector3(9.4f, CanopyY + 0.16f, TerminalZ) };
        Kit("Corrugated").Sweep(under, Vector3.forward * TerminalLength, new Vector2(0.6f, 0.6f));

        // Sodium lamps nobody's switched off yet, and the name of the place.
        foreach (float z in new[] { TerminalZ + 6.5f, TerminalZ + 20.5f, TerminalZ + 34.5f })
        {
            var lamp = new Vector3(6.4f, CanopyY - 0.36f, z);
            Kit("Steelwork").Tube(lamp + Vector3.up * 0.1f, lamp + Vector3.up * 0.34f, 0.012f, 5, MetalTile);
            Kit("Steelwork").RoundedBox(lamp + Vector3.up * 0.06f, new Vector3(0.22f, 0.08f, 0.5f), 0.03f, 2, MetalTile);
            amberGlow.Box(lamp, new Vector3(0.14f, 0.04f, 0.42f), Vector2.one);
            PointLight(lights, "PlatformLamp", lamp + Vector3.down * 0.2f, SodiumColor, 1.6f, 11f);
        }
        foreach (float z in new[] { TerminalZ + 12f, TerminalZ + 28f })
        {
            var sign = new Vector3(6.4f, 2.5f, z);
            Kit("T_SignDepot").Plate(sign, Vector3.back, Vector3.up, 1.6f, 0.46f, 0.02f);
            Kit("T_SignDepot").Plate(sign, Vector3.forward, Vector3.up, 1.6f, 0.46f, 0.02f);
            foreach (float x in new[] { -0.7f, 0.7f })
                Kit("Steelwork").Tube(sign + new Vector3(x, 0.23f, 0f), new Vector3(sign.x + x, CanopyY, z), 0.01f, 5, MetalTile);
        }

        // Benches nobody's ever waited on, and a chain-link fence along the back of the platform.
        foreach (float z in new[] { TerminalZ + 10f, TerminalZ + 24f })
        {
            Kit("Wood").Box(new Vector3(9.6f, 0.44f, z), new Vector3(0.42f, 0.05f, 2.6f), WoodTile);
            Kit("Wood").Box(new Vector3(9.84f, 0.74f, z), new Vector3(0.05f, 0.34f, 2.6f), WoodTile);
            foreach (float at in new[] { z - 1.1f, z + 1.1f })
                Kit("Steelwork").Box(new Vector3(9.66f, 0.22f, at), new Vector3(0.36f, 0.44f, 0.05f), MetalTile);
        }

        for (float z = PortalZ + 2f; z < GateZ - GateWidth / 2f; z += 3f) FencePost(FenceX, z);
        Fence(lights, FenceX, PortalZ + 2f, GateZ - GateWidth / 2f);
        Fence(lights, FenceX, GateZ + GateWidth / 2f, HutZ);
        Fence(lights, FenceX, HutZ + 5.2f, PavementNorth + 0.6f);
        BuildGate();

        // The buffers at the very end of the line.
        foreach (float x in new[] { TrackX - Gauge / 2f, TrackX + Gauge / 2f })
        {
            Kit("TrainRedBuffer").Box(new Vector3(x, TrackY + 0.5f, LineEnd), new Vector3(0.22f, 1f, 0.3f), MetalTile);
            Kit("TrainRedBuffer").Tube(new Vector3(x, TrackY + 0.72f, LineEnd - 0.5f), new Vector3(x, TrackY + 0.72f, LineEnd - 0.15f), 0.1f, 10, MetalTile);
        }
        Kit("TrainRedBuffer").Box(new Vector3(TrackX, TrackY + 0.72f, LineEnd), new Vector3(Gauge + 0.3f, 0.26f, 0.2f), MetalTile);
    }

    static void FencePost(float x, float z)
    {
        Kit("Steelwork").Tube(new Vector3(x, 0f, z), new Vector3(x, 2.2f, z), 0.03f, 6, MetalTile);
    }

    static void Fence(Transform lights, float x, float zStart, float zEnd)
    {
        var strip = new List<Vector3> { new Vector3(x, 0f, zStart), new Vector3(x, 2.15f, zStart) };
        chainlink.Sweep(strip, Vector3.forward * (zEnd - zStart), new Vector2(1.1f, 1.1f));
        var back = new List<Vector3> { new Vector3(x, 2.15f, zStart), new Vector3(x, 0f, zStart) };
        chainlink.Sweep(back, Vector3.forward * (zEnd - zStart), new Vector2(1.1f, 1.1f));
        Kit("Steelwork").Tube(new Vector3(x, 2.17f, zStart), new Vector3(x, 2.17f, zEnd), 0.02f, 6, MetalTile);
        Barrier("Fence", lights, new Vector3(x, 1f, (zStart + zEnd) / 2f), new Vector3(0.2f, 3f, zEnd - zStart));
    }

    // The gate out to the road, a frame of pipe with the wire stretched across it, folded back along the fence years ago.
    static void BuildGate()
    {
        float hinge = GateZ - GateWidth / 2f, end = hinge - GateWidth + 0.04f;
        const float x = FenceX - 0.14f, bottom = 0.06f, top = 2.12f;

        foreach (float z in new[] { GateZ - GateWidth / 2f, GateZ + GateWidth / 2f })
            Kit("Steelwork").Tube(new Vector3(FenceX, 0f, z), new Vector3(FenceX, 2.3f, z), 0.045f, 8, MetalTile);

        foreach ((Vector3 from, Vector3 to) in new[]
        {
            (new Vector3(x, bottom, hinge), new Vector3(x, bottom, end)), (new Vector3(x, top, hinge), new Vector3(x, top, end)),
            (new Vector3(x, bottom, hinge), new Vector3(x, top, hinge)), (new Vector3(x, bottom, end), new Vector3(x, top, end)),
            (new Vector3(x, bottom, hinge), new Vector3(x, top, end)),
        })
        {
            Kit("Steelwork").Tube(from, to, 0.02f, 6, MetalTile);
        }

        var wire = new List<Vector3> { new Vector3(x, bottom, end), new Vector3(x, top, end) };
        chainlink.Sweep(wire, Vector3.forward * (hinge - end), new Vector2(1.1f, 1.1f));
        var wireBack = new List<Vector3> { new Vector3(x, top, end), new Vector3(x, bottom, end) };
        chainlink.Sweep(wireBack, Vector3.forward * (hinge - end), new Vector2(1.1f, 1.1f));
    }

    // The foreman's hut, with the light on and the time clock by the door.
    static void BuildHut(Transform lights)
    {
        const float xMin = 7f, xMax = 10.4f, depth = 5.2f, top = 2.5f;
        float zMax = HutZ + depth;
        Rect[] window = { Rect.MinMaxRect(xMin + 1.5f, 1.05f, xMin + 2.4f, 1.85f) };
        WallAlongX(Kit("Brick"), HutZ, -1f, xMin, xMax, 0f, top, window, BrickTile, Kit("Concrete"), 0.2f);
        WallAlongX(Kit("Brick"), zMax, 1f, xMin, xMax, 0f, top, NoHoles, BrickTile);
        WallAlongZ(Kit("Brick"), xMin, -1f, HutZ, zMax, 0f, top, NoHoles, BrickTile);
        WallAlongZ(Kit("Brick"), xMax, 1f, HutZ, zMax, 0f, top, NoHoles, BrickTile);
        Kit("Corrugated").Panel(new Vector3(xMin - 0.2f, top + 0.05f, HutZ - 0.2f), Vector3.forward * (depth + 0.4f), Vector3.right * (xMax - xMin + 0.4f), new Vector2(0.6f, 0.6f));
        Kit("Corrugated").Panel(new Vector3(xMin - 0.2f, top, HutZ - 0.2f), Vector3.right * (xMax - xMin + 0.4f), Vector3.forward * (depth + 0.4f), new Vector2(0.6f, 0.6f));

        var pane = new Vector3(xMin + 1.95f, 1.45f, HutZ + 0.1f);
        windowGlow.Box(pane + Vector3.forward * 0.1f, new Vector3(0.9f, 0.8f, 0.02f), Vector2.one);
        PointLight(lights, "HutWindow", pane + Vector3.back * 0.5f, SodiumColor, 0.7f, 5f);

        var door = new Vector3(xMin + 0.8f, 1f, HutZ - 0.02f);
        Kit("Steelwork").Box(door, new Vector3(0.9f, 2f, 0.04f), MetalTile);

        // The clock itself, with a rack of cards beside it.
        var clock = new Vector3(ClockX, 1.32f, HutZ - 0.09f);
        Kit("Steelwork").RoundedBox(clock, new Vector3(0.34f, 0.46f, 0.18f), 0.02f, 2, MetalTile);
        Kit("T_TimeClock").Plate(clock + Vector3.back * 0.092f, Vector3.back, Vector3.up, 0.3f, 0.42f, 0.002f);
        Kit("Steelwork").Box(new Vector3(ClockX + 0.3f, 1.3f, HutZ - 0.02f), new Vector3(0.2f, 0.46f, 0.03f), MetalTile);
        for (int i = 0; i < 6; i++)
            Kit("Paper").Box(new Vector3(ClockX + 0.3f, 1.12f + i * 0.07f, HutZ - 0.04f), new Vector3(0.15f, 0.05f, 0.012f), PaperTile);

        // The downpipe, which came away at the bottom some years ago.
        Kit("Steelwork").Tube(new Vector3(xMax + 0.06f, top, zMax - 0.3f), new Vector3(xMax + 0.06f, 0.4f, zMax - 0.3f), 0.04f, 8, MetalTile);
    }

    const float ClockX = 9.82f;

    // A kerb, a long strip of patched road, a rail along the edge of it and the street lamps, still on.
    static void BuildRoad(Transform lights)
    {
        // Lit by the bake as far as anyone can see it, and painted from there on, where it's mostly fog anyway.
        const float south = TerminalZ - 60f, length = 230f, farSouth = TerminalZ - 240f, farLength = 660f;
        float width = RoadXMax - RoadXMin, pavement = RoadXMin - FenceX;
        void Road(MeshKit kit, float start, float along) => kit.Panel(new Vector3(RoadXMax, 0.02f, start), Vector3.left * width, Vector3.forward * along, RoadTile);
        void Slabs(MeshKit kit, float start, float along) => kit.Panel(new Vector3(FenceX, 0.03f, start), Vector3.forward * along, Vector3.right * pavement, SlabTile);

        Road(Kit("Road"), south, length);
        Slabs(Kit("Slabs"), south, length);
        foreach (float x in new[] { RoadXMin - 0.08f, RoadXMax + 0.08f })
            Kit("Kerb").Box(new Vector3(x, 0.07f, south + length / 2f), new Vector3(0.16f, 0.14f, length), KerbTile);

        foreach ((float start, float along) in new[] { (farSouth, south - farSouth), (south + length, farSouth + farLength - south - length) })
        {
            Road(Scenery("T_Road"), start, along);
            Slabs(Scenery("T_Slabs"), start, along);
            Scenery("T_Kerb").Panel(new Vector3(RoadXMin - 0.16f, 0.14f, start), Vector3.forward * along, Vector3.right * 0.16f, KerbTile);
        }

        for (float z = TerminalZ - 186f; z < TerminalZ + 340f; z += 32f)
        {
            var post = new Vector3(RoadXMin - 0.34f, 0f, z);
            Lamp(Kit("Steelwork"), post, Vector3.right);
            amberGlow.Box(post + new Vector3(1.86f, 6.72f, 0f), new Vector3(0.42f, 0.03f, 0.16f), Vector2.one);
            fogGlows.Add(post + new Vector3(1.86f, 6.68f, 0f));
            if (Mathf.Abs(z - GateZ) < 80f) PointLight(lights, "StreetLamp", post + new Vector3(1.86f, 6.4f, 0f), SodiumColor, 7f, 20f);
        }

        // A rail along the kerb, with a gap for the crossing.
        var gaps = new[] { (GateZ - CrossingWidth / 2f, GateZ + CrossingWidth / 2f) };
        float from = PavementSouth;
        foreach ((float gapStart, float gapEnd) in gaps)
        {
            KerbRail(from, gapStart);
            from = gapEnd;
        }
        KerbRail(from, PavementNorth);

        // The ends of the pavement, shut off.
        foreach (float z in new[] { PavementSouth, PavementNorth })
            Trestle(lights, z);
    }

    // A concrete post with a long arm, and a lantern at the end of it. Reach is which way it leans.
    static void Lamp(MeshKit kit, Vector3 post, Vector3 reach)
    {
        kit.Tube(post, post + Vector3.up * 3.2f, 0.085f, 8, MetalTile);
        kit.Tube(post + Vector3.up * 3.2f, post + Vector3.up * 6.2f, 0.06f, 8, MetalTile);

        Vector3 last = post + Vector3.up * 6.2f;
        for (int i = 1; i <= 6; i++)
        {
            float t = i / 6f;
            Vector3 next = post + Vector3.up * (6.2f + Mathf.Sin(t * Mathf.PI * 0.5f) * 0.66f) + reach * (t * t * 1.5f);
            kit.Tube(last, next, 0.04f, 6, MetalTile);
            last = next;
        }
        kit.RoundedBox(post + reach * 1.8f + Vector3.up * 6.8f, new Vector3(Mathf.Abs(reach.x) * 0.56f + Mathf.Abs(reach.z) * 0.22f, 0.11f, Mathf.Abs(reach.z) * 0.56f + Mathf.Abs(reach.x) * 0.22f), 0.04f, 2, MetalTile);
    }

    static void KerbRail(float zFrom, float zTo)
    {
        const float x = RoadXMin - 0.24f;
        for (float z = zFrom; z <= zTo + 0.01f; z += (zTo - zFrom) / Mathf.Max(Mathf.Round((zTo - zFrom) / 2.2f), 1f))
            Kit("Railing").Tube(new Vector3(x, 0f, z), new Vector3(x, 1f, z), 0.03f, 6, MetalTile);
        foreach (float y in new[] { 1f, 0.56f })
            Kit("Railing").Tube(new Vector3(x, y, zFrom), new Vector3(x, y, zTo), 0.024f, 6, MetalTile);
    }

    // Two trestles and a pair of striped boards across the pavement, and a notice.
    static void Trestle(Transform parent, float z)
    {
        float middle = (FenceX + RoadXMin) / 2f, width = RoadXMin - FenceX - 0.2f;
        foreach (float y in new[] { 0.5f, 0.92f })
        {
            Kit("T_Barricade").Plate(new Vector3(middle, y, z), Vector3.back, Vector3.up, width, 0.14f, 0.02f);
            Kit("T_Barricade").Plate(new Vector3(middle, y, z), Vector3.forward, Vector3.up, width, 0.14f, 0.02f);
        }
        foreach (float x in new[] { FenceX + 0.3f, RoadXMin - 0.3f })
        {
            foreach (float lean in new[] { -0.3f, 0.3f })
                Kit("Wood").Tube(new Vector3(x, 1.06f, z), new Vector3(x, 0f, z + lean), 0.024f, 5, WoodTile);
        }
        Kit("T_RoadWorks").Plate(new Vector3(middle, 1.28f, z), z < GateZ ? Vector3.forward : Vector3.back, Vector3.up, 0.74f, 0.37f, 0.012f);
        Barrier("PavementEnd", parent, new Vector3(middle, 1f, z), new Vector3(RoadXMin - FenceX + 0.4f, 3f, 0.2f));
    }

    // The stop: one sweep of poured concrete from the days when even a bus stop was a monument, with a mosaic along the back.
    static void BuildShelter()
    {
        const float back = FenceX + 0.42f, spread = 7f, foot = 0.04f;
        float south = StopZ - spread / 2f;
        Vector2 tile = new Vector2(2.4f, 2.4f);

        Vector2[] inner =
        {
            new Vector2(0f, foot), new Vector2(0f, 1.74f), new Vector2(0.03f, 2.02f), new Vector2(0.14f, 2.26f), new Vector2(0.36f, 2.44f),
            new Vector2(0.72f, 2.55f), new Vector2(1.24f, 2.59f), new Vector2(1.86f, 2.52f),
        };
        Vector2[] outer =
        {
            new Vector2(-0.12f, foot), new Vector2(-0.12f, 1.76f), new Vector2(-0.09f, 2.08f), new Vector2(0.05f, 2.36f), new Vector2(0.3f, 2.56f),
            new Vector2(0.7f, 2.68f), new Vector2(1.24f, 2.72f), new Vector2(1.88f, 2.62f),
        };
        Vector3 Point(Vector2 p, float z) => new Vector3(back + p.x, p.y, z);

        var under = new List<Vector3>();
        foreach (Vector2 p in inner) under.Add(Point(p, south));
        Kit("ShelterConcrete").Sweep(under, Vector3.forward * spread, tile, 50f);
        var over = new List<Vector3>();
        for (int i = outer.Length - 1; i >= 0; i--) over.Add(Point(outer[i], south));
        Kit("ShelterConcrete").Sweep(over, Vector3.forward * spread, tile, 50f);

        // The lip and the two ends of the shell.
        Kit("ShelterConcrete").QuadFacing(Point(inner[inner.Length - 1], south), Point(outer[outer.Length - 1], south), Point(outer[outer.Length - 1], south + spread), Point(inner[inner.Length - 1], south + spread), Vector3.right, Vector3.forward, Vector3.up, tile);
        foreach ((float z, Vector3 facing) in new[] { (south, Vector3.back), (south + spread, Vector3.forward) })
            for (int i = 0; i < inner.Length - 1; i++)
                Kit("ShelterConcrete").QuadFacing(Point(inner[i], z), Point(inner[i + 1], z), Point(outer[i + 1], z), Point(outer[i], z), facing, Vector3.right, Vector3.up, tile);

        // The mosaic, and the bench under it.
        Kit("T_Mosaic").Plate(new Vector3(back + 0.008f, 1.34f, StopZ), Vector3.right, Vector3.up, spread - 0.5f, 1.2f, 0.006f);
        Kit("ShelterConcrete").RoundedBox(new Vector3(back + 0.26f, 0.44f, StopZ), new Vector3(0.5f, 0.07f, 4.8f), 0.02f, 2, tile);
        foreach (float z in new[] { StopZ - 2f, StopZ, StopZ + 2f })
            Kit("ShelterConcrete").Box(new Vector3(back + 0.2f, 0.2f, z), new Vector3(0.36f, 0.42f, 0.1f), tile);
        for (int i = 0; i < 4; i++)
            Kit("Wood").RoundedBox(new Vector3(back + 0.08f + i * 0.125f, 0.488f, StopZ), new Vector3(0.1f, 0.03f, 4.7f), 0.008f, 1, WoodTile);

        // The sign at the edge of the kerb, the timetable, and a bin.
        var stop = new Vector3(RoadXMin - 0.5f, 0f, StopZ - 3.1f);
        Kit("Steelwork").Tube(stop, stop + Vector3.up * 2.7f, 0.03f, 6, MetalTile);
        Kit("T_BusStop").Disc(stop + new Vector3(0f, 2.6f, -0.02f), Vector3.back, Vector3.up, 0.24f, 20);
        Kit("T_BusStop").Disc(stop + new Vector3(0f, 2.6f, 0.02f), Vector3.forward, Vector3.up, 0.24f, 20);
        Kit("Steelwork").Box(stop + new Vector3(0f, 1.62f, 0.03f), new Vector3(0.34f, 0.5f, 0.03f), MetalTile);
        Kit("T_Timetable14").Plate(stop + new Vector3(0f, 1.62f, 0.046f), Vector3.forward, Vector3.up, 0.3f, 0.44f, 0.003f);

        Vector2[] bin = { new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.18f, 0.6f), new Vector2(0.19f, 0.64f), new Vector2(0.16f, 0.64f), new Vector2(0.15f, 0.5f), new Vector2(0f, 0.5f) };
        Kit("Steelwork").Lathe(new Vector3(RoadXMin - 0.7f, 0f, StopZ + 5.2f), bin, 12, MetalTile);
    }

    // The telephone on the side of the hut, under its hood. It has no business working out here.
    static void BuildPayphone()
    {
        const float wall = FenceX + 0.01f, bottom = 1.02f, top = 2.06f, deep = 0.46f, half = 0.36f;
        var arch = new List<Vector3>();
        for (int i = 0; i <= 14; i++)
        {
            float a = Mathf.PI * i / 14f;
            arch.Add(new Vector3(wall, bottom + Mathf.Sin(a) * (top - bottom), PhoneZ - Mathf.Cos(a) * half));
        }
        Kit("PhoneHood").Sweep(arch, Vector3.right * deep, MetalTile, 50f);
        var lining = new List<Vector3>(arch);
        lining.Reverse();
        Kit("PhoneLining").Sweep(lining, Vector3.right * deep, MetalTile, 50f);
        Kit("PhoneLining").Fan(arch, Vector3.right, Vector3.forward, Vector3.up, MetalTile);
        for (int i = 0; i < arch.Count - 1; i++)
            Kit("PhoneHood").Tube(arch[i] + Vector3.right * deep, arch[i + 1] + Vector3.right * deep, 0.012f, 6, MetalTile);

        // The set itself, and the handset on its hook.
        var body = new Vector3(wall + 0.07f, 1.5f, PhoneZ);
        Kit("Steelwork").RoundedBox(body, new Vector3(0.14f, 0.5f, 0.3f), 0.02f, 2, MetalTile);
        Kit("T_Payphone").Plate(body + Vector3.right * 0.071f, Vector3.right, Vector3.up, 0.26f, 0.42f, 0.002f);

        var handle = body + new Vector3(0.11f, 0.02f, -0.19f);
        Kit("Bakelite").RoundedBox(handle, new Vector3(0.05f, 0.22f, 0.05f), 0.018f, 2, MetalTile);
        foreach (float y in new[] { -0.12f, 0.12f })
            Kit("Bakelite").RoundedBox(handle + new Vector3(0.012f, y, 0f), new Vector3(0.07f, 0.06f, 0.06f), 0.024f, 2, MetalTile);

        Vector3 last = handle + Vector3.down * 0.15f, end = body + new Vector3(0.05f, -0.24f, -0.1f);
        for (int i = 1; i <= 30; i++)
        {
            float t = i / 30f;
            Vector3 next = Vector3.Lerp(handle + Vector3.down * 0.15f, end, t) + new Vector3(Mathf.Sin(t * 48f) * 0.007f, -Mathf.Sin(t * Mathf.PI) * 0.15f, Mathf.Cos(t * 48f) * 0.007f);
            Kit("Bakelite").Tube(last, next, 0.0028f, 4, MetalTile);
            last = next;
        }
    }

    const float PhoneX = FenceX + 0.44f;

    // Long sheds and rows of old cars over the track, seen through the morning.
    static void BuildDepotYard()
    {
        foreach ((float x, float z, float length) in new[] { (-20f, TerminalZ - 34f, 80f), (-44f, TerminalZ - 10f, 90f), (-20f, TerminalZ + 66f, 70f) })
        {
            const float width = 16f, wall = 6f, peak = 9f;
            float y = TrackY;
            WallAlongX(Kit("Shed"), z, -1f, x - width / 2f, x + width / 2f, y, y + wall, new[] { Rect.MinMaxRect(x - 5f, y, x + 5f, y + wall - 1f) }, BrickTile);
            WallAlongZ(Kit("Shed"), x + width / 2f, 1f, z, z + length, y, y + wall, NoHoles, BrickTile);
            var roof = new List<Vector3> { new Vector3(x + width / 2f, y + wall, z), new Vector3(x, y + peak, z), new Vector3(x - width / 2f, y + wall, z) };
            Kit("Corrugated").Sweep(roof, Vector3.forward * length, new Vector2(0.9f, 0.9f), 10f);
            Kit("Shed").Fan(roof, Vector3.back, Vector3.right, Vector3.up, BrickTile);
        }

        // The roads of the yard, running off into the morning.
        float yardStart = PortalZ + 50f, yardEnd = TerminalZ + 150f, yardLength = yardEnd - yardStart, yardMiddle = (yardStart + yardEnd) / 2f;
        foreach (float x in new[] { -6f, -11f, -16f, -21f })
        {
            foreach (float side in new[] { -1f, 1f })
                Kit("Rail").Box(new Vector3(x + side * Gauge / 2f, TrackY + 0.14f, yardMiddle), new Vector3(0.09f, 0.16f, yardLength), StoneTile);
            for (float z = yardStart; z < yardEnd; z += 1.6f)
                Kit("Sleeper").Box(new Vector3(x, TrackY + 0.04f, z), new Vector3(2.4f, 0.1f, 0.24f), WoodTile);
        }
    }
}
