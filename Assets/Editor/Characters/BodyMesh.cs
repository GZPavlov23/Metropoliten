using System;
using System.Collections.Generic;
using UnityEngine;

// One slice through a part of the body. Width runs along side, and the two depths along front and back of it.
public struct Ring
{
    public Vector3 Center, Side, Front;
    public float Width, FrontDepth, BackDepth;

    // 2 is an ellipse; higher goes towards a soft-cornered box.
    public float Squareness;

    public Ring(Vector3 center, Vector3 side, Vector3 front, float width, float frontDepth, float backDepth, float squareness = 2.4f)
    {
        Center = center;
        Side = side;
        Front = front;
        Width = width;
        FrontDepth = frontDepth;
        BackDepth = backDepth;
        Squareness = squareness;
    }

    public Vector3 Point(float angle)
    {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle), power = 2f / Squareness;
        float x = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), power), z = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), power);
        return Center + Side * (x * Width) + Front * (z * (z >= 0f ? FrontDepth : BackDepth));
    }
}

// A skinned mesh being put together in world space, a part at a time. Each part is a closed skin laid out
// on its own rectangle of the character's texture, and weighted to the skeleton by a rule the part supplies.
public class BodyMesh
{
    readonly List<Vector3> positions = new List<Vector3>();
    readonly List<Vector2> uvs = new List<Vector2>();
    readonly List<BoneWeight> weights = new List<BoneWeight>();
    readonly List<int> triangles = new List<int>();

    // The columns either side of a texture seam, whose normals get averaged so that the seam doesn't show.
    readonly List<(int a, int b)> seams = new List<(int, int)>();

    int Add(Vector3 position, Vector2 uv, BoneWeight weight)
    {
        positions.Add(position);
        uvs.Add(uv);
        weights.Add(weight);
        return positions.Count - 1;
    }

    void Quad(int a, int b, int c, int d, bool flip)
    {
        if (flip)
        {
            (b, d) = (d, b);
        }
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    // Skins a list of rings, with the front of each ring in the middle of the rectangle and the seam behind.
    // The ends can be closed with a shallow dome.
    public void Loft(IList<Ring> rings, int sides, Rect area, Func<Vector3, BoneWeight> weigh, bool capStart = false, bool capEnd = false)
    {
        var lengths = new float[rings.Count];
        for (int i = 1; i < rings.Count; i++) lengths[i] = lengths[i - 1] + Vector3.Distance(rings[i].Center, rings[i - 1].Center) + 0.001f;
        float total = lengths[rings.Count - 1];

        int first = positions.Count, columns = sides + 1;
        for (int j = 0; j < rings.Count; j++)
        {
            float v = lengths[j] / total;
            for (int i = 0; i < columns; i++)
            {
                float along = (float)i / sides;
                Vector3 point = rings[j].Point(-Mathf.PI / 2f + along * Mathf.PI * 2f);
                Add(point, new Vector2(area.x + area.width * along, area.y + area.height * v), weigh(point));
            }
            seams.Add((first + j * columns, first + j * columns + sides));
        }

        int testRing = Mathf.Clamp(rings.Count / 2, 0, rings.Count - 2), testAt = first + testRing * columns + sides / 2;
        Vector3 faceNormal = Vector3.Cross(positions[testAt + 1] - positions[testAt], positions[testAt + columns] - positions[testAt]);
        bool flip = Vector3.Dot(faceNormal, positions[testAt] - rings[testRing].Center) < 0f;

        for (int j = 0; j < rings.Count - 1; j++)
        {
            for (int i = 0; i < sides; i++)
            {
                int a = first + j * columns + i;
                Quad(a, a + 1, a + columns + 1, a + columns, flip);
            }
        }

        if (capStart) Cap(first, sides, rings[0], area.position, -Normal(rings, 0), weigh);
        if (capEnd) Cap(first + (rings.Count - 1) * columns, sides, rings[rings.Count - 1], area.position + area.size, Normal(rings, rings.Count - 1), weigh);
    }

    static Vector3 Normal(IList<Ring> rings, int index)
    {
        int next = Mathf.Min(index + 1, rings.Count - 1), last = Mathf.Max(index - 1, 0);
        return (rings[next].Center - rings[last].Center).normalized;
    }

    void Cap(int firstInRing, int sides, Ring ring, Vector2 uv, Vector3 outward, Func<Vector3, BoneWeight> weigh)
    {
        Vector3 tip = ring.Center + outward * Mathf.Min(ring.Width, ring.FrontDepth) * 0.3f;
        int middle = Add(tip, uv, weigh(tip));
        for (int i = 0; i < sides; i++)
        {
            int a = firstInRing + i, b = a + 1;
            bool forwards = Vector3.Dot(Vector3.Cross(positions[a] - tip, positions[b] - tip), outward) > 0f;
            triangles.Add(middle);
            triangles.Add(forwards ? a : b);
            triangles.Add(forwards ? b : a);
        }
    }

    // A skin over a grid of points, for shapes that aren't slices along a line: the head, and what goes over it.
    // The first and last columns meet, round the back.
    public void Grid(Vector3[,] points, Vector2[,] texture, Func<Vector3, BoneWeight> weigh, Vector3 center)
    {
        int rows = points.GetLength(0), columns = points.GetLength(1), first = positions.Count;
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < columns; i++) Add(points[j, i], texture[j, i], weigh(points[j, i]));
            seams.Add((first + j * columns, first + j * columns + columns - 1));
        }

        int testAt = first + rows / 2 * columns + columns / 2;
        Vector3 faceNormal = Vector3.Cross(positions[testAt + 1] - positions[testAt], positions[testAt + columns] - positions[testAt]);
        bool flip = Vector3.Dot(faceNormal, positions[testAt] - center) < 0f;

        for (int j = 0; j < rows - 1; j++)
        {
            for (int i = 0; i < columns - 1; i++)
            {
                int a = first + j * columns + i;
                Quad(a, a + 1, a + columns + 1, a + columns, flip);
            }
        }
    }

    public Mesh ToMesh(string name, Matrix4x4 worldToMesh, Matrix4x4[] bindposes)
    {
        var local = new Vector3[positions.Count];
        for (int i = 0; i < local.Length; i++) local[i] = worldToMesh.MultiplyPoint3x4(positions[i]);

        var mesh = new Mesh { name = name };
        mesh.vertices = local;
        mesh.uv = uvs.ToArray();
        mesh.boneWeights = weights.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.bindposes = bindposes;
        mesh.RecalculateNormals();

        Vector3[] normals = mesh.normals;
        foreach ((int a, int b) in seams)
        {
            Vector3 average = (normals[a] + normals[b]).normalized;
            normals[a] = average;
            normals[b] = average;
        }
        mesh.normals = normals;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }
}
