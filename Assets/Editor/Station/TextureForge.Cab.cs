using UnityEngine;

// The cab's paint, dials and paperwork. Every dial reads the same way as the needle: nothing at the bottom left,
// sweeping up and over to full scale at the bottom right.
public static partial class TextureForge
{
    const float DialSweep = 2.3f;

    static void ForgeCab()
    {
        Write("T_CabConsole", CabPanel());
        Write("T_CabPaint", CabPaint());
        Write("T_CabDesk", Painted(128, Rgb(56, 62, 58), 0.7f, 3, 0.18f));
        Write("T_CabSeat", CabSeat());
        Write("T_CabRadio", CabRadio());
        Write("T_CabNotes", CabNotes());
        Write("T_WindscreenDirt", WindscreenDirt());
        Write("T_GaugeSpeed", GaugeFace("км/ч", 80, 5, 20, 50, -1f));
        Write("T_GaugePressure", GaugeFace("БАР", 8, 1, 2, 7, 0.64f));
        Write("T_GaugeVolt", GaugeFace("ВОЛТА", 1000, 100, 250, 900, 0.76f));
    }

    static Canvas GaugeFace(string label, int fullScale, int tick, int numbered, int redFrom, float reading)
    {
        const int size = 256;
        var centre = new Vector2(size / 2f, size / 2f);
        var canvas = new Canvas(size, size, Rgb(16, 16, 15));
        Color ink = Rgb(214, 208, 186), red = Rgb(186, 52, 36);

        Vector2 Way(float t)
        {
            float angle = Mathf.Lerp(-DialSweep, DialSweep, t);
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        for (int value = 0; value <= fullScale; value += tick)
        {
            float t = (float)value / fullScale;
            bool major = value % numbered == 0;
            Color mark = value >= redFrom ? red : ink;
            canvas.DrawLine(centre + Way(t) * (major ? 94f : 104f), centre + Way(t) * 118f, major ? 6f : 3f, mark);
            if (!major) continue;

            Vector2 number = centre + Way(t) * 72f;
            DrawText(canvas, value.ToString(), number.x, number.y - 11f, 30, ink);
        }

        DrawText(canvas, label, centre.x, 48f, 24, Rgb(170, 164, 146));
        if (reading >= 0f)
        {
            canvas.DrawLine(centre - Way(reading) * 16f, centre + Way(reading) * 106f, 5f, Rgb(214, 90, 60));
            canvas.FillCircle(centre.x, centre.y, 12f, Rgb(60, 60, 58));
        }

        // Age has yellowed the glass and left dust round the rim.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float rim = Mathf.InverseLerp(84f, 128f, Vector2.Distance(new Vector2(x, y), centre));
                canvas[x, y] = Opaque(Color.Lerp(canvas[x, y] * new Color(1f, 0.95f, 0.84f), Rgb(46, 42, 34), rim * rim * 0.5f));
            }
        }
        return canvas;
    }

    // Painted steel, darker below the sills, chipped where the driver's knees and boots go.
    static Canvas CabPaint()
    {
        const int size = 256;
        Color lower = Rgb(68, 82, 78), band = Rgb(38, 42, 40), upper = Rgb(146, 156, 142), rust = Rgb(72, 52, 38);
        var canvas = new Canvas(size, size, upper);
        for (int y = 0; y < size; y++)
        {
            float metres = (float)y / size * TrainPaintHeight;
            Color paint = metres < 0.84f ? lower : metres < 0.88f ? band : upper;
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float grime = Mathf.Lerp(0.78f, 1.08f, Fractal(u, v, 5, 9, 4, 121));
                float streak = 1f - Mathf.InverseLerp(0.55f, 0.8f, Fractal(u, v, 28, 3, 3, 122)) * 0.16f;
                float chip = Mathf.InverseLerp(0.71f, 0.76f, Fractal(u, v, 19, 19, 3, 123)) * Mathf.Lerp(0.8f, 0.25f, Mathf.InverseLerp(0f, 1.2f, metres));
                canvas[x, y] = Opaque(Color.Lerp(paint * grime * streak * Detail(x * 2f, y * 2f, 0.14f), rust, chip));
            }
        }

        // Seams between the steel panels, with rivets.
        foreach (int x in new[] { 0, size / 2 })
        {
            canvas.FillRect(x, 0, 2, size, Rgb(20, 20, 18), 0.6f);
            for (int y = 6; y < size; y += 16)
            {
                canvas.FillCircle(x + 7f, y, 1.6f, Rgb(198, 200, 186));
                canvas.FillCircle(x + 7f, y - 1f, 1.1f, Rgb(36, 36, 34));
            }
        }
        return canvas;
    }

    static Canvas CabSeat()
    {
        Canvas canvas = Painted(128, Rgb(72, 50, 36), 0.5f, 4, 0.24f);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float u = x / 128f, v = y / 128f;

                // Worn pale, and cracked along the stitching.
                float worn = Mathf.InverseLerp(0.5f, 0.74f, Fractal(u, v, 4, 4, 3, 125));
                Color c = Color.Lerp(canvas[x, y], Rgb(112, 88, 66), worn * 0.6f);
                if (x % 32 < 2 || y % 42 < 2) c *= 0.6f;
                canvas[x, y] = Opaque(c);
            }
        }
        return canvas;
    }

    // The face let into the sloping panel: engraved labels for what's bolted on top of it.
    static Canvas CabPanel()
    {
        const int width = 1024, height = 288;
        Color plate = Rgb(62, 68, 62), edge = Rgb(30, 32, 30), ink = Rgb(204, 198, 176);
        var canvas = new Canvas(width, height, edge);
        canvas.FillRect(6, 6, width - 12, height - 12, plate);

        foreach (int x in new[] { 18, width - 18 })
        {
            foreach (int y in new[] { 18, height - 18 })
            {
                canvas.FillCircle(x, y, 6f, Rgb(120, 120, 112));
                canvas.DrawLine(new Vector2(x - 4f, y - 2f), new Vector2(x + 4f, y + 2f), 1.5f, edge);
            }
        }

        // Each row sits where its parts are on the desk.
        float Across(float x) => (x - 2f) / 1.42f * width;
        float Up(float along) => along * height;

        string[] switches = { "ВЕНТ", "ОСВ", "ФАР", "КОМПР", "ВОДА", "СИГН", "РЕЗ", "АВАР" };
        for (int i = 0; i < switches.Length; i++) DrawText(canvas, switches[i], Across(2.1f + i * 0.085f), Up(0.28f) - 46f, 15, ink);

        string[] knobs = { "ЯРКОСТ", "ОБДУХ", "ГЛАС" };
        for (int i = 0; i < knobs.Length; i++) DrawText(canvas, knobs[i], Across(2.12f + i * 0.13f), Up(0.72f) - 62f, 16, ink);

        canvas.FillRect(Mathf.RoundToInt(Across(2.82f)), 44, Mathf.RoundToInt(Across(3.24f) - Across(2.82f)), 3, ink, 0.7f);
        DrawText(canvas, "ВРАТИ", Across(3.03f), 18f, 22, Rgb(226, 196, 110), 2f);
        DrawText(canvas, "ОТВОРИ", Across(2.92f), 60f, 18, ink);
        DrawText(canvas, "ЗАТВОРИ", Across(3.14f), 60f, 18, ink);
        return Grimed(canvas, 0.45f);
    }

    static Canvas CabRadio()
    {
        const int width = 320, height = 200;
        var canvas = new Canvas(width, height, Rgb(70, 74, 68));
        canvas.FillRect(4, 4, width - 8, height - 8, Rgb(84, 88, 80));
        for (int y = 26; y < 176; y += 12) canvas.FillRect(20, y, 140, 6, Rgb(18, 18, 18));

        canvas.FillRect(182, 118, 122, 62, Rgb(26, 28, 24));
        canvas.FillRect(188, 124, 110, 50, Rgb(170, 144, 80));
        for (int x = 194; x < 294; x += 10) canvas.FillRect(x, 150, 2, 18, Rgb(30, 26, 20));
        canvas.FillRect(238, 126, 3, 46, Rgb(150, 36, 28));

        canvas.FillCircle(212f, 74f, 22f, Rgb(16, 16, 16));
        canvas.FillCircle(212f, 74f, 7f, Rgb(120, 120, 112));
        canvas.FillCircle(274f, 74f, 16f, Rgb(16, 16, 16));
        canvas.FillCircle(274f, 74f, 5f, Rgb(120, 120, 112));
        DrawText(canvas, "ДИСПЕЧЕР", 244f, 16f, 15, Rgb(214, 208, 186));
        return Grimed(canvas, 0.5f);
    }

    static Canvas CabNotes()
    {
        const int width = 256, height = 360;
        var canvas = new Canvas(width, height, Rgb(208, 200, 176));
        canvas.FillRect(0, 300, width, 60, Rgb(150, 142, 118));
        DrawText(canvas, "ЗАПОВЕД", width / 2f, 322f, 26, Rgb(30, 28, 26), 2f);
        for (int i = 0; i < 11; i++)
        {
            int y = 272 - i * 22;
            canvas.FillRect(18, y, 30, 2, Rgb(40, 40, 50), 0.8f);
            canvas.FillRect(56, y, Mathf.RoundToInt(Mathf.Lerp(90f, 176f, Hash(i, 3, 44))), 2, Rgb(40, 40, 50), 0.7f);
        }
        canvas.FillCircle(190f, 62f, 30f, Rgb(120, 50, 60));
        canvas.FillCircle(190f, 62f, 25f, Rgb(208, 200, 176));
        return Grimed(canvas, 0.6f);
    }

    // Multiplied over what's behind the glass. White is clean, so the wipers leave two white fans.
    static Canvas WindscreenDirt()
    {
        const int width = 512, height = 256;
        var canvas = new Canvas(width, height, Color.white);
        var pivots = new[] { new Vector2(0.3f, -0.05f), new Vector2(0.7f, -0.05f) };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float dirt = Mathf.Lerp(0.68f, 0.96f, Fractal(u, v, 9, 5, 4, 131));
                float speck = Hash(x, y, 132) > 0.992f ? 0.6f : 1f;
                float edge = Mathf.Min(Mathf.Min(u, 1f - u) * 2.4f, Mathf.Min(v, 1f - v) * 1.2f);

                float clean = 0f;
                foreach (Vector2 pivot in pivots)
                {
                    float reach = new Vector2((u - pivot.x) * 2f, v - pivot.y).magnitude;
                    clean = Mathf.Max(clean, Mathf.InverseLerp(0.2f, 0.28f, reach) * Mathf.InverseLerp(0.98f, 0.9f, reach));
                }

                float streak = Mathf.Lerp(0.93f, 1f, Fractal(u * 3f, v, 40, 2, 2, 133));
                float through = Mathf.Lerp(dirt * speck, Mathf.Lerp(0.9f, 1f, dirt) * streak, clean);
                through = Mathf.Lerp(through * 0.8f, through, Mathf.Clamp01(edge * 3f));
                canvas[x, y] = new Color(through, through, through * 0.97f, 1f);
            }
        }
        return canvas;
    }
}
