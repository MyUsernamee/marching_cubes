

using System;
using Godot.Collections;
using System.Dynamic;
using Godot;
using static Godot.GD;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection.Metadata;

[Tool]
[GlobalClass]
public partial class TerrainChunk : MeshInstance3D
{
    public static int COUNT = 8;

    static Vector3[] DIRECTIONS = {
        Vector3.Back,
        Vector3.Right,
        Vector3.Up
    };


    static Vector3[][] ORTH_DIRECTIONS = {
        // Orthogonal to Vector3.Back (Z+)
        new Vector3[] { Vector3.Right, Vector3.Up },
        // Orthogonal to Vector3.Right (X+)
        new Vector3[] { Vector3.Up, Vector3.Back },
        // Orthogonal to Vector3.Up (Y+)
        new Vector3[] { Vector3.Back, Vector3.Right }
    };



    [Export]
    bool debug;

    static RenderingDevice rd;
    static ComputeShader compute_shader;
    static ShaderBufferUniform value_shader_buffer;
    static ShaderBufferUniform normal_shader_buffer;
    static ShaderBufferUniform verts_shader_buffer;
    static ShaderBufferUniform parameters_shader_buffer;

    float[] m_values = new float[(COUNT + 2) * (COUNT + 2) * (COUNT + 2)];
    Vector3[] m_verts = new Vector3[COUNT * COUNT * COUNT];

    Callable m_generation_function;

    Godot.Collections.Array surface_array;
    Array<Vector3> verts;
    Array<int> indicies;
    Godot.Mutex indicies_mutex = new Godot.Mutex();
    Array<Vector3> normals;
    Array<Vector2> uvs;

    ArrayMesh mesh;

    public static int get_count()
    {
        return COUNT;
    }
    public static Vector3[] get_orthogonal_driections(Vector3 d) {
        for (int i = 0; i < DIRECTIONS.Length; i++)
        {
            if (d == DIRECTIONS[i])
                return ORTH_DIRECTIONS[i];
        }
        throw new ArgumentException("Invalid direction vector", nameof(d));
    }

    public static float get_intersection_point(float a, float b) {
        return -a / (b - a);
    }

    /// <summary>
    /// Fills the terrain chunk's value grid by evaluating the generation function at each grid point.
    /// Utilizes parallel processing to compute values efficiently across the chunk's volume.
    /// </summary>
    public void fill_values()
    {
        var t = par_for_cube(-Vector3.One, Vector3.One * (COUNT + 1), Callable.From<Vector3>((_p) =>
                    {
                    var position = ((_p + Vector3.One * 0.5f) / COUNT - Vector3.One * 0.5f);
                    set_value(_p, (float)m_generation_function.Call(GlobalTransform * position));
                    }));

        WorkerThreadPool.WaitForGroupTaskCompletion(t);
    }



    public float get_value(int x, int y, int z)
    {
        return m_values[convert_to_index(x, y, z)];
    }
    public float get_value(Vector3 p)
    {
        return get_value((int)p.X, (int)p.Y, (int)p.Z);
    }
    public void set_value(int x, int y, int z, float value)
    {
        m_values[convert_to_index(x, y, z)] = value;
    }
    public void set_value(Vector3 p, float value)
    {
        set_value((int)p.X, (int)p.Y, (int)p.Z, value);
    }

    public Vector3 get_normal(Vector3 position) {
        Vector3 normal = Vector3.Zero;
        float a = get_value(position);

        foreach (var direction in DIRECTIONS)
        {
            float value = get_value(position + direction);
            normal += direction * (value - a);
        }


        return normal;
    }


    static int  convert_to_index(int x, int y, int z) {
        return (x + 1) + (y + 1) * (COUNT + 2) + (z + 1) * (COUNT + 2) * (COUNT + 2);
    }

    int convert_to_index(Vector3 p) {
        return convert_to_index((int)p.X, (int)p.Y, (int)p.Z);
    }

    public static IEnumerable<Vector3> iter_cube(Vector3 a, Vector3 b)
    {
        for (int x = (int)a.X; x < (int)b.X; x++)
        {
            for (int y = (int)a.Y; y < (int)b.Y; y++)
            {
                for (int z = (int)a.Z; z < (int)b.Z; z++)
                {
                    yield return new Vector3(x, y, z);
                }
            }
        }
    }

    public long par_for_cube(Vector3 start, Vector3 end, Callable function)
    {

        int length = (int)(end.X - start.X);
        int width = (int)(end.Y - start.Y);
        int height = (int)(end.Y - start.Y);
        int elements = length * width * height;

        var task = WorkerThreadPool.AddGroupTask(Callable.From<int>((index) =>
                    {
                    int x = index % length;
                    int y = (int)(index / length) % width;
                    int z = (int)(index / length / width);


                    function.Call(new Vector3(x, y, z) + start);

                    }), elements, -1, true);

        return task;

    }
    public void generate_quads()
    {

        set_vert_gpu();

        var t = par_for_cube(-Vector3.One, Vector3.One * (COUNT), Callable.From<Vector3>((_p) =>
        {
            //            verts[convert_to_index(_p)] = (_p / COUNT - Vector3.One * 0.5f);
            uvs[convert_to_index(_p)] = (Vector2.Zero);

            foreach (var direction in iter_cube(Vector3.Zero, Vector3.One * 2))
            {


                float a = get_value(_p);
                float b = get_value(_p + direction);

                if (a * b >= 0)
                    continue; // No Edge here

                if (direction.X + direction.Y + direction.Z != 1 || _p.X < 0.0 || _p.Y < 0.0 || _p.Z < 0.0)
                    continue;

                var orth_directions = get_orthogonal_driections(direction);
                var right = _p - orth_directions[0];
                var up = _p - orth_directions[1];
                var corner = _p - orth_directions[0] - orth_directions[1];

                indicies_mutex.Lock();
                if (a > b)
                {
                    indicies.Add(convert_to_index(_p));
                    indicies.Add(convert_to_index(right));
                    indicies.Add(convert_to_index(up));
                    indicies.Add(convert_to_index(right));
                    indicies.Add(convert_to_index(corner));
                    indicies.Add(convert_to_index(up));
                }
                else
                {
                    indicies.Add(convert_to_index(_p));
                    indicies.Add(convert_to_index(up));
                    indicies.Add(convert_to_index(right));
                    indicies.Add(convert_to_index(up));
                    indicies.Add(convert_to_index(corner));
                    indicies.Add(convert_to_index(right));
                }
                indicies_mutex.Unlock();


            }


        }));

        WorkerThreadPool.WaitForGroupTaskCompletion(t);

    }

    public static void init_gpu_buffers() {
        // We need a way to read and write data to the gpu for atleast generating the vert positions.
        // We need to read vector3s from the gpu so for this 

        if (rd != null)
            return; 
        rd = RenderingServer.CreateLocalRenderingDevice();
        value_shader_buffer = ShaderBufferUniform.From(rd, new float[1]);
        verts_shader_buffer = ShaderBufferUniform.From(rd, new Vector3[1]); 
        normal_shader_buffer = ShaderBufferUniform.From(rd, new Vector3[1]); 
        parameters_shader_buffer = ShaderBufferUniform.From(rd, new Vector3[1]);

        compute_shader = new ComputeShader("res://scripts/shaders/surface_net.glsl", rd);
        compute_shader.AddUniform(value_shader_buffer);
        compute_shader.AddUniform(verts_shader_buffer);
        compute_shader.AddUniform(parameters_shader_buffer);
        compute_shader.AddUniform(normal_shader_buffer);
    }

    public static (Vector3[], Vector3[]) place_verts_gpu(float[] values, Vector3I size, Vector3 scale) {
        value_shader_buffer.SetData(values);
        verts_shader_buffer.SetData(new Vector3[size.X * size.Y * size.Z]);
        normal_shader_buffer.SetData(new Vector3[size.X * size.Y * size.Z]);
        Vector3[] _params = {new Vector3(size.X, size.Y, size.Z), scale};
        parameters_shader_buffer.SetData(_params);
        value_shader_buffer.UpdateDeviceBuffer();
        verts_shader_buffer.UpdateDeviceBuffer();
        normal_shader_buffer.UpdateDeviceBuffer();
        parameters_shader_buffer.UpdateDeviceBuffer();
        compute_shader.Run(size);
        compute_shader.Sync();
        return (verts_shader_buffer.GetDeviceData<Vector3>(), normal_shader_buffer.GetDeviceData<Vector3>());
;
    }

    public void set_vert_gpu() {

        var _a = place_verts_gpu(m_values, Vector3I.One * (COUNT + 2), Vector3.One * COUNT);
        verts = new Array<Vector3>(_a.Item1);
        normals = new Array<Vector3>(_a.Item2);

    }

    public void generate_mesh()
    {

        if (surface_array != null)
        {
            surface_array.Dispose();
            verts.Clear();
            normals.Clear();
            uvs.Clear();
            indicies.Clear();
        }

        surface_array = new Godot.Collections.Array();
        surface_array.Resize((int)Mesh.ArrayType.Max);

        verts = new Array<Vector3>();
        normals = new Array<Vector3>();
        uvs = new Array<Vector2>();
        indicies = new Array<int>();

        verts.Resize((int)Math.Pow(COUNT + 2, 3));
        normals.Resize((int)Math.Pow(COUNT + 2, 3));
        uvs.Resize((int)Math.Pow(COUNT + 2, 3));

        var start = Time.GetTicksUsec();
        generate_quads();


        surface_array[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surface_array[(int)Mesh.ArrayType.Index] = indicies.ToArray();
        surface_array[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surface_array[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

        start = Time.GetTicksUsec();
        if (verts.Count != 0 && indicies.Count != 0)
        {
            foreach(var child in GetChildren()) {
                child.QueueFree();

            }
            mesh.ClearSurfaces();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface_array);

            CreateTrimeshCollision();

        }


    }

    public void create(Callable generation_function) {
        m_generation_function = generation_function;
        fill_values();
    }

    public override void _Ready()
    {
        init_gpu_buffers();

        mesh = new ArrayMesh();
        Mesh = mesh;

        m_values = new float[(COUNT + 2) * (COUNT + 2) * (COUNT + 2)];


    }

    public override void _Process(double _delta)
    {


        if (debug) {
            foreach (var _p in iter_cube(-Vector3.One, Vector3.One * (COUNT + 1)))
            {

                var vert = verts[convert_to_index(_p)];
                DebugDraw3D.DrawSphere(GlobalTransform * vert, 0.01f, new Color(1, 0, 0, 1), 0);
            }

            for (int i = 0; i < indicies.Count; i++) {
                if (indicies[i] > verts.Count || indicies[i] < 0) {
                    continue;
                }
                var vert = verts[indicies[i]];
                DebugDraw3D.DrawText(GlobalTransform * vert, indicies[i].ToString());
            }
        }

        return;
        foreach (var _p in iter_cube(-Vector3.One, Vector3.One * (COUNT + 1)))
        {
            var value = get_value(_p);
            var position = ((_p + Vector3.One * 0.5f) / COUNT - Vector3.One * 0.5f);
            DebugDraw3D.DrawSphere(GlobalTransform * position, (float)0.01, new Color(value, (float)0.0, (float)0.0, (float)1.0));
        }
    }

}
