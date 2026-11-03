using System.IO;
using UnityEditor;
using UnityEngine;

// What Shift 2 adds to the walls: a station that was shut and left, a staff room, and what's in the lockers.
public static partial class TextureForge
{
    const string DocumentDir = "Assets/Resources/Documents";

    public static void ForgeShift2()
    {
        Write("T_TileOld", BrokenTiles());
        Write("T_TileBandClosed", Tiles(256, 4, Rgb(104, 58, 46), Rgb(62, 46, 40), 0.1f, 37));
        Write("T_SignZavodska", Grimed(Banner("ЗАВОДСКА", 1024, 192, Rgb(176, 170, 150), Rgb(28, 26, 24), 118), 0.75f));
        Write("T_SignHospital", Banner("БОЛНИЧНА", 1024, 192, Rgb(206, 202, 186), Rgb(24, 24, 24), 118));
        Write("T_Gravel", Gravel());
        Write("T_Corrugated", Corrugated());
        Write("T_Chainlink", Chainlink());
        Write("T_BusStop", BusStopSign());
        Write("T_TimeClock", TimeClockFace());
        Write("T_Payphone", PayphoneFace());
        Write("T_SignDepot", Banner("ДЕПО", 512, 150, Rgb(36, 52, 84), Rgb(226, 220, 200), 84));
        Write("T_LockerDoor", LockerDoor(Rgb(98, 112, 100), 0.45f));
        Write("T_LockerDoorOld", LockerDoor(Rgb(82, 72, 62), 0.95f));
        Write("T_LockerInside", LockerInside(Rgb(112, 126, 114), 0.3f));
        Write("T_LockerInsideOld", LockerInside(Rgb(90, 80, 68), 0.9f));
        Write("T_LabelMine", Grimed(Banner("НИКОЛА", 256, 64, Rgb(196, 182, 142), Rgb(30, 30, 40), 34), 0.5f));
        Write("T_LabelScratched", ScratchedLabel());
        Write("T_Hospital", Banner("БОЛНИЦА", 1024, 134, Rgb(206, 204, 196), Rgb(26, 26, 28), 96));
        Write("T_Rota", Rota());
        Write("T_RelayPaint", RelayPaint());
        Write("T_RelaySlate", RelaySlate());
        Write("T_RelayPlate", Grimed(Banner("РЕЛЕ   СИГНАЛ 14", 660, 100, Rgb(146, 34, 26), Rgb(232, 226, 208), 52), 0.5f));
        Write("T_RelayUnit", RelayUnit());
        Write("T_RelayWarning", RelayWarning());
        Write("T_GaugeRelay", GaugeFace("ВОЛТА", 1000, 100, 250, 900, 0.12f));
        Write("T_Badge", TraineeBadge(192, 128));
        ForgeTown();
        ImportWritten();

        Directory.CreateDirectory(DocumentDir);
        ForgeDocument("D_Badge", TraineeBadge(620, 413));
        ForgeDocument("D_List", NamesList());
        ForgeDocument("D_TimeCard", TimeCard());
        AssetDatabase.Refresh();
        foreach (string name in new[] { "D_Badge", "D_List", "D_TimeCard" }) ConfigureDocument(name);
    }

    // What the script holds up to the camera. These stay sharp, because they're read rather than glanced at.
    static void ForgeDocument(string name, Canvas canvas)
    {
        canvas.Save($"{DocumentDir}/{name}.png");
    }

    static void ConfigureDocument(string name)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath($"{DocumentDir}/{name}.png");
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // Tiles that have been coming off the wall for thirty years.
    static Canvas BrokenTiles()
    {
        const int size = 512, count = 8, seed = 41;
        Canvas canvas = Tiles(size, count, Rgb(150, 144, 124), Rgb(70, 66, 60), 0.09f, seed);
        int cell = size / count;
        for (int row = 0; row < count; row++)
        {
            for (int column = 0; column < count; column++)
            {
                if (Hash(column, row, seed + 2) < 0.89f) continue;

                // Bare plaster where a tile has come away, not quite the same shape each time.
                float tone = 0.86f + Hash(column, row, seed + 3) * 0.2f;
                int inset = 1 + Mathf.RoundToInt(Hash(column, row, seed + 4) * 7f);
                canvas.FillRect(column * cell + inset, row * cell + 1, cell - inset - 1, cell - 2, Rgb(104, 98, 86) * tone, 0.86f);
            }
        }
        return Grimed(canvas, 0.6f);
    }

    static Canvas LockerDoor(Color paint, float grime)
    {
        var canvas = new Canvas(128, 384, paint);
        canvas.FillRect(0, 0, 128, 4, paint * 0.55f);
        canvas.FillRect(0, 380, 128, 4, paint * 0.55f);
        canvas.FillRect(0, 0, 4, 384, paint * 0.55f);
        canvas.FillRect(124, 0, 4, 384, paint * 0.55f);

        // Vents near the top and a handle at hand height.
        for (int y = 306; y < 356; y += 11) canvas.FillRect(30, y, 68, 5, Rgb(20, 20, 20));
        canvas.FillRect(102, 170, 12, 44, Rgb(44, 42, 40));
        canvas.FillRect(104, 172, 8, 40, Rgb(120, 116, 106));
        return Grimed(canvas, grime);
    }

    // The side of a locker door nobody repaints: pressed steel with a rib across it, and old tape.
    static Canvas LockerInside(Color paint, float grime)
    {
        var canvas = new Canvas(128, 384, paint);
        Color fold = paint * 0.6f, lit = paint * 1.16f;
        canvas.FillRect(0, 0, 128, 384, fold);
        canvas.FillRect(8, 8, 112, 368, paint);
        foreach (int y in new[] { 128, 256 })
        {
            canvas.FillRect(8, y - 7, 112, 14, paint * 0.86f);
            canvas.FillRect(8, y + 7, 112, 2, lit);
            canvas.FillRect(8, y - 9, 112, 2, fold);
        }
        foreach ((int x, int y) in new[] { (34, 314), (88, 322), (40, 268), (96, 262) })
            canvas.FillRect(x, y, 20, 8, Rgb(176, 164, 128), 0.5f);
        return Grimed(canvas, grime);
    }

    static Canvas ScratchedLabel()
    {
        var canvas = new Canvas(256, 64, Rgb(168, 154, 118));
        for (int i = 0; i < 90; i++)
        {
            float y = 10f + Hash(i, 1, 51) * 44f;
            var from = new Vector2(8f + Hash(i, 2, 51) * 220f, y);
            var to = from + new Vector2(14f + Hash(i, 3, 51) * 36f, (Hash(i, 4, 51) - 0.5f) * 16f);
            canvas.DrawLine(from, to, 2f, i % 3 == 0 ? Rgb(210, 200, 170) : Rgb(58, 50, 40));
        }
        return Grimed(canvas, 0.7f);
    }

    // A long lens never gets anything quite sharp. What comes out of the camera, on fast film in bad light: the shadows gone
    // to mud, the sky gone to nothing, a coarse grain over all of it, and the clock the camera prints in the corner.
    public static Canvas Develop(float[] light, int width, int height)
    {
        var canvas = new Canvas(width, height, Color.black);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width - 0.5f, v = (float)y / height - 0.5f;
                float grey = light[y * width + x];
                grey = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((grey - 0.06f) * 1.26f));

                // Grain in clumps, heavier in the mid-tones.
                float clump = Hash(x / 2, y / 2, 56) * 0.6f + Hash(x, y, 57) * 0.4f - 0.5f;
                grey += clump * 0.19f * (1f - Mathf.Abs(grey - 0.5f));
                grey *= 1f - (u * u + v * v) * 1.2f;
                grey = Mathf.Clamp01(grey);
                canvas[x, y] = new Color(grey, grey * 0.985f, grey * 0.955f, 1f);
            }
        }
        DrawText(canvas, "07:42", width * 0.885f, height * 0.04f, Mathf.RoundToInt(width * 0.05f), Rgb(216, 132, 44), 3f);
        return canvas;
    }

    public static void WritePhoto(Canvas film)
    {
        ForgeDocument("D_Photo", film);

        var small = new Canvas(512, 384, Color.black);
        for (int y = 0; y < small.Height; y++)
            for (int x = 0; x < small.Width; x++)
                small[x, y] = film.Sample(x * film.Width / (float)small.Width, y * film.Height / (float)small.Height);
        written.Clear();
        Write("T_Photo", small);
        ImportWritten();
        AssetDatabase.Refresh();
        ConfigureDocument("D_Photo");
    }

    static Canvas Rota()
    {
        Color paper = Rgb(200, 194, 170), ink = Rgb(36, 36, 44);
        var canvas = new Canvas(256, 360, paper);
        DrawText(canvas, "СМЕНИ", 128f, 326f, 26, ink, 3f);
        canvas.FillRect(20, 316, 216, 2, ink);

        for (int row = 0; row < 11; row++)
        {
            int y = 292 - row * 25;
            canvas.FillRect(20, y - 6, 216, 1, Rgb(120, 118, 110));
            int reach = 60 + Mathf.RoundToInt(Hash(row, 1, 53) * 70f);
            for (int x = 0; x < reach; x += 3) canvas.FillRect(26 + x, y + Mathf.RoundToInt(Mathf.Sin(x * 0.5f + row) * 2f) + 2, 2, 2, ink, 0.8f);
            canvas.FillRect(180, y, 40, 3, ink, 0.6f);
        }
        return Grimed(canvas, 0.5f);
    }

    // Hammered grey-green enamel from another age, chipped through to the red primer and the bare steel.
    static Canvas RelayPaint()
    {
        const int size = 512;
        var canvas = new Canvas(size, size, Color.black);
        Color paint = Rgb(80, 94, 84), primer = Rgb(116, 58, 38), bare = Rgb(70, 62, 56);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float wear = Fractal(u, v, 6, 6, 6, 301);
                float hammer = Fractal(u, v, 60, 60, 2, 303);
                Color c = paint * Mathf.Lerp(0.8f, 1.1f, Fractal(u, v, 3, 12, 3, 302)) * Mathf.Lerp(0.92f, 1.08f, hammer);
                c = Color.Lerp(c, primer, Mathf.InverseLerp(0.6f, 0.625f, wear));
                c = Color.Lerp(c, bare, Mathf.InverseLerp(0.645f, 0.67f, wear));
                float drip = Mathf.Pow(Fractal(u, v, 24, 2, 3, 304), 3f) * Mathf.InverseLerp(0.5f, 0.68f, Fractal(u, v, 3, 3, 2, 305));
                canvas[x, y] = Opaque(c * (1f - drip * 1.6f) * Detail(x, y, 0.25f));
            }
        }
        return canvas;
    }

    // Black slate with the lettering cut in and filled, the way they did it when the line opened.
    static Canvas RelaySlate()
    {
        const int width = 816, height = 640;
        const float perMetre = width / 1.02f;
        Color cut = Rgb(196, 188, 164);
        var canvas = new Canvas(width, height, Color.black);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float cloud = Fractal(u, v, 4, 3, 5, 311);
                float vein = Mathf.Pow(1f - Mathf.Abs(Fractal(u, v, 5, 4, 4, 312) * 2f - 1f), 26f);
                canvas[x, y] = Opaque(Rgb(40, 42, 44) * Mathf.Lerp(0.74f, 1.2f, cloud) + Rgb(70, 68, 62) * vein * 0.16f);
            }
        }

        float Across(float metres) => width / 2f + metres * perMetre;
        float Up(float metres) => (metres - SlateBottomY) * perMetre;
        DrawText(canvas, "НАПРЕЖЕНИЕ", Across(RelayDialAcross), Up(1.33f), 18, cut, 2f);
        DrawText(canvas, "БЛОКИРАН", Across(RelayDialAcross - 0.1f), Up(0.81f), 15, cut, 1f);
        DrawText(canvas, "СВОБОДЕН", Across(RelayDialAcross + 0.1f), Up(0.81f), 15, cut, 1f);
        canvas.DrawLine(new Vector2(Across(-0.46f), Up(1.42f)), new Vector2(Across(-0.08f), Up(1.42f)), 2f, cut);
        DrawText(canvas, "РЪЧНО", Across(0.25f), Up(1.4f), 22, cut, 4f);
        DrawText(canvas, "ОСВОБОЖДАВАНЕ", Across(0.25f), Up(0.74f), 17, cut, 2f);
        foreach ((float x, float y) in new[] { (-0.48f, 0.74f), (0.48f, 0.74f), (-0.48f, 1.46f), (0.48f, 1.46f) })
        {
            canvas.FillCircle(Across(x), Up(y), 9f, Rgb(120, 104, 70));
            canvas.FillCircle(Across(x), Up(y), 5f, Rgb(50, 44, 34));
        }
        return Grimed(canvas, 0.35f);
    }

    const float SlateBottomY = 0.7f, RelayDialAcross = -0.28f;

    // One relay behind its glass: a coil, a stack of contacts over it, and a strip of paper saying what it was for.
    static Canvas RelayUnit()
    {
        const int width = 128, height = 192;
        var canvas = new Canvas(width, height, Rgb(22, 21, 20));
        canvas.FillRect(6, 6, width - 12, height - 12, Rgb(34, 33, 31));

        for (int y = 62; y < 122; y += 3)
        {
            Color wire = Rgb(146, 84, 44) * (0.75f + 0.45f * Hash(y, 1, 321));
            canvas.FillRect(28, y, 72, 2, Opaque(wire));
        }
        canvas.FillRect(24, 56, 80, 5, Rgb(60, 58, 54));
        canvas.FillRect(24, 122, 80, 5, Rgb(60, 58, 54));

        for (int i = 0; i < 4; i++)
        {
            int x = 26 + i * 22;
            canvas.DrawLine(new Vector2(x, 132f), new Vector2(x + 4f, 168f), 2f, Rgb(168, 146, 92));
            canvas.FillCircle(x + 4f, 170f, 3f, Rgb(190, 176, 140));
        }

        canvas.FillRect(20, 16, 88, 24, Rgb(176, 164, 132));
        for (int x = 26; x < 98; x += 4)
            if (Hash(x, 2, 322) > 0.3f) canvas.FillRect(x, 24, 3, 8, Rgb(46, 44, 50));
        return Grimed(canvas, 0.5f);
    }

    static Canvas RelayWarning()
    {
        const int size = 192;
        Color yellow = Rgb(206, 164, 40), ink = Rgb(16, 16, 16);
        var canvas = new Canvas(size, size, Rgb(196, 190, 170));
        canvas.FillPolygon(new[] { new Vector2(96f, 182f), new Vector2(186f, 34f), new Vector2(6f, 34f) }, ink);
        canvas.FillPolygon(new[] { new Vector2(96f, 164f), new Vector2(168f, 44f), new Vector2(24f, 44f) }, yellow);
        canvas.FillPolygon(new[] { new Vector2(104f, 146f), new Vector2(76f, 96f), new Vector2(96f, 96f), new Vector2(82f, 54f), new Vector2(122f, 108f), new Vector2(100f, 108f), new Vector2(114f, 146f) }, ink);
        DrawText(canvas, "ОПАСНО", 96f, 8f, 22, ink, 1f);
        return Grimed(canvas, 0.6f);
    }

    static void Soften(Canvas canvas)
    {
        var before = (Color[])canvas.Pixels.Clone();
        for (int y = 1; y < canvas.Height - 1; y++)
        {
            for (int x = 1; x < canvas.Width - 1; x++)
            {
                int at = y * canvas.Width + x;
                canvas.Pixels[at] = (before[at] * 2f + before[at - 1] + before[at + 1] + before[at - canvas.Width] + before[at + canvas.Width]) / 6f;
            }
        }
    }

    static Canvas TraineeBadge(int width, int height)
    {
        float s = width / 192f;
        int Px(float value) => Mathf.RoundToInt(value * s);
        Color card = Rgb(198, 192, 170), ink = Rgb(30, 30, 36);
        var canvas = new Canvas(width, height, card);
        canvas.FillRect(0, Px(100), width, height - Px(100), Rgb(36, 52, 84));
        DrawText(canvas, "СТАЖАНТ", 96f * s, 106f * s, Px(18), card, 2f * s);

        // Where the photograph used to be: cut out with something blunt.
        canvas.FillRect(Px(12), Px(14), Px(62), Px(76), Rgb(58, 54, 48));
        for (int i = 0; i < 26; i++)
        {
            var from = new Vector2(Px(12f + Hash(i, 1, 58) * 62f), Px(14f + Hash(i, 2, 58) * 76f));
            canvas.DrawLine(from, from + new Vector2(Px(Hash(i, 3, 58) * 8f - 4f), Px(Hash(i, 4, 58) * 8f - 4f)), s, Rgb(106, 100, 88));
        }

        DrawText(canvas, "СТОЯН", 134f * s, 66f * s, Px(15), ink);
        DrawText(canvas, "ПЕТКОВ", 134f * s, 44f * s, Px(15), ink);
        DrawText(canvas, "No 0417", 134f * s, 20f * s, Px(11), ink);
        return Grimed(canvas, 0.55f);
    }

    // The registry's own form, typed: everyone who came before, and the next one.
    static Canvas NamesList()
    {
        Color paper = Rgb(208, 202, 180), ink = Rgb(34, 34, 40), strike = Rgb(22, 22, 66);
        var canvas = new Canvas(600, 820, paper);

        DrawText(canvas, "ФОРМУЛЯР", 300f, 752f, 34, ink, 4f);
        canvas.FillRect(46, 738, 508, 3, ink);
        DrawText(canvas, "СЛЕДВАЩИ", 300f, 696f, 22, ink, 3f);

        string[] names = { "ИВАН ДОНЕВ", "ПЕТЪР КОЛЕВ", "ГЕОРГИ МАРИНОВ", "АСЕН ТОДОРОВ", "СТОЯН ПЕТКОВ" };
        for (int i = 0; i < names.Length; i++)
        {
            int y = 638 - i * 60;
            DrawTextFrom(canvas, 56f, y, names[i], 33, ink, 1f);
            float reach = 370f + Hash(i, 1, 59) * 130f;
            canvas.DrawLine(new Vector2(46f, y + 11f + Hash(i, 2, 59) * 6f), new Vector2(reach, y + 13f + Hash(i, 3, 59) * 6f), 5f, strike);
        }

        // The next line isn't crossed out, and has more on it than any of the others ever did.
        const int last = 296;
        DrawTextFrom(canvas, 56f, last, "НИКОЛА", 58, ink, 3f);
        canvas.FillRect(46, last - 20, 508, 3, ink);
        DrawTextFrom(canvas, 56f, last - 74, "ДЪЛГ:  4 180 ЛВ.", 31, ink, 1f);
        DrawTextFrom(canvas, 56f, last - 122, "МАЙКА:  ОТД. 4,  СТАЯ 12", 31, ink, 1f);

        DrawTextFrom(canvas, 56f, 60f, "ДАТА:  09.10.", 24, ink, 1f);
        return Grimed(canvas, 0.45f);
    }
}
