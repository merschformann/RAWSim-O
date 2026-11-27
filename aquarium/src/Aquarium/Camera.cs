using System.Numerics;

namespace Aquarium;

/// <summary>
/// An orbit camera looking at a target point (z-up).
/// </summary>
public sealed class Camera
{
    private const float MinPitch = -1.55f, MaxPitch = 1.55f;

    /// <summary>
    /// The point the camera orbits around.
    /// </summary>
    public Vector3 Target { get; set; }

    /// <summary>
    /// The distance between camera and target.
    /// </summary>
    public float Distance { get => _distance; set => _distance = float.IsFinite(value) ? Math.Clamp(value, 1e-4f, 1e7f) : _distance; }
    private float _distance = 10;

    /// <summary>
    /// The rotation around the z-axis (radians, 0 = looking along the negative x-axis).
    /// </summary>
    public float Yaw { get; set; } = -MathF.PI / 2 + 0.6f;

    /// <summary>
    /// The elevation angle (radians, positive = looking down).
    /// </summary>
    public float Pitch { get => _pitch; set => _pitch = Math.Clamp(value, MinPitch, MaxPitch); }
    private float _pitch = 0.6f;

    /// <summary>
    /// The vertical field of view (radians).
    /// </summary>
    public float FieldOfView { get; set; } = MathF.PI / 4;

    /// <summary>
    /// Whether to use an orthographic instead of a perspective projection.
    /// </summary>
    public bool Orthographic { get; set; }

    /// <summary>
    /// The radius of the scene (used to choose the clipping planes).
    /// </summary>
    public float SceneRadius { get; set; } = 10;

    public Vector3 Position => Target + Distance * Direction;

    /// <summary>
    /// The unit vector from the target to the camera.
    /// </summary>
    public Vector3 Direction => new(MathF.Cos(Pitch) * MathF.Cos(Yaw), MathF.Cos(Pitch) * MathF.Sin(Yaw), MathF.Sin(Pitch));

    public Matrix4x4 View => Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitZ);

    public Matrix4x4 GetProjection(float aspectRatio)
    {
        if (!(aspectRatio > 0) || float.IsInfinity(aspectRatio)) aspectRatio = 1;
        float far = Distance + SceneRadius * 4 + 1;
        if (Orthographic)
        {
            float height = 2 * Distance * MathF.Tan(FieldOfView / 2);
            return Matrix4x4.CreateOrthographic(height * aspectRatio, height, -far, far);
        }
        float near = Math.Max(Distance * 0.01f, far * 1e-5f);
        return Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, near, far);
    }

    public Matrix4x4 GetViewProjection(float aspectRatio) => View * GetProjection(aspectRatio);

    /// <summary>
    /// Rotates the camera around the target.
    /// </summary>
    public void Orbit(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch += deltaPitch;
    }

    /// <summary>
    /// Moves the target parallel to the screen by the given amount of pixels.
    /// </summary>
    public void Pan(float dxPixels, float dyPixels, float viewportHeight)
    {
        float worldPerPixel = 2 * Distance * MathF.Tan(FieldOfView / 2) / Math.Max(viewportHeight, 1);
        Vector3 forward = -Direction;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitZ));
        Vector3 up = Vector3.Cross(right, forward);
        Target += (-dxPixels * right + dyPixels * up) * worldPerPixel;
    }

    /// <summary>
    /// Moves the camera towards (factor &lt; 1) or away from (factor &gt; 1) the target.
    /// </summary>
    public void Zoom(float factor) => Distance *= factor;

    /// <summary>
    /// Zooms towards a point (e.g. the point under the mouse cursor), keeping it at the same screen position.
    /// </summary>
    public void ZoomTowards(Vector3 point, float factor)
    {
        Target = point + (Target - point) * factor;
        Distance *= factor;
    }

    /// <summary>
    /// Positions the camera such that the given box is completely visible.
    /// </summary>
    public void Fit(BoundingBox bounds, float aspectRatio = 1)
    {
        if (bounds.IsEmpty) return;
        if (!(aspectRatio > 0) || float.IsInfinity(aspectRatio)) aspectRatio = 1; // E.g. not laid out yet
        Target = bounds.Center;
        SceneRadius = Math.Max(bounds.Radius, 1e-3f);
        // Find the distance at which all corners of the box are within the view frustum
        float tanV = MathF.Tan(FieldOfView / 2), tanH = tanV * aspectRatio;
        Vector3 toCamera = Direction;
        Vector3 right = Vector3.Normalize(Vector3.Cross(-toCamera, Vector3.UnitZ));
        Vector3 up = Vector3.Cross(right, -toCamera);
        float distance = 0, maxExtent = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new((i & 1) == 0 ? bounds.Min.X : bounds.Max.X, (i & 2) == 0 ? bounds.Min.Y : bounds.Max.Y, (i & 4) == 0 ? bounds.Min.Z : bounds.Max.Z);
            Vector3 offset = corner - Target;
            float r = MathF.Abs(Vector3.Dot(offset, right)), u = MathF.Abs(Vector3.Dot(offset, up)), f = Vector3.Dot(offset, toCamera);
            distance = Math.Max(distance, f + Math.Max(r / tanH, u / tanV));
            maxExtent = Math.Max(maxExtent, Math.Max(r / aspectRatio, u));
        }
        // Orthographic: the visible height is derived from the distance as well (see GetProjection)
        if (Orthographic)
            distance = maxExtent / tanV;
        Distance = Math.Max(distance * 1.05f, 1e-3f);
    }

    /// <summary>
    /// Returns the ray through the given pixel.
    /// </summary>
    public Ray GetRay(float x, float y, float width, float height)
    {
        Matrix4x4 viewProjection = GetViewProjection(width / Math.Max(height, 1));
        if (!Matrix4x4.Invert(viewProjection, out Matrix4x4 inverse))
            return new Ray(Position, -Direction);
        float ndcX = 2 * x / width - 1, ndcY = 1 - 2 * y / height;
        Vector3 near = Unproject(new Vector4(ndcX, ndcY, 0, 1), inverse);
        Vector3 far = Unproject(new Vector4(ndcX, ndcY, 1, 1), inverse);
        return new Ray(near, Vector3.Normalize(far - near));
    }

    private static Vector3 Unproject(Vector4 ndc, Matrix4x4 inverse)
    {
        Vector4 v = Vector4.Transform(ndc, inverse);
        return new Vector3(v.X, v.Y, v.Z) / v.W;
    }
}
