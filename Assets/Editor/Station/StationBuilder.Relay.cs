using System.Collections.Generic;
using UnityEngine;
using static SceneForge;

// The end of the passage at Zavodska: the relay cabinet for signal 14. A steel case with its rack of relays behind the
// glass, and a slate panel with the release on it, a knife switch somebody has kept greased for thirty years.
public static partial class StationBuilder
{
    const float RelayWidth = 1.26f, RelayFoot = 0.1f, RelayTop = 2.12f, RelayDepth = 0.36f, RelaySkin = 0.03f;
    const float SlateBottom = 0.7f, SlateTop = 1.5f, SlateWidth = 1.02f, RackBottom = 1.58f, RackTop = 1.98f;
    // Looking at the cabinet the passage runs away behind you, so positive across is on your left.
    const float SwitchAcross = -0.25f, SwitchHinge = 0.88f, BladeLength = 0.42f, SwitchPole = 0.07f;
    const float VoltDialAcross = 0.28f, VoltDialY = 1.2f, RelayLampY = 0.9f, RelayLampApart = 0.08f;

    static readonly Vector2 CaseTile = new Vector2(1.3f, 1.3f);

    static float RelayX => (DoorXMin + DoorXMax) / 2f;
    static float RelayFront => RelayPassageEnd + RelayDepth;

    static void BuildRelayCabinet(MeshKit redLamp, MeshKit clearLamp)
    {
        BuildRelayCase();
        BuildRelayRack();
        BuildRelaySlate(redLamp, clearLamp);
        BuildRelayCables();

        // The rubber mat that's meant to save your life, gone hard with age.
        Kit("Rubber").RoundedBox(new Vector3(RelayX, 0.009f, RelayFront + 0.46f), new Vector3(1.04f, 0.018f, 0.78f), 0.006f, 1, MetalTile);
    }

    static void BuildRelayCase()
    {
        float x = RelayX, back = RelayPassageEnd, front = RelayFront, height = RelayTop - RelayFoot;
        float middleY = (RelayFoot + RelayTop) / 2f, middleZ = (back + front) / 2f;
        MeshKit shell = Kit("RelayCase");

        foreach (float side in new[] { -1f, 1f })
            shell.RoundedBox(new Vector3(x + side * (RelayWidth - RelaySkin) / 2f, middleY, middleZ), new Vector3(RelaySkin, height, RelayDepth), 0.008f, 2, CaseTile);
        shell.RoundedBox(new Vector3(x, RelayTop - RelaySkin / 2f, middleZ), new Vector3(RelayWidth, RelaySkin, RelayDepth), 0.008f, 2, CaseTile);
        shell.RoundedBox(new Vector3(x, RelayFoot + RelaySkin / 2f, middleZ), new Vector3(RelayWidth, RelaySkin, RelayDepth), 0.008f, 2, CaseTile);
        Kit("RelayInner").Box(new Vector3(x, middleY, back + 0.012f), new Vector3(RelayWidth - RelaySkin, height - RelaySkin, 0.024f), MetalTile);
        Kit("Steelwork").Box(new Vector3(x, RelayFoot / 2f, middleZ), new Vector3(RelayWidth - 0.08f, RelayFoot, RelayDepth - 0.06f), MetalTile);

        // What's across the front: a band over the rack, a rail under it, stiles either side of the slate, and the door below.
        void Band(float yMin, float yMax, float width, float across) =>
            shell.RoundedBox(new Vector3(x + across, (yMin + yMax) / 2f, front - 0.014f), new Vector3(width, yMax - yMin, 0.028f), 0.007f, 1, CaseTile);

        float stile = (RelayWidth - SlateWidth) / 2f;
        Band(RackTop, RelayTop, RelayWidth, 0f);
        Band(SlateTop, RackBottom, RelayWidth, 0f);
        Band(RelayFoot, SlateBottom, RelayWidth, 0f);
        foreach (float side in new[] { -1f, 1f })
        {
            Band(SlateBottom, SlateTop, stile, side * (SlateWidth + stile) / 2f);
            Band(RackBottom, RackTop, 0.05f, side * (RelayWidth - 0.05f) / 2f);
        }

        // The lower door: louvres to let the heat out, three hinges, and a hasp with no padlock in it.
        for (int i = 0; i < 10; i++)
        {
            float y = 0.2f + i * 0.04f;
            shell.Box(Matrix4x4.TRS(new Vector3(x - 0.08f, y, front + 0.006f), Quaternion.Euler(40f, 0f, 0f), Vector3.one), new Vector3(0.74f, 0.042f, 0.005f), CaseTile);
        }
        foreach (float y in new[] { 0.2f, 0.62f })
            Kit("Steelwork").Tube(new Vector3(x - RelayWidth / 2f + 0.012f, y - 0.03f, front + 0.012f), new Vector3(x - RelayWidth / 2f + 0.012f, y + 0.03f, front + 0.012f), 0.012f, 8, MetalTile);

        var hasp = new Vector3(x + 0.5f, 0.42f, front + 0.006f);
        Kit("Steelwork").RoundedBox(hasp, new Vector3(0.05f, 0.11f, 0.012f), 0.004f, 1, MetalTile);
        Kit("Steelwork").Tube(hasp + Vector3.down * 0.03f, hasp + new Vector3(0f, -0.03f, 0.05f), 0.008f, 6, MetalTile);
        Kit("T_RelayWarning").Plate(new Vector3(x + 0.5f, 0.6f, front), Vector3.forward, Vector3.up, 0.15f, 0.15f, 0.002f);

        // The enamel plate that says what all this is.
        Kit("T_RelayPlate").Plate(new Vector3(x, (RackTop + RelayTop) / 2f + 0.003f, front), Vector3.forward, Vector3.up, 0.66f, 0.1f, 0.003f);
        foreach (float side in new[] { -0.34f, 0.34f })
            Kit("Brass").Tube(new Vector3(x + side, (RackTop + RelayTop) / 2f + 0.003f, front - 0.002f), new Vector3(x + side, (RackTop + RelayTop) / 2f + 0.003f, front + 0.005f), 0.007f, 6, MetalTile);
    }

    // Two shelves of relays. Nothing in them has moved since the station closed, except the one on the circuit that matters.
    static void BuildRelayRack()
    {
        const int columns = 8;
        float x = RelayX, front = RelayFront, cell = (RelayWidth - 0.16f) / columns;

        foreach (float y in new[] { RackBottom, RackBottom + 0.2f })
        {
            Kit("RelayInner").Box(new Vector3(x, y - 0.004f, front - 0.14f), new Vector3(RelayWidth - 0.1f, 0.012f, 0.28f), MetalTile);
            for (int column = 0; column < columns; column++)
            {
                var unit = new Vector3(x + (column - (columns - 1) / 2f) * cell, y + 0.092f, front - 0.15f);
                Kit("Bakelite").RoundedBox(unit, new Vector3(cell - 0.016f, 0.17f, 0.15f), 0.008f, 2, MetalTile);
                Kit("T_RelayUnit").Plate(unit + Vector3.forward * 0.075f, Vector3.forward, Vector3.up, cell - 0.03f, 0.158f, 0.002f);
            }
        }

        // Thirty years of dust on the glass in front of them.
        dirtyGlass.Plate(new Vector3(x, (RackBottom + RackTop) / 2f, front - 0.018f), Vector3.forward, Vector3.up, RelayWidth - 0.1f, RackTop - RackBottom, 0.002f);
    }

    static void BuildRelaySlate(MeshKit redLamp, MeshKit clearLamp)
    {
        float x = RelayX, front = RelayFront, face = front - 0.01f;
        Kit("T_RelaySlate").Plate(new Vector3(x, (SlateBottom + SlateTop) / 2f, front - 0.03f), Vector3.forward, Vector3.up, SlateWidth, SlateTop - SlateBottom, 0.02f);

        // Line voltage, which still reads something.
        var dial = new Vector3(x + VoltDialAcross, VoltDialY, face);
        Kit("Brass").Tube(dial, dial + Vector3.forward * 0.02f, 0.098f, 28, MetalTile);
        Kit("Brass").Annulus(dial + Vector3.forward * 0.02f, Vector3.forward, Vector3.up, 0.086f, 0.098f, 28, MetalTile);
        Kit("T_GaugeRelay").Disc(dial + Vector3.forward * 0.012f, Vector3.forward, Vector3.up, 0.088f, 32);

        // Two lamps in brass rings. The red one has been lit since the day the station shut.
        foreach ((float side, MeshKit lamp) in new[] { (1f, redLamp), (-1f, clearLamp) })
        {
            var at = new Vector3(x + VoltDialAcross + side * RelayLampApart, RelayLampY, face);
            Kit("Brass").Tube(at, at + Vector3.forward * 0.014f, 0.036f, 16, MetalTile);
            Kit("Brass").Annulus(at + Vector3.forward * 0.014f, Vector3.forward, Vector3.up, 0.027f, 0.036f, 16, MetalTile);
            Kit("Bakelite").Disc(at + Vector3.forward * 0.009f, Vector3.forward, Vector3.up, 0.028f, 14);
            lamp.Disc(at + Vector3.forward * 0.012f, Vector3.forward, Vector3.up, 0.027f, 14);
        }

        // The switch's two poles: porcelain blocks, brass jaws at the top and a hinge at the bottom, copper bars going off behind the panel.
        foreach (float side in new[] { -SwitchPole, SwitchPole })
        {
            float poleX = x + SwitchAcross + side;
            foreach (float y in new[] { SwitchHinge, SwitchHinge + BladeLength })
            {
                Kit("Ceramic").RoundedBox(new Vector3(poleX, y, face + 0.02f), new Vector3(0.06f, 0.09f, 0.04f), 0.012f, 2, MetalTile);
                foreach (float stud in new[] { -0.03f, 0.03f })
                    Kit("Brass").Tube(new Vector3(poleX, y + stud, face + 0.04f), new Vector3(poleX, y + stud, face + 0.046f), 0.01f, 6, MetalTile);
            }

            foreach (float jaw in new[] { -0.012f, 0.012f })
                Kit("Copper").Box(new Vector3(poleX + jaw, SwitchHinge + BladeLength, face + 0.058f), new Vector3(0.005f, 0.06f, 0.04f), MetalTile);
            foreach (float cheek in new[] { -0.012f, 0.012f })
                Kit("Copper").Box(new Vector3(poleX + cheek, SwitchHinge, face + 0.056f), new Vector3(0.005f, 0.05f, 0.036f), MetalTile);

            Kit("Copper").Box(new Vector3(poleX, (SwitchHinge + BladeLength + SlateTop) / 2f, face + 0.006f), new Vector3(0.022f, SlateTop - SwitchHinge - BladeLength, 0.005f), MetalTile);
            Kit("Copper").Box(new Vector3(poleX, (SlateBottom + SwitchHinge) / 2f, face + 0.006f), new Vector3(0.022f, SwitchHinge - SlateBottom, 0.005f), MetalTile);
        }

        // A row of terminals along the top of the slate, each with its wire going off.
        for (int i = 0; i < 6; i++)
        {
            var stud = new Vector3(x - 0.44f + i * 0.09f, SlateTop - 0.06f, face);
            Kit("Brass").Tube(stud, stud + Vector3.forward * 0.018f, 0.012f, 6, MetalTile);
            Kit("TunnelCable").Tube(stud + Vector3.forward * 0.012f, new Vector3(stud.x + 0.01f, SlateTop + 0.004f, face + 0.012f), 0.004f, 5, MetalTile);
        }
    }

    // Fat lead-sheathed cable out of the top of the case and away along the ceiling, on brackets.
    static void BuildRelayCables()
    {
        float x = RelayX, top = PassageTop - 0.09f, start = RelayPassageEnd + 0.14f;

        void Run(float cableX, float radius, float y, string kit)
        {
            var bend = new List<Vector3> { new Vector3(cableX, RelayTop - 0.02f, start) };
            for (int i = 1; i <= 5; i++)
            {
                float a = i / 5f * Mathf.PI / 2f, reach = y - RelayTop;
                bend.Add(new Vector3(cableX, RelayTop + Mathf.Sin(a) * reach, start + (1f - Mathf.Cos(a)) * reach));
            }
            bend.Add(new Vector3(cableX, y, 0.02f));
            for (int i = 0; i < bend.Count - 1; i++) Kit(kit).Tube(bend[i], bend[i + 1], radius, 8, MetalTile);
        }

        Run(x - 0.3f, 0.022f, top, "TunnelCable");
        Run(x - 0.2f, 0.018f, top - 0.012f, "TunnelCable");
        Run(x - 0.11f, 0.018f, top - 0.008f, "TunnelCable");
        Run(x + 0.24f, 0.014f, top + 0.012f, "Steelwork");

        for (float z = RelayPassageEnd + 1.2f; z < -0.4f; z += 2.2f)
        {
            Kit("Steelwork").Box(new Vector3(x - 0.04f, top - 0.034f, z), new Vector3(0.74f, 0.012f, 0.03f), MetalTile);
            foreach (float across in new[] { -0.4f, 0.32f })
                Kit("Steelwork").Box(new Vector3(x + across, top + 0.036f, z), new Vector3(0.012f, 0.13f, 0.03f), MetalTile);
        }

        // A junction box part way along, with its lid hanging by one screw.
        var box = new Vector3(DoorXMin + 0.07f, 1.86f, RelayPassageEnd + 3.4f);
        Kit("RelayCase").RoundedBox(box, new Vector3(0.14f, 0.34f, 0.3f), 0.012f, 2, CaseTile);
        Kit("RelayCase").RoundedBox(Matrix4x4.TRS(box + new Vector3(0.08f, -0.02f, 0f), Quaternion.Euler(9f, 0f, 0f), Vector3.one), new Vector3(0.012f, 0.32f, 0.28f), 0.004f, 1, CaseTile);
        Kit("TunnelCable").Tube(box + new Vector3(-0.02f, 0.17f, -0.05f), new Vector3(box.x - 0.02f, top, box.z - 0.05f), 0.01f, 6, MetalTile);
    }
}
