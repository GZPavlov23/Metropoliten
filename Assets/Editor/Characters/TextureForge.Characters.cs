using System;
using UnityEngine;

// Paints each person's texture: the same rectangles the mesh is laid out on, one part at a time.
// On a body part, across runs round the body with the front in the middle, and along runs the way the part was built.
public static partial class TextureForge
{
    public static string CharacterTexture(Look look) => $"T_Npc{look.Name}";

    public static void ForgeCast()
    {
        written.Clear();
        foreach (Look look in Cast.Everyone) Write(CharacterTexture(look), PaintPerson(look));
        ImportWritten();
    }

    static float Bell(float x, float width) => Mathf.Exp(-(x / width) * (x / width));
    static float Band(float x, float from, float to, float soft) => Mathf.Clamp01((x - from) / soft) * Mathf.Clamp01((to - x) / soft);

    static void PaintArea(Canvas canvas, Rect area, Func<float, float, Color> shade)
    {
        int x0 = Mathf.RoundToInt(area.x * canvas.Width), y0 = Mathf.RoundToInt(area.y * canvas.Height);
        int width = Mathf.RoundToInt(area.width * canvas.Width), height = Mathf.RoundToInt(area.height * canvas.Height);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                canvas[x0 + x, y0 + y] = shade((x + 0.5f) / width, (y + 0.5f) / height);
    }

    // Cloth at this size is a colour with a slow unevenness in it and a fine grain over the top.
    static Color Fabric(Color color, float across, float along, int seed, float strength = 1f)
    {
        float slow = Lattice(across * 7f, along * 7f, 7, 7, seed) - 0.5f;
        float grain = Hash(Mathf.FloorToInt(across * 300f), Mathf.FloorToInt(along * 300f), seed) - 0.5f;
        color *= 1f + (slow * 0.3f + grain * 0.1f) * strength;
        color.a = 1f;
        return color;
    }

    static Color PaintTop(Look look, float across, float along)
    {
        bool longBody = look.Top == Top.Coat || look.Top == Top.Nurse;
        float up = longBody ? Mathf.InverseLerp(0.42f, 1f, along) : along;
        float off = Mathf.Abs(across - 0.5f), left = across - 0.5f;

        Color cloth = look.TopColor;
        const float brass = 0.8f;
        void Button(float x, float y, Color color)
        {
            float d = new Vector2((off - x) * 3f, up - y).magnitude;
            if (d < 0.013f) cloth = color;
            else if (d < 0.017f) cloth *= 0.65f;
        }

        switch (look.Top)
        {
            case Top.Suit:
            {
                float open = Mathf.Lerp(0f, 0.11f, Mathf.InverseLerp(0.42f, 0.95f, up));
                if (up > 0.42f && off < open)
                {
                    cloth = look.Under;
                    if (look.Tie)
                    {
                        float tieWidth = up > 0.86f ? 0.013f : Mathf.Lerp(0.02f, 0.012f, Mathf.InverseLerp(0.45f, 0.86f, up));
                        if (off < tieWidth) cloth = look.TieColor;
                    }
                }
                else if (up > 0.42f && off < open + 0.026f)
                {
                    cloth = look.TopColor * 1.2f;
                }
                if (off < 0.003f && up < 0.42f) cloth *= 0.6f;
                for (float y = 0.14f; y < 0.4f; y += 0.13f) Button(0f, y, look.TopColor * 0.6f);
                if (Band(up, 0.14f, 0.2f, 0.004f) > 0.5f && Band(off, 0.09f, 0.17f, 0.004f) > 0.5f) cloth *= 0.82f;
                break;
            }

            case Top.Jacket:
            {
                float open = Mathf.Lerp(0f, 0.06f, Mathf.InverseLerp(0.74f, 1f, up));
                if (off < open) cloth = look.Under;
                else if (off < 0.004f) cloth = Color.Lerp(cloth, new Color(0.6f, 0.58f, 0.52f), 0.6f);
                if (up < 0.07f) cloth *= 0.72f;
                if (Band(up, 0.16f, 0.3f, 0.004f) > 0.5f && Mathf.Abs(off - 0.13f) < 0.003f) cloth *= 0.6f;
                break;
            }

            case Top.Coat:
            {
                if (off < 0.004f) cloth *= 0.5f;
                if (Band(up, 0.3f, 0.36f, 0.006f) > 0.5f) cloth *= 0.68f;
                for (float y = 0.42f; y < 0.9f; y += 0.13f) Button(0.024f, y, look.TopTrim);
                if (along < 0.42f)
                {
                    float d = Mathf.Abs(Mathf.Sin(across * 37f)) * 0.5f + 0.5f;
                    cloth *= Mathf.Lerp(1f, d, 0.16f);
                }
                if (up > 0.82f && off < 0.06f) cloth = look.Under * Mathf.Lerp(0.9f, 1f, Hash(Mathf.FloorToInt(across * 90f), 3, 8));
                break;
            }

            case Top.Cardigan:
            {
                cloth *= 0.93f + 0.13f * Mathf.Abs(Mathf.Sin(across * 170f));
                if (off < 0.02f + 0.05f * Mathf.InverseLerp(0.7f, 1f, up)) cloth = look.Under;
                else if (off < 0.032f + 0.05f * Mathf.InverseLerp(0.7f, 1f, up)) cloth = look.TopTrim;
                for (float y = 0.1f; y < 0.7f; y += 0.12f) Button(0.027f, y, look.TopTrim * 0.6f);
                if (up < 0.08f) cloth *= 0.9f + 0.2f * Mathf.Abs(Mathf.Sin(across * 300f));
                break;
            }

            case Top.Hoodie:
            {
                float pocket = 0.13f - 0.25f * Mathf.Abs(up - 0.08f);
                if (up > 0.12f && up < 0.34f && off < pocket) cloth *= 0.84f;
                if (up < 0.07f) cloth *= 0.8f + 0.14f * Mathf.Abs(Mathf.Sin(across * 300f));
                if (Mathf.Abs(off - 0.022f) < 0.0035f && up > 0.68f && up < 0.94f) cloth = Color.Lerp(cloth, Color.white, 0.5f);
                break;
            }

            case Top.Tracksuit:
            {
                foreach (float stripe in new[] { 0.2f, 0.23f, 0.26f })
                    if (Mathf.Abs(off - stripe) < 0.0075f) cloth = look.TopTrim;
                if (off < 0.004f) cloth = new Color(0.72f, 0.72f, 0.7f);
                if (up < 0.07f) cloth *= 0.78f;
                break;
            }

            case Top.Quilted:
            {
                float row = Mathf.Repeat(up * 9f, 1f);
                cloth *= Mathf.Lerp(0.62f, 1.06f, Mathf.Clamp01(Mathf.Min(row, 1f - row) * 6f));
                if (off < 0.004f) cloth = new Color(0.14f, 0.14f, 0.13f);
                break;
            }

            case Top.Uniform:
            {
                float open = Mathf.Lerp(0f, 0.07f, Mathf.InverseLerp(0.68f, 1f, up));
                if (off < open) cloth = look.Under;
                else if (off < open + 0.02f && up > 0.66f) cloth = look.TopColor * 1.25f;
                if (off < 0.003f && up < 0.68f) cloth *= 0.55f;
                for (float y = 0.14f; y < 0.68f; y += 0.13f)
                {
                    Button(0.016f, y, new Color(brass, brass * 0.74f, brass * 0.34f));
                }
                if (Band(up, 0.5f, 0.56f, 0.004f) > 0.5f && Band(left, -0.14f, -0.08f, 0.004f) > 0.5f) cloth = new Color(0.68f, 0.56f, 0.3f);
                if (up > 0.84f && Band(off, 0.13f, 0.22f, 0.006f) > 0.5f) cloth = Color.Lerp(cloth, look.TopTrim, 0.8f);
                break;
            }

            case Top.Nurse:
            {
                if (off < 0.003f) cloth *= 0.82f;
                for (float y = 0.05f; y < 0.8f; y += 0.13f) Button(0f, y, look.TopTrim);
                if (Band(up, 0.3f, 0.36f, 0.006f) > 0.5f) cloth = Color.Lerp(cloth, look.TopTrim, 0.7f);
                if (up > 0.88f && off > 0.03f && off < 0.1f) cloth = look.TopTrim;
                break;
            }
        }

        if (look.HiVis)
        {
            Color yellow = new Color(0.84f, 0.42f, 0.08f);
            bool vest = up < 0.93f && off > 0.012f + 0.1f * Mathf.InverseLerp(0.76f, 0.93f, up);
            if (vest)
            {
                cloth = yellow;
                if (Band(up, 0.16f, 0.21f, 0.003f) > 0.5f || Band(up, 0.3f, 0.35f, 0.003f) > 0.5f) cloth = new Color(0.86f, 0.86f, 0.8f);
                if (up > 0.35f && Mathf.Abs(off - 0.14f) < 0.02f) cloth = new Color(0.86f, 0.86f, 0.8f);
                if (up > 0.35f && Mathf.Abs(off - 0.36f) < 0.02f) cloth = new Color(0.86f, 0.86f, 0.8f);
            }
        }

        // Where cloth bunches: under the arms, at the waist and across the small of the back.
        float folds = 0.2f * Bell(up - 0.72f, 0.07f) * Bell(off - 0.25f, 0.05f) + 0.08f * Bell(up - 0.34f, 0.03f);
        float wrinkle = Lattice(across * 13f, along * 34f, 13, 34, look.Seed + 3) - 0.5f;
        cloth *= 1f - folds + wrinkle * 0.1f;
        return Fabric(cloth, across, along, look.Seed);
    }

    static Color PaintSleeve(Look look, float across, float along)
    {
        Color cloth = look.HiVis ? look.Under : look.TopColor;
        if (look.Top == Top.Quilted)
        {
            float row = Mathf.Repeat(along * 9f, 1f);
            cloth *= Mathf.Lerp(0.62f, 1.06f, Mathf.Clamp01(Mathf.Min(row, 1f - row) * 6f));
        }
        if (look.Top == Top.Tracksuit && Mathf.Abs(Mathf.Abs(across - 0.25f) - 0.03f) < 0.012f) cloth = look.TopTrim;
        if (look.Top == Top.Uniform && along > 0.86f && along < 0.9f) cloth = new Color(0.72f, 0.6f, 0.3f);
        if (look.Top == Top.Cardigan) cloth *= 0.93f + 0.13f * Mathf.Abs(Mathf.Sin(across * 90f));

        // Rucked up at the elbow, turned in at the cuff.
        cloth *= 1f - 0.2f * Bell(along - 0.58f, 0.04f) * (0.5f + Lattice(across * 9f, 1f, 9, 2, 5));
        if (along > 0.95f) cloth *= look.Top == Top.Hoodie || look.Top == Top.Tracksuit ? 0.8f : 0.7f;
        return Fabric(cloth, across, along, look.Seed + 1);
    }

    static Color PaintLeg(Look look, float across, float along)
    {
        bool bare = look.Bottom == Bottom.Skirt || look.Top == Top.Nurse;
        Color cloth = bare ? Color.Lerp(look.Skin, new Color(0.13f, 0.11f, 0.1f), look.Top == Top.Nurse ? 0.25f : 0.62f) : look.BottomColor;
        if (!bare)
        {
            if (look.Top == Top.Tracksuit && Mathf.Abs(Mathf.Abs(across - 0.5f) - 0.25f) < 0.02f) cloth = look.TopTrim;
            cloth *= 1f - 0.16f * Bell(along - 0.5f, 0.03f) * (0.5f + Lattice(across * 9f, 1f, 9, 2, 6));
            cloth *= 1f - 0.14f * Bell(along - 0.97f, 0.05f);
            if (Mathf.Abs(across - 0.5f) < 0.006f) cloth *= 0.9f;
        }
        return Fabric(cloth, across, along, look.Seed + 2, bare ? 0.4f : 1f);
    }

    static Color PaintHair(Look look, float across, float along)
    {
        float strand = Lattice(across * 90f, along * 5f, 90, 5, look.Seed + 4);
        float fine = Hash(Mathf.FloorToInt(across * 200f), Mathf.FloorToInt(along * 24f), look.Seed + 5);
        Color hair = look.HairColor * Mathf.Lerp(0.72f, 1.3f, strand * 0.7f + fine * 0.3f);
        hair.a = 1f;
        return hair;
    }

    static Color PaintHat(Look look, float across, float along)
    {
        Color hat = look.HatColor;
        switch (look.Hat)
        {
            case Hat.Beanie:
                hat *= 0.84f + 0.24f * Mathf.Abs(Mathf.Sin(across * 150f));
                if (along < 0.6f) hat *= 0.92f + 0.14f * Mathf.Abs(Mathf.Sin(across * 75f));
                break;

            case Hat.Headscarf:
                float dots = Mathf.Sin(across * 90f) * Mathf.Sin(along * 60f);
                if (dots > 0.62f) hat = look.HatPrint;
                else if (dots < -0.8f) hat = Color.Lerp(hat, look.HatPrint, 0.4f);
                break;

            case Hat.Ushanka:
                hat *= Mathf.Lerp(0.66f, 1.34f, Hash(Mathf.FloorToInt(across * 120f), Mathf.FloorToInt(along * 120f), 11));
                break;
        }
        return Fabric(hat, across, along, look.Seed + 6);
    }

    static Color PaintFace(Look look, float theta, float phi)
    {
        float side = Mathf.Abs(theta);
        float eyeX = side - CharacterForge.EyeTheta, eyeY = phi - CharacterForge.EyePhi;
        Color skin = look.Skin;

        // Blood under the skin, more of it the more outdoors or the older the face.
        Color flush = new Color(skin.r * 1.02f, skin.g * 0.78f, skin.b * 0.72f);
        skin = Color.Lerp(skin, flush, 0.32f * Bell(phi + 11f, 11f) * Bell(side - 36f, 16f));
        skin = Color.Lerp(skin, flush, 0.14f * Bell(phi - 34f, 16f) * Bell(theta, 40f));
        if (look.RedNose) skin = Color.Lerp(skin, new Color(0.72f, 0.3f, 0.26f), 0.65f * Bell(phi + 6f, 13f) * Bell(theta, 8f));

        // Shadow pressed into the shapes of the face, so it reads in flat light.
        float shade = 0f;
        shade += 0.34f * Bell(eyeY - 1f, 6.5f) * Bell(eyeX, 12f);
        shade += 0.14f * Bell(side - 9f, 3.5f) * Bell(phi + 2f, 13f);
        shade += 0.3f * Bell(phi + 21f, 2.8f) * Bell(theta, 7f);
        shade += 0.24f * Bell(phi + 39f, 3.5f) * Bell(theta, 11f);
        shade += 0.26f * Mathf.InverseLerp(-58f, -86f, phi) * Bell(theta, 100f);
        shade += look.Gaunt * 0.3f * Bell(phi + 25f, 12f) * Bell(side - 40f, 13f);
        skin *= 1f - shade;

        // What the years leave.
        float age = look.Age;
        float laugh = Mathf.Abs(side - Mathf.Lerp(9f, 19f, Mathf.InverseLerp(-13f, -33f, phi)));
        float lines = Band(phi, -36f, -11f, 2f) * Bell(laugh, 1.1f) * 0.55f;
        foreach (float y in new[] { 31f, 37f, 43f }) lines = Mathf.Max(lines, 0.38f * Bell(phi - y, 0.9f) * Bell(theta, 34f) * (0.7f + 0.3f * Mathf.Sin(theta * 0.3f + y)));
        lines = Mathf.Max(lines, 0.4f * Bell(phi + 3f, 1f) * Bell(eyeX - 2f, 8f));
        lines = Mathf.Max(lines, 0.42f * Bell(side - 37f - (phi - 7f) * 0.3f, 1f) * Band(phi, -2f, 15f, 2f));
        skin = Color.Lerp(skin, skin * 0.58f, Mathf.Clamp01(lines * age * 1.8f));

        float pores = Hash(Mathf.FloorToInt(theta * 3f), Mathf.FloorToInt(phi * 3f), look.Seed) - 0.5f;
        skin *= 1f + pores * 0.05f + (Lattice(theta / 20f + 18f, phi / 20f + 9f, 36, 18, look.Seed) - 0.5f) * 0.14f;

        // The hair on the face.
        float cheekEdge = Mathf.Lerp(-13f, -30f, Mathf.InverseLerp(30f, 92f, side));
        float shaved = Hash(Mathf.FloorToInt(theta * 5f), Mathf.FloorToInt(phi * 5f), look.Seed + 1);
        float jaw = Mathf.Clamp01((cheekEdge - phi) / 7f) * Mathf.Clamp01((92f - side) / 12f) * Mathf.Clamp01((phi + 74f) / 10f);
        jaw *= 1f - 0.8f * Bell(phi + 15f, 7f) * Bell(theta, 12f);
        Color hair = look.HairColor;
        if (look.Stubble > 0f && shaved < look.Stubble * jaw * 0.8f) skin = Color.Lerp(skin, hair, 0.62f);
        if (look.Beard && jaw > 0f && phi < -22f + Mathf.Abs(theta) * 0.05f && !(side < 10f && phi > -36f && phi < -26f)) skin = hair * Mathf.Lerp(0.8f, 1.25f, shaved);
        if (look.Moustache && phi > -28f && phi < -19.5f + side * -0.08f && side < 23f && side > 0.8f) skin = hair * Mathf.Lerp(0.8f, 1.3f, shaved);

        // The mouth, turned up or down at the corners.
        float mouth = -31f + look.Mouth * 2.4f * (theta / 13f) * (theta / 13f);
        float width = 13f * (look.Woman ? 0.86f : 1f);
        if (side < width)
        {
            float edge = 1f - (theta / width) * (theta / width), high = 3f * edge * (look.Woman ? 1.2f : 0.75f), low = 3.4f * edge * (look.Woman ? 1.2f : 0.9f);
            Color lip = Color.Lerp(skin, look.Woman ? new Color(0.62f, 0.28f, 0.28f) : new Color(0.64f, 0.38f, 0.34f), 0.7f);
            if (phi - mouth < high && mouth - phi < low) skin = lip;
            if (Mathf.Abs(phi - mouth) < 0.75f) skin = skin * 0.34f;
        }

        // Nostrils.
        if (new Vector2((side - 3.8f * look.NoseWidth) / 1.5f, (phi + 15.5f) / 1.1f).magnitude < 1f) skin *= 0.4f;

        // The eyes: a white, an iris, and the heavy lid that gives each person their look.
        const float eyeWidth = 7.4f, eyeHeight = 3.7f;
        float gaze = theta > 0f ? 0.5f : -0.5f;
        float eye = (eyeX / eyeWidth) * (eyeX / eyeWidth) + (eyeY / eyeHeight) * (eyeY / eyeHeight);
        if (eye < 1f)
        {
            Color white = new Color(0.78f, 0.74f, 0.68f);
            float iris = new Vector2(eyeX * (theta > 0f ? 1f : 1f) - gaze * 0f, eyeY * 0.72f).magnitude;
            skin = iris < 1.25f ? new Color(0.03f, 0.03f, 0.03f) : iris < 3f ? look.Eyes : white;

            // The lid comes down further on tired people.
            float lid = Mathf.Lerp(0.9f, -0.2f, look.Age * 0.75f + (look.Gaunt * 0.2f));
            if (eyeY / eyeHeight > lid) skin = look.Skin * 0.66f;
        }
        else if (eye < 1.6f && eyeY > 0f)
        {
            skin = Color.Lerp(skin, new Color(0.08f, 0.06f, 0.05f), Mathf.Clamp01((1.6f - eye) * 3f));
        }
        else if (eye < 1.3f)
        {
            skin *= 0.72f;
        }

        // The eyebrows, whose tilt says most of what the face is saying.
        float browY = 15.2f - 0.016f * eyeX * eyeX + look.BrowTilt * -0.12f * eyeX;
        if (side > 11f && side < 38f && Mathf.Abs(phi - browY) < 1.5f * Mathf.Lerp(1.1f, 0.7f, (side - 11f) / 27f))
            skin = Color.Lerp(skin, look.Hair == HairStyle.Receding || look.Age > 0.9f ? look.HairColor * 0.8f : look.HairColor, 0.9f);

        skin.a = 1f;
        return skin;
    }

    static Canvas PaintPerson(Look look)
    {
        var canvas = new Canvas(look.TextureSize, look.TextureSize, look.Skin);
        PaintArea(canvas, CharacterForge.TorsoArea, (u, v) => PaintTop(look, u, v));
        PaintArea(canvas, CharacterForge.ArmArea, (u, v) => PaintSleeve(look, u, v));
        PaintArea(canvas, CharacterForge.LegArea, (u, v) => PaintLeg(look, u, v));
        PaintArea(canvas, CharacterForge.HairArea, (u, v) => PaintHair(look, u, v));
        PaintArea(canvas, CharacterForge.HatArea, (u, v) => PaintHat(look, u, v));
        PaintArea(canvas, CharacterForge.SkirtArea, (u, v) => Fabric(look.BottomColor * (0.9f + 0.18f * Mathf.Abs(Mathf.Sin(u * 90f))), u, v, look.Seed + 7));
        PaintArea(canvas, CharacterForge.TrimArea, (u, v) => Fabric(look.Top == Top.Hoodie ? look.TopColor * 0.86f : look.TopTrim, u, v, look.Seed + 8));
        PaintArea(canvas, CharacterForge.CarriedArea, (u, v) => Fabric(look.CarriedColor * (Mathf.Abs(u - 0.5f) < 0.02f ? 0.7f : 1f), u, v, look.Seed + 9));
        PaintArea(canvas, CharacterForge.EarArea, (u, v) => look.Skin * Mathf.Lerp(0.72f, 0.9f, v));
        PaintArea(canvas, CharacterForge.ShoeArea, (u, v) =>
        {
            Color shoe = look.ShoeColor * (Mathf.Abs(u - 0.5f) > 0.34f ? 0.4f : 1f);
            if (Mathf.Abs(u - 0.5f) < 0.2f && v > 0.25f && v < 0.5f && Mathf.Repeat(v * 40f, 1f) < 0.25f) shoe = look.ShoeColor * 1.8f;
            shoe.a = 1f;
            return Fabric(shoe, u, v, look.Seed + 10, 0.6f);
        });
        PaintArea(canvas, CharacterForge.HandArea, (u, v) =>
        {
            Color skin = look.Skin * 0.94f;
            float knuckle = Mathf.Abs(u - 0.5f);
            if (v > 0.5f && knuckle < 0.36f && Mathf.Abs(Mathf.Repeat(u * 8f, 2f) - 1f) < 0.1f) skin *= 0.7f;
            skin = Color.Lerp(skin, skin * 0.8f, Bell(v - 0.5f, 0.05f) * 0.5f);
            skin.a = 1f;
            return skin;
        });

        PaintArea(canvas, CharacterForge.HeadArea, (u, v) => PaintFace(look, CharacterForge.UToTheta(u), v * 180f - 90f));
        if (look.Scratched) ScratchOut(canvas, look);
        return canvas;
    }

    // What someone did to the photograph on his badge: gone at with a key, again and again, until there's no one there.
    static void ScratchOut(Canvas canvas, Look look)
    {
        Rect head = CharacterForge.HeadArea;
        float Reach(float theta, float phi) => (theta / 58f) * (theta / 58f) + ((phi + 8f) / 54f) * ((phi + 8f) / 54f);
        Vector2 Pixel(float theta, float phi) => new Vector2((head.x + head.width * CharacterForge.ThetaToU(theta)) * canvas.Width, (head.y + head.height * CharacterForge.PhiToV(phi)) * canvas.Height);

        Color raw = new Color(0.2f, 0.09f, 0.08f), hole = new Color(0.016f, 0.012f, 0.012f);
        Color before(float theta, float phi) => PaintFace(look, theta, phi);
        PaintArea(canvas, head, (u, v) =>
        {
            float theta = CharacterForge.UToTheta(u), phi = v * 180f - 90f;
            float edge = Reach(theta, phi) + (Lattice(u * 40f, v * 40f, 40, 40, look.Seed + 20) - 0.5f) * 0.7f;
            if (edge > 1.28f) return before(theta, phi);

            Color scraped = Color.Lerp(raw, look.Skin * 0.6f, Mathf.Clamp01((edge - 1.02f) * 4f));
            Color color = Color.Lerp(hole, scraped, Mathf.Clamp01((edge - 0.72f) * 3.4f));
            color.a = 1f;
            return color;
        });

        // Strokes back and forth, hard and fast, each time from a little further over.
        var strokes = new System.Random(look.Seed);
        float Next() => (float)strokes.NextDouble();
        for (int i = 0; i < 1500; i++)
        {
            float a = Next() * Mathf.PI * 2f, r = Mathf.Sqrt(Next()) * 1.04f;
            float theta = Mathf.Cos(a) * r * 58f, phi = Mathf.Sin(a) * r * 54f - 8f;
            float slant = (i % 7 == 0 ? -1.1f : 1.25f) + (Next() - 0.5f) * 0.7f, length = 10f + Next() * Next() * 46f;
            Vector2 start = Pixel(theta, phi);
            Vector2 end = Pixel(theta + Mathf.Cos(slant) * length, phi + Mathf.Sin(slant) * length);

            float pick = Next();
            Color mark = pick < 0.5f ? new Color(0.6f, 0.56f, 0.5f) * (0.5f + Next() * 0.7f) : pick < 0.82f ? new Color(0.03f, 0.02f, 0.02f) : new Color(0.3f, 0.07f, 0.06f);
            mark.a = 1f;
            canvas.DrawLine(start, end, pick < 0.5f ? 1.1f + Next() * 1.4f : 2f + Next() * 4f, mark);
        }

        // Nothing has been able to touch the sockets or the mouth: they're simply holes, and it's that that makes it a face.
        PaintArea(canvas, head, (u, v) =>
        {
            float theta = CharacterForge.UToTheta(u), phi = v * 180f - 90f;
            float rough = (Lattice(u * 60f, v * 60f, 60, 60, look.Seed + 21) - 0.5f) * 0.6f;
            float eye = Bell(Mathf.Abs(theta) - CharacterForge.EyeTheta, 10.5f) * Bell(phi - CharacterForge.EyePhi, 9f);
            float mouth = Bell(theta, 16f) * Bell(phi + 34f, 13.5f);
            float hole = Mathf.Clamp01((Mathf.Max(eye, mouth) + rough * 0.5f - 0.42f) * 6f);
            Color color = canvas[Mathf.RoundToInt(head.x * canvas.Width + u * head.width * canvas.Width), Mathf.RoundToInt(head.y * canvas.Height + v * head.height * canvas.Height)];
            color = Color.Lerp(color, new Color(0.008f, 0.006f, 0.006f), hole);
            color.a = 1f;
            return color;
        });
    }
}
