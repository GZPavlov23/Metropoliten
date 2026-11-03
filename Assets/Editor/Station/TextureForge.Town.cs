using UnityEngine;

// The town at the end of the line, painted a surface at a time. Most of it is lit by nothing but an overcast sky, so the
// light is painted in: what's under a ledge is dark, what faces the sky has gone pale, and everything has been rained on.
public static partial class TextureForge
{
    public const int BlockBayPixels = 120, BlockFloorPixels = 96, BlockPlinthPixels = 64, BlockTopPixels = 40;

    // One bay in four and the one beside it are balconies. The builder puts the same ones in front of the wall.
    public static bool BlockBalcony(int bay) => bay % 4 == 1 || bay % 4 == 2;

    public static void ForgeTown()
    {
        Write("T_BlockLong", BlockFace(16, 8, 501, Rgb(150, 146, 134), true));
        Write("T_BlockLongPlain", BlockFace(16, 8, 502, Rgb(134, 134, 128), false));
        Write("T_BlockGable", BlockGable(502));
        Write("T_BlockParts", BlockParts());
        Write("T_Road", Road());
        Write("T_StreetEast", StreetEast());
        Write("T_Slabs", Slabs());
        Write("T_HospitalRender", HospitalRender());
        Write("T_HospitalGlass", Painted(64, Rgb(92, 98, 106), 0.3f, 3, 0.12f));
        Write("T_HospitalLobby", HospitalLobby());
        Write("T_Kerb", Kerb());
        Write("T_Verge", Verge());
        Write("T_Kiosk", Kiosk());
        Write("T_Garages", Garages());
        Write("T_ShelterConcrete", ShelterConcrete());
        Write("T_Mosaic", Mosaic());
        Write("T_Timetable14", Timetable14());
        Write("T_RoadWorks", RoadWorks());
        Write("T_Barricade", Barricade());
        Write("T_DeadCar", DeadCar());
        Write("T_Bark", Painted(128, Rgb(40, 36, 34), 0.5f, 5, 0.3f));
        Write("T_CarPale", Painted(64, Rgb(150, 146, 128), 0.4f, 3, 0.12f));
        Write("T_CarRed", Painted(64, Rgb(110, 44, 36), 0.4f, 3, 0.12f));
        Write("T_CarDark", Painted(64, Rgb(52, 62, 72), 0.4f, 3, 0.12f));
        Write("T_CarGlass", Painted(64, Rgb(34, 38, 42), 0.3f, 3, 0.1f));
        Write("T_ChimneyPale", Painted(32, Rgb(92, 94, 100), 0.2f, 3, 0.1f));
        Write("T_ChimneyRed", Painted(32, Rgb(84, 40, 38), 0.2f, 3, 0.1f));
    }

    static Color Pastel(float pick)
    {
        Color[] paints = { Rgb(204, 190, 150), Rgb(190, 170, 150), Rgb(176, 186, 164), Rgb(206, 196, 182), Rgb(196, 160, 132), Rgb(168, 176, 180) };
        return paints[Mathf.Clamp(Mathf.FloorToInt(pick * paints.Length), 0, paints.Length - 1)];
    }

    // One long side of a panel block, floor by floor: slabs with their joints, a window in every bay, and thirty years of
    // weather and other people's repairs. The balconies are built out in front of it, so this paints what's behind them.
    static Canvas BlockFace(int bays, int floors, int seed, Color concrete, bool shops)
    {
        int width = bays * BlockBayPixels, height = BlockPlinthPixels + floors * BlockFloorPixels + BlockTopPixels;
        var canvas = new Canvas(width, height, concrete);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                int bay = x / BlockBayPixels, floor = Mathf.FloorToInt((y - BlockPlinthPixels) / (float)BlockFloorPixels);

                float panel = 0.9f + 0.17f * Hash(bay, floor, seed);
                float blotch = Mathf.Lerp(0.76f, 1.14f, Fractal(u, v, 16, 9, 5, seed + 1));
                float rain = Lattice(u * bays * 7f, v * 3f, bays * 7, 3, seed + 2);
                float pebble = 0.93f + 0.14f * Hash(x / 2, y / 2, seed + 3);
                canvas[x, y] = Opaque(concrete * panel * blotch * Mathf.Lerp(0.86f, 1.06f, rain) * pebble);
            }
        }

        // Where people have paid to have their own bit of the outside wall insulated.
        for (int i = 0; i < bays * floors / 11; i++)
        {
            int bay = Mathf.FloorToInt(Hash(i, 1, seed + 4) * (bays - 2)), floor = Mathf.FloorToInt(Hash(i, 2, seed + 4) * floors);
            int across = 1 + Mathf.FloorToInt(Hash(i, 3, seed + 4) * 2.4f);
            int x = bay * BlockBayPixels + 4, y = BlockPlinthPixels + floor * BlockFloorPixels + 3;
            Color paint = Pastel(Hash(i, 4, seed + 4));
            for (int py = y; py < y + BlockFloorPixels - 6; py++)
            {
                for (int px = x; px < x + across * BlockBayPixels - 8; px++)
                {
                    float grain = 0.93f + 0.14f * Hash(px, py, seed + 5);
                    canvas.Blend(px, py, paint * grain, 0.9f);
                }
            }
        }

        // The joints between the panels, sealed with something black that has run.
        for (int floor = 0; floor <= floors; floor++)
        {
            int y = BlockPlinthPixels + floor * BlockFloorPixels;
            canvas.FillRect(0, y - 1, width, 2, Rgb(46, 44, 42), 0.72f);
            canvas.FillRect(0, y - 4, width, 3, Rgb(46, 44, 42), 0.14f);
        }
        for (int bay = 0; bay <= bays; bay++)
            canvas.FillRect(bay * BlockBayPixels - 1, BlockPlinthPixels, 2, floors * BlockFloorPixels, Rgb(46, 44, 42), 0.55f);

        // The windows.
        for (int floor = 0; floor < floors; floor++)
        {
            int y = BlockPlinthPixels + floor * BlockFloorPixels;
            for (int bay = 0; bay < bays; bay++)
            {
                int x = bay * BlockBayPixels;
                if (BlockBalcony(bay)) BalconyWall(canvas, x, y, bay, floor, seed);
                else BlockWindow(canvas, x, y, bay, floor, seed);
            }
        }

        BlockStains(canvas, bays, floors, seed);

        // The plinth, the parapet along the top, and damp climbing up from the ground.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                if (y < BlockPlinthPixels)
                {
                    float pebbled = 0.8f + 0.3f * Hash(x / 2, y / 2, seed + 6);
                    canvas[x, y] = Opaque(Rgb(94, 90, 84) * pebbled * Mathf.Lerp(0.72f, 1.04f, Fractal(u, (float)y / height, 20, 6, 3, seed + 7)));
                }
                else if (y > height - BlockTopPixels)
                {
                    canvas[x, y] = Opaque(canvas[x, y] * 0.74f);
                }
                if (y < BlockPlinthPixels + 70)
                {
                    float damp = Mathf.InverseLerp(BlockPlinthPixels + 70f, 0f, y) * Mathf.Lerp(0.5f, 1.3f, Fractal(u, 0.5f, 34, 2, 3, seed + 8));
                    canvas[x, y] = Opaque(canvas[x, y] * (1f - Mathf.Clamp01(damp) * 0.5f));
                }
            }
        }

        if (shops) BlockShops(canvas, bays, seed);
        else
        {
            // Basement vents along the plinth.
            for (int bay = 0; bay < bays; bay++)
            {
                int x = bay * BlockBayPixels + 40;
                canvas.FillRect(x, 26, 40, 22, Rgb(20, 20, 20));
                for (int i = 0; i < 5; i++) canvas.FillRect(x, 28 + i * 4, 40, 1, Rgb(70, 66, 60));
            }
        }
        return canvas;
    }

    static void BlockWindow(Canvas canvas, int x, int y, int bay, int floor, int seed)
    {
        int left = x + 24, bottom = y + 22, width = 72, height = 56;
        float kind = Hash(bay, floor, seed + 9), lit = Hash(bay, floor, seed + 10);

        canvas.FillRect(left - 3, bottom - 3, width + 6, height + 7, Rgb(30, 28, 28), 0.8f);
        Color frame = kind > 0.55f ? Rgb(196, 194, 186) : kind > 0.2f ? Rgb(92, 72, 56) : Rgb(150, 140, 124);
        canvas.FillRect(left, bottom, width, height, frame);
        GlassPane(canvas, left + 4, bottom + 4, width / 2 - 6, height - 8, lit, bay + 100, floor, seed);
        GlassPane(canvas, left + width / 2 + 2, bottom + 4, width / 2 - 6, height - 8, lit, bay + 200, floor, seed);

        // The sill, and what's stood out on it.
        canvas.FillRect(left - 5, bottom - 6, width + 10, 4, Rgb(170, 166, 156));
        if (Hash(bay, floor, seed + 11) > 0.84f) canvas.FillRect(left + 8, bottom - 28, 34, 20, Rgb(176, 172, 162));
        if (Hash(bay, floor, seed + 12) > 0.9f)
        {
            canvas.FillCircle(left + width + 6f, bottom + 42f, 9f, Rgb(170, 170, 166));
            canvas.FillCircle(left + width + 6f, bottom + 42f, 6f, Rgb(120, 118, 112));
        }
    }

    // A dark room, or someone already up.
    static void GlassPane(Canvas canvas, int x, int y, int width, int height, float lit, int bay, int floor, int seed)
    {
        for (int j = 0; j < height; j++)
        {
            float up = (float)j / height;
            for (int i = 0; i < width; i++)
            {
                Color glass = lit > 0.9f ? Color.Lerp(Rgb(214, 150, 72), Rgb(246, 206, 130), up) * (0.86f + 0.2f * Hash(bay, i / 5, 7)) : Color.Lerp(Rgb(30, 34, 40), Rgb(82, 92, 104), up * up);
                float curtain = Hash(bay, floor, seed + 13);
                if (curtain > 0.42f && lit <= 0.9f)
                {
                    float fold = 0.8f + 0.26f * Mathf.Abs(Mathf.Sin(i * 0.7f + bay));
                    if ((float)i / width < curtain * 0.7f || (float)i / width > 1f - curtain * 0.4f) glass = Rgb(112, 106, 96) * fold;
                }
                canvas[x + i, y + j] = Opaque(glass);
            }
        }
    }

    // What's at the back of a balcony: the shade under the slab, a door and a window into the flat.
    static void BalconyWall(Canvas canvas, int x, int y, int bay, int floor, int seed)
    {
        canvas.FillRect(x + 1, y + 1, BlockBayPixels - 2, BlockFloorPixels - 2, Rgb(52, 52, 52), 0.62f);
        for (int j = 0; j < 16; j++) canvas.FillRect(x + 1, y + BlockFloorPixels - 2 - j, BlockBayPixels - 2, 1, Rgb(12, 12, 12), (16 - j) / 16f * 0.8f);

        float lit = Hash(bay, floor, seed + 14);
        canvas.FillRect(x + 12, y + 8, 40, 70, Rgb(70, 62, 54));
        GlassPane(canvas, x + 16, y + 28, 32, 46, lit, bay + 300, floor, seed);
        canvas.FillRect(x + 58, y + 26, 52, 52, Rgb(70, 62, 54));
        GlassPane(canvas, x + 62, y + 30, 44, 44, lit, bay + 400, floor, seed);
    }

    static void BlockStains(Canvas canvas, int bays, int floors, int seed)
    {
        // Rust and soot running down from every ledge.
        for (int i = 0; i < bays * floors * 2; i++)
        {
            int x = Mathf.FloorToInt(Hash(i, 1, seed + 15) * canvas.Width);
            int top = Mathf.FloorToInt(Hash(i, 2, seed + 15) * (canvas.Height - BlockPlinthPixels)) + BlockPlinthPixels;
            int length = 20 + Mathf.FloorToInt(Hash(i, 3, seed + 15) * Hash(i, 4, seed + 15) * 260f);
            int wide = 1 + Mathf.FloorToInt(Hash(i, 5, seed + 15) * 5f);
            Color stain = Hash(i, 6, seed + 15) > 0.7f ? Rgb(92, 58, 36) : Rgb(34, 32, 30);
            for (int j = 0; j < length; j++)
            {
                float fade = (1f - (float)j / length) * 0.34f;
                for (int w = 0; w < wide; w++) canvas.Blend(x + w, top - j, stain, fade);
            }
        }

        // The great tide-marks that come off the roof.
        for (int x = 0; x < canvas.Width; x++)
        {
            float reach = Mathf.Pow(Fractal((float)x / canvas.Width, 0.5f, 22, 2, 4, seed + 16), 2.4f) * canvas.Height * 1.1f;
            for (int j = 0; j < reach && j < canvas.Height; j++)
                canvas.Blend(x, canvas.Height - 1 - j, Rgb(30, 30, 30), (1f - j / reach) * 0.42f);
        }
    }

    static void BlockShops(Canvas canvas, int bays, int seed)
    {
        (int from, int to, string sign, Color ground, Color ink, bool shut)[] units =
        {
            (0, 4, "ХРАНИТЕЛНИ СТОКИ", Rgb(58, 92, 74), Rgb(226, 220, 196), true),
            (4, 6, "АПТЕКА", Rgb(200, 196, 182), Rgb(36, 96, 70), false),
            (7, 10, "РЕМОНТ НА ОБУВКИ", Rgb(120, 50, 40), Rgb(226, 214, 186), true),
            (11, 14, "ЗАКУСКА", Rgb(64, 66, 96), Rgb(232, 206, 150), true),
        };

        foreach ((int from, int to, string sign, Color ground, Color ink, bool shut) in units)
        {
            int x = from * BlockBayPixels + 10, width = (to - from) * BlockBayPixels - 20, top = BlockPlinthPixels + 48;
            canvas.FillRect(x, top + 2, width, 36, ground);
            DrawText(canvas, sign, x + width / 2f, top + 11, 22, ink, 1f);

            canvas.FillRect(x, 8, width, top - 8, Rgb(20, 20, 20));
            if (shut)
            {
                for (int y = 10; y < top - 2; y++)
                {
                    float rib = y % 6 < 3 ? 0.84f : 0.62f, rust = Mathf.InverseLerp(0.5f, 0.7f, Fractal((float)(x) / canvas.Width, y / 96f, 9, 5, 3, seed + from));
                    canvas.FillRect(x + 3, y, width - 6, 1, Opaque(Color.Lerp(Rgb(126, 124, 118), Rgb(104, 66, 44), rust) * rib));
                }
            }
            else
            {
                GlassPane(canvas, x + 4, 12, width - 8, top - 18, 1f, from, 0, seed);
                canvas.FillRect(x + width / 2 - 1, 12, 2, top - 18, Rgb(20, 20, 20));
            }

            // Something sprayed over it, and something sprayed over that.
            for (int i = 0; i < 26; i++)
            {
                var start = new Vector2(x + Hash(i, from, seed + 17) * width, 14f + Hash(i, from + 1, seed + 17) * 60f);
                canvas.DrawLine(start, start + new Vector2((Hash(i, from + 2, seed + 17) - 0.5f) * 60f, (Hash(i, from + 3, seed + 17) - 0.5f) * 34f), 3f, i % 2 == 0 ? Rgb(40, 40, 46) : Rgb(150, 40, 36));
            }
        }
    }

    // The end of a block, where there's nothing to look at but the weather.
    static Canvas BlockGable(int seed)
    {
        const int width = 480, height = 960;
        var canvas = new Canvas(width, height, Color.black);
        Color concrete = Rgb(142, 140, 132);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                int panel = Hash(x / 120, y / 96, seed + 20) > 0.5f ? 1 : 0;
                float tone = (0.88f + 0.12f * panel) * Mathf.Lerp(0.72f, 1.14f, Fractal(u, v, 6, 12, 5, seed + 21));
                float rain = Lattice(u * 40f, v * 3f, 40, 3, seed + 22);
                Color c = concrete * tone * Mathf.Lerp(0.78f, 1.06f, rain) * (0.94f + 0.12f * Hash(x / 2, y / 2, seed + 23));
                if (x % 120 < 2 || y % 96 < 2) c *= 0.5f;
                c *= 1f - 0.5f * Mathf.InverseLerp(120f, 0f, y);
                canvas[x, y] = Opaque(c);
            }
        }
        DrawText(canvas, "214", width / 2f, 520f, 120, Rgb(70, 72, 76), 8f);
        return canvas;
    }

    // Pieces of block, four by four: balcony fronts, slab edges and undersides, roof clutter, plain concrete.
    public static Rect BlockPart(int column, int row) => new Rect(column / 4f, row / 4f, 0.25f, 0.25f);

    static Canvas BlockParts()
    {
        const int size = 1024, cell = size / 4;
        var canvas = new Canvas(size, size, Rgb(120, 118, 112));

        void Cell(int column, int row, System.Func<float, float, Color> paint)
        {
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                    canvas[column * cell + x, row * cell + y] = Opaque(paint((x + 0.5f) / cell, (y + 0.5f) / cell));
        }

        Color Worn(Color paint, float u, float v, int seed) => paint * Mathf.Lerp(0.72f, 1.14f, Fractal(u, v, 7, 7, 4, seed)) * (1f - 0.42f * Mathf.InverseLerp(0.7f, 1f, 1f - v) * Fractal(u, 0.5f, 14, 2, 3, seed + 1));

        Cell(0, 0, (u, v) => Worn(Rgb(146, 142, 130) * (0.86f + 0.26f * Hash((int)(u * 200f), (int)(v * 60f), 601)), u, v, 602));
        Cell(1, 0, (u, v) => Worn(Rgb(134, 132, 124) * (Mathf.Repeat(u * 18f, 1f) < 0.3f ? 0.68f : 1.04f), u, v, 604));
        Cell(2, 0, (u, v) => Worn(Rgb(196, 182, 148), u, v, 606));
        Cell(3, 0, (u, v) => Worn(Rgb(104, 62, 42) * (Mathf.Repeat(u * 9f, 1f) < 0.5f ? 0.8f : 1.06f), u, v, 608));

        // Glassed in by someone with a brother-in-law in aluminium.
        Cell(0, 1, (u, v) =>
        {
            bool frame = Mathf.Repeat(u * 3f, 1f) < 0.05f || v < 0.42f && v > 0.36f || v > 0.95f;
            if (frame) return Rgb(190, 188, 180);
            if (v < 0.36f) return Worn(Rgb(160, 154, 140), u, v, 610);
            return Color.Lerp(Rgb(30, 34, 40), Rgb(92, 100, 110), (v - 0.4f) * 1.6f);
        });
        Cell(1, 1, (u, v) =>
        {
            bool frame = Mathf.Repeat(u * 4f, 1f) < 0.06f || v < 0.42f && v > 0.37f || v > 0.96f;
            if (frame) return Rgb(84, 64, 50);
            if (v < 0.37f) return Worn(Rgb(146, 142, 130), u, v, 612);
            return Color.Lerp(Rgb(40, 40, 42), Rgb(110, 112, 116), (v - 0.4f) * 1.6f);
        });

        Cell(2, 1, (u, v) => Rgb(40, 40, 40) * Mathf.Lerp(0.8f, 1.2f, Fractal(u, v, 5, 5, 3, 614)));
        Cell(3, 1, (u, v) => Worn(Rgb(132, 130, 122), u, v, 616));

        Cell(0, 2, (u, v) => Rgb(62, 60, 58) * Mathf.Lerp(0.6f, 1.2f, Fractal(u, v, 12, 12, 4, 618)));
        Cell(1, 2, (u, v) => Worn(Rgb(120, 118, 112), u, v, 620));
        Cell(2, 2, (u, v) => Worn(Rgb(96, 94, 90), u, v, 622));
        Cell(3, 2, (u, v) => Worn(Rgb(146, 142, 134), u, v, 624));
        return canvas;
    }

    // Three lanes' worth of patched road, tiling along its length: tar on the cracks, a lighter strip where a trench
    // was filled, tyre lines worn pale, and the paint nearly gone. V runs up the road.
    static Canvas Road()
    {
        const int width = 512, height = 2048;
        var canvas = new Canvas(width, height, Color.black);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float stones = 0.82f + 0.3f * Hash(x, y, 171) * Hash(x + 1, y / 2, 172);
                float slow = Mathf.Lerp(0.8f, 1.14f, Fractal(u, v, 5, 20, 5, 173));
                float tyres = 1f + 0.1f * Bell(u - 0.3f, 0.05f) + 0.1f * Bell(u - 0.62f, 0.05f);
                float wetEdge = 1f - 0.34f * Mathf.InverseLerp(0.1f, 0f, Mathf.Min(u, 1f - u));

                Color c = Rgb(76, 76, 78) * stones * slow * tyres * wetEdge;

                // A trench, filled and left.
                if (Band(u, 0.74f, 0.86f, 0.004f) > 0.5f) c *= 1.2f;
                if (Mathf.Abs(u - 0.74f) < 0.003f || Mathf.Abs(u - 0.86f) < 0.003f) c *= 0.45f;

                // Square patches, each a slightly different age.
                float patchChoice = Hash(Mathf.FloorToInt(u * 3f), Mathf.FloorToInt(v * 7f), 174);
                if (patchChoice > 0.62f)
                {
                    float px = Mathf.Repeat(u * 3f, 1f), py = Mathf.Repeat(v * 7f, 1f);
                    if (px > 0.14f && px < 0.84f && py > 0.2f && py < 0.72f)
                    {
                        c *= 0.7f + patchChoice * 0.46f;
                        float inside = Mathf.Min(Mathf.Min(px - 0.14f, 0.84f - px), Mathf.Min(py - 0.2f, 0.72f - py));
                        if (inside < 0.012f) c *= 0.5f;
                    }
                }

                // Cracks with tar poured along them.
                float crackField = Fractal(u, v, 6, 24, 3, 175);
                if (Mathf.Abs(crackField - 0.5f) < 0.0035f) c = Rgb(24, 24, 26);
                float fine = Fractal(u, v, 14, 56, 2, 176);
                if (Mathf.Abs(fine - 0.5f) < 0.002f) c *= 0.6f;
                canvas[x, y] = Opaque(c);
            }
        }

        // What's left of the white line, and a drain by the kerb.
        for (int y = 0; y < height; y += 256)
        {
            for (int j = 0; j < 150; j++)
            {
                for (int i = 0; i < 9; i++)
                {
                    float left = Fractal(i / 9f, (y + j) / (float)height, 4, 30, 3, 177);
                    if (left > 0.46f) canvas.Blend(254 + i, y + j, Rgb(176, 174, 164), 0.68f);
                }
            }
        }
        return canvas;
    }

    // The street going east from the gate: older, narrower and in worse repair.
    static Canvas StreetEast()
    {
        const int size = 512;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float grit = 0.82f + 0.3f * Hash(x, y, 181) * Hash(x + 1, y / 2, 182);
                float slow = Mathf.Lerp(0.74f, 1.16f, Fractal(u, v, 8, 8, 5, 183));
                Color c = Rgb(80, 80, 80) * grit * slow;
                if (Mathf.Abs(Fractal(u, v, 7, 7, 3, 184) - 0.5f) < 0.004f) c = Rgb(26, 26, 28);
                canvas[x, y] = Opaque(c);
            }
        }
        return canvas;
    }

    // Old lime render on a public building: pale, patched, and streaked under every sill.
    static Canvas HospitalRender()
    {
        const int size = 512;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float blotch = Mathf.Lerp(0.76f, 1.1f, Fractal(u, v, 6, 6, 5, 251));
                float rain = Mathf.Lerp(0.84f, 1.06f, Lattice(u * 24f, v * 2f, 24, 2, 252));
                canvas[x, y] = Opaque(Rgb(186, 182, 170) * blotch * rain * (0.96f + 0.08f * Hash(x, y, 253)));
            }
        }
        return canvas;
    }

    // What's behind the doors: tubes on a low ceiling, a pale floor, and a counter with nobody at it.
    static Canvas HospitalLobby()
    {
        const int width = 512, height = 216;
        var canvas = new Canvas(width, height, Color.black);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float wall = Mathf.Lerp(0.5f, 0.86f, Mathf.InverseLerp(0.2f, 0.92f, v)) * Mathf.Lerp(0.82f, 1.06f, Fractal(u, v, 4, 3, 3, 261));
                canvas[x, y] = new Color(wall, wall, wall * 0.97f, 1f);
            }
        }
        foreach (float x in new[] { 90f, 256f, 420f }) canvas.FillRect(Mathf.RoundToInt(x) - 42, 190, 84, 12, Color.white);
        canvas.FillRect(0, 0, width, 40, Rgb(116, 114, 108));
        canvas.FillRect(300, 28, 150, 54, Rgb(70, 68, 66));
        canvas.FillRect(36, 40, 44, 92, Rgb(92, 90, 86));
        return canvas;
    }

    // Half-metre concrete slabs, some sunk, some cracked across, with moss in the joints.
    static Canvas Slabs()
    {
        const int size = 512, count = 6, cell = size / count;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                int column = x / cell, row = y / cell;
                float tone = 0.8f + 0.3f * Hash(column, row, 191);
                float fx = (x % cell) / (float)cell, fy = (y % cell) / (float)cell, edge = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
                Color c = Rgb(136, 132, 124) * tone * Mathf.Lerp(0.78f, 1.1f, Fractal(u, v, 9, 9, 4, 192)) * (0.92f + 0.15f * Hash(x, y, 193));

                if (Hash(column, row, 194) > 0.74f && Mathf.Abs(fx - 0.3f - 0.4f * fy + Mathf.Sin(fy * 9f) * 0.03f) < 0.008f) c *= 0.4f;
                if (edge < 0.016f) c = Color.Lerp(Rgb(34, 36, 28), Rgb(56, 62, 38), Hash(x, y, 195));
                else if (edge < 0.04f) c *= 0.84f;
                canvas[x, y] = Opaque(c);
            }
        }
        return canvas;
    }

    static Canvas Kerb()
    {
        const int size = 256;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                Color c = Rgb(104, 100, 94) * Mathf.Lerp(0.66f, 1.1f, Fractal(u, v, 7, 7, 4, 196)) * (0.9f + 0.18f * Hash(x, y, 197));
                if (x % (size / 2) < 3) c *= 0.34f;
                canvas[x, y] = Opaque(c);
            }
        }
        return canvas;
    }

    // Dead grass and mud, trodden into paths.
    static Canvas Verge()
    {
        const int size = 1024;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float grassy = Mathf.InverseLerp(0.4f, 0.62f, Fractal(u, v, 6, 6, 5, 201));
                float blades = Hash(x, y / 3, 202) * Hash(x / 2, y, 203);
                Color mud = Rgb(70, 62, 50) * Mathf.Lerp(0.7f, 1.14f, Fractal(u, v, 18, 18, 3, 204));
                Color grass = Color.Lerp(Rgb(88, 84, 58), Rgb(142, 130, 88), blades);
                canvas[x, y] = Opaque(Color.Lerp(mud, grass, grassy));
            }
        }
        return canvas;
    }

    // A steel newspaper kiosk with its shutters down. U runs right round it: front, side, back, side.
    static Canvas Kiosk()
    {
        const int width = 1024, height = 256;
        var canvas = new Canvas(width, height, Rgb(70, 92, 84));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                float rust = Mathf.InverseLerp(0.52f, 0.68f, Fractal(u, v, 16, 4, 5, 211));
                Color c = Color.Lerp(Rgb(78, 104, 94), Rgb(96, 60, 42), rust) * Mathf.Lerp(0.8f, 1.1f, Fractal(u, v, 30, 8, 3, 212));
                c *= Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(0f, 0.18f, v));
                canvas[x, y] = Opaque(c);
            }
        }

        // The front, with its shutter and sign.
        canvas.FillRect(0, 196, 400, 50, Rgb(170, 42, 34));
        DrawText(canvas, "ВЕСТНИЦИ  ТЮТЮН", 200f, 208f, 28, Rgb(232, 224, 200), 2f);
        for (int y = 60; y < 190; y++)
        {
            float rib = y % 6 < 3 ? 0.9f : 0.62f;
            canvas.FillRect(30, y, 340, 1, Opaque(Rgb(136, 134, 126) * rib * (0.86f + 0.24f * Hash(y, 3, 213))));
        }
        canvas.FillRect(30, 44, 340, 14, Rgb(60, 56, 52));

        // Layers of posters on the side.
        foreach ((int x, int y, int w, int h, int seed) in new[] { (430, 50, 120, 160, 1), (560, 40, 90, 110, 2), (660, 60, 130, 150, 3), (800, 44, 100, 170, 4) })
        {
            Color paper = Color.Lerp(Rgb(190, 184, 164), Rgb(170, 90, 70), Hash(seed, 1, 214));
            canvas.FillRect(x, y, w, h, paper);
            for (int i = 0; i < 6; i++) canvas.FillRect(x + 8, y + 12 + i * 22, Mathf.RoundToInt(w * (0.5f + Hash(seed, i, 215) * 0.4f)), 8, Rgb(40, 38, 40), 0.7f);
            for (int i = 0; i < 90; i++)
            {
                int tx = x + Mathf.RoundToInt(Hash(i, seed, 216) * w), ty = y + Mathf.RoundToInt(Hash(i, seed + 9, 216) * h);
                canvas.FillRect(tx, ty, 6, 3, Rgb(80, 76, 66), 0.6f);
            }
        }
        return canvas;
    }

    // A row of ten lock-up garages, each painted by whoever owns it.
    static Canvas Garages()
    {
        const int width = 2048, height = 256;
        var canvas = new Canvas(width, height, Rgb(120, 118, 110));
        for (int i = 0; i < 10; i++)
        {
            int x = i * width / 10;
            Color paint = Color.Lerp(Pastel(Hash(i, 1, 221)) * 0.6f, Rgb(96, 62, 44), Hash(i, 2, 221) * 0.8f);
            for (int y = 0; y < height; y++)
            {
                for (int px = x; px < x + width / 10; px++)
                {
                    float u = (float)px / width, v = (float)y / height;
                    int inside = px - x;
                    bool frame = inside < 10 || inside > 194 || y > 226;
                    float rust = Mathf.InverseLerp(0.46f, 0.72f, Fractal(u, v, 50, 5, 5, 222 + i));
                    Color c = frame ? Rgb(120, 118, 110) * Mathf.Lerp(0.66f, 1.1f, Fractal(u, v, 40, 4, 4, 233)) : Color.Lerp(paint, Rgb(88, 52, 34), rust);
                    if (!frame && inside % 34 < 2) c *= 0.7f;
                    if (!frame && Mathf.Abs(inside - 102) < 2) c *= 0.4f;
                    c *= Mathf.Lerp(0.56f, 1f, Mathf.InverseLerp(0f, 40f, y));
                    canvas[px, y] = Opaque(c);
                }
            }
            DrawText(canvas, (31 + i).ToString(), x + 102f, 190f, 26, Rgb(214, 208, 190));
        }
        return canvas;
    }

    // Board-marked concrete, poured as a single sweep, rained on since the day it was struck.
    static Canvas ShelterConcrete()
    {
        const int size = 512;
        var canvas = new Canvas(size, size, Color.black);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float board = 0.92f + 0.14f * Hash(y / 28, 1, 241);
                float grain = 1f + 0.05f * Mathf.Sin((v * 90f + Fractal(u, v, 7, 3, 3, 242) * 16f));
                float stain = Mathf.Lerp(0.58f, 1.12f, Fractal(u, v, 7, 5, 5, 243));
                Color c = Rgb(142, 138, 128) * board * grain * stain * (0.93f + 0.13f * Hash(x, y, 244));
                if (y % 28 < 1) c *= 0.7f;
                canvas[x, y] = Opaque(c);
            }
        }

        // People have been here.
        foreach ((float x, float y, string text, Color color) in new[] { (140f, 300f, "Г + М", Rgb(40, 42, 60)), (360f, 140f, "14", Rgb(120, 36, 32)), (300f, 400f, "АЗ БЯХ ТУК", Rgb(30, 30, 34)) })
            DrawText(canvas, text, x, y, 36, color, 2f);
        return canvas;
    }

    // Workers and a rising sun in chips of glass, from when there was a budget for such things. A good many have fallen out.
    static Canvas Mosaic()
    {
        const int width = 1024, height = 256, tile = 8;
        var canvas = new Canvas(width, height, Rgb(86, 84, 78));
        var picture = new Canvas(width, height, Rgb(170, 154, 110));

        // The picture first, then cut up into chips.
        Vector2 sun = new Vector2(width * 0.5f, 40f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x, y) - sun;
                float angle = Mathf.Atan2(p.x, p.y), ray = Mathf.Repeat(angle * 7f, 1f);
                Color c = ray < 0.5f ? Rgb(196, 148, 66) : Rgb(170, 92, 52);
                if (p.magnitude < 88f) c = Rgb(216, 176, 80);
                if (p.magnitude > 300f) c = ray < 0.5f ? Rgb(60, 92, 104) : Rgb(44, 72, 90);
                picture[x, y] = c;
            }
        }
        foreach ((float x, float tall, Color coat) in new[] { (170f, 190f, Rgb(40, 56, 88)), (290f, 210f, Rgb(120, 40, 34)), (734f, 206f, Rgb(40, 56, 88)), (854f, 186f, Rgb(50, 70, 60)) })
        {
            picture.FillRect(Mathf.RoundToInt(x - 26f), 0, 52, Mathf.RoundToInt(tall * 0.72f), coat);
            picture.FillCircle(x, tall * 0.84f, 24f, Rgb(202, 160, 120));
            picture.DrawLine(new Vector2(x + 20f, tall * 0.66f), new Vector2(x + 66f, tall * 1.02f), 18f, coat);
        }

        for (int row = 0; row < height / tile; row++)
        {
            for (int column = 0; column < width / tile; column++)
            {
                float gone = Fractal(column * tile / (float)width, row * tile / (float)height, 7, 3, 4, 251);
                if (gone > 0.56f + 0.1f * Hash(column, row, 252)) continue;
                Color chip = picture[column * tile + tile / 2, row * tile + tile / 2] * (0.82f + 0.32f * Hash(column, row, 253));
                canvas.FillRect(column * tile + 1, row * tile + 1, tile - 1, tile - 1, Opaque(chip));
            }
        }
        return canvas;
    }

    static Canvas Timetable14()
    {
        Color paper = Rgb(204, 198, 174), ink = Rgb(30, 30, 40);
        var canvas = new Canvas(256, 360, paper);
        DrawText(canvas, "14", 128f, 296f, 52, ink, 2f);
        DrawText(canvas, "ДЕПО  -  ЦЕНТЪР", 128f, 264f, 20, ink, 1f);
        canvas.FillRect(20, 254, 216, 2, ink);
        for (int row = 0; row < 11; row++)
        {
            int y = 232 - row * 20;
            canvas.FillRect(32, y, 30, 8, ink, 0.8f);
            for (int i = 0; i < 6; i++)
                if (Hash(row, i, 254) > 0.4f) canvas.FillRect(84 + i * 26, y, 18, 8, ink, 0.6f);
        }
        return Grimed(canvas, 0.8f);
    }

    static Canvas RoadWorks()
    {
        var canvas = new Canvas(256, 128, Rgb(214, 170, 50));
        DrawText(canvas, "ЗАТВОРЕНО", 128f, 72f, 34, Rgb(20, 20, 20), 1f);
        DrawText(canvas, "РЕМОНТ", 128f, 28f, 30, Rgb(20, 20, 20), 3f);
        return Grimed(canvas, 0.7f);
    }

    static Canvas Barricade()
    {
        const int size = 128;
        var canvas = new Canvas(size, size / 4, Color.white);
        for (int y = 0; y < size / 4; y++)
            for (int x = 0; x < size; x++)
                canvas[x, y] = Opaque(Mathf.Repeat((x + y) / 32f, 1f) < 0.5f ? Rgb(170, 44, 34) : Rgb(208, 204, 192));
        return Grimed(canvas, 0.8f);
    }

    // The line's retired cars: one long strip, windows, doors and all, gone the colour of the weather.
    static Canvas DeadCar()
    {
        const int width = 1024, height = 192;
        var canvas = new Canvas(width, height, Rgb(70, 92, 112));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width, v = (float)y / height;
                Color paint = v > 0.54f ? Rgb(150, 144, 124) : Rgb(62, 82, 104);
                if (Mathf.Abs(v - 0.54f) < 0.012f) paint = Rgb(120, 40, 34);
                float rust = Mathf.InverseLerp(0.48f, 0.7f, Fractal(u, v, 24, 4, 5, 261));
                canvas[x, y] = Opaque(Color.Lerp(paint, Rgb(96, 62, 42), rust) * Mathf.Lerp(0.6f, 1.08f, Fractal(u, v, 10, 3, 4, 262)) * Mathf.Lerp(0.6f, 1f, v * 3f));
            }
        }
        for (int i = 0; i < 9; i++)
        {
            int x = 40 + i * 110;
            if (i % 3 == 1)
            {
                canvas.FillRect(x, 28, 76, 138, Rgb(50, 58, 66));
                canvas.FillRect(x + 37, 28, 2, 138, Rgb(20, 20, 20));
                canvas.FillRect(x + 8, 92, 26, 60, Rgb(26, 28, 32));
                canvas.FillRect(x + 42, 92, 26, 60, Rgb(26, 28, 32));
            }
            else
            {
                canvas.FillRect(x, 90, 92, 62, Rgb(16, 18, 20));
                canvas.FillRect(x + 3, 93, 86, 56, Hash(i, 1, 263) > 0.7f ? Rgb(60, 64, 66) : Rgb(26, 30, 34));
            }
        }
        return canvas;
    }
}
