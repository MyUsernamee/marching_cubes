

using System;
using Godot.Collections;
using System.Dynamic;
using Godot;
using static Godot.GD;
using System.Collections.Generic;
using System.Linq;

[Tool]
[GlobalClass]
public partial class TerrainChunk : MeshInstance3D
{
    public delegate float GenerationFunction(Vector3 position);

    const int COUNT = 4;

    float[] m_values = new float[(COUNT + 2) * (COUNT + 2) * (COUNT + 2)];
    GenerationFunction m_generation_function;

    Godot.Collections.Array surface_array;
    Array<Vector3> verts;
    Array<int> indicies;
    Array<Vector3> normals;
    Array<Vector2> uvs;

    ArrayMesh mesh;


    public IEnumerable<Vector3> iter_cube(Vector3 a, Vector3 b)
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

    int convert_to_index(int x, int y, int z) {
        return (x + 1) + (y + 1) * (COUNT + 2) + (z + 1) * (COUNT + 2) * (COUNT + 2);
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

    public void fill_values()
    {
        foreach (var _p in iter_cube(-Vector3.One, Vector3.One * (COUNT + 1)))
        {
            var position = ((_p + Vector3.One * 0.5f) / COUNT - Vector3.One * 0.5f);
            set_value(_p, m_generation_function(GlobalTransform * position));
        }
    }

    public void generate_mesh()
    {

        surface_array = new Godot.Collections.Array();
        surface_array.Resize((int)Mesh.ArrayType.Max);

        verts = new Array<Vector3>();
        normals = new Array<Vector3>();
        uvs = new Array<Vector2>();
        indicies = new Array<int>();

        verts.Add(Vector3.Forward);
        verts.Add(Vector3.Right);
        verts.Add(Vector3.Back);
        verts.Add(Vector3.Back);
        verts.Add(Vector3.Left);
        verts.Add(Vector3.Forward);



        for (int i = 0; i < 6; i++)
        {
            indicies.Add(i);
            normals.Add(Vector3.Up);
            uvs.Add(Vector2.Zero);
        }

        Print(verts);
        Print(indicies);

        surface_array[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        surface_array[(int)Mesh.ArrayType.Index] = indicies.ToArray();
        surface_array[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        surface_array[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        mesh.ClearSurfaces();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surface_array);

    }

    public override void _Ready()
    {

        mesh = new ArrayMesh();
        Mesh = mesh;
        generate_mesh();
        

        m_values = new float[(COUNT + 2) * (COUNT + 2) * (COUNT + 2)];
        Print("SIZE:" + m_values.Length.ToString());

        m_generation_function = (Vector3 p) =>
        {
            return (float)(p.Length() - 1.0);
        };
        fill_values();
    }

    public override void _Process(double _delta)
    {
        foreach (var _p in iter_cube(-Vector3.One, Vector3.One * (COUNT + 1)))
        {
            var value = get_value(_p);
            var position = ((_p + Vector3.One * 0.5f) / COUNT - Vector3.One * 0.5f);
            DebugDraw3D.DrawSphere(GlobalTransform * position, (float)0.01, new Color(value, (float)0.0, (float)0.0, (float)1.0));
        }
    }

}