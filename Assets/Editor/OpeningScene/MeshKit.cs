using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public readonly struct ArchProfile
{
    readonly float centerX, centerY, radius, halfAngle;

    public ArchProfile(float xMin, float xMax, float spring, float rise)
    {
        float span = xMax - xMin;
        radius = (span * span / 4f + rise * rise) / (2f * rise);
        centerX = (xMin + xMax) / 2f;
        centerY = spring + rise - radius;
        halfAngle = Mathf.Asin(span / 2f / radius);
    }

    float Angle(float t) => Mathf.Lerp(-halfAngle, halfAngle, t);

    public Vector2 Point(float t, float inset = 0f)
    {
        float a = Angle(t);
        float r = radius - inset;
        return new Vector2(centerX + r * Mathf.Sin(a), centerY + r * Mathf.Cos(a));
    }

    public Vector3 InwardNormal(float t)
    {
        float a = Angle(t);
        return new Vector3(-Mathf.Sin(a), -Mathf.Cos(a), 0f);
    }

    public float ArcLength(float t) => radius * 2f * halfAngle * t;

    public float HeightAt(float x) => centerY + Mathf.Sqrt(radius * radius - (x - centerX) * (x - centerX));
}

// Accumulates world-space geometry for one material and turns it into a single Mesh.
// UVs come from world position (tile = metres per texture repeat), so a texture keeps the same
// density and stays continuous across every piece built with it.
public class MeshKit
{
    readonly List<Vector3> positions = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<int> triangles = new List<int>();

    public bool IsEmpty => positions.Count == 0;

    public Mesh ToMesh(string name)
    {
        Weld();
        var mesh = new Mesh { name = name };
        if (positions.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(positions);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // Neighbouring panels share world-space UVs, so welding them gives the lightmap unwrap one unbroken
    // chart per surface instead of a visible seam along every panel edge.
    void Weld()
    {
        var merged = new Dictionary<(Vector3Int, Vector3Int, Vector2Int), int>();
        var remap = new int[positions.Count];
        var weldedPositions = new List<Vector3>();
        var weldedNormals = new List<Vector3>();
        var weldedUvs = new List<Vector2>();

        for (int i = 0; i < positions.Count; i++)
        {
            var key = (Vector3Int.RoundToInt(positions[i] * 2000f), Vector3Int.RoundToInt(normals[i] * 500f), Vector2Int.RoundToInt(uvs[i] * 2000f));
            if (!merged.TryGetValue(key, out int index))
            {
                index = weldedPositions.Count;
                merged[key] = index;
                weldedPositions.Add(positions[i]);
                weldedNormals.Add(normals[i]);
                weldedUvs.Add(uvs[i]);
            }
            remap[i] = index;
        }

        for (int i = 0; i < triangles.Count; i++) triangles[i] = remap[triangles[i]];
        positions.Clear();
        positions.AddRange(weldedPositions);
        normals.Clear();
        normals.AddRange(weldedNormals);
        uvs.Clear();
        uvs.AddRange(weldedUvs);
    }

    int Add(Vector3 position, Vector3 normal, Vector2 uv)
    {
        positions.Add(position);
        normals.Add(normal);
        uvs.Add(uv);
        return positions.Count - 1;
    }

    void Triangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }

    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        int i = Add(a, n, uvA);
        Add(b, n, uvB);
        Add(c, n, uvC);
        Add(d, n, uvD);
        Triangle(i, i + 1, i + 2);
        Triangle(i, i + 2, i + 3);
    }

    // The visible side of a panel is Cross(u, v).
    public void Panel(Vector3 origin, Vector3 u, Vector3 v, Vector2 tile)
    {
        Vector3 uDir = u.normalized, vDir = v.normalized;
        Vector2 Map(Vector3 p) => new Vector2(Vector3.Dot(p, uDir) / tile.x, Vector3.Dot(p, vDir) / tile.y);
        Vector3 b = origin + u, c = origin + u + v, d = origin + v;
        Quad(origin, b, c, d, Map(origin), Map(b), Map(c), Map(d));
    }

    public void PanelFacing(Vector3 origin, Vector3 u, Vector3 v, Vector3 facing, Vector2 tile)
    {
        if (Vector3.Dot(Vector3.Cross(u, v), facing) >= 0f) Panel(origin, u, v, tile);
        else Panel(origin, v, u, tile);
    }

    // Any four corners in order, flipped if needed so the front points along facing.
    public void QuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, Vector3 uAxis, Vector3 vAxis, Vector2 tile)
    {
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f)
        {
            Vector3 swap = b;
            b = d;
            d = swap;
        }
        Vector2 Map(Vector3 p) => new Vector2(Vector3.Dot(p, uAxis) / tile.x, Vector3.Dot(p, vAxis) / tile.y);
        Quad(a, b, c, d, Map(a), Map(b), Map(c), Map(d));
    }

    public void Box(Vector3 center, Vector3 size, Vector2 tile)
    {
        Box(Matrix4x4.Translate(center), size, tile);
    }

    public void Box(Matrix4x4 m, Vector3 size, Vector2 tile)
    {
        Vector3 h = size * 0.5f;
        void Face(Vector3 origin, Vector3 u, Vector3 v) =>
            Panel(m.MultiplyPoint3x4(origin), m.MultiplyVector(u), m.MultiplyVector(v), tile);

        Face(new Vector3(h.x, -h.y, h.z), Vector3.back * size.z, Vector3.up * size.y);
        Face(new Vector3(-h.x, -h.y, -h.z), Vector3.forward * size.z, Vector3.up * size.y);
        Face(new Vector3(-h.x, h.y, -h.z), Vector3.forward * size.z, Vector3.right * size.x);
        Face(new Vector3(-h.x, -h.y, -h.z), Vector3.right * size.x, Vector3.forward * size.z);
        Face(new Vector3(-h.x, -h.y, h.z), Vector3.right * size.x, Vector3.up * size.y);
        Face(new Vector3(h.x, -h.y, -h.z), Vector3.left * size.x, Vector3.up * size.y);
    }

    // A thin board whose front face shows the whole texture once; its edges reuse one border texel.
    public void Plate(Vector3 center, Vector3 normal, Vector3 up, float width, float height, float thickness)
    {
        Vector3 toViewerLeft = Vector3.Cross(up, normal).normalized * width;
        Vector3 rise = up * height;
        Vector3 front = center + normal * thickness - toViewerLeft * 0.5f - rise * 0.5f;
        Vector3 back = front - normal * thickness;
        Quad(front, front + toViewerLeft, front + toViewerLeft + rise, front + rise,
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Vector2 edge = new Vector2(0.01f, 0.01f);
        void Side(Vector3 a, Vector3 b) => Quad(a, b, b - normal * thickness, a - normal * thickness, edge, edge, edge, edge);
        Side(front, front + rise);
        Side(front + rise, front + rise + toViewerLeft);
        Side(front + rise + toViewerLeft, front + toViewerLeft);
        Side(front + toViewerLeft, front);
    }

    public void Disc(Vector3 center, Vector3 normal, Vector3 up, float radius, int segments)
    {
        Vector3 viewerRight = -Vector3.Cross(up, normal).normalized;
        int middle = Add(center, normal, new Vector2(0.5f, 0.5f));
        int first = positions.Count;
        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector2 c = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Add(center + (viewerRight * c.x + up * c.y) * radius, normal, new Vector2(0.5f, 0.5f) + c * 0.5f);
        }
        for (int i = 0; i < segments; i++)
        {
            Vector3 winding = Vector3.Cross(positions[first + i] - center, positions[first + i + 1] - center);
            if (Vector3.Dot(winding, normal) >= 0f) Triangle(middle, first + i, first + i + 1);
            else Triangle(middle, first + i + 1, first + i);
        }
    }

    public void Tube(Vector3 a, Vector3 b, float radius, int sides, Vector2 tile)
    {
        Vector3 axis = (b - a).normalized;
        Vector3 e1 = Vector3.Cross(axis, Mathf.Abs(axis.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 e2 = Vector3.Cross(axis, e1);
        float length = Vector3.Distance(a, b);
        int first = positions.Count;
        for (int i = 0; i <= sides; i++)
        {
            float t = i * Mathf.PI * 2f / sides;
            Vector3 n = e1 * Mathf.Cos(t) + e2 * Mathf.Sin(t);
            float u = (float)i / sides * radius * Mathf.PI * 2f / tile.x;
            Add(a + n * radius, n, new Vector2(u, 0f));
            Add(b + n * radius, n, new Vector2(u, length / tile.y));
        }
        for (int i = 0; i < sides; i++)
        {
            int p = first + i * 2;
            Triangle(p, p + 3, p + 1);
            Triangle(p, p + 2, p + 3);
        }
    }

    // Revolves a (radius, height) profile around a vertical axis. List the profile bottom to top.
    public void Lathe(Vector3 baseCenter, Vector2[] profile, int sides, Vector2 tile)
    {
        int first = positions.Count;
        for (int j = 0; j < profile.Length; j++)
        {
            Vector2 prev = profile[Mathf.Max(j - 1, 0)], next = profile[Mathf.Min(j + 1, profile.Length - 1)];
            Vector2 tangent = (next - prev).normalized;
            Vector2 outward = new Vector2(tangent.y, -tangent.x);
            for (int i = 0; i <= sides; i++)
            {
                float t = i * Mathf.PI * 2f / sides;
                Vector3 radial = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t));
                Vector3 p = baseCenter + radial * profile[j].x + Vector3.up * profile[j].y;
                Vector3 n = (radial * outward.x + Vector3.up * outward.y).normalized;
                Add(p, n, new Vector2((float)i / sides * Mathf.Max(profile[j].x, 0.02f) * Mathf.PI * 2f / tile.x, profile[j].y / tile.y));
            }
        }
        int ring = sides + 1;
        for (int j = 0; j < profile.Length - 1; j++)
        {
            for (int i = 0; i < sides; i++)
            {
                int p = first + j * ring + i;
                Triangle(p, p + ring, p + ring + 1);
                Triangle(p, p + ring + 1, p + 1);
            }
        }
    }

    public void Vault(ArchProfile arch, float zMin, float zMax, int segments, Vector2 tile)
    {
        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments, t1 = (float)(i + 1) / segments;
            Vector2 p0 = arch.Point(t0), p1 = arch.Point(t1);
            Vector3 n0 = arch.InwardNormal(t0), n1 = arch.InwardNormal(t1);
            float u0 = arch.ArcLength(t0) / tile.x, u1 = arch.ArcLength(t1) / tile.x;
            int a = Add(new Vector3(p0.x, p0.y, zMin), n0, new Vector2(u0, zMin / tile.y));
            int b = Add(new Vector3(p1.x, p1.y, zMin), n1, new Vector2(u1, zMin / tile.y));
            int c = Add(new Vector3(p1.x, p1.y, zMax), n1, new Vector2(u1, zMax / tile.y));
            int d = Add(new Vector3(p0.x, p0.y, zMax), n0, new Vector2(u0, zMax / tile.y));
            Triangle(a, b, c);
            Triangle(a, c, d);
        }
    }

    // A flat rib hanging under the vault: underside plus the two faces either side of it.
    public void ArchBand(ArchProfile arch, float zMin, float zMax, float drop, int segments, Vector2 tile)
    {
        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments, t1 = (float)(i + 1) / segments;
            Vector2 in0 = arch.Point(t0, drop), in1 = arch.Point(t1, drop);
            Vector2 out0 = arch.Point(t0), out1 = arch.Point(t1);
            Vector3 n0 = arch.InwardNormal(t0), n1 = arch.InwardNormal(t1);
            float u0 = arch.ArcLength(t0) / tile.x, u1 = arch.ArcLength(t1) / tile.x;

            int a = Add(new Vector3(in0.x, in0.y, zMin), n0, new Vector2(u0, zMin / tile.y));
            int b = Add(new Vector3(in1.x, in1.y, zMin), n1, new Vector2(u1, zMin / tile.y));
            int c = Add(new Vector3(in1.x, in1.y, zMax), n1, new Vector2(u1, zMax / tile.y));
            int d = Add(new Vector3(in0.x, in0.y, zMax), n0, new Vector2(u0, zMax / tile.y));
            Triangle(a, b, c);
            Triangle(a, c, d);

            foreach (float z in new[] { zMin, zMax })
            {
                Vector3 facing = z == zMin ? Vector3.back : Vector3.forward;
                QuadFacing(new Vector3(in0.x, in0.y, z), new Vector3(in1.x, in1.y, z), new Vector3(out1.x, out1.y, z),
                    new Vector3(out0.x, out0.y, z), facing, Vector3.right, Vector3.up, tile);
            }
        }
    }

    // Fills the half-moon of an end wall between the springing line and the vault.
    public void ArchFill(ArchProfile arch, float z, float spring, Vector3 facing, Vector3 uAxis, int segments, Vector2 tile)
    {
        for (int i = 0; i < segments; i++)
        {
            Vector2 p0 = arch.Point((float)i / segments), p1 = arch.Point((float)(i + 1) / segments);
            QuadFacing(new Vector3(p0.x, spring, z), new Vector3(p1.x, spring, z), new Vector3(p1.x, p1.y, z),
                new Vector3(p0.x, p0.y, z), facing, uAxis, Vector3.up, tile);
        }
    }

    // A flat wall with rectangular holes. Hole rects are in metres along u (x) and v (y) from origin.
    // Extra cuts add vertices along u, so another piece of surface can weld onto the wall's edge.
    public void WallWithHoles(Vector3 origin, Vector3 u, Vector3 v, float width, float height, Rect[] holes, Vector2 tile, IEnumerable<float> extraCuts = null)
    {
        var uCuts = new SortedSet<float> { 0f, width };
        var vCuts = new SortedSet<float> { 0f, height };
        if (extraCuts != null) uCuts.UnionWith(extraCuts);
        foreach (Rect hole in holes)
        {
            uCuts.Add(Mathf.Clamp(hole.xMin, 0f, width));
            uCuts.Add(Mathf.Clamp(hole.xMax, 0f, width));
            vCuts.Add(Mathf.Clamp(hole.yMin, 0f, height));
            vCuts.Add(Mathf.Clamp(hole.yMax, 0f, height));
        }

        var us = new List<float>(uCuts);
        var vs = new List<float>(vCuts);
        for (int i = 0; i < us.Count - 1; i++)
        {
            for (int j = 0; j < vs.Count - 1; j++)
            {
                Vector2 middle = new Vector2((us[i] + us[i + 1]) / 2f, (vs[j] + vs[j + 1]) / 2f);
                bool insideHole = false;
                foreach (Rect hole in holes) insideHole |= hole.Contains(middle);
                if (insideHole) continue;
                Panel(origin + u * us[i] + v * vs[j], u * (us[i + 1] - us[i]), v * (vs[j + 1] - vs[j]), tile);
            }
        }
    }

    // The inner faces of a hole, running from the wall plane along depth.
    public void Reveal(Vector3 origin, Vector3 u, Vector3 v, Rect hole, Vector3 depth, Vector2 tile)
    {
        Vector3 Corner(float s, float t) => origin + u * s + v * t;
        Vector3 width = u * hole.width, height = v * hole.height;
        PanelFacing(Corner(hole.xMin, hole.yMin), depth, height, u, tile);
        PanelFacing(Corner(hole.xMax, hole.yMin), depth, height, -u, tile);
        PanelFacing(Corner(hole.xMin, hole.yMin), depth, width, v, tile);
        PanelFacing(Corner(hole.xMin, hole.yMax), depth, width, -v, tile);
    }
}
