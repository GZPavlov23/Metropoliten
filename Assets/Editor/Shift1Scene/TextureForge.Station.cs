using UnityEngine;

// The station's textures: tiles, track, the train and everything pinned to the walls.
public static partial class TextureForge
{
    // The train's walls repeat this texture every so many metres up, so its V is world height
    // and the paint bands line up all the way round the body.
    public const float TrainPaintHeight = 3.04f;

    // The console is 512 by 192. The cab builder puts the speedometer's real needle over this point.
    public const float ConsoleWidth = 512f, ConsoleHeight = 192f, SpeedDialX = 250f, SpeedDialY = 112f;

    // How many distance boards the run has, a hundred metres apart.
    public const int RunMarkers = 5;

    public static void ForgeStation()
    {
        ForgeShared();
        Write("T_Tile", Tiles(512, 8, Rgb(196, 192, 172), Rgb(96, 92, 84), 0.05f, 31));
        Write("T_TileBand", Tiles(256, 4, Rgb(172, 134, 62), Rgb(82, 66, 44), 0.08f, 33));
        Write("T_TileBandNext", Tiles(256, 4, Rgb(74, 112, 104), Rgb(44, 58, 54), 0.08f, 35));
        Write("T_Ballast", Ballast());
        Write("T_Tubing", Tubing());
        Write("T_ControllerPlate", ControllerPlate());
        Write("T_YellowLine", Painted(128, Rgb(188, 150, 44), 0.5f, 6, 0.3f));
        Write("T_TrainSide", TrainSide());
        Write("T_TrainLining", TrainLining());
        Write("T_RubberFloor", Painted(128, Rgb(70, 66, 60), 0.35f, 3, 0.1f));
        Write("T_CabConsole", CabConsole());

        Write("T_SignStation", Banner("ЦЕНТРАЛНА", 1024, 192, Rgb(206, 202, 186), Rgb(24, 24, 24), 118));
        Write("T_SignNext", Banner("СЕВЕРНА", 1024, 192, Rgb(206, 202, 186), Rgb(24, 24, 24), 118));
        Write("T_SignStop", Banner("СТОП", 256, 128, Rgb(150, 36, 28), Rgb(232, 226, 208), 62));
        for (int i = 1; i <= RunMarkers; i++) Write($"T_Mark{i}", Banner((i * 100).ToString(), 128, 64, Rgb(214, 210, 192), Rgb(20, 20, 20), 40));
        Write("T_SignExit", Banner("ИЗХОД", 512, 150, Rgb(36, 52, 84), Rgb(226, 220, 200), 84));
        Write("T_SignDuty", Banner("ДЕЖУРЕН", 512, 110, Rgb(36, 52, 84), Rgb(226, 220, 200), 58));
        Write("T_SignStaff", Banner("САМО ЗА ПЕРСОНАЛА", 512, 96, Rgb(150, 36, 28), Rgb(232, 226, 208), 36));
        Write("T_SignTrain", Banner("СЛУЖЕБЕН", 256, 64, Rgb(20, 20, 18), Rgb(214, 170, 80), 34));

        Write("T_MetroMapStation", MetroMap(true));
        Write("T_PosterDrivers", PosterDrivers());
        Write("T_PosterLine", PosterLine());
        Write("T_Intercom", IntercomFace());
        Write("T_KeyBoard", KeyBoard());
        Write("T_LogBook", LogBook());
        ImportWritten();
    }

    // Square tiles on a straight grid, a few gone darker than their neighbours.
    static Canvas Tiles(int size, int count, Color glaze, Color grout, float variance, int seed)
    {
        var canvas = new Canvas(size, size, grout);
        float cell = (float)size / count, toPhoto = 512f / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int column = Mathf.FloorToInt(x / cell), row = Mathf.FloorToInt(y / cell);
                float fu = x / cell - column, fv = y / cell - row;
                float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv)) * cell;
                float face = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 2.2f, edge));

                float shade = 1f + (Hash(column, row, seed) - 0.5f) * 2f * variance;
                float stained = Hash(column, row, seed + 1) > 0.92f ? 0.8f : 1f;
                Color tile = glaze * shade * stained * Detail(x * toPhoto, y * toPhoto, 0.2f);
                canvas[x, y] = Opaque(Color.Lerp(grout * Detail(x * toPhoto, y * toPhoto, 0.4f), tile, face));
            }
        }
        return canvas;
    }

    static Canvas Ballast()
    {
        var canvas = new Canvas(256, 256, Color.black);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float u = x / 256f, v = y / 256f;
                float stones = Mathf.Clamp01((Fractal(u, v, 22, 22, 3, 91) - 0.36f) * 3.4f);
                float oil = Mathf.Lerp(0.6f, 1.1f, Fractal(u, v, 3, 3, 3, 92));
                canvas[x, y] = Opaque(Color.Lerp(Rgb(30, 28, 26), Rgb(98, 92, 84), stones) * oil);
            }
        }
        return canvas;
    }

    static Canvas TrainSide()
    {
        const int width = 512, height = 256;
        Color lower = Rgb(60, 78, 94), cream = Rgb(176, 170, 146), stripe = Rgb(138, 44, 34), roof = Rgb(84, 86, 84), rust = Rgb(62, 44, 32);
        var canvas = new Canvas(width, height, lower);
        for (int y = 0; y < height; y++)
        {
            // Anything above the roofline is really the skirt below the floor, wrapped round.
            float metres = (float)y / height * TrainPaintHeight;
            if (metres > 2.42f) metres -= TrainPaintHeight;
            Color paint = metres < 0.66f ? lower : metres < 0.76f ? stripe : metres < 1.94f ? cream : metres < 2.24f ? lower : roof;

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float streaks = 1f - Mathf.InverseLerp(0.52f, 0.78f, Fractal(u, v, 46, 3, 3, 96)) * 0.24f;
                float chips = Mathf.InverseLerp(0.73f, 0.8f, Fractal(u, v, 22, 14, 3, 97)) * 0.5f;
                canvas[x, y] = Opaque(Color.Lerp(paint * Detail(x, y * 2f, 0.22f) * streaks, rust, chips));
            }
        }

        // Body panels, joined every 1.75m.
        for (int x = 0; x < width; x += width / 4) canvas.FillRect(x, 0, 2, height, Rgb(24, 24, 24), 0.6f);
        return canvas;
    }

    // Wood-effect laminate up to the sills and pale panels above, tiled like the outside so V is world height.
    static Canvas TrainLining()
    {
        const int size = 256;
        Color wood = Rgb(108, 86, 62), rail = Rgb(58, 54, 48), panel = Rgb(166, 164, 142);
        var canvas = new Canvas(size, size, panel);
        for (int y = 0; y < size; y++)
        {
            float metres = (float)y / size * TrainPaintHeight;
            Color paint = metres < 0.88f ? wood : metres < 0.92f ? rail : panel;
            for (int x = 0; x < size; x++)
            {
                float grain = metres < 0.88f ? Mathf.Lerp(0.82f, 1.14f, Fractal(x / 256f, y / 256f, 3, 34, 3, 94)) : 1f;
                canvas[x, y] = Opaque(paint * Detail(x * 2f, y * 2f, 0.16f) * grain);
            }
        }
        return canvas;
    }

    // Leave reading out to paint the dial without a needle, for one that moves. Numbering writes every other tick.
    static void Dial(Canvas canvas, float cx, float cy, float radius, string label, float reading = -1f, int numbersStep = 0)
    {
        var centre = new Vector2(cx, cy);
        Vector2 Direction(float t)
        {
            float angle = Mathf.Lerp(-2.3f, 2.3f, t);
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        canvas.FillCircle(cx, cy, radius + 3f, Rgb(20, 20, 20));
        canvas.FillCircle(cx, cy, radius, Rgb(206, 200, 178));
        for (int i = 0; i <= 8; i++)
        {
            canvas.DrawLine(centre + Direction(i / 8f) * radius * 0.76f, centre + Direction(i / 8f) * radius * 0.92f, 1.5f, Rgb(24, 24, 24));
            if (numbersStep == 0 || i % 2 != 0) continue;

            Vector2 number = centre + Direction(i / 8f) * radius * 0.58f;
            DrawText(canvas, (i * numbersStep).ToString(), number.x, number.y - 5f, 12, Rgb(24, 24, 24));
        }
        if (reading >= 0f) canvas.DrawLine(centre, centre + Direction(reading) * radius * 0.82f, 2.5f, Rgb(130, 30, 24));
        canvas.FillCircle(cx, cy, 3f, Rgb(20, 20, 20));
        DrawText(canvas, label, cx, cy - radius - 20f, 13, Rgb(204, 200, 182));
    }

    static Canvas CabConsole()
    {
        var canvas = new Canvas(512, 192, Rgb(58, 66, 60));
        canvas.FillRect(10, 10, 492, 172, Rgb(70, 78, 72));

        // The left of the desk is bare: the controller stands there.
        Dial(canvas, SpeedDialX, SpeedDialY, 46f, "СКОРОСТ", numbersStep: 10);
        Dial(canvas, 352f, 126f, 30f, "НАЛЯГАНЕ", 0.62f);
        Dial(canvas, 434f, 126f, 30f, "ВОЛТА", 0.36f);

        // A row of switches, and the two buttons a trainee is told not to touch.
        for (int i = 0; i < 5; i++)
        {
            canvas.FillRect(326 + i * 16, 22, 9, 24, Rgb(26, 26, 26));
            canvas.FillRect(328 + i * 16, 35, 5, 9, Rgb(178, 172, 150));
        }
        canvas.FillCircle(428f, 36f, 13f, Rgb(24, 24, 24));
        canvas.FillCircle(428f, 36f, 10f, Rgb(140, 36, 28));
        canvas.FillCircle(466f, 36f, 13f, Rgb(24, 24, 24));
        canvas.FillCircle(466f, 36f, 10f, Rgb(46, 104, 62));
        return Grimed(canvas, 0.35f);
    }

    // Cast-iron tunnel lining: staggered rings of bolted segments, with rust along the joints and damp running down.
    // U runs round the tunnel and V along it.
    static Canvas Tubing()
    {
        const int width = 1024, height = 512, segments = 6, rings = 8, bolts = 8;
        const float rimWidth = 15f, jointWidth = 2.4f;
        Color iron = Rgb(74, 74, 72), rim = Rgb(104, 102, 96), joint = Rgb(24, 22, 20), rust = Rgb(78, 62, 50), bolt = Rgb(132, 126, 116);

        var canvas = new Canvas(width, height, iron);
        float cellW = (float)width / segments, cellH = (float)height / rings;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                int ring = Mathf.FloorToInt(y / cellH);
                float along = x / cellW + (ring % 2) * 0.5f;
                float fu = along - Mathf.Floor(along), fv = y / cellH - ring;
                float edge = Mathf.Min(Mathf.Min(fu, 1f - fu) * cellW, Mathf.Min(fv, 1f - fv) * cellH);

                float raised = Mathf.InverseLerp(rimWidth + 2f, rimWidth - 2f, edge);
                float shadow = Mathf.Exp(-Mathf.Pow((edge - rimWidth - 3f) / 3f, 2f)) * 0.35f;
                Color metal = Color.Lerp(iron * (1f - shadow), rim, raised) * Detail(x * 0.5f, y, 0.55f);

                // Bolts along the flanges between one ring and the next.
                float boltU = (fu * bolts) % 1f - 0.5f, flange = Mathf.Min(fv, 1f - fv) * cellH - 8f;
                float boltDistance = Mathf.Sqrt(boltU * cellW / bolts * boltU * cellW / bolts + flange * flange);
                metal = Color.Lerp(metal, bolt * Detail(x, y, 0.5f), Mathf.InverseLerp(4.5f, 3f, boltDistance));

                // Rust gathers at the joints; damp runs down the wall, along U.
                float jointRust = Mathf.Exp(-edge / 10f);
                float patch = Fractal(u, v, 12, 8, 4, 101) + jointRust * 0.2f;
                metal = Color.Lerp(metal, rust * Detail(x * 0.5f, y, 0.6f), Mathf.InverseLerp(0.6f, 0.76f, patch));

                float damp = Mathf.InverseLerp(0.54f, 0.72f, Fractal(u, v, 3, 36, 3, 103));
                metal *= Mathf.Lerp(1f, 0.62f, damp);

                canvas[x, y] = Opaque(Color.Lerp(joint, metal, Mathf.InverseLerp(jointWidth * 0.5f, jointWidth, edge)));
            }
        }
        return canvas;
    }

    // The scale on the driver's controller: power forward, brake back, with a slot for the arm between.
    static Canvas ControllerPlate()
    {
        Color plate = Rgb(52, 54, 50), ink = Rgb(216, 210, 190), slot = Rgb(6, 6, 6);
        var canvas = new Canvas(128, 256, plate);
        canvas.FillRect(4, 4, 120, 248, Rgb(30, 30, 28));
        canvas.FillRect(8, 8, 112, 240, plate);
        canvas.FillRect(56, 40, 16, 176, slot);

        for (int i = -3; i <= 3; i++)
        {
            bool off = i == 0;
            canvas.FillRect(off ? 24 : 34, 127 + i * 26, off ? 26 : 16, off ? 3 : 2, ink);
            canvas.FillRect(78, 127 + i * 26, off ? 26 : 16, off ? 3 : 2, ink);
        }
        DrawText(canvas, "ТЯГА", 64f, 224f, 20, Rgb(224, 196, 110), 1f);
        DrawText(canvas, "СПИРАЧКА", 64f, 18f, 15, Rgb(214, 96, 76));
        return Grimed(canvas, 0.3f);
    }

    static Canvas PosterDrivers()
    {
        Color ink = Rgb(26, 24, 22), body = Rgb(56, 72, 88), glass = Rgb(190, 184, 160), lamp = Rgb(226, 220, 196);
        var canvas = new Canvas(256, 360, Rgb(172, 134, 62));
        canvas.FillRect(0, 0, 256, 92, Rgb(36, 52, 84));

        // The front of a train, straight on.
        canvas.FillRect(62, 84, 132, 170, body);
        canvas.FillRect(78, 176, 100, 58, glass);
        canvas.FillRect(62, 158, 132, 8, Rgb(130, 40, 32));
        canvas.FillCircle(88f, 124f, 13f, lamp);
        canvas.FillCircle(168f, 124f, 13f, lamp);

        DrawText(canvas, "ТЪРСИМ", 128f, 306f, 42, ink, 2f);
        DrawText(canvas, "МАШИНИСТИ", 128f, 270f, 32, ink, 1f);
        DrawText(canvas, "МЕТРОПОЛИТЕН", 128f, 36f, 26, Rgb(226, 220, 200), 1f);
        return Grimed(canvas, 0.6f);
    }

    static Canvas PosterLine()
    {
        Color ground = Rgb(180, 174, 152), ink = Rgb(26, 24, 22);
        var canvas = new Canvas(256, 360, ground);
        canvas.FillRect(0, 0, 256, 130, Rgb(84, 80, 74));
        canvas.FillRect(0, 118, 256, 18, Rgb(196, 156, 46));

        // A figure standing well back from the edge.
        canvas.FillCircle(128f, 246f, 16f, ink);
        canvas.FillRect(112, 150, 32, 80, ink);

        DrawText(canvas, "СТОЙТЕ ЗАД", 128f, 314f, 30, ink, 1f);
        DrawText(canvas, "ЖЪЛТАТА ЛИНИЯ", 128f, 284f, 25, ink, 1f);
        return Grimed(canvas, 0.6f);
    }

    static Canvas IntercomFace()
    {
        var canvas = new Canvas(128, 192, Rgb(98, 104, 98));
        for (int y = 74; y < 150; y += 7) canvas.FillRect(22, y, 84, 3, Rgb(20, 20, 20));
        canvas.FillCircle(64f, 38f, 15f, Rgb(22, 22, 22));
        canvas.FillCircle(64f, 38f, 11f, Rgb(156, 150, 132));
        DrawText(canvas, "ДИСПЕЧЕР", 64f, 162f, 15, Rgb(18, 18, 18));
        return Grimed(canvas, 0.5f);
    }

    static Canvas KeyBoard()
    {
        Color brass = Rgb(172, 142, 74);
        var canvas = new Canvas(256, 176, Rgb(96, 74, 54));
        for (int y = 0; y < 176; y++)
            for (int x = 0; x < 256; x++)
                canvas[x, y] = Opaque(canvas[x, y] * (0.8f + Fractal(x / 256f, y / 176f, 3, 24, 3, 98) * 0.4f));

        for (int i = 0; i < 8; i++)
        {
            float x = 34f + i % 4 * 62f, y = i < 4 ? 138f : 66f;
            canvas.FillCircle(x, y, 4f, Rgb(40, 34, 28));
            DrawText(canvas, (i + 1).ToString(), x, y + 8f, 13, Rgb(214, 208, 190));

            // Hook two is left bare here: the real key hangs on it.
            if (i == 1) continue;
            canvas.FillCircle(x, y - 12f, 6f, brass);
            canvas.FillCircle(x, y - 12f, 3f, Rgb(84, 64, 46));
            canvas.FillRect(Mathf.RoundToInt(x) - 1, Mathf.RoundToInt(y) - 40, 3, 24, brass);
            canvas.FillRect(Mathf.RoundToInt(x) + 2, Mathf.RoundToInt(y) - 38, 5, 3, brass);
        }
        return Grimed(canvas, 0.4f);
    }

    static Canvas LogBook()
    {
        Color paper = Rgb(204, 198, 176), rule = Rgb(120, 126, 134), ink = Rgb(36, 40, 70);
        var canvas = new Canvas(256, 176, paper);
        canvas.FillRect(125, 0, 6, 176, Rgb(110, 104, 92));

        DrawText(canvas, "СМЯНА", 64f, 154f, 15, Rgb(30, 30, 30));
        DrawText(canvas, "ПОДПИС", 192f, 154f, 15, Rgb(30, 30, 30));
        for (int line = 0; line < 12; line++)
        {
            int y = 140 - line * 11;
            canvas.FillRect(8, y, 112, 1, rule, 0.7f);
            canvas.FillRect(136, y, 112, 1, rule, 0.7f);
            if (line >= 7) continue;

            // Old entries in the same two hands.
            int reach = 40 + Mathf.RoundToInt(Hash(line, 3, 99) * 56f);
            for (int x = 0; x < reach; x += 3)
            {
                float wave = Mathf.Sin(x * 0.55f + line) * 2f + Mathf.Sin(x * 0.2f) * 1.5f;
                canvas.FillRect(140 + x, y + 3 + Mathf.RoundToInt(wave), 2, 2, ink, 0.8f);
                if (x < 64) canvas.FillRect(12 + x, y + 3 + Mathf.RoundToInt(wave * 0.5f), 1, 2, ink, 0.7f);
            }
        }
        return Grimed(canvas, 0.25f);
    }
}
