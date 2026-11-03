using System.Collections.Generic;
using UnityEngine;
using static SceneForge;

// Severna, where Shift 1 pulls in and Shift 2 starts: nothing like Centralna's tiled tube. It was built when the line
// still had something to prove, so it has marble, a row of great square pillars, and brass lamps gone the colour of tea.
public static partial class StationBuilder
{
    const float SevernaWidth = 12.6f, PillarX = 7.6f, PillarSize = 0.86f, SevernaSpring = 3.05f, SevernaDado = 0.62f;
    const float SevernaExitZ = 19f, SevernaExitWidth = 2.6f, SevernaExitTop = 2.5f, ExitDoorSet = 0.34f;

    // How much further across the platform Severna's way in and staff door are than Centralna's.
    const float SevernaShift = 3.9f;
    const float SevernaStaffDoorX = 10.7f;

    static readonly ArchProfile NaveArch = new ArchProfile(0f, PillarX, SevernaSpring, 1.1f);
    static readonly ArchProfile AisleArch = new ArchProfile(PillarX, SevernaWidth, SevernaSpring, 0.8f);
    static readonly Vector2 MarbleTile = new Vector2(2.4f, 2.4f), TerrazzoTile = new Vector2(3.2f, 3.2f);
    static readonly Color GlobeColor = new Color(1f, 0.74f, 0.46f);

    static MeshKit amberGlow;

    // Old panes, laid over whatever's behind them like a stain, so that it still shows through.
    static MeshKit dirtyGlass;

    // Everything that's the same whether the train is pulling in or the shift is starting here.
    static void BuildSevernaHall(Transform lights, string sign, bool staffRoom)
    {
        MeshKit floor = Kit("Terrazzo"), marble = Kit("Marble"), dark = Kit("MarbleDark"), vault = Kit("Vault");
        float zLength = Length + 0.2f;
        dirtyGlass = new MeshKit();

        floor.Panel(new Vector3(EdgeX, 0f, -0.1f), Vector3.forward * zLength, Vector3.right * (SevernaWidth - EdgeX + 0.1f), TerrazzoTile);
        Kit("Stone").Box(new Vector3(EdgeX + 0.16f, -0.02f, Length / 2f), new Vector3(0.32f, 0.06f, Length), StoneTile);
        Kit("Yellow").Panel(new Vector3(EdgeX + 0.5f, 0.004f, 0f), Vector3.forward * Length, Vector3.right * 0.14f, TileTile);
        WallAlongZ(Kit("TrackWall"), EdgeX, -1f, -0.1f, Length + 0.1f, TrackY, 0.01f, NoHoles, PlasterTile);
        Kit("Ballast").Panel(new Vector3(-0.1f, TrackY, -0.1f), Vector3.forward * zLength, Vector3.right * (EdgeX + 0.2f), BallastTile);

        vault.Vault(NaveArch, -0.1f, Length + 0.1f, ArchSegments, PlasterTile);
        vault.Vault(AisleArch, -0.1f, Length + 0.1f, ArchSegments, PlasterTile);

        float exitMin = SevernaExitZ - SevernaExitWidth / 2f, exitMax = SevernaExitZ + SevernaExitWidth / 2f;
        Rect lowerExit = Rect.MinMaxRect(exitMin, 0f, exitMax, SevernaDado), upperExit = Rect.MinMaxRect(exitMin, SevernaDado, exitMax, SevernaExitTop);
        WallAlongZ(dark, SevernaWidth, -1f, -0.1f, Length + 0.1f, 0f, SevernaDado, new[] { lowerExit }, MarbleTile);
        WallAlongZ(marble, SevernaWidth, -1f, -0.1f, Length + 0.1f, SevernaDado, SevernaSpring, new[] { upperExit }, MarbleTile);
        WallAlongZ(dark, 0f, 1f, -0.1f, Length + 0.1f, TrackY, SevernaDado, NoHoles, MarbleTile);
        WallAlongZ(marble, 0f, 1f, -0.1f, Length + 0.1f, SevernaDado, SevernaSpring, NoHoles, MarbleTile);

        Rect portal = Rect.MinMaxRect(PortalXMin, TrackY, PortalXMax, PortalTop);
        Rect wayIn = Rect.MinMaxRect(DoorXMin + SevernaShift, 0f, DoorXMax + SevernaShift, DoorTop);
        Rect staffDoor = Rect.MinMaxRect(SevernaStaffDoorX - StaffDoorWidth / 2f, 0f, SevernaStaffDoorX + StaffDoorWidth / 2f, StaffDoorTop);
        WallAlongX(marble, 0f, 1f, -0.1f, SevernaWidth + 0.1f, TrackY, SevernaSpring, staffRoom ? new[] { portal, wayIn } : new[] { portal }, MarbleTile);
        WallAlongX(marble, Length, -1f, -0.1f, SevernaWidth + 0.1f, TrackY, SevernaSpring, staffRoom ? new[] { portal, staffDoor } : new[] { portal }, MarbleTile);
        foreach ((float z, Vector3 facing) in new[] { (0f, Vector3.forward), (Length, Vector3.back) })
        {
            Vector3 across = facing.z > 0f ? Vector3.right : Vector3.left;
            Kit("Plaster").ArchFill(NaveArch, z, SevernaSpring, facing, across, ArchSegments, PlasterTile);
            Kit("Plaster").ArchFill(AisleArch, z, SevernaSpring, facing, across, ArchSegments, PlasterTile);

            Kit("Brass").Box(new Vector3((PortalXMin + PortalXMax) / 2f, PortalTop + 0.08f, z + facing.z * 0.03f), new Vector3(PortalXMax - PortalXMin + 0.32f, 0.16f, 0.08f), MetalTile);
            foreach (float x in new[] { PortalXMin - 0.08f, PortalXMax + 0.08f })
                dark.Box(new Vector3(x, (TrackY + PortalTop) / 2f, z + facing.z * 0.03f), new Vector3(0.16f, PortalTop - TrackY, 0.08f), MarbleTile);
        }

        // The great square pillars, with the ribs of the roof springing from a beam along their heads.
        marble.Box(new Vector3(PillarX, SevernaSpring - 0.2f, Length / 2f), new Vector3(PillarSize + 0.1f, 0.5f, zLength), MarbleTile);
        foreach (float z in RibZ)
        {
            dark.RoundedBox(new Vector3(PillarX, SevernaDado / 2f, z), new Vector3(PillarSize + 0.14f, SevernaDado, PillarSize + 0.14f), 0.03f, 2, MarbleTile);
            marble.RoundedBox(new Vector3(PillarX, (SevernaDado + SevernaSpring - 0.4f) / 2f, z), new Vector3(PillarSize, SevernaSpring - 0.4f - SevernaDado, PillarSize), 0.05f, 2, MarbleTile);
            Kit("Brass").RoundedBox(new Vector3(PillarX, SevernaDado + 0.02f, z), new Vector3(PillarSize + 0.03f, 0.05f, PillarSize + 0.03f), 0.012f, 1, MetalTile);
            Kit("Brass").RoundedBox(new Vector3(PillarX, SevernaSpring - 0.42f, z), new Vector3(PillarSize + 0.1f, 0.05f, PillarSize + 0.1f), 0.012f, 1, MetalTile);

            vault.ArchBand(NaveArch, z - 0.3f, z + 0.3f, 0.15f, ArchSegments, PlasterTile);
            vault.ArchBand(AisleArch, z - 0.3f, z + 0.3f, 0.15f, ArchSegments, PlasterTile);
        }

        // A brass rail along each wall, and the name of the place in dark bronze on both sides of the track.
        foreach ((float x, float inward) in new[] { (0.01f, 1f), (SevernaWidth - 0.01f, -1f) })
        {
            Kit("Brass").Box(new Vector3(x, SevernaDado + 0.015f, Length / 2f), new Vector3(0.03f, 0.03f, Length), MetalTile);
            Kit("Brass").Box(new Vector3(x, 2.38f, Length / 2f), new Vector3(0.024f, 0.02f, Length), MetalTile);
            foreach (float z in new[] { 10.5f, 31.5f })
                Kit(sign).Plate(new Vector3(x + inward * 0.012f, 1.72f, z), Vector3.right * inward, Vector3.up, 3.4f, 0.64f, 0.03f);
        }

        GlobeLamps(lights, NaveArch, 5.5f, (3.5f, 2.6f), (10.5f, 2.2f), (17.5f, 0f), (24.5f, 2.6f), (31.5f, 2.2f), (38.5f, 2.4f));
        GlobeLamps(lights, AisleArch, 10.1f, (3.5f, 1.5f), (10.5f, 0f), (17.5f, 1.7f), (24.5f, 0f), (31.5f, 2.2f), (38.5f, 1.5f));

        BuildSevernaExit(lights);
        BuildSevernaBench(22.2f, 25.6f);
        BuildSevernaBench(29f, 32.4f);
        BuildSevernaBench(36f, 39.4f);
        BuildSevernaBench(2.5f, 5.9f + 0f);

        Vector2[] bin =
        {
            new Vector2(0f, 0f), new Vector2(0.17f, 0f), new Vector2(0.19f, 0.03f), new Vector2(0.21f, 0.62f),
            new Vector2(0.225f, 0.64f), new Vector2(0.225f, 0.67f), new Vector2(0.19f, 0.67f), new Vector2(0.17f, 0.56f), new Vector2(0f, 0.56f)
        };
        Kit("Metal").Lathe(new Vector3(SevernaWidth - 0.5f, 0f, 27.2f), bin, 14, MetalTile);
        Kit("Metal").Lathe(new Vector3(PillarX - 0.7f, 0f, 21f), bin, 14, MetalTile);

        var clock = new Vector3(5.5f, 2.62f, 14f);
        Kit("Metal").Tube(clock + Vector3.back * 0.05f, clock + Vector3.forward * 0.05f, 0.33f, 28, MetalTile);
        Kit("T_Clock").Disc(clock + Vector3.back * 0.052f, Vector3.back, Vector3.up, 0.32f, 28);
        Kit("T_Clock").Disc(clock + Vector3.forward * 0.052f, Vector3.forward, Vector3.up, 0.32f, 28);
        Kit("Brass").Tube(clock + Vector3.up * 0.33f, new Vector3(clock.x, NaveArch.HeightAt(clock.x) - 0.15f, clock.z), 0.02f, 6, MetalTile);
    }

    // Lamps on brass stems, five globes to a lamp. A power of nothing is a lamp that's gone out.
    static void GlobeLamps(Transform lights, ArchProfile arch, float x, params (float z, float power)[] lamps)
    {
        const float y = 2.46f, reach = 0.3f, radius = 0.1f;
        Vector2[] globe = new Vector2[9];
        for (int i = 0; i < globe.Length; i++)
        {
            float a = Mathf.Lerp(-90f, 90f, i / (globe.Length - 1f)) * Mathf.Deg2Rad;
            globe[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius + radius);
        }

        foreach ((float z, float power) in lamps)
        {
            var center = new Vector3(x, y, z);
            Kit("Brass").Tube(center + Vector3.up * 0.12f, new Vector3(x, arch.HeightAt(x), z), 0.016f, 6, MetalTile);
            Kit("Brass").Box(center + Vector3.up * 0.12f, new Vector3(reach * 2f, 0.018f, 0.018f), MetalTile);
            Kit("Brass").Box(center + Vector3.up * 0.12f, new Vector3(0.018f, 0.018f, reach * 2f), MetalTile);

            MeshKit globes = power > 0f ? amberGlow : Kit("DoorGlass");
            foreach (Vector3 offset in new[] { Vector3.zero, Vector3.right * reach, Vector3.left * reach, Vector3.forward * reach, Vector3.back * reach })
                globes.Lathe(center + offset + Vector3.down * (offset == Vector3.zero ? 0.12f : 0.06f), globe, 10, Vector2.one);

            if (power <= 0f) continue;
            Light lamp = PointLight(lights, "Globe", center + Vector3.down * 0.05f, GlobeColor, power, 12f);
            lamp.shapeRadius = 0.26f;
        }
    }

    static void BuildSevernaBench(float zStart, float zEnd)
    {
        float keep = backWall;
        backWall = SevernaWidth;
        BuildBench(zStart, zEnd);
        backWall = keep;
    }

    // The way up to the street, with the night at the top of the stairs.
    static void BuildSevernaExit(Transform lights)
    {
        float zMin = SevernaExitZ - SevernaExitWidth / 2f, zMax = SevernaExitZ + SevernaExitWidth / 2f;
        const float depth = 3.4f, top = SevernaExitTop + 0.1f;
        float xStart = SevernaWidth, xEnd = xStart + depth, width = zMax - zMin;

        Kit("Terrazzo").Panel(new Vector3(xStart - 0.02f, 0f, zMin), Vector3.forward * width, Vector3.right * (depth + 0.02f), TerrazzoTile);
        Kit("Plaster").Panel(new Vector3(xStart - 0.02f, top, zMin), Vector3.right * (depth + 3f), Vector3.forward * width, PlasterTile);
        foreach ((float z, float facing) in new[] { (zMin, 1f), (zMax, -1f) })
        {
            WallAlongX(Kit("MarbleDark"), z, facing, xStart - 0.02f, xEnd + 3f, 0f, SevernaDado, NoHoles, MarbleTile);
            WallAlongX(Kit("Marble"), z, facing, xStart - 0.02f, xEnd + 3f, SevernaDado, top, NoHoles, MarbleTile);
        }

        for (int i = 0; i < 12; i++)
        {
            float x = xEnd + i * 0.28f, y = i * 0.17f;
            Kit("Stone").Panel(new Vector3(x, y, zMin), Vector3.forward * width, Vector3.right * 0.28f, StoneTile);
            Kit("Stone").PanelFacing(new Vector3(x, y - 0.17f, zMin), Vector3.forward * width, Vector3.up * 0.17f, Vector3.left, StoneTile);
        }
        Kit("Plaster").PanelFacing(new Vector3(xEnd + 3.2f, 0f, zMin), Vector3.forward * width, Vector3.up * top, Vector3.left, PlasterTile);

        Kit("Brass").Box(new Vector3(SevernaWidth - 0.03f, SevernaExitTop + 0.04f, SevernaExitZ), new Vector3(0.06f, 0.08f, SevernaExitWidth + 0.16f), MetalTile);
        foreach (float z in new[] { zMin - 0.05f, zMax + 0.05f })
            Kit("MarbleDark").Box(new Vector3(SevernaWidth - 0.03f, SevernaExitTop / 2f, z), new Vector3(0.06f, SevernaExitTop, 0.1f), MarbleTile);

        Kit("T_SignExit").Plate(new Vector3(SevernaWidth - 0.012f, 2.74f, SevernaExitZ), Vector3.left, Vector3.up, 1.1f, 0.32f, 0.02f);

        // A little of the street gets down the stairs.
        PointLight(lights, "StreetGlow", new Vector3(xEnd + 2.5f, 2.1f, SevernaExitZ), new Color(0.56f, 0.64f, 0.84f), 0.5f, 7f);
        PointLight(lights, "ExitLamp", new Vector3(SevernaWidth - 0.35f, 2.3f, SevernaExitZ), GlobeColor, 1.3f, 4.5f);
        BuildExitDoors();
    }

    // The way up is shut for the night: three tall doors in a bronze screen, with a chain through the push bars.
    static void BuildExitDoors()
    {
        const int leaves = 3;
        const float x = SevernaWidth + ExitDoorSet, post = 0.07f, doorTop = 2.12f, top = SevernaExitTop + 0.1f, frame = 0.1f, stile = 0.11f;
        float zMin = SevernaExitZ - SevernaExitWidth / 2f, width = SevernaExitWidth, leaf = (width - post * (leaves + 1)) / leaves;
        MeshKit bronze = Kit("Bronze"), oak = Kit("DoorOak"), brass = Kit("Brass");

        for (int i = 0; i <= leaves; i++)
            bronze.Box(new Vector3(x, top / 2f, zMin + post / 2f + i * (leaf + post)), new Vector3(frame, top, post), MetalTile);
        bronze.Box(new Vector3(x, doorTop + 0.04f, SevernaExitZ), new Vector3(frame, 0.08f, width), MetalTile);
        bronze.Box(new Vector3(x, top - 0.03f, SevernaExitZ), new Vector3(frame, 0.06f, width), MetalTile);
        bronze.Box(new Vector3(x, 0.012f, SevernaExitZ), new Vector3(frame + 0.04f, 0.024f, width), MetalTile);

        var bars = new Vector3[leaves];
        for (int i = 0; i < leaves; i++)
        {
            float z = zMin + post + i * (leaf + post) + leaf / 2f, pane = leaf - stile * 2f;

            foreach (float side in new[] { -1f, 1f })
                oak.Box(new Vector3(x, doorTop / 2f, z + side * (leaf - stile) / 2f), new Vector3(0.06f, doorTop, stile), WoodTile);
            oak.Box(new Vector3(x, 0.19f, z), new Vector3(0.06f, 0.38f, pane), WoodTile);
            oak.Box(new Vector3(x, 1.04f, z), new Vector3(0.06f, 0.1f, pane), WoodTile);
            oak.Box(new Vector3(x, doorTop - 0.06f, z), new Vector3(0.06f, 0.12f, pane), WoodTile);

            dirtyGlass.Plate(new Vector3(x, 0.71f, z), Vector3.left, Vector3.up, pane, 0.56f, 0.002f);
            dirtyGlass.Plate(new Vector3(x, 1.55f, z), Vector3.left, Vector3.up, pane, 0.92f, 0.002f);
            dirtyGlass.Plate(new Vector3(x, (doorTop + top) / 2f + 0.01f, z), Vector3.left, Vector3.up, leaf, top - doorTop - 0.14f, 0.002f);

            // Brass where hands and feet go, and it has gone dull everywhere but there.
            brass.RoundedBox(new Vector3(x - 0.034f, 0.17f, z), new Vector3(0.008f, 0.26f, leaf - 0.1f), 0.003f, 1, MetalTile);
            bars[i] = new Vector3(x - 0.09f, 1.06f, z);
            brass.Tube(bars[i] + Vector3.back * (leaf / 2f - 0.1f), bars[i] + Vector3.forward * (leaf / 2f - 0.1f), 0.016f, 8, MetalTile);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 foot = bars[i] + Vector3.forward * side * (leaf / 2f - 0.14f);
                brass.Tube(foot, foot + Vector3.right * 0.06f, 0.012f, 6, MetalTile);
            }
        }

        // The chain, round the two bars in the middle, and the padlock hanging off it.
        Vector3 from = bars[0] + Vector3.forward * (leaf / 2f - 0.24f) + Vector3.left * 0.02f, to = bars[1] + Vector3.back * (leaf / 2f - 0.24f) + Vector3.left * 0.02f;
        Chain(Kit("Steelwork"), from, to, 0.2f, 15);
        var lockAt = (from + to) / 2f + new Vector3(0f, -0.28f, 0f);
        Kit("Steelwork").Tube(lockAt + new Vector3(0f, 0.03f, -0.018f), lockAt + new Vector3(0f, 0.075f, -0.012f), 0.005f, 5, MetalTile);
        Kit("Steelwork").Tube(lockAt + new Vector3(0f, 0.03f, 0.018f), lockAt + new Vector3(0f, 0.075f, 0.012f), 0.005f, 5, MetalTile);
        brass.RoundedBox(lockAt, new Vector3(0.022f, 0.06f, 0.05f), 0.008f, 2, MetalTile);
        Kit("T_NoticeShut").Plate(new Vector3(x - 0.055f, 1.5f, SevernaExitZ + 0.44f), Vector3.left, Vector3.up, 0.34f, 0.17f, 0.003f);
    }

    // Links turned a quarter to each other, hanging between two points.
    static void Chain(MeshKit kit, Vector3 from, Vector3 to, float sag, int links)
    {
        Vector3 At(float t) => Vector3.Lerp(from, to, t) + Vector3.down * (4f * sag * t * (1f - t));
        for (int i = 0; i < links; i++)
        {
            Vector3 a = At((float)i / links), b = At((i + 1f) / links), middle = (a + b) / 2f, along = (b - a).normalized;
            Vector3 across = Vector3.Cross(along, Vector3.left).normalized;
            if (i % 2 == 1) across = Vector3.Cross(along, across).normalized;

            const int steps = 8;
            float half = Vector3.Distance(a, b) * 0.72f, wide = half * 0.5f;
            Vector3 Link(int step)
            {
                float angle = step * Mathf.PI * 2f / steps;
                return middle + along * Mathf.Cos(angle) * half + across * Mathf.Sin(angle) * wide;
            }
            for (int s = 0; s < steps; s++) kit.Tube(Link(s), Link(s + 1), 0.0035f, 4, MetalTile);
        }
    }

    // Grime that follows Severna's walls instead of Centralna's.
    static void StainSeverna(MeshKit rising, MeshKit streaks, MeshKit blotches)
    {
        Stain(rising, new Vector3(SevernaWidth, 0f, 0f), Vector3.forward * (SevernaExitZ - SevernaExitWidth / 2f), Vector3.up * 0.5f, Vector3.left, 4f);
        Stain(rising, new Vector3(SevernaWidth, 0f, SevernaExitZ + SevernaExitWidth / 2f), Vector3.forward * (Length - SevernaExitZ - SevernaExitWidth / 2f), Vector3.up * 0.5f, Vector3.left, 4f);
        Stain(rising, new Vector3(SevernaWidth, 0f, 0f), Vector3.forward * Length, Vector3.left * 1.5f, Vector3.up, 8f);
        Stain(rising, new Vector3(EdgeX, 0f, 0f), Vector3.right * (SevernaWidth - EdgeX), Vector3.forward * 1.4f, Vector3.up, 3f);
        Stain(rising, new Vector3(EdgeX, 0f, Length), Vector3.right * (SevernaWidth - EdgeX), Vector3.back * 1.4f, Vector3.up, 3f);

        Stain(streaks, new Vector3(SevernaWidth, 1f, 0f), Vector3.forward * (SevernaExitZ - SevernaExitWidth / 2f), Vector3.up * 2f, Vector3.left, 5f);
        Stain(streaks, new Vector3(SevernaWidth, 1f, SevernaExitZ + SevernaExitWidth / 2f), Vector3.forward * (Length - SevernaExitZ - SevernaExitWidth / 2f), Vector3.up * 2f, Vector3.left, 5f);
        Stain(streaks, new Vector3(0f, 0.7f, 0f), Vector3.forward * Length, Vector3.up * 2.2f, Vector3.right, 8f);

        foreach (float z in new[] { 3.5f, 10.5f, 24.5f, 31.5f, 38.5f })
            VaultStain(blotches, NaveArch, 4.2f, 6.8f, z - 1.6f, z + 1.6f);
        Stain(blotches, new Vector3(4.6f, 0f, 21f), Vector3.right * 2.6f, Vector3.forward * 4f, Vector3.up, 1f);
        Stain(blotches, new Vector3(4.8f, 0f, 27f), Vector3.right * 2.4f, Vector3.forward * 4f, Vector3.up, 1f);
        Stain(blotches, new Vector3(9f, 0f, 16f), Vector3.right * 3f, Vector3.forward * 6f, Vector3.up, 1f);
    }

    static void ProbeGrid(List<Vector3> positions, float[] xs, float[] ys, float zStart, float zEnd, float step)
    {
        foreach (float y in ys)
            foreach (float x in xs)
                for (float z = zStart; z <= zEnd; z += step)
                    positions.Add(new Vector3(x, y, z));
    }
}
