using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static SceneForge;

// The town: panel blocks with their balconies and repairs, a street running east from the depot gate into the fog, bare
// trees, parked cars and a chimney a long way off. None of it is ever close, so it's lit by what's painted on it, and the
// fog does the rest: the nearest block has every balcony on it, and the furthest are shapes.
public static partial class StationBuilder
{
    const float BayWidth = 3.6f, FloorHeight = 2.8f, BlockPlinth = TextureForge.BlockPlinthPixels / 34.3f, BlockParapet = TextureForge.BlockTopPixels / 34.3f;
    const float BlockDepth = 12f, BalconyOut = 1.2f, ParapetHeight = 1.08f, SlabThick = 0.2f;
    const float StreetWidth = 7f, StreetStart = RoadXMax + 0.16f, StreetLength = 240f, FarPavement = 3f;

    static readonly Rect WholeSheet = new Rect(0f, 0f, 1f, 1f);

    static Dictionary<string, MeshKit> sceneryKits;

    // Where the lamps are, for the glow round each one.
    static List<Vector3> fogGlows;

    static MeshKit Scenery(string texture)
    {
        if (!sceneryKits.TryGetValue(texture, out MeshKit kit)) sceneryKits[texture] = kit = new MeshKit();
        return kit;
    }

    // A rectangle with a texture, or a piece of one, laid on it the right way round for whoever is looking at it.
    // The side that shows is Cross(across, up).
    static void Sheet(MeshKit kit, Vector3 corner, Vector3 across, Vector3 up, Rect uv)
    {
        kit.Quad(corner, corner + across, corner + across + up, corner + up,
            new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax));
    }

    static void Cuboid(MeshKit kit, Vector3 foot, Vector3 size, Rect sides, Rect top)
    {
        Vector3 h = new Vector3(size.x / 2f, 0f, size.z / 2f);
        Vector3 up = Vector3.up * size.y, west = foot + new Vector3(-h.x, 0f, -h.z);
        Sheet(kit, west, Vector3.forward * size.z, up, sides);
        Sheet(kit, west + Vector3.forward * size.z, Vector3.right * size.x, up, sides);
        Sheet(kit, west + new Vector3(size.x, 0f, size.z), Vector3.back * size.z, up, sides);
        Sheet(kit, west + Vector3.right * size.x, Vector3.left * size.x, up, sides);
        Sheet(kit, west + Vector3.up * size.y, Vector3.forward * size.z, Vector3.right * size.x, top);
    }

    // Everything out past the far kerb.
    static void BuildTown(Transform parent)
    {
        BuildGround();

        float north = GateZ + 12f, longWay = 16 * BayWidth;

        // The two nearest, either side of the street, have everything on them.
        Block(new Vector3(32f, 0f, north), 16, 8, true, 701, "T_BlockLong");
        Block(new Vector3(32f, 0f, GateZ - 12f - longWay), 16, 8, true, 702, "T_BlockLong");

        // The next rank is further back and less finished, and after that they're only shapes in the fog.
        foreach ((float x, float z, int floors, bool balconies) in new[]
        {
            (66f, north + 8f, 10, true), (66f, GateZ - 16f - longWay, 8, true), (32f, north + longWay + 26f, 8, true), (32f, GateZ - 34f - longWay * 2f, 8, true),
            (102f, north - 2f, 8, false), (102f, GateZ - 14f - longWay, 12, false), (70f, north + longWay + 34f, 12, false), (70f, GateZ - 46f - longWay * 2f, 8, false),
            (140f, north + 20f, 14, false), (146f, GateZ - 40f - longWay, 10, false), (120f, north + longWay + 52f, 16, false), (112f, GateZ - 70f - longWay * 2f, 12, false),
            (190f, north - 30f, 16, false), (204f, GateZ + 10f - longWay, 12, false), (180f, north + longWay + 60f, 12, false), (168f, GateZ - 100f - longWay * 2f, 16, false),
        })
        {
            Block(new Vector3(x, 0f, z), 16, floors, balconies, Mathf.RoundToInt(x + z), "T_BlockLongPlain");
        }

        BuildKiosk();
        BuildGarages();
        BuildChimney();
        BuildStreetFurniture();

        // A few that the depot has given up on, out on the yard roads.
        foreach ((float x, float z) in new[] { (-6f, TerminalZ - 18f), (-6f, TerminalZ + 12f), (-11f, TerminalZ + 2f), (-11f, TerminalZ + 34f) })
            DeadCar(x, z);

        foreach (KeyValuePair<string, MeshKit> kit in sceneryKits) CommitScenery(kit.Key, kit.Key, kit.Value, parent);
        CommitLampGlows(parent);

        var dome = new MeshKit();
        var profile = new Vector2[17];
        const float radius = 390f;
        for (int i = 0; i < profile.Length; i++)
        {
            float a = Mathf.Lerp(-0.2f, Mathf.PI / 2f, i / (profile.Length - 1f));
            profile[i] = new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
        }
        dome.Lathe(Vector3.zero, profile, 32, Vector2.one);

        var sky = new GameObject("Sky");
        sky.transform.position = new Vector3(20f, 0f, TerminalZ);
        sky.AddComponent<MeshFilter>().sharedMesh = SaveMesh(dome, "Sky");
        var renderer = sky.AddComponent<MeshRenderer>();
        Material material = LoadOrCreateMaterial("Sky", "Metropoliten/Sky");
        material.SetTexture("_Noise", FogForge.Noise());
        material.SetColor("_Low", new Color(0.5f, 0.515f, 0.54f));
        material.SetColor("_High", new Color(0.31f, 0.34f, 0.39f));
        material.SetColor("_Dawn", new Color(0.36f, 0.27f, 0.2f));
        EditorUtility.SetDirty(material);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static void CommitScenery(string name, string texture, MeshKit kit, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        Mesh mesh = kit.ToMesh(name);
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/{name}.asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = LoadOrCreateMaterial($"Scenery{name}", "Universal Render Pipeline/Unlit");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(texture)));
        EditorUtility.SetDirty(material);

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    // Every corner of a glow sits at its lamp, and the shader pushes them apart to face the viewer.
    static void CommitLampGlows(Transform parent)
    {
        var kit = new MeshKit();
        foreach (Vector3 lamp in fogGlows)
            kit.Quad(lamp, lamp, lamp, lamp, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));

        var go = new GameObject("LampGlows");
        go.transform.SetParent(parent);
        Mesh mesh = kit.ToMesh("LampGlows");
        mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 20f);
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/LampGlows.asset");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        Material material = LoadOrCreateMaterial("LampGlows", "Metropoliten/FogGlow");
        material.SetColor("_Color", new Color(0.8f, 0.56f, 0.32f));
        material.SetFloat("_Size", 3.4f);
        material.SetFloat("_Falloff", 0.012f);
        EditorUtility.SetDirty(material);

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
    }

    static void BuildGround()
    {
        Vector2 Tile(float size) => new Vector2(size, size);

        Scenery("T_Verge").Panel(new Vector3(StreetStart, 0.01f, TerminalZ - 400f), Vector3.forward * 900f, Vector3.right * 520f, Tile(16f));
        Scenery("T_StreetEast").Panel(new Vector3(StreetStart, 0.03f, GateZ - StreetWidth / 2f), Vector3.forward * StreetWidth, Vector3.right * StreetLength, Tile(7f));

        // The pavement down the far side of the road, and along both sides of the street.
        Scenery("T_Slabs").Panel(new Vector3(StreetStart, 0.05f, TerminalZ - 200f), Vector3.forward * 560f, Vector3.right * FarPavement, SlabTile);
        foreach (float side in new[] { -1f, 1f })
        {
            float z = GateZ + side * (StreetWidth / 2f + 1.2f);
            Scenery("T_Slabs").Panel(new Vector3(StreetStart, 0.05f, z - 1.2f), Vector3.forward * 2.4f, Vector3.right * StreetLength, SlabTile);
            Scenery("T_Kerb").Panel(new Vector3(StreetStart, 0.1f, GateZ + side * (StreetWidth / 2f + 0.04f) - 0.08f), Vector3.forward * 0.16f, Vector3.right * StreetLength, KerbTile);
        }
        Scenery("T_Kerb").Panel(new Vector3(RoadXMax, 0.14f, TerminalZ - 240f), Vector3.forward * 660f, Vector3.right * 0.16f, KerbTile);
    }

    // A panel block, standing with its corner at foot and running north, its long sides east and west.
    static void Block(Vector3 foot, int bays, int floors, bool balconies, int seed, string frontTexture)
    {
        float length = bays * BayWidth, height = BlockPlinth + floors * FloorHeight + BlockParapet;
        float fraction = height / (BlockPlinth + 8f * FloorHeight + BlockParapet);

        MeshKit front = Scenery(frontTexture), rear = Scenery("T_BlockLongPlain"), ends = Scenery("T_BlockGable"), parts = Scenery("T_BlockParts");
        Vector3 up = Vector3.up * height, along = Vector3.forward * length, across = Vector3.right * BlockDepth;

        Wall(front, foot, along, floors);
        Wall(rear, foot + along + across, -along, floors);
        Sheet(ends, foot + across, -across, up, new Rect(0f, 0f, 1f, fraction));
        Sheet(ends, foot + along, across, up, new Rect(0f, 0f, 1f, fraction));
        Sheet(parts, foot + Vector3.up * height, along, across, TextureForge.BlockPart(0, 2));

        // What's on the roof.
        Rect plain = TextureForge.BlockPart(2, 2), roof = TextureForge.BlockPart(0, 2);
        foreach (float at in new[] { 0.2f, 0.72f })
        {
            var housing = foot + new Vector3(BlockDepth / 2f, height, length * at);
            Cuboid(parts, housing, new Vector3(3.4f, 2.5f, 4.6f), plain, roof);
            for (int i = 0; i < 3; i++)
            {
                var mast = housing + new Vector3(-1.2f + i * 1.4f, 2.5f, 3.4f + i * 0.7f);
                Scenery("T_Bark").Tube(mast, mast + Vector3.up * (2.2f + TextureForge.Hash(seed, i, 711) * 1.8f), 0.022f, 4, MetalTile);
                Scenery("T_Bark").Tube(mast + new Vector3(-0.5f, 1.8f, 0f), mast + new Vector3(0.5f, 1.8f, 0f), 0.012f, 4, MetalTile);
            }
        }

        if (!balconies) return;

        for (int floor = 1; floor < floors; floor++)
        {
            float y = foot.y + BlockPlinth + floor * FloorHeight;
            for (int bay = 0; bay < bays; bay++)
            {
                if (!TextureForge.BlockBalcony(bay)) continue;

                // Bay 0 is at the north end, on the left as you look at the wall.
                float zMax = foot.z + length - bay * BayWidth, zMin = zMax - BayWidth, x = foot.x - BalconyOut;
                float pick = TextureForge.Hash(bay / 4, floor, seed), pick2 = TextureForge.Hash(bay, floor, seed + 1);

                Sheet(parts, new Vector3(x, y - SlabThick, zMin), Vector3.forward * BayWidth, Vector3.up * SlabThick, TextureForge.BlockPart(3, 1));
                Sheet(parts, new Vector3(x, y - SlabThick, zMax), Vector3.back * BayWidth, Vector3.right * BalconyOut, TextureForge.BlockPart(2, 1));

                if (pick2 > 0.72f)
                {
                    // Glassed in, floor to ceiling, by whoever could get the frames.
                    Sheet(parts, new Vector3(x, y, zMin), Vector3.forward * BayWidth, Vector3.up * (FloorHeight - SlabThick), TextureForge.BlockPart(pick2 > 0.86f ? 0 : 1, 1));
                }
                else
                {
                    int style = pick < 0.5f ? 0 : pick < 0.72f ? 1 : pick < 0.88f ? 2 : 3;
                    Sheet(parts, new Vector3(x, y, zMin), Vector3.forward * BayWidth, Vector3.up * ParapetHeight, TextureForge.BlockPart(style, 0));
                    Sheet(parts, new Vector3(x + 0.08f, y + ParapetHeight, zMin), Vector3.left * 0.08f, Vector3.forward * BayWidth, TextureForge.BlockPart(2, 2));

                    // Someone's washing.
                    if (TextureForge.Hash(bay, floor, seed + 2) > 0.8f)
                        Sheet(parts, new Vector3(x + 0.5f, y + ParapetHeight + 0.2f, zMin + 0.8f), Vector3.forward * 1.8f, Vector3.up * 0.7f, TextureForge.BlockPart(3, 2));
                }

                // Each pair of balconies has a concrete fin either end.
                if (bay % 4 == 1)
                {
                    Sheet(parts, new Vector3(foot.x, y - SlabThick, zMax), Vector3.left * BalconyOut, Vector3.up * FloorHeight, TextureForge.BlockPart(1, 2));
                    Sheet(parts, new Vector3(x, y - SlabThick, zMax), Vector3.right * BalconyOut, Vector3.up * FloorHeight, TextureForge.BlockPart(1, 2));
                }
                else
                {
                    Sheet(parts, new Vector3(x, y - SlabThick, zMin), Vector3.right * BalconyOut, Vector3.up * FloorHeight, TextureForge.BlockPart(1, 2));
                    Sheet(parts, new Vector3(foot.x, y - SlabThick, zMin), Vector3.left * BalconyOut, Vector3.up * FloorHeight, TextureForge.BlockPart(1, 2));
                }
            }
        }
    }

    // The wall of a block, a floor at a time, so that a tall one can go round the same painted floors again.
    static void Wall(MeshKit kit, Vector3 corner, Vector3 along, int floors)
    {
        const int painted = 8;
        float total = TextureForge.BlockPlinthPixels + painted * TextureForge.BlockFloorPixels + TextureForge.BlockTopPixels;
        float plinth = TextureForge.BlockPlinthPixels / total, floor = TextureForge.BlockFloorPixels / total;

        Sheet(kit, corner, along, Vector3.up * BlockPlinth, new Rect(0f, 0f, 1f, plinth));
        for (int i = 0; i < floors; i++)
            Sheet(kit, corner + Vector3.up * (BlockPlinth + i * FloorHeight), along, Vector3.up * FloorHeight, new Rect(0f, plinth + (i % painted) * floor, 1f, floor));
        Sheet(kit, corner + Vector3.up * (BlockPlinth + floors * FloorHeight), along, Vector3.up * BlockParapet, new Rect(0f, 1f - TextureForge.BlockTopPixels / total, 1f, TextureForge.BlockTopPixels / total));
    }

    // The line's old cars, left out on the yard roads.
    static void DeadCar(float x, float z)
    {
        const float length = 14f, half = 1.35f, bottom = TrackY + 0.5f, top = TrackY + 3f;
        MeshKit side = Scenery("T_DeadCar"), parts = Scenery("T_BlockParts");
        float south = z - length / 2f, height = top - bottom;

        Sheet(side, new Vector3(x + half, bottom, south + length), Vector3.back * length, Vector3.up * height, WholeSheet);
        Sheet(side, new Vector3(x - half, bottom, south), Vector3.forward * length, Vector3.up * height, WholeSheet);
        Sheet(side, new Vector3(x + half, bottom, south), Vector3.left * (half * 2f), Vector3.up * height, new Rect(0f, 0f, 0.13f, 1f));
        Sheet(side, new Vector3(x - half, bottom, south + length), Vector3.right * (half * 2f), Vector3.up * height, new Rect(0f, 0f, 0.13f, 1f));

        var crown = new List<Vector3>();
        for (int i = 0; i <= 8; i++)
        {
            float a = Mathf.PI * i / 8f;
            crown.Add(new Vector3(x + Mathf.Cos(a) * half, top + Mathf.Sin(a) * 0.32f, south));
        }
        Scenery("T_Bark").Sweep(crown, Vector3.forward * length, new Vector2(3f, 3f));
        Scenery("T_Bark").Box(new Vector3(x, bottom - 0.2f, z), new Vector3(2.4f, 0.4f, length - 1f), new Vector2(3f, 3f));
    }

    // A steel kiosk on the corner of the street, shut until someone comes with the papers. It's on the far side of the road.
    static void BuildKiosk()
    {
        const float wide = 3.2f, deep = 2.2f, tall = 2.6f;
        var foot = new Vector3(RoadXMax + 5.4f, 0.05f, GateZ + 7.2f);
        MeshKit kit = Scenery("T_Kiosk");
        Vector3 up = Vector3.up * tall, west = foot + new Vector3(-deep / 2f, 0f, -wide / 2f);

        Sheet(kit, west, Vector3.forward * wide, up, new Rect(0f, 0f, 0.4f, 1f));
        Sheet(kit, west + Vector3.forward * wide, Vector3.right * deep, up, new Rect(0.4f, 0f, 0.27f, 1f));
        Sheet(kit, west + new Vector3(deep, 0f, wide), Vector3.back * wide, up, new Rect(0.4f, 0f, 0.4f, 1f));
        Sheet(kit, west + Vector3.right * deep, Vector3.left * deep, up, new Rect(0.67f, 0f, 0.27f, 1f));
        Cuboid(Scenery("T_BlockParts"), foot + Vector3.up * tall, new Vector3(deep + 0.7f, 0.14f, wide + 0.7f), TextureForge.BlockPart(2, 1), TextureForge.BlockPart(0, 2));
    }

    // Lock-ups behind the pavement, south of the street.
    static void BuildGarages()
    {
        const float x = 26.4f, deep = 5f, tall = 2.5f, length = 34f;
        float south = GateZ - 7f - length;
        MeshKit parts = Scenery("T_BlockParts");
        Sheet(Scenery("T_Garages"), new Vector3(x, 0.03f, south), Vector3.forward * length, Vector3.up * tall, WholeSheet);
        Cuboid(parts, new Vector3(x + deep / 2f + 0.2f, tall, south + length / 2f), new Vector3(deep + 0.7f, 0.18f, length + 0.4f), TextureForge.BlockPart(2, 1), TextureForge.BlockPart(0, 2));
        Sheet(parts, new Vector3(x, 0.03f, south + length), Vector3.right * deep, Vector3.up * tall, TextureForge.BlockPart(1, 2));
    }

    // The heating plant's stack, well beyond everything, with the top of it clear of the fog.
    static void BuildChimney()
    {
        var foot = new Vector3(330f, 0f, GateZ + 190f);
        const int bands = 7, sides = 16;
        const float bandHeight = 22f;
        for (int i = 0; i < bands; i++)
        {
            float from = Mathf.Lerp(9f, 5f, i / (float)bands), to = Mathf.Lerp(9f, 5f, (i + 1f) / bands);
            MeshKit kit = Scenery(i >= bands - 3 && i % 2 == 0 ? "T_ChimneyRed" : "T_ChimneyPale");
            kit.Lathe(foot + Vector3.up * (i * bandHeight), new[] { new Vector2(from, 0f), new Vector2(to, bandHeight) }, sides, new Vector2(1000f, 1000f));
        }
    }

    // What runs along the far side of the road and up the street: lamps, poles, wires and bare trees, and the odd car.
    static void BuildStreetFurniture()
    {
        MeshKit bark = Scenery("T_Bark");

        // Up the street, a lamp every so far on each side, out to where the fog takes them.
        for (int i = 0; i < 9; i++)
        {
            float x = StreetStart + 8f + i * 25f;
            foreach (float side in new[] { -1f, 1f })
            {
                var post = new Vector3(x + side * 6f, 0.05f, GateZ + side * (StreetWidth / 2f + 0.5f));
                Lamp(bark, post, Vector3.back * side);
                fogGlows.Add(post + Vector3.back * side * 1.86f + Vector3.up * 6.68f);
            }
        }

        // Down the far side of the road.
        for (float z = TerminalZ - 170f; z < TerminalZ + 340f; z += 32f)
        {
            if (Mathf.Abs(z - GateZ) < 10f) continue;
            var post = new Vector3(RoadXMax + 0.5f, 0.05f, z + 16f);
            Lamp(bark, post, Vector3.left);
            fogGlows.Add(post + Vector3.left * 1.86f + Vector3.up * 6.68f);
        }

        // Wooden poles and the wires between them, sagging.
        const float wireY = 8.4f;
        Vector3 previous = Vector3.zero;
        for (float z = TerminalZ - 180f; z < TerminalZ + 340f; z += 38f)
        {
            var pole = new Vector3(RoadXMax + 3.6f, 0f, z + TextureForge.Hash(Mathf.RoundToInt(z), 1, 721) * 3f);
            if (Mathf.Abs(pole.z - GateZ) < StreetWidth) continue;

            bark.Tube(pole, pole + Vector3.up * (wireY + 0.4f), 0.11f, 6, MetalTile);
            bark.Tube(pole + new Vector3(-0.8f, wireY, 0f), pole + new Vector3(0.8f, wireY, 0f), 0.04f, 4, MetalTile);
            if (previous != Vector3.zero)
            {
                foreach (float across in new[] { -0.7f, -0.25f, 0.25f, 0.7f })
                {
                    Vector3 last = previous + new Vector3(across, wireY + 0.05f, 0f);
                    for (int i = 1; i <= 8; i++)
                    {
                        float t = i / 8f;
                        Vector3 next = Vector3.Lerp(previous, pole, t) + new Vector3(across, wireY + 0.05f - Mathf.Sin(t * Mathf.PI) * 0.7f, 0f);
                        bark.Tube(last, next, 0.009f, 3, MetalTile);
                        last = next;
                    }
                }
            }
            previous = pole;
        }

        // Limes down the far pavement, and more along the street and round the blocks.
        int seed = 730;
        foreach (float z in new[] { -48f, -39f, -30f, -20f, -11f, 9f, 18f, 27f, 38f, 46f, 57f, 68f, 78f })
            BareTree(bark, new Vector3(RoadXMax + 5.6f + TextureForge.Hash(seed, 1, 731) * 1.6f, 0f, GateZ + z), 8.5f + TextureForge.Hash(seed, 2, 731) * 3.6f, seed++);
        foreach (float x in new[] { 34f, 48f, 60f, 76f, 92f, 112f })
        {
            foreach (float side in new[] { -1f, 1f })
                BareTree(bark, new Vector3(x + side * 3f, 0f, GateZ + side * 7.4f), 7.5f + TextureForge.Hash(seed, 2, 731) * 4f, seed++);
        }
        foreach ((float x, float z) in new[] { (24f, 1385f), (27f, 1410f), (25f, 1290f), (29f, 1262f), (26f, 1440f), (29.6f, 1320f) })
            BareTree(bark, new Vector3(x, 0f, z), 9f + TextureForge.Hash(seed, 2, 731) * 3f, seed++);

        // Cars that haven't gone anywhere in a while.
        foreach ((Vector3 at, float yaw, string paint) in new[]
        {
            (new Vector3(23.6f, 0.05f, GateZ - 5.6f), 90f, "CarPale"), (new Vector3(30f, 0.05f, GateZ - 5.6f), 90f, "CarRed"),
            (new Vector3(38f, 0.05f, GateZ + 2.6f), 90f, "CarDark"), (new Vector3(52f, 0.05f, GateZ - 2.5f), 92f, "CarPale"),
            (new Vector3(RoadXMax + 1.4f, 0.05f, GateZ + 32f), 0f, "CarRed"), (new Vector3(29f, 0.05f, GateZ + 14f), 6f, "CarDark"),
        })
        {
            ParkedCar(Kit(paint), Kit("CarGlass"), Kit("CarTrim"), at, yaw);
        }
    }

    // A trunk that forks and forks again, thinner each time, with the last of it no more than a scratch against the fog.
    public static void BareTree(MeshKit kit, Vector3 root, float height, int seed)
    {
        var random = new System.Random(seed);
        float Next() => (float)random.NextDouble();
        Vector2 tile = new Vector2(0.6f, 2f);

        void Branch(Vector3 from, Vector3 way, float length, float radius, int depth)
        {
            int pieces = depth == 0 ? 3 : 2;
            Vector3 at = from;
            for (int i = 0; i < pieces; i++)
            {
                way = (way + new Vector3(Next() - 0.5f, Next() * 0.36f - 0.06f, Next() - 0.5f) * (0.3f + depth * 0.1f)).normalized;
                Vector3 next = at + way * (length / pieces);
                kit.Tube(at, next, radius * (1f - 0.2f * i / pieces), depth < 2 ? 6 : depth < 3 ? 4 : 3, tile);
                at = next;

                // Anything finer than this is thinner than a pixel by the time the fog lets you see it.
                if (depth >= 4) continue;
                int forks = depth == 0 ? (i == pieces - 1 ? 4 : i == 1 ? 2 : 0) : (i == pieces - 1 ? 3 : Next() > 0.5f ? 1 : 0);
                for (int f = 0; f < forks; f++)
                {
                    float a = Next() * Mathf.PI * 2f, spread = 0.55f + Next() * 0.5f;
                    Vector3 sideways = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Branch(at, (way + sideways * spread + Vector3.up * 0.2f).normalized, length * (0.62f + Next() * 0.14f), radius * 0.58f, depth + 1);
                }
            }
        }

        Branch(root, Vector3.up, height * 0.42f, height * 0.024f, 0);
    }

    // The little saloon every family here had, modelled the way it was pressed: one skin, nose to tail, drawn over a side
    // view. The cabin grows out of the body where the side view rises above the waistline, and the glass is part of the skin.
    public static void ParkedCar(MeshKit paint, MeshKit glass, MeshKit dark, Vector3 at, float yaw)
    {
        // Along the car, and how high its top line is at each point.
        (float z, float top)[] line =
        {
            (-2.07f, 0.46f), (-2.05f, 0.63f), (-1.97f, 0.73f), (-1.7f, 0.77f), (-1.3f, 0.82f), (-0.92f, 0.87f), (-0.76f, 1.02f), (-0.58f, 1.2f), (-0.42f, 1.33f),
            (-0.26f, 1.37f), (0.2f, 1.385f), (0.62f, 1.38f), (0.82f, 1.34f), (1.02f, 1.17f), (1.2f, 1.02f), (1.36f, 0.93f), (1.7f, 0.91f), (1.96f, 0.87f), (2.04f, 0.64f), (2.07f, 0.46f),
        };
        const int half = 7, ribs = half * 2 + 1;
        const float sill = 0.25f;
        int stations = line.Length;
        float Waist(float z) => Mathf.Lerp(0.87f, 0.93f, Mathf.InverseLerp(-0.92f, 1.36f, z));

        var skin = new Vector3[stations, ribs];
        for (int i = 0; i < stations; i++)
        {
            float z = line[i].z, top = line[i].top, waist = Mathf.Min(Waist(z), top);
            float wide = 0.8f * (1f - 0.09f * Mathf.Pow(Mathf.Abs(z) / 2.07f, 4f));
            float cabin = Mathf.Clamp01((top - waist) / 0.42f), inward = Mathf.Lerp(0.03f, 0.2f, cabin);
            Vector2[] side =
            {
                new Vector2(wide - 0.01f, sill), new Vector2(wide + 0.012f, (sill + waist) * 0.5f), new Vector2(wide, waist - 0.05f), new Vector2(wide - 0.03f, waist),
                new Vector2(wide - inward * 0.5f, Mathf.Lerp(waist, top, 0.28f)), new Vector2(wide - inward * 1.3f, Mathf.Lerp(waist, top, 0.84f)), new Vector2(wide - inward * 2f - 0.05f, top),
            };
            for (int r = 0; r < half; r++)
            {
                skin[i, r] = new Vector3(-side[r].x, side[r].y, z);
                skin[i, ribs - 1 - r] = new Vector3(side[r].x, side[r].y, z);
            }
            skin[i, half] = new Vector3(0f, top + 0.02f, z);
        }

        Matrix4x4 place = Matrix4x4.TRS(at, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
        Vector3 Normal(int i, int r)
        {
            Vector3 along = skin[Mathf.Min(i + 1, stations - 1), r] - skin[Mathf.Max(i - 1, 0), r];
            Vector3 round = skin[i, Mathf.Min(r + 1, ribs - 1)] - skin[i, Mathf.Max(r - 1, 0)];
            Vector3 normal = Vector3.Cross(along, round).normalized;
            Vector3 centre = new Vector3(0f, 0.6f, 0f);
            if (Vector3.Dot(normal, skin[i, r] - centre) < 0f) normal = -normal;
            return place.MultiplyVector(normal);
        }

        // Windscreen, back window, and the side windows with a pillar between them.
        bool Glass(int i, int r)
        {
            float z = (line[i].z + line[i + 1].z) / 2f;
            bool topRibs = r >= 4 && r < ribs - 5;
            if (z > -0.9f && z < -0.42f) return topRibs;
            if (z > 0.82f && z < 1.36f) return topRibs;
            bool side = r == 4 || r == ribs - 6;
            return side && z > -0.42f && z < 0.82f && !(z > 0.05f && z < 0.3f);
        }

        Vector2 tile = new Vector2(2f, 2f);
        for (int i = 0; i < stations - 1; i++)
        {
            for (int r = 0; r < ribs - 1; r++)
            {
                MeshKit kit = Glass(i, r) ? glass : paint;
                kit.SkinQuad(place.MultiplyPoint3x4(skin[i, r]), place.MultiplyPoint3x4(skin[i + 1, r]), place.MultiplyPoint3x4(skin[i + 1, r + 1]), place.MultiplyPoint3x4(skin[i, r + 1]),
                    Normal(i, r), Normal(i + 1, r), Normal(i + 1, r + 1), Normal(i, r + 1), tile);
            }
        }

        // Everything under the skin and bolted on to it.
        Vector3 P(float x, float y, float z) => place.MultiplyPoint3x4(new Vector3(x, y, z));
        dark.RoundedBox(place * Matrix4x4.Translate(new Vector3(0f, 0.26f, 0f)), new Vector3(1.5f, 0.16f, 3.96f), 0.04f, 1, tile);
        foreach ((float z, float reach) in new[] { (-2.09f, -1f), (2.09f, 1f) })
            dark.RoundedBox(place * Matrix4x4.Translate(new Vector3(0f, 0.42f, z)), new Vector3(1.66f, 0.09f, 0.1f), 0.03f, 2, tile);
        foreach (float x in new[] { -0.54f, 0.54f })
        {
            dark.RoundedBox(place * Matrix4x4.Translate(new Vector3(x, 0.62f, -2.04f)), new Vector3(0.34f, 0.15f, 0.05f), 0.02f, 1, tile);
            glass.RoundedBox(place * Matrix4x4.Translate(new Vector3(x, 0.62f, -2.065f)), new Vector3(0.28f, 0.1f, 0.02f), 0.01f, 1, tile);
        }

        foreach (float z in new[] { -1.27f, 1.24f })
        {
            foreach (float x in new[] { -1f, 1f })
            {
                Vector3 outward = place.MultiplyVector(Vector3.right * x);
                Vector3 hub = P(x * 0.72f, 0.29f, z);
                dark.Disc(P(x * 0.815f, 0.33f, z), outward, Vector3.up, 0.34f, 16);
                dark.Tube(hub - outward * 0.08f, hub + outward * 0.1f, 0.285f, 14, tile);
                dark.Disc(hub + outward * 0.1f, outward, Vector3.up, 0.285f, 14);
                paint.Disc(hub + outward * 0.102f, outward, Vector3.up, 0.13f, 10);
            }
        }
    }
}
