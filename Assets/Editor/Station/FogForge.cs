using UnityEditor;
using UnityEngine;

// The block of noise that fog and cloud are cut from: soft billows with finer detail eating into them. It tiles in all
// three directions, so that it can drift for as long as the game runs.
public static class FogForge
{
    const string NoisePath = "Assets/Art/Generated/T_FogNoise.asset";
    const int Size = 64;

    static float Hash(int x, int y, int z, int period, int seed)
    {
        x = (x % period + period) % period;
        y = (y % period + period) % period;
        z = (z % period + period) % period;
        return TextureForge.Hash(x + z * 131, y + z * 67, seed);
    }

    static float Value(float x, float y, float z, int period, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
        float fx = x - ix, fy = y - iy, fz = z - iz;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        fz = fz * fz * (3f - 2f * fz);

        float Plane(int k) => Mathf.Lerp(
            Mathf.Lerp(Hash(ix, iy, k, period, seed), Hash(ix + 1, iy, k, period, seed), fx),
            Mathf.Lerp(Hash(ix, iy + 1, k, period, seed), Hash(ix + 1, iy + 1, k, period, seed), fx), fy);
        return Mathf.Lerp(Plane(iz), Plane(iz + 1), fz);
    }

    // Distance to the nearest of a scatter of points, turned inside out so that each point is the middle of a billow.
    static float Billow(float x, float y, float z, int period, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
        float nearest = 3f;
        for (int k = iz - 1; k <= iz + 1; k++)
        {
            for (int j = iy - 1; j <= iy + 1; j++)
            {
                for (int i = ix - 1; i <= ix + 1; i++)
                {
                    float dx = i + Hash(i, j, k, period, seed) - x, dy = j + Hash(i, j, k, period, seed + 1) - y, dz = k + Hash(i, j, k, period, seed + 2) - z;
                    nearest = Mathf.Min(nearest, dx * dx + dy * dy + dz * dz);
                }
            }
        }
        return 1f - Mathf.Clamp01(Mathf.Sqrt(nearest));
    }

    static float Sample(float u, float v, float w)
    {
        float cloud = 0f, weight = 0.5f, total = 0f;
        for (int octave = 0, period = 4; octave < 5; octave++, period *= 2)
        {
            cloud += Value(u * period, v * period, w * period, period, 401 + octave) * weight;
            total += weight;
            weight *= 0.5f;
        }
        cloud /= total;

        float billows = Billow(u * 5f, v * 5f, w * 5f, 5, 411) * 0.62f + Billow(u * 11f, v * 11f, w * 11f, 11, 421) * 0.38f;
        return Mathf.Clamp01(Mathf.InverseLerp(0.28f, 0.72f, cloud * 0.6f + billows * 0.4f));
    }

    public static Texture3D Noise()
    {
        var made = AssetDatabase.LoadAssetAtPath<Texture3D>(NoisePath);
        if (made != null) return made;

        var pixels = new byte[Size * Size * Size];
        for (int w = 0; w < Size; w++)
            for (int v = 0; v < Size; v++)
                for (int u = 0; u < Size; u++)
                    pixels[(w * Size + v) * Size + u] = (byte)(Sample((float)u / Size, (float)v / Size, (float)w / Size) * 255f);

        var texture = new Texture3D(Size, Size, Size, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "T_FogNoise" };
        texture.SetPixelData(pixels, 0);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, NoisePath);
        return texture;
    }
}
