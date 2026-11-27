# Aquarium

Fast, simple 3D visualization of many shapes for [Avalonia](https://avaloniaui.net/) applications - think warehouses full of shelves and robots, or containers packed with boxes.

- **Instanced rendering:** all instances of a shape (box, cylinder, sphere or a custom mesh) are drawn with a single draw call. Tens of thousands of moving objects stay smooth.
- **Simple model:** a `Scene` consists of batches of instances (position, size, rotation, color) and lines. Batches can be updated from any thread.
- **Interaction:** orbit / pan / zoom camera, picking (click on an instance), fit to scene, screenshots.
- **Cross-platform:** OpenGL 3.3 / OpenGL ES 3.0 via Avalonia's `OpenGlControlBase` (Linux, Windows, macOS). No dependencies besides Avalonia.

| Package | Content |
|---|---|
| `Aquarium` | UI-independent scene model, camera, picking (no dependencies) |
| `Aquarium.Avalonia` | `AquariumView` control rendering a scene with OpenGL |

## Usage

```csharp
using Aquarium;
using Aquarium.Avalonia;
using System.Numerics;

var scene = new Scene();
var boxes = scene.Add(new InstanceBatch(Mesh.Box, "Boxes"));
boxes.Set(new[]
{
    Instance.FromCorner(new Vector3(0, 0, 0), new Vector3(1, 2, 1), new Color4(100, 149, 237)),
    Instance.WithYaw(new Vector3(3, 1, 0.5f), new Vector3(1, 1, 1), MathF.PI / 4, new Color4(255, 99, 71)),
});

var view = new AquariumView { Scene = scene };
view.Picked += hit => Console.WriteLine(hit is { } h ? $"{h.Batch.Name} #{h.Index}" : "nothing");
view.FitToScene();
```

Coordinates are z-up. Meshes span `[-0.5, 0.5]` in every dimension and are scaled by the instance size, i.e. `Instance.Position` is the center and `Instance.Size` the full extent.

Mouse: left drag = orbit, right / middle drag (or shift + left drag) = pan, wheel = zoom (towards the cursor), click = pick, double click = fit.

## Sample

```bash
dotnet run --project samples/Aquarium.Sample -- warehouse --scale 4
dotnet run --project samples/Aquarium.Sample -- container
```

Options: `--scale N` (size of the demo), `--benchmark seconds` (render continuously and print the frame rate), `--screenshot file.png`, `--zoom factor`, `--allow-software-gl` (allow Mesa's software renderer, e.g. for screenshots with Xvfb: `xvfb-run -a dotnet run --project samples/Aquarium.Sample -- --allow-software-gl --screenshot shot.png`).

## Notes

- Rendering requires OpenGL 3.3 or OpenGL ES 3.0. If Avalonia cannot provide an OpenGL context (e.g. software rendering), the view stays empty and `AquariumView.Error` / the `GlInitialized` event tell why.
- Avalonia refuses Mesa's software renderer (llvmpipe) by default. It can be allowed via `X11PlatformOptions.GlxRendererBlacklist`.

## License

MIT, see [LICENSE](LICENSE).
