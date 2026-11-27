using System.Numerics;

namespace Aquarium;

/// <summary>
/// Base class of the content of a scene. The data can be replaced from any thread, renderers pick up the latest version.
/// </summary>
public abstract class SceneBatch
{
    private readonly object _sync = new();
    private long _version;

    protected SceneBatch(string? name) { Name = name; }

    /// <summary>
    /// An optional name (e.g. "Pods").
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Whether the batch is drawn.
    /// </summary>
    public bool Visible { get => _visible; set { _visible = value; Touch(); } }
    private bool _visible = true;

    /// <summary>
    /// Incremented whenever the content changes.
    /// </summary>
    public long Version => Interlocked.Read(ref _version);

    /// <summary>
    /// The scene the batch belongs to (if any).
    /// </summary>
    internal Scene? Scene { get; set; }

    protected object Sync => _sync;

    protected void Touch()
    {
        Interlocked.Increment(ref _version);
        Scene?.NotifyChanged();
    }
}

/// <summary>
/// Many instances of the same mesh (drawn with a single draw call).
/// </summary>
public sealed class InstanceBatch : SceneBatch
{
    private Instance[] _instances = Array.Empty<Instance>();
    private int _count;

    public InstanceBatch(Mesh mesh, string? name = null) : base(name) { Mesh = mesh; }

    public Mesh Mesh { get; }

    /// <summary>
    /// Whether the edges of box meshes are drawn darker (makes neighboring boxes distinguishable).
    /// </summary>
    public bool ShowEdges { get => _showEdges; set { _showEdges = value; Touch(); } }
    private bool _showEdges = true;

    /// <summary>
    /// Whether the instances are (partially) transparent (alpha of their colors). Transparent batches are drawn after the opaque ones.
    /// </summary>
    public bool Transparent { get => _transparent; set { _transparent = value; Touch(); } }
    private bool _transparent;

    /// <summary>
    /// Whether the instances can be picked.
    /// </summary>
    public bool Pickable { get; set; } = true;

    /// <summary>
    /// The number of instances.
    /// </summary>
    public int Count { get { lock (Sync) return _count; } }

    /// <summary>
    /// Replaces all instances (the data is copied).
    /// </summary>
    public void Set(ReadOnlySpan<Instance> instances)
    {
        lock (Sync)
        {
            if (_instances.Length < instances.Length)
                _instances = new Instance[Math.Max(instances.Length, _instances.Length * 2)];
            instances.CopyTo(_instances);
            _count = instances.Length;
        }
        Touch();
    }

    /// <summary>
    /// Modifies the instances in place (e.g. to move a few of them) without copying all of them.
    /// </summary>
    public void Update(SpanAction update)
    {
        lock (Sync)
            update(_instances.AsSpan(0, _count));
        Touch();
    }

    public delegate void SpanAction(Span<Instance> instances);

    /// <summary>
    /// Provides read access to the current instances (while holding the lock of the batch - keep it short).
    /// </summary>
    public void Read(ReadOnlySpanAction read)
    {
        lock (Sync)
            read(_instances.AsSpan(0, _count));
    }

    public delegate void ReadOnlySpanAction(ReadOnlySpan<Instance> instances);

    /// <summary>
    /// Returns a copy of the instance with the given index.
    /// </summary>
    public Instance Get(int index)
    {
        lock (Sync)
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            return _instances[index];
        }
    }
}

/// <summary>
/// Line segments (e.g. a grid, a graph or wireframes).
/// </summary>
public sealed class LineBatch : SceneBatch
{
    private Line[] _lines = Array.Empty<Line>();

    public LineBatch(string? name = null) : base(name) { }

    public int Count { get { lock (Sync) return _lines.Length; } }

    /// <summary>
    /// Replaces all lines (the data is copied).
    /// </summary>
    public void Set(ReadOnlySpan<Line> lines)
    {
        lock (Sync)
            _lines = lines.ToArray();
        Touch();
    }

    public void Read(Action<ReadOnlyMemory<Line>> read)
    {
        Line[] lines;
        lock (Sync)
            lines = _lines;
        read(lines); // The array is never modified after creation, hence, no lock needed
    }

    /// <summary>
    /// Adds the 12 edges of a box (e.g. a container) to the given list.
    /// </summary>
    public static void AddBoxEdges(List<Line> lines, BoundingBox box, Color4 color)
    {
        Vector3 a = box.Min, b = box.Max;
        Vector3[] c =
        {
            new(a.X, a.Y, a.Z), new(b.X, a.Y, a.Z), new(b.X, b.Y, a.Z), new(a.X, b.Y, a.Z),
            new(a.X, a.Y, b.Z), new(b.X, a.Y, b.Z), new(b.X, b.Y, b.Z), new(a.X, b.Y, b.Z),
        };
        for (int i = 0; i < 4; i++)
        {
            lines.Add(new Line(c[i], c[(i + 1) % 4], color));
            lines.Add(new Line(c[i + 4], c[(i + 1) % 4 + 4], color));
            lines.Add(new Line(c[i], c[i + 4], color));
        }
    }
}

/// <summary>
/// The result of picking.
/// </summary>
/// <param name="Batch">The batch the picked instance belongs to.</param>
/// <param name="Index">The index of the instance within the batch.</param>
/// <param name="Distance">The distance from the ray origin.</param>
public readonly record struct PickResult(InstanceBatch Batch, int Index, float Distance);

/// <summary>
/// A 3D scene: batches of instanced meshes and lines. Coordinates are z-up.
/// </summary>
public sealed class Scene
{
    private readonly object _sync = new();
    private readonly List<SceneBatch> _batches = new();

    /// <summary>
    /// Raised (on the thread modifying the scene) whenever something changed.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The background color.
    /// </summary>
    public Color4 Background { get => _background; set { _background = value; NotifyChanged(); } }
    private Color4 _background = new(235, 238, 242);

    public T Add<T>(T batch) where T : SceneBatch
    {
        lock (_sync)
        {
            if (batch.Scene != null) throw new InvalidOperationException("The batch already belongs to a scene.");
            batch.Scene = this;
            _batches.Add(batch);
        }
        NotifyChanged();
        return batch;
    }

    public bool Remove(SceneBatch batch)
    {
        bool removed;
        lock (_sync)
        {
            removed = _batches.Remove(batch);
            if (removed) batch.Scene = null;
        }
        if (removed) NotifyChanged();
        return removed;
    }

    public void Clear()
    {
        lock (_sync)
        {
            foreach (var batch in _batches) batch.Scene = null;
            _batches.Clear();
        }
        NotifyChanged();
    }

    /// <summary>
    /// A snapshot of the batches.
    /// </summary>
    public IReadOnlyList<SceneBatch> Batches { get { lock (_sync) return _batches.ToArray(); } }

    internal void NotifyChanged() => Changed?.Invoke();

    /// <summary>
    /// The bounding box of all visible content.
    /// </summary>
    public BoundingBox GetBounds()
    {
        BoundingBox bounds = BoundingBox.Empty;
        foreach (var batch in Batches)
        {
            if (!batch.Visible) continue;
            switch (batch)
            {
                case InstanceBatch instances:
                    instances.Read(span =>
                    {
                        foreach (ref readonly Instance instance in span)
                            bounds = bounds.Include(BoundingBox.Of(instance));
                    });
                    break;
                case LineBatch lines:
                    lines.Read(memory =>
                    {
                        foreach (var line in memory.Span)
                            bounds = bounds.Include(line.From).Include(line.To);
                    });
                    break;
            }
        }
        return bounds;
    }

    /// <summary>
    /// Returns the closest pickable instance hit by the ray (if any).
    /// </summary>
    public PickResult? Pick(Ray ray)
    {
        PickResult? best = null;
        foreach (var batch in Batches)
        {
            if (batch is not InstanceBatch instances || !instances.Visible || !instances.Pickable) continue;
            instances.Read(span =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    float? distance = ray.Intersect(span[i]);
                    if (distance.HasValue && (best == null || distance.Value < best.Value.Distance))
                        best = new PickResult(instances, i, distance.Value);
                }
            });
        }
        return best;
    }
}
