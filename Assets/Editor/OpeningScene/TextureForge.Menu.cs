using System.IO;
using UnityEditor;
using UnityEngine;

// The title lettering, in the same bold face as the station's signs and with the tile band's yellow under it.
public static partial class TextureForge
{
    const string LogoPath = "Assets/Resources/Menu/T_Logo.png";
    const int LogoWidth = 1536, LogoHeight = 384;

    public static void ForgeMenu()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogoPath));

        // Transparent white, so that the soft edges of the letters don't pick up a dark fringe.
        var canvas = new Canvas(LogoWidth, LogoHeight, new Color(1f, 1f, 1f, 0f));
        Color cream = Rgb(226, 220, 200), amber = Rgb(204, 156, 62);

        const int titleSize = 150, nameSize = 46;
        const float titleTracking = 15f, nameTracking = 34f, titleBaseline = 168f;
        DrawTextFrom(canvas, 0f, titleBaseline, "МЕТРОПОЛИТЕН", titleSize, cream, titleTracking);
        float titleWidth = MeasureText("МЕТРОПОЛИТЕН", titleSize, titleTracking);
        canvas.FillRect(6, 122, Mathf.RoundToInt(titleWidth) - 10, 5, amber);
        DrawTextFrom(canvas, 4f, 54f, "METROPOLITEN", nameSize, amber, nameTracking);

        // A little wear, like paint that's been on a wall for a long time.
        for (int i = 0; i < canvas.Pixels.Length; i++)
        {
            float x = i % LogoWidth, y = i / LogoWidth;
            float wear = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f + Mathf.PerlinNoise(x * 0.31f, y * 0.31f) * 0.4f;
            canvas.Pixels[i].a *= Mathf.Lerp(0.72f, 1f, Mathf.SmoothStep(0.26f, 0.5f, wear));
        }

        canvas.Save(LogoPath);
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    // Draws text with its first letter at x, rather than centred.
    static void DrawTextFrom(Canvas canvas, float x, float y, string text, int size, Color color, float tracking)
    {
        PrepareGlyphs(text, size);
        DrawText(canvas, text, x + MeasureText(text, size, tracking) / 2f, y, size, color, tracking);
    }
}
