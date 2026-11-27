using System.Numerics;
using Xunit;

namespace Aquarium.Tests;

public class MeshTests
{
    public static TheoryData<string> Meshes => new() { "Box", "Cylinder", "Sphere" };

    private static Mesh Get(string name) => name switch { "Box" => Mesh.Box, "Cylinder" => Mesh.Cylinder, _ => Mesh.Sphere };

    [Theory]
    [MemberData(nameof(Meshes))]
    public void TrianglesFaceOutwardsAndFitTheUnitCube(string name)
    {
        Mesh mesh = Get(name);
        Assert.All(mesh.Positions, p => Assert.True(MathF.Abs(p.X) <= 0.5001f && MathF.Abs(p.Y) <= 0.5001f && MathF.Abs(p.Z) <= 0.5001f));
        Assert.Equal(0, mesh.Indices.Length % 3);
        for (int i = 0; i < mesh.Indices.Length; i += 3)
        {
            Vector3 a = mesh.Positions[mesh.Indices[i]], b = mesh.Positions[mesh.Indices[i + 1]], c = mesh.Positions[mesh.Indices[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.LengthSquared() < 1e-12f) continue; // Degenerate triangles at the poles of the sphere
            // Counter-clockwise when seen from outside (required for back-face culling): the face normal points away from the center
            Assert.True(Vector3.Dot(normal, (a + b + c) / 3) > 0, $"{name}: triangle {i / 3} faces inwards");
            // Vertex normals agree with the face orientation
            Assert.True(Vector3.Dot(normal, mesh.Normals[mesh.Indices[i]]) > 0, $"{name}: normal of triangle {i / 3} is inconsistent");
        }
    }
}

public class SceneTests
{
    [Fact]
    public void PicksTheClosestInstance()
    {
        var scene = new Scene();
        var boxes = scene.Add(new InstanceBatch(Mesh.Box, "Boxes"));
        boxes.Set(new[]
        {
            new Instance(new Vector3(0, 0, 0), Vector3.One, Color4.White),
            new Instance(new Vector3(0, 0, 3), Vector3.One, Color4.White),
            new Instance(new Vector3(5, 0, 3), Vector3.One, Color4.White),
        });
        // Looking down from above: the upper box is hit first
        PickResult? hit = scene.Pick(new Ray(new Vector3(0.2f, 0.1f, 10), -Vector3.UnitZ));
        Assert.NotNull(hit);
        Assert.Equal(1, hit.Value.Index);
        Assert.Equal(6.5f, hit.Value.Distance, 3);
        // Missing everything
        Assert.Null(scene.Pick(new Ray(new Vector3(2.5f, 0, 10), -Vector3.UnitZ)));
        // Not pickable
        boxes.Pickable = false;
        Assert.Null(scene.Pick(new Ray(new Vector3(0.2f, 0.1f, 10), -Vector3.UnitZ)));
    }

    [Fact]
    public void PickingRespectsRotation()
    {
        // A long thin box rotated by 90° around z: extends along y instead of x
        var instance = Instance.WithYaw(Vector3.Zero, new Vector3(4, 0.2f, 0.2f), MathF.PI / 2, Color4.White);
        Assert.NotNull(new Ray(new Vector3(0, 1.5f, 5), -Vector3.UnitZ).Intersect(instance));
        Assert.Null(new Ray(new Vector3(1.5f, 0, 5), -Vector3.UnitZ).Intersect(instance));
        BoundingBox bounds = BoundingBox.Of(instance);
        Assert.Equal(0.1f, bounds.Max.X, 3);
        Assert.Equal(2f, bounds.Max.Y, 3);
    }

    [Fact]
    public void BatchesNotifyTheScene()
    {
        var scene = new Scene();
        int changes = 0;
        scene.Changed += () => changes++;
        var batch = scene.Add(new InstanceBatch(Mesh.Box));
        long version = batch.Version;
        batch.Set(new[] { Instance.FromCorner(Vector3.Zero, new Vector3(2, 2, 2), Color4.Gray) });
        Assert.True(batch.Version > version);
        Assert.Equal(new Vector3(1, 1, 1), batch.Get(0).Position);
        batch.Update(span => span[0].Color = Color4.Black);
        Assert.Equal(Color4.Black, batch.Get(0).Color);
        Assert.True(changes >= 3);
        Assert.Equal(new BoundingBox(Vector3.Zero, new Vector3(2, 2, 2)), scene.GetBounds());
        batch.Visible = false;
        Assert.True(scene.GetBounds().IsEmpty);
    }
}

public class CameraTests
{
    [Theory]
    [InlineData(false, 1.0f)]
    [InlineData(false, 2.0f)]
    [InlineData(true, 1.5f)]
    public void FitShowsAllCornersAndTheRayHitsTheCenter(bool orthographic, float aspect)
    {
        var camera = new Camera { Orthographic = orthographic, Yaw = 0.3f, Pitch = 0.7f };
        var bounds = new BoundingBox(new Vector3(-3, -1, 0), new Vector3(10, 4, 2));
        camera.Fit(bounds, aspect);
        Matrix4x4 viewProjection = camera.GetViewProjection(aspect);
        float maxNdc = 0;
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3((i & 1) == 0 ? bounds.Min.X : bounds.Max.X, (i & 2) == 0 ? bounds.Min.Y : bounds.Max.Y, (i & 4) == 0 ? bounds.Min.Z : bounds.Max.Z);
            Vector4 clip = Vector4.Transform(new Vector4(corner, 1), viewProjection);
            maxNdc = Math.Max(maxNdc, Math.Max(MathF.Abs(clip.X / clip.W), MathF.Abs(clip.Y / clip.W)));
        }
        // All corners visible, and the fit is tight (not much smaller than the viewport)
        Assert.InRange(maxNdc, 0.85f, 1.0f);

        // A ray through the center of the viewport hits the target
        Ray ray = camera.GetRay(400 * aspect, 400, 800 * aspect, 800);
        Vector3 closest = ray.Origin + ray.Direction * Vector3.Dot(bounds.Center - ray.Origin, ray.Direction);
        Assert.True(Vector3.Distance(closest, bounds.Center) < 1e-2f);
    }

    [Fact]
    public void CameraIsRobustAgainstDegenerateInput()
    {
        var camera = new Camera();
        camera.Fit(new BoundingBox(Vector3.Zero, Vector3.One), 0); // E.g. a control that is not laid out yet
        Assert.True(float.IsFinite(camera.Distance));
        camera.Distance = float.PositiveInfinity;
        Assert.True(float.IsFinite(camera.Distance));
        Assert.True(float.IsFinite(camera.GetViewProjection(0).M11));
        camera.Pitch = 10;
        Assert.True(camera.Pitch < MathF.PI / 2);
    }

    [Fact]
    public void ZoomTowardsKeepsThePointFixed()
    {
        var camera = new Camera();
        camera.Fit(new BoundingBox(Vector3.Zero, new Vector3(10, 10, 1)));
        var point = new Vector3(8, 2, 0);
        Matrix4x4 before = camera.GetViewProjection(1);
        camera.ZoomTowards(point, 0.5f);
        Matrix4x4 after = camera.GetViewProjection(1);
        Vector4 a = Vector4.Transform(new Vector4(point, 1), before), b = Vector4.Transform(new Vector4(point, 1), after);
        Assert.Equal(a.X / a.W, b.X / b.W, 3);
        Assert.Equal(a.Y / a.W, b.Y / b.W, 3);
    }

    [Fact]
    public void ColorsFromHsv()
    {
        Assert.Equal(new Color4(255, 0, 0), Color4.FromHsv(0, 1, 1));
        Assert.Equal(new Color4(0, 255, 0), Color4.FromHsv(120, 1, 1));
        Assert.Equal(new Color4(0, 0, 255), Color4.FromHsv(240, 1, 1));
        Assert.Equal(new Color4(255, 0, 0), Color4.FromHsv(360, 1, 1));
    }
}
