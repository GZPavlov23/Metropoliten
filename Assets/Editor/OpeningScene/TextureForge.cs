using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// A plain pixel buffer. Row 0 is the bottom of the image, matching texture V.
public class Canvas
{
    public readonly int Width, Height;
    public readonly Color[] Pixels;

    public Canvas(int width, int height, Color fill)
    {
        Width = width;
        Height = height;
        Pixels = new Color[width * height];
        for (int i = 0; i < Pixels.Length; i++) Pixels[i] = fill;
    }

    public Color this[int x, int y]
    {
        get => Pixels[Mod(y, Height) * Width + Mod(x, Width)];
        set => Pixels[Mod(y, Height) * Width + Mod(x, Width)] = value;
    }

    static int Mod(int a, int b) => ((a % b) + b) % b;

    public Color Sample(float x, float y)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        return Color.Lerp(Color.Lerp(this[ix, iy], this[ix + 1, iy], fx),
            Color.Lerp(this[ix, iy + 1], this[ix + 1, iy + 1], fx), fy);
    }

    public void Blend(int x, int y, Color color, float alpha)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        this[x, y] = Color.Lerp(this[x, y], color, alpha);
    }

    public void FillRect(int x, int y, int width, int height, Color color, float alpha = 1f)
    {
        for (int j = y; j < y + height; j++)
            for (int i = x; i < x + width; i++)
                Blend(i, j, color, alpha);
    }

    public void FillCircle(float cx, float cy, float radius, Color color)
    {
        for (int j = Mathf.FloorToInt(cy - radius - 1); j <= cy + radius + 1; j++)
        {
            for (int i = Mathf.FloorToInt(cx - radius - 1); i <= cx + radius + 1; i++)
            {
                float d = radius - Mathf.Sqrt((i - cx) * (i - cx) + (j - cy) * (j - cy));
                Blend(i, j, color, Mathf.Clamp01(d + 0.5f));
            }
        }
    }

    public void DrawLine(Vector2 a, Vector2 b, float thickness, Color color)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f);
        for (int s = 0; s <= steps; s++)
        {
            Vector2 p = Vector2.Lerp(a, b, s / (float)Mathf.Max(steps, 1));
            FillCircle(p.x, p.y, thickness / 2f, color);
        }
    }

    public void FillPolygon(Vector2[] points, Color color)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 p in points)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }
        for (int y = Mathf.FloorToInt(minY); y <= maxY; y++)
        {
            for (int x = Mathf.FloorToInt(minX); x <= maxX; x++)
            {
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2 a = points[i], b = points[j];
                    if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                if (inside) Blend(x, y, color, 1f);
            }
        }
    }

    public void Save(string path)
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.SetPixels(Pixels);
        texture.Apply();

        // Rewriting an unchanged file only risks tripping over whatever has it open right now.
        byte[] png = texture.EncodeToPNG();
        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png)) File.WriteAllBytes(path, png);
        UnityEngine.Object.DestroyImmediate(texture);
    }
}

// Generates the game's textures from the CC0 concrete and linoleum photos plus procedural patterns:
// low-res, dirty and photographic, the way early-2000s game textures looked.
public static partial class TextureForge
{
    public const string TextureDir = "Assets/Art/Generated/Textures";
    public const int BinderSpines = 8;

    // The middle of each painted eye on the clerk's atlas.
    public static readonly Vector2 ClerkEyeA = new Vector2(95f / 1024f, 1f - 102f / 1024f);
    public static readonly Vector2 ClerkEyeB = new Vector2(160f / 1024f, 1f - 102f / 1024f);

    const string ConcretePhoto = "Assets/Art/Textures/ConcreteWall006/concrete_wall_006_diff_2k.jpg";
    const string LinoleumPhoto = "Assets/Art/Textures/OldLinoleumFlooring01/old_linoleum_flooring_01_diff_2k.jpg";
    const string FontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    const string ClerkAtlas = "Assets/Art/Mixamo_NPC/Clerk_OrdinaryMan/OrdinaryManTexture_1024x1024.png";
    const string AdultAtlas = "Assets/Art/Mixamo_NPC/Waiting1_AdultMan/ADULT_Texture_HIGHRES.jpg";
    const string YoungAtlas = "Assets/Art/Mixamo_NPC/Waiting2_YoungMan/Texture_HIGHRES.png";

    static Canvas concrete;
    static float concreteMean;
    static readonly List<string> written = new List<string>();

    public static string Path(string name) => $"{TextureDir}/{name}.png";

    // Everything more than one scene uses.
    static void ForgeShared()
    {
        Directory.CreateDirectory(TextureDir);
        written.Clear();
        concrete = LoadPhoto(ConcretePhoto, 512);
        concreteMean = 0f;
        foreach (Color c in concrete.Pixels) concreteMean += c.grayscale;
        concreteMean /= concrete.Pixels.Length;
        FillPits(concrete, concreteMean);

        Write("T_Concrete", Concrete());
        Write("T_Stone", Painted(512, Rgb(228, 224, 214), 0.2f, 7, 0.05f));
        Write("T_Brick", Blocks(512, 16, 8, 3.2f, Rgb(96, 78, 68), Rgb(88, 84, 78), 0.2f, 11));
        Write("T_Pavers", Blocks(512, 12, 6, 2f, Rgb(116, 110, 102), Rgb(72, 68, 62), 0.1f, 23));
        Write("T_Wood", Wood());
        Write("T_Metal", Metal());
        Write("T_Paper", Painted(128, Rgb(214, 208, 190), 0.12f, 3, 0.03f));
        Write("T_RedPaint", Painted(128, Rgb(128, 34, 28), 0.3f, 4, 0.14f));
        Write("T_GrimeRising", GrimeRising());
        Write("T_GrimeStreaks", GrimeStreaks());
        Write("T_GrimeBlotch", GrimeBlotch());
        Write("T_UiShade", UiShade());
        Write("T_DoorGlass", Painted(64, Rgb(30, 34, 30), 0.3f, 3, 0.1f));
        Write("T_Clock", ClockFace());
    }

    public static void ForgeOpening()
    {
        ForgeShared();
        Write("T_Ochre", Ochre());
        Write("T_Folder", Folders());
        Write("T_Linoleum", Linoleum());

        Write("T_Plate1", NumberPlate("1"));
        Write("T_Plate2", NumberPlate("2"));
        Write("T_SignRegistry", Banner("РЕГИСТРАТУРА", 1024, 128, Rgb(34, 40, 52), Rgb(214, 206, 176), 84));
        Write("T_SignPlatform", ArrowSign("ПЕРОН", true));
        Write("T_SignPlatformBack", ArrowSign("ПЕРОН", false));
        Write("T_SignClosed", ClosedNotice());
        Write("T_NoticeBoard", NoticeBoard());
        Write("T_MetroMap", MetroMap(false));
        Write("T_Form", Form());
        Write("T_Timetable", Timetable());
        Write("T_SignFire", Banner("ПОЖАРЕН КРАН", 512, 176, Rgb(150, 36, 28), Rgb(232, 226, 208), 44));

        Write("T_NpcClerk", ClerkSuit());
        Write("T_NpcAdult", AdultCoat());
        Write("T_NpcYoung", YoungJacket());
        ImportWritten();
    }

    static void ImportWritten()
    {
        AssetDatabase.Refresh();
        foreach (string name in written) ConfigureImport(name);
    }

    static void Write(string name, Canvas canvas)
    {
        canvas.Save(Path(name));
        written.Add(name);
    }

    static void ConfigureImport(string name)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Path(name));
        bool isSurface = name != "T_Clock" && name != "T_GrimeBlotch" && !name.StartsWith("T_Plate") && !name.StartsWith("T_Sign")
            && !name.StartsWith("T_Npc") && !name.StartsWith("T_Ui");
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 0;
        importer.wrapMode = isSurface ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    static Canvas LoadPhoto(string path, int size)
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(path));
        Color32[] pixels = source.GetPixels32();
        int scale = source.width / size;
        var photo = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 sum = Vector3.zero;
                for (int j = 0; j < scale; j++)
                {
                    for (int i = 0; i < scale; i++)
                    {
                        Color32 c = pixels[(y * scale + j) * source.width + x * scale + i];
                        sum += new Vector3(c.r, c.g, c.b);
                    }
                }
                sum /= 255f * scale * scale;
                photo[x, y] = new Color(sum.x, sum.y, sum.z, 1f);
            }
        }
        UnityEngine.Object.DestroyImmediate(source);
        return photo;
    }

    static Canvas LoadAtlas(string path)
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(path));
        int size = Mathf.Min(source.width, 1024);
        var atlas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                atlas[x, y] = source.GetPixelBilinear((x + 0.5f) / size, (y + 0.5f) / size);
        UnityEngine.Object.DestroyImmediate(source);
        return atlas;
    }

    // Brightness of the concrete photo around 1, for dirtying anything else with real photo detail.
    static float Detail(float x, float y, float contrast)
    {
        float luma = concrete.Sample(x, y).grayscale / concreteMean;
        return Mathf.Lerp(1f, luma, contrast);
    }

    public static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }
    }

    static float Lattice(float x, float y, int periodX, int periodY, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        int Wrap(int v, int p) => ((v % p) + p) % p;
        int x0 = Wrap(ix, periodX), x1 = Wrap(ix + 1, periodX), y0 = Wrap(iy, periodY), y1 = Wrap(iy + 1, periodY);
        return Mathf.Lerp(Mathf.Lerp(Hash(x0, y0, seed), Hash(x1, y0, seed), fx),
            Mathf.Lerp(Hash(x0, y1, seed), Hash(x1, y1, seed), fx), fy);
    }

    // Tiling fractal noise over the unit square, around 0.5.
    static float Fractal(float u, float v, int periodX, int periodY, int octaves, int seed)
    {
        float sum = 0f, weight = 0.5f, total = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Lattice(u * periodX, v * periodY, periodX, periodY, seed + o) * weight;
            total += weight;
            weight *= 0.5f;
            periodX *= 2;
            periodY *= 2;
        }
        return sum / total;
    }

    // The photo's darkest pits would read as a stamp once tiled across a whole wall.
    static void FillPits(Canvas photo, float mean)
    {
        for (int i = 0; i < photo.Pixels.Length; i++)
        {
            float ratio = Mathf.Max(photo.Pixels[i].grayscale / mean, 0.01f);
            if (ratio < 1f) photo.Pixels[i] *= Mathf.Lerp(0.8f, 1f, ratio) / ratio;
        }
    }

    static Canvas Concrete()
    {
        Color albedo = Rgb(166, 163, 154);
        var canvas = new Canvas(512, 512, Color.black);
        for (int y = 0; y < 512; y++)
            for (int x = 0; x < 512; x++)
                canvas[x, y] = Opaque(albedo * Detail(x, y, 1f));
        return canvas;
    }

    static Color Opaque(Color c)
    {
        c.a = 1f;
        return c;
    }

    static Canvas Painted(int size, Color paint, float detail, int blotchPeriod, float blotch)
    {
        var canvas = new Canvas(size, size, paint);
        float toPhoto = 512f / size;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float stain = 1f + (Fractal((float)x / size, (float)y / size, blotchPeriod, blotchPeriod, 4, 5) - 0.5f) * 2f * blotch;
                canvas[x, y] = Opaque(paint * Detail(x * toPhoto, y * toPhoto, detail) * stain);
            }
        }
        return canvas;
    }

    static Canvas Ochre()
    {
        Canvas canvas = Painted(512, Rgb(170, 138, 70), 0.2f, 3, 0.08f);
        for (int y = 0; y < 512; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                // Vertical damp streaks, the paint's one concession to the subway being wet.
                float streak = Fractal((float)x / 512f, (float)y / 512f, 22, 2, 3, 41);
                float damp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.75f, streak));
                canvas[x, y] = Opaque(canvas[x, y] * Mathf.Lerp(1f, 0.92f, damp));
            }
        }
        return canvas;
    }

    // Running-bond blocks: bricks and pavers. Every block samples its own patch of the concrete photo,
    // so it reads as rough stone rather than a flat pattern.
    static Canvas Blocks(int size, int rows, int columns, float mortarPixels, Color block, Color mortar, float variance, int seed)
    {
        var canvas = new Canvas(size, size, mortar);
        float cellW = (float)size / columns, cellH = (float)size / rows;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int row = Mathf.FloorToInt(y / cellH);
                float bx = x / cellW + (row % 2) * 0.5f;
                int column = Mathf.FloorToInt(bx) % columns;
                float fu = bx - Mathf.Floor(bx), fv = y / cellH - row;

                float wobble = (Fractal((float)x / size, (float)y / size, 24, 24, 3, seed) - 0.5f) * mortarPixels * 2.2f;
                float edge = Mathf.Min(Mathf.Min(fu, 1f - fu) * cellW, Mathf.Min(fv, 1f - fv) * cellH) + wobble;
                float face = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(mortarPixels * 0.6f, mortarPixels * 1.6f, edge));
                float bevel = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(edge / (mortarPixels * 4f)));

                float shade = 1f + (Hash(column, row, seed) - 0.5f) * 2f * variance;
                float offsetX = Hash(column, row, seed + 1) * 512f, offsetY = Hash(column, row, seed + 2) * 512f;
                float rough = Detail(x * 512f / size + offsetX, y * 512f / size + offsetY, 0.85f);
                Color warm = Color.Lerp(block, block * new Color(1.06f, 0.99f, 0.93f), Hash(column, row, seed + 3));

                Color stone = warm * shade * rough * bevel;
                Color joint = mortar * Detail(x * 512f / size, y * 512f / size, 0.6f);
                canvas[x, y] = Opaque(Color.Lerp(joint, stone, face));
            }
        }
        return canvas;
    }

    static Canvas Wood()
    {
        var canvas = new Canvas(256, 256, Color.black);
        Color light = Rgb(142, 114, 84), dark = Rgb(80, 62, 46);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float u = x / 256f, v = y / 256f;
                float grain = Fractal(u, v, 2, 26, 4, 7);
                float rings = Mathf.Abs(Mathf.Sin((v + Fractal(u, v, 3, 3, 2, 9) * 0.35f) * Mathf.PI * 18f));
                float t = Mathf.Clamp01(grain * 1.3f - 0.15f - rings * 0.16f);
                canvas[x, y] = Opaque(Color.Lerp(dark, light, t) * Detail(x * 2f, y * 2f, 0.3f));
            }
        }
        return canvas;
    }

    // Behind the conversation text: dark along the bottom of the screen, clear by the top.
    static Canvas UiShade()
    {
        var canvas = new Canvas(4, 128, Color.clear);
        for (int y = 0; y < 128; y++)
        {
            float t = 1f - y / 127f;
            Color shade = new Color(0f, 0f, 0f, t * t * (3f - 2f * t) * 0.85f);
            for (int x = 0; x < 4; x++) canvas[x, y] = shade;
        }
        return canvas;
    }

    static readonly Color Soot = new Color(0.5f, 0.46f, 0.41f, 1f);

    // Dark along the bottom row, fading to white at the top with a torn, uneven edge. Tiles across.
    static Canvas GrimeRising()
    {
        var canvas = new Canvas(512, 256, Color.white);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float u = x / 512f, v = y / 255f;
                float tatter = (Fractal(u, v, 7, 3, 4, 51) - 0.5f) * 0.9f;
                float depth = Mathf.Clamp01(1f - (v + tatter * Mathf.Sin(v * Mathf.PI)));
                depth = depth * depth * (3f - 2f * depth) * (1f - v * v);
                canvas[x, y] = Opaque(Color.Lerp(Color.white, Soot, depth));
            }
        }
        return canvas;
    }

    // Long drips hanging from the top row, gone by the bottom. Tiles across.
    static Canvas GrimeStreaks()
    {
        var canvas = new Canvas(512, 256, Color.white);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float u = x / 512f, v = y / 255f;
                float drips = Mathf.InverseLerp(0.46f, 0.78f, Fractal(u, v * 0.3f, 34, 2, 3, 61));
                float reach = Mathf.Pow(v, 1.6f) * (0.6f + Fractal(u, 0.5f, 9, 1, 2, 67) * 0.8f);
                float ends = Mathf.Clamp01(v * 3f) * Mathf.Clamp01((1f - v) * 8f);
                canvas[x, y] = Opaque(Color.Lerp(Color.white, Soot, Mathf.Clamp01(drips * reach) * ends));
            }
        }
        return canvas;
    }

    // A soft uneven patch, white right out to its borders.
    static Canvas GrimeBlotch()
    {
        var canvas = new Canvas(256, 256, Color.white);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float u = x / 255f, v = y / 255f;
                float r = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float noise = Fractal(u, v, 4, 4, 5, 71);
                float strength = Mathf.Clamp01((1f - r) * 1.2f - (noise - 0.5f) * 1.5f - 0.1f);
                strength = Mathf.Min(strength, Mathf.Clamp01((1f - r) * 3f));
                canvas[x, y] = Opaque(Color.Lerp(Color.white, Soot, strength * strength * (3f - 2f * strength) * 0.55f));
            }
        }
        return canvas;
    }

    static Canvas Metal()
    {
        Canvas canvas = Painted(256, Rgb(94, 102, 96), 0.4f, 5, 0.14f);
        Color rust = Rgb(72, 58, 46);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float chips = Fractal(x / 256f, y / 256f, 9, 9, 4, 17);
                canvas[x, y] = Opaque(Color.Lerp(canvas[x, y], rust * Detail(x * 2f, y * 2f, 0.5f), Mathf.InverseLerp(0.66f, 0.76f, chips) * 0.8f));
            }
        }
        return canvas;
    }

    static Canvas Folders()
    {
        Color[] covers =
        {
            Rgb(96, 88, 74), Rgb(112, 96, 68), Rgb(70, 76, 84), Rgb(88, 62, 52),
            Rgb(120, 112, 96), Rgb(64, 72, 62), Rgb(104, 92, 80), Rgb(58, 56, 56)
        };
        var canvas = new Canvas(512, 256, Color.black);
        int width = canvas.Width / BinderSpines;
        for (int i = 0; i < BinderSpines; i++)
        {
            int x = i * width;
            canvas.FillRect(x, 0, width, 256, covers[i]);
            canvas.FillRect(x + 8, 136, width - 16, 74, Rgb(206, 200, 182));
            for (int line = 0; line < 3; line++)
                canvas.FillRect(x + 14, 190 - line * 14, width - 28 - (i + line) % 3 * 6, 3, Rgb(52, 50, 48), 0.8f);
            canvas.FillCircle(x + width / 2f, 62f, 11f, Rgb(36, 32, 30));
            canvas.FillRect(x, 0, 3, 256, Rgb(30, 26, 24));
            canvas.FillRect(x + width - 3, 0, 3, 256, Rgb(30, 26, 24));
        }
        return Grimed(canvas, 0.3f);
    }

    static Canvas Linoleum()
    {
        Canvas photo = LoadPhoto(LinoleumPhoto, 512);
        var canvas = new Canvas(512, 512, Color.black);
        for (int i = 0; i < canvas.Pixels.Length; i++)
        {
            Color c = photo.Pixels[i];
            canvas.Pixels[i] = Opaque(Color.Lerp(c, new Color(c.grayscale, c.grayscale, c.grayscale), 0.7f) * 0.62f);
        }
        return canvas;
    }

    // Layers photo dirt and darker edges over a clean sign so it looks handled for decades.
    static Canvas Grimed(Canvas canvas, float amount)
    {
        float toPhoto = 512f / canvas.Width;
        for (int y = 0; y < canvas.Height; y++)
        {
            for (int x = 0; x < canvas.Width; x++)
            {
                float u = (float)x / canvas.Width, v = (float)y / canvas.Height;
                float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                float rim = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(edge * 12f));
                float stain = Mathf.Lerp(1f, 0.55f + Fractal(u, v, 5, 5, 4, 31) * 0.9f, amount);
                canvas[x, y] = Opaque(canvas[x, y] * Detail(x * toPhoto, y * toPhoto, amount) * stain * rim);
            }
        }
        return canvas;
    }

    static Font labelFont;
    static Canvas glyphAtlas;
    const FontStyle LabelStyle = FontStyle.Bold;

    static void PrepareGlyphs(string text, int size)
    {
        if (labelFont == null) labelFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        labelFont.RequestCharactersInTexture(text, size, LabelStyle);

        Texture atlas = labelFont.material.mainTexture;
        var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(atlas, target);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var readable = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);

        glyphAtlas = new Canvas(atlas.width, atlas.height, Color.clear);
        Array.Copy(readable.GetPixels(), glyphAtlas.Pixels, glyphAtlas.Pixels.Length);
        UnityEngine.Object.DestroyImmediate(readable);
    }

    static float MeasureText(string text, int size, float tracking)
    {
        float width = 0f;
        foreach (char c in text)
        {
            labelFont.GetCharacterInfo(c, out CharacterInfo info, size, LabelStyle);
            width += info.advance + tracking;
        }
        return width - tracking;
    }

    // Draws text with its baseline at y, horizontally centred on cx.
    static void DrawText(Canvas canvas, string text, float cx, float y, int size, Color color, float tracking = 0f)
    {
        PrepareGlyphs(text, size);
        float pen = cx - MeasureText(text, size, tracking) / 2f;
        foreach (char c in text)
        {
            labelFont.GetCharacterInfo(c, out CharacterInfo info, size, LabelStyle);
            int x0 = Mathf.FloorToInt(pen + info.minX), x1 = Mathf.CeilToInt(pen + info.maxX);
            int y0 = Mathf.FloorToInt(y + info.minY), y1 = Mathf.CeilToInt(y + info.maxY);
            for (int py = y0; py < y1; py++)
            {
                for (int px = x0; px < x1; px++)
                {
                    float s = (px + 0.5f - pen - info.minX) / Mathf.Max(info.maxX - info.minX, 1);
                    float t = (py + 0.5f - y - info.minY) / Mathf.Max(info.maxY - info.minY, 1);
                    if (s < 0f || s > 1f || t < 0f || t > 1f) continue;
                    Vector2 uv = Vector2.Lerp(Vector2.Lerp(info.uvBottomLeft, info.uvBottomRight, s),
                        Vector2.Lerp(info.uvTopLeft, info.uvTopRight, s), t);
                    float coverage = glyphAtlas.Sample(uv.x * glyphAtlas.Width - 0.5f, uv.y * glyphAtlas.Height - 0.5f).a;
                    canvas.Blend(px, py, color, coverage);
                }
            }
            pen += info.advance + tracking;
        }
    }

    static Canvas NumberPlate(string digit)
    {
        var canvas = new Canvas(192, 256, Rgb(214, 210, 196));
        canvas.FillRect(0, 0, 192, 6, Rgb(40, 38, 34));
        canvas.FillRect(0, 250, 192, 6, Rgb(40, 38, 34));
        canvas.FillRect(0, 0, 6, 256, Rgb(40, 38, 34));
        canvas.FillRect(186, 0, 6, 256, Rgb(40, 38, 34));
        DrawText(canvas, digit, 96f, 52f, 210, Rgb(18, 18, 18));
        return Grimed(canvas, 0.5f);
    }

    static Canvas Banner(string text, int width, int height, Color background, Color ink, int size)
    {
        var canvas = new Canvas(width, height, background);
        canvas.FillRect(6, 6, width - 12, 3, ink, 0.7f);
        canvas.FillRect(6, height - 9, width - 12, 3, ink, 0.7f);
        DrawText(canvas, text, width / 2f, height * 0.5f - size * 0.36f, size, ink, size * 0.12f);
        return Grimed(canvas, 0.55f);
    }

    static Canvas ArrowSign(string text, bool arrowRight)
    {
        Color ground = Rgb(36, 52, 84), ink = Rgb(226, 220, 200);
        var canvas = new Canvas(512, 160, ground);
        DrawText(canvas, text, arrowRight ? 208f : 304f, 44f, 104, ink, 10f);
        Vector2 Point(float x, float y) => new Vector2(arrowRight ? x : 512f - x, y);
        canvas.FillPolygon(new[]
        {
            Point(388f, 98f), Point(436f, 98f), Point(436f, 126f), Point(492f, 80f),
            Point(436f, 34f), Point(436f, 62f), Point(388f, 62f)
        }, ink);
        return Grimed(canvas, 0.5f);
    }

    static Canvas ClosedNotice()
    {
        var canvas = new Canvas(256, 128, Rgb(202, 194, 170));
        DrawText(canvas, "ЗАТВОРЕНО", 128f, 50f, 38, Rgb(120, 26, 22), 1f);
        canvas.FillRect(44, 36, 168, 4, Rgb(120, 26, 22));
        return Grimed(canvas, 0.4f);
    }

    static void TextLines(Canvas canvas, int x, int y, int width, int lines, int pitch, int seed)
    {
        for (int i = 0; i < lines; i++)
        {
            int length = Mathf.RoundToInt(width * (0.55f + Hash(i, seed, 2) * 0.45f));
            canvas.FillRect(x, y - i * pitch, length, 2, Rgb(46, 44, 42), 0.75f);
        }
    }

    static Canvas NoticeBoard()
    {
        var canvas = new Canvas(512, 360, Rgb(92, 72, 52));
        for (int y = 0; y < 360; y++)
            for (int x = 0; x < 512; x++)
                canvas[x, y] = Opaque(canvas[x, y] * (0.7f + Hash(x / 2, y / 2, 3) * 0.6f));

        Color paper = Rgb(206, 200, 180), yellowed = Rgb(196, 180, 136), pink = Rgb(190, 150, 136);
        canvas.FillRect(24, 110, 176, 226, paper);
        DrawText(canvas, "СЪОБЩЕНИЕ", 112f, 306f, 22, Rgb(30, 30, 30));
        TextLines(canvas, 38, 288, 148, 15, 11, 1);
        canvas.FillCircle(150f, 134f, 15f, Rgb(70, 66, 120));
        canvas.FillCircle(150f, 134f, 11f, paper);

        canvas.FillRect(218, 176, 142, 160, yellowed);
        DrawText(canvas, "ГРАФИК", 288f, 310f, 20, Rgb(30, 30, 30));
        TextLines(canvas, 230, 292, 118, 10, 11, 2);

        canvas.FillRect(376, 148, 116, 188, paper);
        TextLines(canvas, 386, 322, 96, 15, 11, 3);

        canvas.FillRect(44, 26, 170, 66, pink);
        DrawText(canvas, "ЧАКАЛНЯ", 128f, 48f, 26, Rgb(40, 26, 24));

        canvas.FillRect(238, 30, 236, 120, yellowed);
        TextLines(canvas, 252, 136, 208, 9, 11, 4);

        foreach (Vector2 pin in new[] { new Vector2(112f, 332f), new Vector2(288f, 332f), new Vector2(434f, 332f), new Vector2(128f, 88f), new Vector2(356f, 146f) })
            canvas.FillCircle(pin.x, pin.y, 3f, Rgb(120, 30, 26));
        return Grimed(canvas, 0.45f);
    }

    static Canvas Timetable()
    {
        var canvas = new Canvas(512, 364, Rgb(204, 198, 176));
        canvas.FillRect(0, 312, 512, 52, Rgb(36, 44, 62));
        DrawText(canvas, "РАЗПИСАНИЕ", 256f, 326f, 34, Rgb(226, 220, 200), 3f);
        DrawText(canvas, "НОЩНА ЛИНИЯ", 256f, 282f, 20, Rgb(40, 38, 36));
        for (int column = 0; column < 4; column++)
        {
            int x = 34 + column * 116;
            canvas.FillRect(x - 12, 22, 1, 248, Rgb(60, 56, 52), column == 0 ? 0f : 0.7f);
            for (int row = 0; row < 11; row++)
            {
                int minutes = 23 * 60 + 4 + (column * 11 + row) * 13;
                DrawText(canvas, $"{minutes / 60 % 24:00}:{minutes % 60:00}", x + 40, 252 - row * 21, 15, Rgb(46, 44, 42));
            }
        }
        return Grimed(canvas, 0.5f);
    }

    // The platform's copy has one stop that the registry's doesn't: the wrong detail on Shift 1.
    static Canvas MetroMap(bool extraStop)
    {
        var canvas = new Canvas(512, 384, Rgb(206, 198, 172));
        canvas.FillRect(0, 330, 512, 54, Rgb(36, 44, 62));
        DrawText(canvas, "МЕТРОПОЛИТЕН", 256f, 344f, 38, Rgb(222, 216, 196), 3f);

        void Line(Color color, params Vector2[] stops)
        {
            for (int i = 0; i < stops.Length - 1; i++) canvas.DrawLine(stops[i], stops[i + 1], 8f, color);
            foreach (Vector2 stop in stops)
            {
                canvas.FillCircle(stop.x, stop.y, 7f, Rgb(30, 30, 30));
                canvas.FillCircle(stop.x, stop.y, 4f, Rgb(226, 220, 200));
            }
        }

        Line(Rgb(150, 44, 36), new Vector2(40f, 70f), new Vector2(120f, 110f), new Vector2(200f, 170f), new Vector2(280f, 170f), new Vector2(360f, 230f), new Vector2(470f, 280f));
        if (extraStop) Line(Rgb(150, 44, 36), new Vector2(415f, 255f));
        Line(Rgb(44, 72, 120), new Vector2(60f, 290f), new Vector2(140f, 240f), new Vector2(200f, 170f), new Vector2(240f, 100f), new Vector2(330f, 60f));
        Line(Rgb(176, 140, 48), new Vector2(280f, 170f), new Vector2(340f, 110f), new Vector2(430f, 90f));
        return Grimed(canvas, 0.45f);
    }

    static Canvas ClockFace()
    {
        var canvas = new Canvas(256, 256, Rgb(20, 20, 20));
        canvas.FillCircle(128f, 128f, 118f, Rgb(216, 212, 196));
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            Vector2 dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            canvas.DrawLine(new Vector2(128f, 128f) + dir * 92f, new Vector2(128f, 128f) + dir * 110f, i % 3 == 0 ? 8f : 4f, Rgb(24, 24, 24));
        }
        Vector2 Hand(float turns, float length) => new Vector2(128f, 128f) + new Vector2(Mathf.Sin(turns * Mathf.PI * 2f), Mathf.Cos(turns * Mathf.PI * 2f)) * length;
        canvas.DrawLine(new Vector2(128f, 128f), Hand(4.2f / 12f, 62f), 8f, Rgb(20, 20, 20));
        canvas.DrawLine(new Vector2(128f, 128f), Hand(0.2f, 94f), 5f, Rgb(20, 20, 20));
        canvas.FillCircle(128f, 128f, 7f, Rgb(20, 20, 20));
        return Grimed(canvas, 0.35f);
    }

    static Canvas Form()
    {
        var canvas = new Canvas(128, 180, Rgb(216, 212, 196));
        DrawText(canvas, "ФОРМУЛЯР", 64f, 156f, 15, Rgb(26, 26, 26));
        canvas.FillRect(10, 148, 108, 1, Rgb(26, 26, 26));
        for (int i = 0; i < 9; i++)
        {
            canvas.FillRect(12, 132 - i * 13, 34, 2, Rgb(46, 44, 42), 0.8f);
            canvas.FillRect(52, 132 - i * 13, 64, 1, Rgb(46, 44, 42), 0.6f);
        }
        return Grimed(canvas, 0.2f);
    }

    // Grows painted UV islands into the empty background so mipmaps don't bleed it in along seams.
    static void BleedIntoBackground(Canvas atlas, Func<Color, bool> isEmpty, int iterations)
    {
        var empty = new bool[atlas.Pixels.Length];
        for (int i = 0; i < empty.Length; i++) empty[i] = isEmpty(atlas.Pixels[i]);
        for (int pass = 0; pass < iterations; pass++)
        {
            var next = (bool[])empty.Clone();
            var result = (Color[])atlas.Pixels.Clone();
            for (int y = 1; y < atlas.Height - 1; y++)
            {
                for (int x = 1; x < atlas.Width - 1; x++)
                {
                    int i = y * atlas.Width + x;
                    if (!empty[i]) continue;
                    Color sum = Color.clear;
                    int count = 0;
                    foreach (int n in new[] { i - 1, i + 1, i - atlas.Width, i + atlas.Width })
                    {
                        if (empty[n]) continue;
                        sum += atlas.Pixels[n];
                        count++;
                    }
                    if (count == 0) continue;
                    result[i] = sum / count;
                    next[i] = false;
                }
            }
            Array.Copy(result, atlas.Pixels, result.Length);
            empty = next;
        }
    }

    static void Dirty(Canvas atlas, float amount)
    {
        float toPhoto = 512f / atlas.Width;
        for (int y = 0; y < atlas.Height; y++)
            for (int x = 0; x < atlas.Width; x++)
                atlas[x, y] = Opaque(atlas[x, y] * Detail(x * toPhoto, y * toPhoto, amount));
    }

    // Keeps the shading painted into a garment while swapping its colour.
    static Color Reshade(Color source, float sourceLuma, Color cloth)
    {
        return Opaque(cloth * Mathf.Clamp(source.grayscale / sourceLuma, 0.35f, 1.5f));
    }

    static bool Near(Color c, Color target, float tolerance)
    {
        return Mathf.Abs(c.r - target.r) + Mathf.Abs(c.g - target.g) + Mathf.Abs(c.b - target.b) < tolerance;
    }

    static bool InPixels(int x, int y, int xMin, int yMinFromTop, int xMax, int yMaxFromTop, int size)
    {
        return x >= xMin && x <= xMax && size - 1 - y >= yMinFromTop && size - 1 - y <= yMaxFromTop;
    }

    // UV coordinates below are read off the 1024px atlas, measured from its top-left corner.
    static Canvas ClerkSuit()
    {
        Canvas atlas = LoadAtlas(ClerkAtlas);
        int s = atlas.Width;
        Color jacket = Rgb(60, 58, 58), trousers = Rgb(50, 48, 48), shirt = Rgb(206, 204, 194), skinShift = new Color(0.94f, 0.88f, 0.82f);
        Color greyHair = Rgb(112, 108, 104), scalp = Rgb(204, 156, 122);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = atlas[x, y];
                if (c.r > 0.97f && c.g > 0.97f && c.b > 0.97f) continue;

                bool denim = c.b > c.r * 1.25f;
                bool sleeveIsland = InPixels(x, y, 618, 30, 750, 290, s) || InPixels(x, y, 0, 585, 140, 835, s);
                bool torsoIsland = InPixels(x, y, 425, 25, 618, 315, s) || InPixels(x, y, 136, 540, 440, 835, s);
                bool armStrip = InPixels(x, y, 90, 850, 668, 1024, s);
                bool neck = c.r > c.b * 1.4f && (InPixels(x, y, 245, 540, 330, 600, s) || InPixels(x, y, 480, 30, 585, 60, s));

                // Grey at the sides and back of the head, bald on top.
                bool scalpArea = InPixels(x, y, 0, 0, 270, 505, s) && !InPixels(x, y, 62, 74, 200, 250, s);
                bool brownHair = scalpArea && c.grayscale < 0.42f;
                bool crown = InPixels(x, y, 30, 0, 230, 74, s) || InPixels(x, y, 30, 255, 230, 342, s);

                if (denim) atlas[x, y] = Reshade(c, 0.46f, trousers);
                else if (neck) atlas[x, y] = Opaque(c * skinShift);
                else if (sleeveIsland || torsoIsland || armStrip) atlas[x, y] = Reshade(c.grayscale > 0.45f ? c : new Color(0.7f, 0.7f, 0.7f), 0.72f, jacket);
                else if (brownHair) atlas[x, y] = Opaque((crown ? scalp : greyHair) * Mathf.Lerp(0.9f, 1.05f, Hash(x, y, 9)));
                else atlas[x, y] = Opaque(c * skinShift);
            }
        }

        // Lapels, shirt front and tie, on the lower torso island: that one is the chest.
        void FromTop(int x0, int y0, int x1, int y1, Color color) => atlas.FillRect(x0, s - y1, x1 - x0, y1 - y0, color);
        const int middle = 288, vTop = 576, vBottom = 742;
        for (int yTop = vTop; yTop < vBottom; yTop++)
        {
            int half = Mathf.RoundToInt(Mathf.Lerp(52f, 2f, (yTop - (float)vTop) / (vBottom - vTop)));
            FromTop(middle - half - 7, yTop, middle + half + 7, yTop + 1, Rgb(34, 32, 32));
            FromTop(middle - half, yTop, middle + half, yTop + 1, shirt);
        }
        for (int yTop = vTop + 2; yTop < vBottom - 4; yTop++)
        {
            int half = yTop < vTop + 18 ? 11 : Mathf.RoundToInt(Mathf.Lerp(6f, 13f, (yTop - vTop - 18f) / (vBottom - vTop - 22f)));
            bool stripe = (yTop / 9) % 2 == 0;
            FromTop(middle - half, yTop, middle + half, yTop + 1, stripe ? Rgb(92, 44, 42) : Rgb(142, 118, 96));
        }

        BleedIntoBackground(atlas, c => c.r > 0.97f && c.g > 0.97f && c.b > 0.97f, 10);
        Dirty(atlas, 0.28f);
        return atlas;
    }

    static Canvas AdultCoat()
    {
        Canvas atlas = LoadAtlas(AdultAtlas);
        int s = atlas.Width;
        Color coat = Rgb(70, 60, 50), trousers = Rgb(50, 48, 46), collar = Rgb(150, 144, 130), boots = Rgb(30, 24, 22), hair = Rgb(74, 68, 62);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = atlas[x, y];
                if (c.r + c.g + c.b < 0.1f) continue;

                float u = (float)x / s, vTop = 1f - (float)y / s;
                bool sleeve = u < 0.215f && vTop > 0.6f;
                if (c.r > 0.6f && c.g < 0.45f && c.b < 0.15f) atlas[x, y] = Reshade(c, 0.42f, coat);
                else if (c.g > c.r * 1.1f && c.g > c.b * 1.3f) atlas[x, y] = Reshade(c, 0.26f, trousers);
                else if (sleeve) atlas[x, y] = Reshade(c, 0.86f, coat);
                else if (vTop < 0.09f && u > 0.45f) atlas[x, y] = Reshade(c, 0.8f, collar);
                else if (vTop > 0.8f && u > 0.14f && u < 0.42f) atlas[x, y] = Reshade(c, 0.18f, boots);
                else if (c.r < 0.62f && c.g < 0.45f && vTop < 0.4f && u < 0.4f) atlas[x, y] = Reshade(c, 0.4f, hair);
                else atlas[x, y] = Opaque(c * new Color(0.92f, 0.85f, 0.78f));
            }
        }
        BleedIntoBackground(atlas, c => c.r + c.g + c.b < 0.1f, 10);
        Dirty(atlas, 0.3f);
        return atlas;
    }

    static Canvas YoungJacket()
    {
        Canvas atlas = LoadAtlas(YoungAtlas);
        int s = atlas.Width;
        Color jacket = Rgb(58, 64, 70), trousers = Rgb(44, 42, 44), shirt = Rgb(150, 146, 136), boots = Rgb(34, 26, 24), hair = Rgb(30, 24, 20), belt = Rgb(40, 32, 26);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = atlas[x, y];
                if (c.r + c.g + c.b < 0.1f) continue;

                float u = (float)x / s, vTop = 1f - (float)y / s;
                bool green = c.g > c.r * 1.2f && c.g > c.b * 1.5f;
                bool blue = c.b > c.r * 1.8f && c.b > 0.35f;
                bool red = c.r > c.g * 1.7f && c.r > c.b * 1.7f;
                bool white = c.r > 0.82f && c.g > 0.82f && c.b > 0.8f;

                if (green) atlas[x, y] = Reshade(c, 0.42f, vTop > 0.68f ? trousers : jacket);
                else if (blue && u < 0.4f && vTop < 0.32f) atlas[x, y] = Reshade(c, 0.3f, hair);
                else if (red) atlas[x, y] = Reshade(c, 0.32f, vTop > 0.8f ? boots : belt);
                else if (white) atlas[x, y] = Reshade(c, 0.92f, u < 0.22f ? jacket : shirt);
                else atlas[x, y] = Opaque(c * new Color(0.92f, 0.85f, 0.78f));
            }
        }
        BleedIntoBackground(atlas, c => c.r + c.g + c.b < 0.1f, 10);
        Dirty(atlas, 0.3f);
        return atlas;
    }
}
