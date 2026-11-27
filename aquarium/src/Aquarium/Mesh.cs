using System.Numerics;

namespace Aquarium;

/// <summary>
/// A triangle mesh spanning [-0.5, 0.5] in every dimension (it is scaled by the <see cref="Instance.Size"/> of each instance).
/// </summary>
public sealed class Mesh
{
    public Mesh(string name, Vector3[] positions, Vector3[] normals, uint[] indices, bool hasEdges = false)
    {
        if (positions.Length != normals.Length)
            throw new ArgumentException("Every position needs a normal.");
        Name = name;
        Positions = positions;
        Normals = normals;
        Indices = indices;
        HasEdges = hasEdges;
    }

    public string Name { get; }
    public Vector3[] Positions { get; }
    public Vector3[] Normals { get; }
    public uint[] Indices { get; }
    /// <summary>
    /// Whether the mesh is a box (edges can be highlighted by the renderer).
    /// </summary>
    public bool HasEdges { get; }

    public override string ToString() => Name;

    /// <summary>
    /// A unit box.
    /// </summary>
    public static Mesh Box { get; } = CreateBox();

    /// <summary>
    /// A unit cylinder along the z-axis.
    /// </summary>
    public static Mesh Cylinder { get; } = CreateCylinder(32);

    /// <summary>
    /// A unit sphere.
    /// </summary>
    public static Mesh Sphere { get; } = CreateSphere(24, 16);

    private static Mesh CreateBox()
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<uint>();
        // One quad per face (separate vertices for flat normals)
        foreach (Vector3 normal in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ })
        {
            // Two axes spanning the face, oriented such that (u x v) == normal (counter-clockwise front faces)
            Vector3 u = MathF.Abs(normal.Z) > 0.5f ? Vector3.UnitX : Vector3.UnitZ;
            Vector3 v = Vector3.Cross(normal, u);
            Vector3 center = normal * 0.5f;
            uint start = (uint)positions.Count;
            foreach ((float a, float b) in new[] { (-0.5f, -0.5f), (0.5f, -0.5f), (0.5f, 0.5f), (-0.5f, 0.5f) })
            {
                positions.Add(center + a * u + b * v);
                normals.Add(normal);
            }
            // Make sure the triangles are counter-clockwise when seen from outside
            bool flip = Vector3.Dot(Vector3.Cross(u, v), normal) < 0;
            indices.AddRange(flip
                ? new[] { start, start + 2, start + 1, start, start + 3, start + 2 }
                : new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        return new Mesh("Box", positions.ToArray(), normals.ToArray(), indices.ToArray(), hasEdges: true);
    }

    private static Mesh CreateCylinder(int segments)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<uint>();
        // Side
        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * MathF.PI * i / segments;
            var dir = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0);
            positions.Add(dir * 0.5f + new Vector3(0, 0, -0.5f));
            normals.Add(dir);
            positions.Add(dir * 0.5f + new Vector3(0, 0, 0.5f));
            normals.Add(dir);
        }
        for (uint i = 0; i < segments; i++)
        {
            uint b0 = 2 * i, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
            indices.AddRange(new[] { b0, b1, t1, b0, t1, t0 });
        }
        // Caps
        foreach (float z in new[] { -0.5f, 0.5f })
        {
            var normal = new Vector3(0, 0, MathF.Sign(z));
            uint center = (uint)positions.Count;
            positions.Add(new Vector3(0, 0, z));
            normals.Add(normal);
            for (int i = 0; i <= segments; i++)
            {
                float angle = 2 * MathF.PI * i / segments;
                positions.Add(new Vector3(MathF.Cos(angle) * 0.5f, MathF.Sin(angle) * 0.5f, z));
                normals.Add(normal);
            }
            for (uint i = 0; i < segments; i++)
            {
                if (z > 0) indices.AddRange(new[] { center, center + 1 + i, center + 2 + i });
                else indices.AddRange(new[] { center, center + 2 + i, center + 1 + i });
            }
        }
        return new Mesh("Cylinder", positions.ToArray(), normals.ToArray(), indices.ToArray());
    }

    private static Mesh CreateSphere(int slices, int stacks)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<uint>();
        for (int stack = 0; stack <= stacks; stack++)
        {
            float phi = MathF.PI * stack / stacks - MathF.PI / 2;
            for (int slice = 0; slice <= slices; slice++)
            {
                float theta = 2 * MathF.PI * slice / slices;
                var n = new Vector3(MathF.Cos(phi) * MathF.Cos(theta), MathF.Cos(phi) * MathF.Sin(theta), MathF.Sin(phi));
                positions.Add(n * 0.5f);
                normals.Add(n);
            }
        }
        uint row = (uint)slices + 1;
        for (uint stack = 0; stack < stacks; stack++)
            for (uint slice = 0; slice < slices; slice++)
            {
                uint a = stack * row + slice, b = a + row;
                indices.AddRange(new[] { a, a + 1, b + 1, a, b + 1, b });
            }
        return new Mesh("Sphere", positions.ToArray(), normals.ToArray(), indices.ToArray());
    }
}
