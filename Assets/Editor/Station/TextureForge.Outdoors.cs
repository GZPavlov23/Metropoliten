using UnityEngine;

// Severna's stone, and everything out in the open at the end of the line.
public static partial class TextureForge
{
    // Big slabs of polished stone, each with its veins running a different way.
    static Canvas Marble(Color stone, Color vein, int seed)
    {
        const int size = 512, slabs = 3;
        var canvas = new Canvas(size, size, stone);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                int column = x * slabs / size, row = y * slabs * 2 / size;
                float turn = Hash(column, row, seed) * Mathf.PI, along = u * Mathf.Cos(turn) + v * Mathf.Sin(turn);
                float warp = Fractal(u, v, 4, 4, 4, seed + 1);
                float line = Mathf.Abs(Mathf.Sin((along * 7f + warp * 3.2f) * Mathf.PI));
                float veins = Mathf.Pow(1f - line, 9f);
                float cloud = Mathf.Lerp(0.86f, 1.1f, Fractal(u, v, 6, 6, 4, seed + 2));

                Color c = Color.Lerp(stone * cloud, vein, veins * 0.7f);
                if (x % (size / slabs) < 2 || y % (size / (slabs * 2)) < 2) c *= 0.62f;
                canvas[x, y] = Opaque(c * Detail(x, y, 0.1f));
            }
        }
        return canvas;
    }

    // Glass nobody has cleaned: a film of dust laid over whatever is behind it, thicker in the corners.
    static Canvas GlassDirt()
    {
        const int size = 256;
        var canvas = new Canvas(size, size, Color.white);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float film = Mathf.Lerp(0.5f, 0.86f, Fractal(u, v, 5, 5, 5, 221));
                float corner = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                float tone = film * Mathf.Lerp(0.62f, 1f, Mathf.Clamp01(corner * 7f));
                canvas[x, y] = new Color(tone, tone * 0.98f, tone * 0.93f, 1f);
            }
        }
        return canvas;
    }

    // Poured stone with chips in it, worn dull along the middle of the platform.
    static Canvas Terrazzo()
    {
        const int size = 512, slab = size / 2;
        var canvas = new Canvas(size, size, Rgb(120, 112, 100));
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float chip = Hash(x / 2, y / 2, 151);
                Color c = Rgb(112, 104, 92);
                if (chip > 0.86f) c = Rgb(168, 158, 136);
                else if (chip > 0.74f) c = Rgb(70, 60, 54);
                else if (chip > 0.68f) c = Rgb(120, 70, 56);

                c *= Mathf.Lerp(0.8f, 1.08f, Fractal(u, v, 5, 5, 4, 152));
                bool line = x % slab < 3 || y % slab < 3;
                bool chequer = (x / slab + y / slab) % 2 == 0;
                if (chequer) c *= 0.8f;
                if (line) c = Rgb(170, 140, 84);
                canvas[x, y] = Opaque(c);
            }
        }
        return canvas;
    }

    static Canvas Gravel()
    {
        const int size = 256;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float stones = Mathf.Clamp01((Fractal(u, v, 34, 34, 3, 161) - 0.38f) * 3f);
                float mud = Fractal(u, v, 4, 4, 4, 162);
                canvas[x, y] = Opaque(Color.Lerp(Rgb(62, 58, 50), Rgb(94, 88, 78), stones) * Mathf.Lerp(0.74f, 1.1f, mud));
            }
        }
        return canvas;
    }

    static Canvas Corrugated()
    {
        const int size = 128;
        var canvas = new Canvas(size, size, Rgb(100, 102, 100));
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float rib = 0.72f + 0.34f * Mathf.Sin(u * Mathf.PI * 2f * 8f);
                float rust = Mathf.InverseLerp(0.46f, 0.72f, Fractal(u, v, 5, 5, 4, 181));
                canvas[x, y] = Opaque(Color.Lerp(Rgb(112, 114, 112), Rgb(96, 62, 42), rust) * rib);
            }
        }
        return canvas;
    }

    // Diamonds of wire with a good deal of nothing between them. It's painted dark, and the gaps are nearly the air.
    static Canvas Chainlink()
    {
        const int size = 512, mesh = 32;
        var canvas = new Canvas(size, size, Color.white);
        Color wire = Rgb(64, 66, 68);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float a = Mathf.Repeat(x + y, mesh), b = Mathf.Repeat(x - y, mesh);
                float toWire = Mathf.Min(Mathf.Min(a, mesh - a), Mathf.Min(b, mesh - b)) * 0.707f;
                canvas[x, y] = Color.Lerp(wire, Color.white, Mathf.Clamp01(toWire - 1f));
            }
        }
        return canvas;
    }

    static Canvas BusStopSign()
    {
        const int size = 128;
        var canvas = new Canvas(size, size, Rgb(30, 30, 30));
        canvas.FillCircle(64f, 64f, 60f, Rgb(36, 60, 110));
        canvas.FillCircle(64f, 64f, 50f, Rgb(214, 208, 190));
        DrawText(canvas, "14", 64f, 44f, 54, Rgb(36, 60, 110));
        return Grimed(canvas, 0.4f);
    }

    static Canvas TimeClockFace()
    {
        const int width = 192, height = 270;
        var canvas = new Canvas(width, height, Rgb(70, 72, 68));
        canvas.FillRect(6, 6, width - 12, height - 12, Rgb(84, 86, 80));
        canvas.FillCircle(96f, 182f, 66f, Rgb(20, 20, 20));
        canvas.FillCircle(96f, 182f, 60f, Rgb(204, 198, 178));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            var way = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            canvas.DrawLine(new Vector2(96f, 182f) + way * 46f, new Vector2(96f, 182f) + way * 56f, 3f, Rgb(24, 24, 24));
        }
        canvas.DrawLine(new Vector2(96f, 182f), new Vector2(120f, 200f), 5f, Rgb(20, 20, 20));
        canvas.DrawLine(new Vector2(96f, 182f), new Vector2(96f, 232f), 3f, Rgb(20, 20, 20));
        canvas.FillRect(46, 56, 100, 30, Rgb(14, 14, 14));
        canvas.FillRect(52, 60, 88, 22, Rgb(30, 30, 30));
        DrawText(canvas, "КАРТА", 96f, 26f, 22, Rgb(214, 208, 186), 2f);
        return Grimed(canvas, 0.5f);
    }

    static Canvas PayphoneFace()
    {
        const int width = 192, height = 300;
        var canvas = new Canvas(width, height, Rgb(70, 70, 66));
        canvas.FillRect(10, 200, 172, 80, Rgb(28, 28, 26));
        DrawText(canvas, "ТЕЛЕФОН", 96f, 232f, 26, Rgb(214, 208, 186), 2f);
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 3; column++)
                canvas.FillCircle(56f + column * 40f, 158f - row * 36f, 13f, Rgb(170, 166, 150));
        canvas.FillRect(160, 96, 12, 40, Rgb(20, 20, 18));
        return Grimed(canvas, 0.6f);
    }

    // The card out of the rack, with a line for every shift and one too many stamped.
    static Canvas TimeCard()
    {
        const int width = 600, height = 760;
        Color ink = Rgb(30, 30, 44), stamp = Rgb(110, 40, 44);
        var canvas = new Canvas(width, height, Rgb(208, 200, 176));

        DrawText(canvas, "МЕТРОПОЛИТЕН", width / 2f, 690f, 36, ink, 3f);
        canvas.FillRect(30, 670, width - 60, 3, ink);
        DrawText(canvas, "НИКОЛА", width / 2f, 620f, 34, ink, 2f);
        DrawText(canvas, "СТАЖАНТ", width / 2f, 584f, 22, ink, 2f);

        canvas.FillRect(30, 560, width - 60, 2, ink);
        foreach ((float x, string title) in new[] { (110f, "СМЯНА"), (300f, "ВХОД"), (490f, "ИЗХОД") })
            DrawText(canvas, title, x, 528f, 22, ink);
        canvas.FillRect(30, 516, width - 60, 2, ink);

        (string shift, string inTime, string outTime)[] lines =
        {
            ("1", "22:00", "05:41"), ("2", "22:00", "06:12"), ("3", "22:00", ""), ("4", "", ""),
        };
        for (int i = 0; i < lines.Length; i++)
        {
            float y = 460f - i * 86f;
            canvas.FillRect(30, Mathf.RoundToInt(y) - 24, width - 60, 1, ink, 0.5f);
            DrawText(canvas, lines[i].shift, 110f, y, 34, ink);
            DrawText(canvas, lines[i].inTime, 300f, y, 34, stamp);
            DrawText(canvas, lines[i].outTime, 490f, y, 34, stamp);
        }
        return Grimed(canvas, 0.3f);
    }
}
