using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Aquarium.Avalonia.Tests.TestApp))]

namespace Aquarium.Avalonia.Tests;

public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// Mouse interaction (OpenGL is not available headless, but input handling does not depend on it).
/// </summary>
public class InteractionTests
{
    private static (Window window, AquariumView view, InstanceBatch boxes) Create()
    {
        var scene = new Scene();
        var boxes = scene.Add(new InstanceBatch(Mesh.Box, "Boxes"));
        boxes.Set(new[] { new Instance(Vector3.Zero, Vector3.One, Color4.White), new Instance(new Vector3(5, 0, 0), Vector3.One, Color4.White) });
        var view = new AquariumView { Scene = scene };
        var window = new Window { Width = 800, Height = 600, Content = view };
        window.Show();
        view.FitToScene();
        return (window, view, boxes);
    }

    [AvaloniaFact]
    public void DraggingOrbitsAndPans()
    {
        var (window, view, _) = Create();
        float yaw = view.Camera.Yaw;
        window.MouseDown(new Point(400, 300), MouseButton.Left);
        window.MouseMove(new Point(450, 320));
        window.MouseMove(new Point(500, 340));
        window.MouseUp(new Point(500, 340), MouseButton.Left);
        Assert.NotEqual(yaw, view.Camera.Yaw);

        Vector3 target = view.Camera.Target;
        window.MouseDown(new Point(400, 300), MouseButton.Right);
        window.MouseMove(new Point(450, 300));
        window.MouseMove(new Point(500, 300));
        window.MouseUp(new Point(500, 300), MouseButton.Right);
        Assert.NotEqual(target, view.Camera.Target);
    }

    [AvaloniaFact]
    public void ClickPicksAndWheelZooms()
    {
        var (window, view, boxes) = Create();
        // Click on the projection of the second box
        PickResult? picked = null;
        bool raised = false;
        view.Picked += hit => { picked = hit; raised = true; };
        Matrix4x4 viewProjection = view.Camera.GetViewProjection((float)(view.Bounds.Width / view.Bounds.Height));
        Vector4 clip = Vector4.Transform(new Vector4(5, 0, 0, 1), viewProjection);
        var point = new Point((clip.X / clip.W + 1) / 2 * view.Bounds.Width, (1 - clip.Y / clip.W) / 2 * view.Bounds.Height);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.True(raised, $"point {point} bounds {view.Bounds}");
        Assert.Equal(boxes, picked?.Batch);
        Assert.Equal(1, picked?.Index);

        float distance = view.Camera.Distance;
        window.MouseWheel(new Point(400, 300), new global::Avalonia.Vector(0, 2));
        Assert.True(view.Camera.Distance < distance);
    }
}
