using Godot;
using static Godot.GD;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class Leaf : Node3D
{
    // --------------------------------------------------------------------
    //  Fields  (names preserved)
    // --------------------------------------------------------------------
    public List<Leaf> children = new();
    public bool is_split = false;
    public int level = 0;

    public bool built = false; // If the terrain is built and if we have terrain
    public bool building = false;

    private const float WIGGLE_ROOM = 1.0f;

    public TerrainChunk terrain;
    [Export]
    public Camera3D camera;

    public static Resource terrain_material = Load("res://addons/prototype_mini_bundle/M_prototype_green.tres");

    public static Vector3 camera_position;

    FastNoiseLite noise = new FastNoiseLite();
    bool same_sign = true;

    static Timer split_timer;

    // --------------------------------------------------------------------
    //  Functions (names preserved)
    // --------------------------------------------------------------------
    public float gen_fun(Vector3 x)
    {
        return Math.Min(x.DistanceTo(Vector3.Zero) - 4_000f
            - 50.0f * noise.GetNoise3D(x.X, x.Y, x.Z)
            ,100.0f * noise.GetNoise3D(x.X / 1000.0f, x.Y / 1000.0f, x.Z / 1000.0f));
    }

    public Vector3 get_center() => GlobalPosition;

    public int get_cell_scale()
    {
        return Mathf.FloorToInt(Scale.X / TerrainChunk.get_count());
    }

    public Vector3 get_world_size()
    {
        return GlobalBasis * Vector3.One;
    }

    public bool has_children() {
        return children.Count != 0;
    }

    // Returns true if point is inside the leaf
    public bool is_inside(Vector3 x, float wiggle)
    {
        Vector3 local_position = ToLocal(x);
        float max = 0.5f + wiggle;

        return Mathf.Abs(local_position.X) <= max &&
               Mathf.Abs(local_position.Y) <= max &&
               Mathf.Abs(local_position.Z) <= max;
    }

    public bool should_split()
    {


        return is_inside(camera_position, WIGGLE_ROOM) &&
               (get_world_size().X * 2.0f > TerrainChunk.get_count()) &&
               !same_sign;
    }

    public bool should_combine()
    {
        Leaf parent = GetParent() as Leaf;

        return !is_inside(camera_position, WIGGLE_ROOM) &&
               is_split &&
               parent != null &&
               !parent.should_combine();
    }

    public void split()
    {
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Leaf child = new Leaf
                    {
                    };
                    child.camera = camera;
                    child.level = level + 1;

                    AddChild(child);
                    child.Scale = 0.5f * Vector3.One;
                    child.Position = (new Vector3(x - 0.5f, y - 0.5f, z - 0.5f)) * 0.5f;

                    child.auto_split();
                    children.Add(child);
                }

        is_split = true;
    }

    // Actually builds the terrain
    public void build_terrain()
    {
        if (!building)
            return;

        building = false;
        built = true;

        foreach (Leaf child in children)
            child.QueueFree();

        children.Clear();


        terrain = new TerrainChunk();
        AddChild(terrain);

        terrain.create(Callable.From<Vector3, float>(gen_fun));
        terrain.MaterialOverride = (Material)terrain_material;
        terrain.generate_mesh();

        if (GetParent() is Leaf && ((Leaf)GetParent()).is_ready())
        {
            ((Leaf)GetParent()).unload_terrain();
        }

        DebugDraw3D.DrawBoxAb(GlobalTransform * (Vector3.One * -0.5f), GlobalTransform * (Vector3.One * 0.5f));
    }

    public void gen_terrain()
    {
        if (built || building)
            return;

        building = true;
        TerrainLoadingManager.queue_build(this, level);
    }

    public bool is_ready()
    {
        if (is_split)
        {
            foreach (var child in children)
            {
                if (!child.is_ready())
                {
                    return false;
                }
            }
            return true;
        }
        return built;
    }

    public void unload_terrain()
    {
        building = false;

        if (!built)
            return;


        foreach (var child in children)
        {
            if (!child.is_ready())
            {
                return;
            }
        }

        terrain?.QueueFree();
        built = false;
    }

    public void combine()
    {
        is_split = false;
        // gen_terrain();
    }

    public void auto_split()
    {
        if (should_split() && !is_split)
        {
            GD.Print("Split");
            split();
        }

        if (should_combine() && is_split)
            combine();
    }

    // --------------------------------------------------------------------
    //  Godot callbacks (capitalised for C#)
    // --------------------------------------------------------------------
    public override void _Ready()
    {
        // Are all sample points the same sign?
        float value = 0f;

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                for (int k = -1; k <= 1; k++)
                {
                    float local_value = Mathf.Sign(
                        gen_fun(GlobalTransform * (new Vector3(i, j, k) * 0.5f)));

                    if (i + j + k == -3)   // first corner visited
                    {
                        value = local_value;
                        continue;
                    }

                    if (!Mathf.IsEqualApprox(local_value, value))
                        same_sign = false;
                }

        if (split_timer == null)
        {
            split_timer = new Timer();
            split_timer.Autostart = true;
            split_timer.OneShot = false;
            split_timer.WaitTime = 0.5f;
            AddChild(split_timer);
        }

        split_timer.Connect("timeout", Callable.From(auto_split));


    }

    public float get_value(Vector3 position)
    {
        if (!is_inside(position, 0.0f))
            return 0.0f;

        if (has_children())
        {
            foreach (var child in children)
            {
                var value = child.get_value(position);
                if (value != 0.0f)
                    return value;
            }
        }

        if (!built)
            return 0.0f; // Damn


        Vector3 local_position = ToLocal(position) + Vector3.One * 0.5f;
        local_position *= TerrainChunk.get_count(); // Gilbert
        local_position.X = Mathf.Floor(local_position.X);
        local_position.Y = Mathf.Floor(local_position.Y);
        local_position.Z = Mathf.Floor(local_position.Z);

        if (local_position.X < -1 || local_position.X >= TerrainChunk.get_count() + 1 ||
            local_position.Y < -1 || local_position.Y >= TerrainChunk.get_count() + 1 ||
            local_position.Z < -1 || local_position.Z >= TerrainChunk.get_count() + 1)
            return 0.0f;

        return terrain.get_value(local_position);

    }

    public void set_value(Vector3 position, float value)
    {
        if (!is_inside(position, 0.0f))
            return;

        if (has_children())
        {
            foreach (var child in children)
            {
                child.set_value(position, value);
            }
            return;
        }

        if (!built)
            return; // Damn


        Vector3 local_position = ToLocal(position) + Vector3.One * 0.5f;
        local_position *= TerrainChunk.get_count(); // Gilbert
        local_position.X = Mathf.Floor(local_position.X);
        local_position.Y = Mathf.Floor(local_position.Y);
        local_position.Z = Mathf.Floor(local_position.Z);

        if (local_position.X < -1 || local_position.X >= TerrainChunk.get_count() + 1 ||
            local_position.Y < -1 || local_position.Y >= TerrainChunk.get_count() + 1 ||
            local_position.Z < -1 || local_position.Z >= TerrainChunk.get_count() + 1)
            return;

        terrain.set_value(local_position, value);
        terrain.generate_mesh();

    }

    public override void _Process(double delta)
    {

        if (!built && !building && !is_split)
            gen_terrain();

        if (level == 0)
            camera_position = camera.GlobalPosition;

        // Debug drawing (commented out in original)
    }
}
