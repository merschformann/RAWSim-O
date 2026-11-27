using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace Aquarium.Avalonia;

/// <summary>
/// Renders a <see cref="Scene"/> with OpenGL 3.3 / OpenGL ES 3.0 (one instanced draw call per batch).
/// Must only be used on the thread owning the GL context.
/// </summary>
internal sealed unsafe class GlSceneRenderer : IDisposable
{
    private const int GL_UNSIGNED_INT = 0x1405, GL_LINES = 0x0001, GL_CULL_FACE = 0x0B44, GL_BLEND = 0x0BE2,
        GL_SRC_ALPHA = 0x0302, GL_ONE_MINUS_SRC_ALPHA = 0x0303, GL_DYNAMIC_DRAW = 0x88E8, GL_PACK_ALIGNMENT = 0x0D05, GL_LEQUAL = 0x0203;

    private const string VertexShader = """
        uniform mat4 uViewProj;
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec3 aNormal;
        layout(location = 2) in vec3 iPos;
        layout(location = 3) in vec3 iSize;
        layout(location = 4) in vec4 iRot;
        layout(location = 5) in vec4 iColor;
        out vec3 vNormal;
        out vec3 vLocal;
        out vec3 vSize;
        out vec4 vColor;
        out vec3 vWorld;
        vec3 rotate(vec4 q, vec3 v) { return v + 2.0 * cross(q.xyz, cross(q.xyz, v) + q.w * v); }
        void main() {
            vec3 world = iPos + rotate(iRot, aPos * iSize);
            vNormal = rotate(iRot, aNormal / max(iSize, vec3(1e-6)));
            vLocal = aPos;
            vSize = iSize;
            vColor = iColor;
            vWorld = world;
            gl_Position = uViewProj * vec4(world, 1.0);
        }
        """;

    private const string FragmentShader = """
        uniform vec3 uCameraPos;
        uniform float uEdges;
        in vec3 vNormal;
        in vec3 vLocal;
        in vec3 vSize;
        in vec4 vColor;
        in vec3 vWorld;
        out vec4 fragColor;
        void main() {
            vec3 n = normalize(vNormal);
            vec3 toCamera = normalize(uCameraPos - vWorld);
            vec3 sun = normalize(vec3(0.35, 0.25, 1.0));
            float light = 0.35 + 0.4 * max(dot(n, sun), 0.0) + 0.3 * abs(dot(n, toCamera));
            vec3 color = vColor.rgb * light;
            if (uEdges > 0.5) {
                // Distance to the faces per axis (world units): on a face one of them is ~0, the second smallest is the distance to the closest edge
                vec3 d = (0.5 - abs(vLocal)) * vSize;
                float lo = min(d.x, min(d.y, d.z)), hi = max(d.x, max(d.y, d.z));
                float mid = d.x + d.y + d.z - lo - hi;
                float w = max(fwidth(mid), 1e-6);
                float edge = 1.0 - smoothstep(0.5 * w, 1.5 * w, mid);
                // Fade out the edges of boxes only a few pixels large (they would be completely dark otherwise)
                float pixels = min(vSize.x, min(vSize.y, vSize.z)) / w;
                edge *= smoothstep(3.0, 10.0, pixels);
                color = mix(color, color * 0.4, edge);
            }
            fragColor = vec4(color, vColor.a);
        }
        """;

    private const string LineVertexShader = """
        uniform mat4 uViewProj;
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec4 aColor;
        out vec4 vColor;
        void main() { vColor = aColor; gl_Position = uViewProj * vec4(aPos, 1.0); }
        """;

    private const string LineFragmentShader = """
        in vec4 vColor;
        out vec4 fragColor;
        void main() { fragColor = vColor; }
        """;

    private readonly GlInterface _gl;
    private readonly delegate* unmanaged<int, int, int, IntPtr, int, void> _drawElementsInstanced;
    private readonly delegate* unmanaged<int, int, void> _vertexAttribDivisor;
    private readonly delegate* unmanaged<int, float, float, float, void> _uniform3f;
    private readonly delegate* unmanaged<int, int, void> _blendFunc;
    private readonly delegate* unmanaged<int, void> _disable;
    private readonly delegate* unmanaged<int, int, int, int, int, int, void*, void> _readPixels;
    private readonly delegate* unmanaged<int, int, void> _pixelStorei;

    private readonly int _program, _lineProgram;
    private readonly int _uViewProj, _uCameraPos, _uEdges, _uLineViewProj;
    private readonly Dictionary<Mesh, (int vertexBuffer, int indexBuffer, int indexCount)> _meshes = new();
    private readonly Dictionary<SceneBatch, GpuBatch> _batches = new();

    private sealed class GpuBatch
    {
        public int VertexArray, Buffer, Count;
        public long Version = -1;
    }

    public GlSceneRenderer(GlInterface gl)
    {
        _gl = gl;
        GlVersion version = gl.ContextInfo.Version;
        bool es = version.Type == GlProfileType.OpenGLES;
        if (es ? version.Major < 3 : version.Major < 3 || version.Major == 3 && version.Minor < 3)
            throw new NotSupportedException($"OpenGL 3.3 or OpenGL ES 3.0 is required, but only {version.Type} {version.Major}.{version.Minor} is available.");
        Info = $"{version.Type} {version.Major}.{version.Minor}";

        IntPtr Load(string name)
        {
            IntPtr proc = gl.GetProcAddress(name);
            return proc != IntPtr.Zero ? proc : throw new NotSupportedException($"The OpenGL function {name} is not available.");
        }
        _drawElementsInstanced = (delegate* unmanaged<int, int, int, IntPtr, int, void>)Load("glDrawElementsInstanced");
        _vertexAttribDivisor = (delegate* unmanaged<int, int, void>)Load("glVertexAttribDivisor");
        _uniform3f = (delegate* unmanaged<int, float, float, float, void>)Load("glUniform3f");
        _blendFunc = (delegate* unmanaged<int, int, void>)Load("glBlendFunc");
        _disable = (delegate* unmanaged<int, void>)Load("glDisable");
        _readPixels = (delegate* unmanaged<int, int, int, int, int, int, void*, void>)Load("glReadPixels");
        _pixelStorei = (delegate* unmanaged<int, int, void>)Load("glPixelStorei");

        string header = es ? "#version 300 es\nprecision highp float;\n" : "#version 330 core\n";
        _program = CreateProgram(header + VertexShader, header + FragmentShader);
        _uViewProj = gl.GetUniformLocationString(_program, "uViewProj");
        _uCameraPos = gl.GetUniformLocationString(_program, "uCameraPos");
        _uEdges = gl.GetUniformLocationString(_program, "uEdges");
        _lineProgram = CreateProgram(header + LineVertexShader, header + LineFragmentShader);
        _uLineViewProj = gl.GetUniformLocationString(_lineProgram, "uViewProj");
    }

    /// <summary>
    /// A description of the OpenGL context.
    /// </summary>
    public string Info { get; }

    private int CreateProgram(string vertexSource, string fragmentSource)
    {
        int CompileShader(int type, string source)
        {
            int shader = _gl.CreateShader(type);
            string? error = _gl.CompileShaderAndGetError(shader, source);
            if (error != null)
                throw new InvalidOperationException("Shader compilation failed: " + error);
            return shader;
        }
        int vertex = CompileShader(GL_VERTEX_SHADER, vertexSource), fragment = CompileShader(GL_FRAGMENT_SHADER, fragmentSource);
        int program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        string? linkError = _gl.LinkProgramAndGetError(program);
        if (linkError != null)
            throw new InvalidOperationException("Shader linking failed: " + linkError);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    private (int vertexBuffer, int indexBuffer, int indexCount) GetMesh(Mesh mesh)
    {
        if (_meshes.TryGetValue(mesh, out var gpu))
            return gpu;
        // Make sure no vertex array is bound (binding the element buffer would modify it otherwise)
        _gl.BindVertexArray(0);
        // Interleaved position + normal
        var data = new float[mesh.Positions.Length * 6];
        for (int i = 0; i < mesh.Positions.Length; i++)
        {
            Vector3 p = mesh.Positions[i], n = mesh.Normals[i];
            data[i * 6 + 0] = p.X; data[i * 6 + 1] = p.Y; data[i * 6 + 2] = p.Z;
            data[i * 6 + 3] = n.X; data[i * 6 + 4] = n.Y; data[i * 6 + 5] = n.Z;
        }
        int vertexBuffer = _gl.GenBuffer();
        _gl.BindBuffer(GL_ARRAY_BUFFER, vertexBuffer);
        fixed (float* ptr = data)
            _gl.BufferData(GL_ARRAY_BUFFER, data.Length * sizeof(float), (IntPtr)ptr, GL_STATIC_DRAW);
        int indexBuffer = _gl.GenBuffer();
        _gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, indexBuffer);
        fixed (uint* ptr = mesh.Indices)
            _gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, mesh.Indices.Length * sizeof(uint), (IntPtr)ptr, GL_STATIC_DRAW);
        gpu = (vertexBuffer, indexBuffer, mesh.Indices.Length);
        _meshes[mesh] = gpu;
        return gpu;
    }

    private GpuBatch CreateInstanceBatch(InstanceBatch batch)
    {
        var (vertexBuffer, indexBuffer, _) = GetMesh(batch.Mesh); // Before binding the new vertex array (see GetMesh)
        var gpu = new GpuBatch { VertexArray = _gl.GenVertexArray(), Buffer = _gl.GenBuffer() };
        _gl.BindVertexArray(gpu.VertexArray);
        // Mesh attributes
        _gl.BindBuffer(GL_ARRAY_BUFFER, vertexBuffer);
        _gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, indexBuffer);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 6 * sizeof(float), IntPtr.Zero);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, 6 * sizeof(float), 3 * sizeof(float));
        // Instance attributes (see Instance layout: position, size, rotation, color)
        _gl.BindBuffer(GL_ARRAY_BUFFER, gpu.Buffer);
        int stride = Instance.SizeInBytes;
        (int location, int size, int type, int normalized, int offset)[] attributes =
        {
            (2, 3, GL_FLOAT, 0, 0),
            (3, 3, GL_FLOAT, 0, 12),
            (4, 4, GL_FLOAT, 0, 24),
            (5, 4, GL_UNSIGNED_BYTE, 1, 40),
        };
        foreach (var a in attributes)
        {
            _gl.EnableVertexAttribArray(a.location);
            _gl.VertexAttribPointer(a.location, a.size, a.type, a.normalized, stride, a.offset);
            _vertexAttribDivisor(a.location, 1);
        }
        _gl.BindVertexArray(0);
        return gpu;
    }

    private GpuBatch CreateLineBatch()
    {
        var gpu = new GpuBatch { VertexArray = _gl.GenVertexArray(), Buffer = _gl.GenBuffer() };
        _gl.BindVertexArray(gpu.VertexArray);
        _gl.BindBuffer(GL_ARRAY_BUFFER, gpu.Buffer);
        // A line consists of two vertices: position (12 bytes) followed by the shared color (4 bytes) - see Upload
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, 16, IntPtr.Zero);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, GL_UNSIGNED_BYTE, 1, 16, 12);
        _gl.BindVertexArray(0);
        return gpu;
    }

    private void Upload(SceneBatch batch, GpuBatch gpu)
    {
        long version = batch.Version;
        if (version == gpu.Version) return;
        _gl.BindBuffer(GL_ARRAY_BUFFER, gpu.Buffer);
        switch (batch)
        {
            case InstanceBatch instances:
                instances.Read(span =>
                {
                    fixed (Instance* ptr = span)
                        _gl.BufferData(GL_ARRAY_BUFFER, Math.Max(span.Length, 1) * Instance.SizeInBytes, (IntPtr)ptr, GL_DYNAMIC_DRAW);
                    gpu.Count = span.Length;
                });
                break;
            case LineBatch lines:
                lines.Read(memory =>
                {
                    ReadOnlySpan<Line> span = memory.Span;
                    var vertices = new LineVertex[Math.Max(span.Length * 2, 1)];
                    for (int i = 0; i < span.Length; i++)
                    {
                        vertices[2 * i] = new LineVertex(span[i].From, span[i].Color);
                        vertices[2 * i + 1] = new LineVertex(span[i].To, span[i].Color);
                    }
                    fixed (LineVertex* ptr = vertices)
                        _gl.BufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(LineVertex), (IntPtr)ptr, GL_DYNAMIC_DRAW);
                    gpu.Count = span.Length * 2;
                });
                break;
        }
        gpu.Version = version;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct LineVertex(Vector3 Position, Color4 Color);

    /// <summary>
    /// Renders the scene into the given framebuffer.
    /// </summary>
    public void Render(Scene scene, Camera camera, int framebuffer, int width, int height)
    {
        _gl.BindFramebuffer(GL_FRAMEBUFFER, framebuffer);
        _gl.Viewport(0, 0, width, height);
        Color4 bg = scene.Background;
        _gl.ClearColor(bg.R / 255f, bg.G / 255f, bg.B / 255f, 1);
        _gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        _gl.Enable(GL_DEPTH_TEST);
        _gl.DepthFunc(GL_LEQUAL);
        _gl.DepthMask(1);
        _gl.Enable(GL_CULL_FACE);
        _disable(GL_BLEND);

        IReadOnlyList<SceneBatch> batches = scene.Batches;
        // Release resources of removed batches
        foreach (SceneBatch removed in _batches.Keys.Where(b => !batches.Contains(b)).ToList())
        {
            _gl.DeleteVertexArray(_batches[removed].VertexArray);
            _gl.DeleteBuffer(_batches[removed].Buffer);
            _batches.Remove(removed);
        }

        Matrix4x4 viewProjection = camera.GetViewProjection(width / (float)Math.Max(height, 1));
        Vector3 cameraPosition = camera.Position;

        // Instances (opaque first, then transparent ones without writing depth)
        _gl.UseProgram(_program);
        _gl.UniformMatrix4fv(_uViewProj, 1, false, &viewProjection);
        _uniform3f(_uCameraPos, cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
        foreach (bool transparentPass in new[] { false, true })
        {
            if (transparentPass)
            {
                _gl.Enable(GL_BLEND);
                _blendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                _gl.DepthMask(0);
            }
            foreach (SceneBatch batch in batches)
            {
                if (batch is not InstanceBatch instances || !instances.Visible || instances.Transparent != transparentPass) continue;
                if (!_batches.TryGetValue(batch, out GpuBatch? gpu))
                    _batches[batch] = gpu = CreateInstanceBatch(instances);
                Upload(batch, gpu);
                if (gpu.Count == 0) continue;
                _gl.Uniform1f(_uEdges, instances.ShowEdges && instances.Mesh.HasEdges ? 1 : 0);
                _gl.BindVertexArray(gpu.VertexArray);
                _drawElementsInstanced(GL_TRIANGLES, GetMesh(instances.Mesh).indexCount, GL_UNSIGNED_INT, IntPtr.Zero, gpu.Count);
            }
        }
        _gl.DepthMask(1);
        _disable(GL_BLEND);

        // Lines
        _gl.UseProgram(_lineProgram);
        _gl.UniformMatrix4fv(_uLineViewProj, 1, false, &viewProjection);
        foreach (SceneBatch batch in batches)
        {
            if (batch is not LineBatch lines || !lines.Visible) continue;
            if (!_batches.TryGetValue(batch, out GpuBatch? gpu))
                _batches[batch] = gpu = CreateLineBatch();
            Upload(batch, gpu);
            if (gpu.Count == 0) continue;
            _gl.BindVertexArray(gpu.VertexArray);
            _gl.DrawArrays(GL_LINES, 0, gpu.Count);
        }
        _gl.BindVertexArray(0);
        _gl.UseProgram(0);
    }

    /// <summary>
    /// Reads the content of the current framebuffer (RGBA, top row first).
    /// </summary>
    public byte[] ReadPixels(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        _pixelStorei(GL_PACK_ALIGNMENT, 1);
        fixed (byte* ptr = pixels)
            _readPixels(0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
        // OpenGL starts with the bottom row
        var flipped = new byte[pixels.Length];
        int row = width * 4;
        for (int y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, y * row, flipped, (height - 1 - y) * row, row);
        return flipped;
    }

    public void Dispose()
    {
        foreach (GpuBatch gpu in _batches.Values)
        {
            _gl.DeleteVertexArray(gpu.VertexArray);
            _gl.DeleteBuffer(gpu.Buffer);
        }
        _batches.Clear();
        foreach (var (vertexBuffer, indexBuffer, _) in _meshes.Values)
        {
            _gl.DeleteBuffer(vertexBuffer);
            _gl.DeleteBuffer(indexBuffer);
        }
        _meshes.Clear();
        _gl.DeleteProgram(_program);
        _gl.DeleteProgram(_lineProgram);
    }
}
