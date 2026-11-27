using System.Numerics;
using System.Runtime.InteropServices;

namespace Aquarium;

/// <summary>
/// An 8-bit RGBA color (memory layout matches what is uploaded to the GPU).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Color4(byte R, byte G, byte B, byte A = 255)
{
    public static readonly Color4 White = new(255, 255, 255);
    public static readonly Color4 Black = new(0, 0, 0);
    public static readonly Color4 Gray = new(128, 128, 128);

    /// <summary>
    /// Returns the same color with the given alpha value.
    /// </summary>
    public Color4 WithAlpha(byte alpha) => this with { A = alpha };

    /// <summary>
    /// Creates a color from HSV values (hue in degrees, saturation and value in [0,1]).
    /// </summary>
    public static Color4 FromHsv(double hue, double saturation, double value, byte alpha = 255)
    {
        hue = ((hue % 360) + 360) % 360 / 60;
        double c = value * saturation, x = c * (1 - Math.Abs(hue % 2 - 1)), m = value - c;
        (double r, double g, double b) = (int)hue switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        static byte B(double v) => (byte)Math.Clamp(Math.Round(v * 255), 0, 255);
        return new Color4(B(r + m), B(g + m), B(b + m), alpha);
    }
}

/// <summary>
/// One instance of a mesh: its center, size (extents along the mesh's local axes), rotation and color.
/// The memory layout matches the GPU instance buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Instance
{
    /// <summary>
    /// The center of the instance.
    /// </summary>
    public Vector3 Position;
    /// <summary>
    /// The size of the instance (the meshes span [-0.5, 0.5] in every dimension, hence, this is the full extent).
    /// </summary>
    public Vector3 Size;
    /// <summary>
    /// The rotation around the center.
    /// </summary>
    public Quaternion Rotation;
    /// <summary>
    /// The color.
    /// </summary>
    public Color4 Color;

    public Instance(Vector3 position, Vector3 size, Color4 color) : this(position, size, Quaternion.Identity, color) { }

    public Instance(Vector3 position, Vector3 size, Quaternion rotation, Color4 color)
    {
        Position = position;
        Size = size;
        Rotation = rotation;
        Color = color;
    }

    /// <summary>
    /// Creates an axis-aligned box instance from its minimum corner and its size (convenient for packing problems).
    /// </summary>
    public static Instance FromCorner(Vector3 minCorner, Vector3 size, Color4 color) => new(minCorner + size / 2, size, color);

    /// <summary>
    /// Creates an instance rotated around the z-axis (yaw, in radians).
    /// </summary>
    public static Instance WithYaw(Vector3 position, Vector3 size, float yaw, Color4 color) =>
        new(position, size, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, yaw), color);

    /// <summary>
    /// The size of an instance in bytes (as uploaded to the GPU).
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf<Instance>();
}

/// <summary>
/// A colored line segment.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Line(Vector3 From, Vector3 To, Color4 Color);

/// <summary>
/// An axis-aligned bounding box.
/// </summary>
public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    public static readonly BoundingBox Empty = new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));

    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y || Min.Z > Max.Z;
    public Vector3 Center => (Min + Max) / 2;
    public Vector3 Size => IsEmpty ? Vector3.Zero : Max - Min;
    public float Radius => Size.Length() / 2;

    public BoundingBox Include(Vector3 point) => new(Vector3.Min(Min, point), Vector3.Max(Max, point));
    public BoundingBox Include(BoundingBox other) => other.IsEmpty ? this : IsEmpty ? other : new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    /// <summary>
    /// The bounding box of a (possibly rotated) instance.
    /// </summary>
    public static BoundingBox Of(in Instance instance)
    {
        Vector3 half = instance.Size / 2;
        if (instance.Rotation == Quaternion.Identity)
            return new BoundingBox(instance.Position - half, instance.Position + half);
        // Extents of the rotated box: sum of the absolute rotated half axes
        Vector3 ax = Vector3.Abs(Vector3.Transform(new Vector3(half.X, 0, 0), instance.Rotation));
        Vector3 ay = Vector3.Abs(Vector3.Transform(new Vector3(0, half.Y, 0), instance.Rotation));
        Vector3 az = Vector3.Abs(Vector3.Transform(new Vector3(0, 0, half.Z), instance.Rotation));
        Vector3 extent = ax + ay + az;
        return new BoundingBox(instance.Position - extent, instance.Position + extent);
    }
}

/// <summary>
/// A ray (e.g. from the camera through a pixel).
/// </summary>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
{
    /// <summary>
    /// Intersects the ray with an instance (treated as box) and returns the distance to the hit, or null.
    /// </summary>
    public float? Intersect(in Instance instance)
    {
        // Transform the ray into the local (unrotated) space of the instance
        Quaternion inverse = Quaternion.Inverse(instance.Rotation);
        Vector3 origin = Vector3.Transform(Origin - instance.Position, inverse);
        Vector3 direction = Vector3.Transform(Direction, inverse);
        Vector3 half = instance.Size / 2;
        // Slab test
        float tMin = float.NegativeInfinity, tMax = float.PositiveInfinity;
        for (int axis = 0; axis < 3; axis++)
        {
            float o = origin[axis], d = direction[axis], h = half[axis];
            if (MathF.Abs(d) < 1e-12f)
            {
                if (o < -h || o > h) return null;
                continue;
            }
            float t1 = (-h - o) / d, t2 = (h - o) / d;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return null;
        }
        if (tMax < 0) return null;
        return tMin >= 0 ? tMin : tMax;
    }
}
