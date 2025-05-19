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
    public int  level    = 0;
    public bool has_terrain = false;

    public bool built    = false;
    public bool building = false;

    private const float WIGGLE_ROOM = 0.5f;

    public TerrainChunk terrain;
    public Camera3D     camera;


    FastNoiseLite noise = new FastNoiseLite();

    // --------------------------------------------------------------------
    //  Functions (names preserved)
    // --------------------------------------------------------------------
    public float gen_fun(Vector3 x)
    {

        

        return x.DistanceTo(Vector3.Zero) - 40_000f;
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

    // Returns true if point is inside the leaf
    public bool is_inside(Vector3 x, float wiggle)
    {
        Vector3 local_position = ToLocal(x);
        float   max            = 0.5f + wiggle;

        return Mathf.Abs(local_position.X) <= max &&
               Mathf.Abs(local_position.Y) <= max &&
               Mathf.Abs(local_position.Z) <= max;
    }

    public bool should_split()
    {
        // Are all sample points the same sign?
        bool  same_sign = true;
        float value     = 0f;

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

        return is_inside(camera.GlobalPosition, WIGGLE_ROOM) &&
               (get_world_size().X > TerrainChunk.get_count()) &&
               !same_sign;
    }

    public bool should_combine()
    {
        Leaf parent = GetParent() as Leaf;

        return !is_inside(camera.GlobalPosition, WIGGLE_ROOM) &&
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
                camera = camera,
                level  = level + 1
            };

            AddChild(child);
            child.Scale    = 0.5f * Vector3.One;
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
        built    = true;

        terrain = new TerrainChunk();
        AddChild(terrain);

        terrain.create(Callable.From<Vector3, float>(gen_fun));
        has_terrain            = true;
    }

    public void gen_terrain()
    {
        if (built || building)
            return;

        building = true;
        TerrainLoadingManager.queue_build(this, level);
    }

    public void unload_terrain()
    {
        building = false;

        if (!built)
            return;

        terrain?.QueueFree();
        built = false;
    }

    public void combine()
    {
        is_split = false;

        foreach (Leaf child in children)
            child.QueueFree();

        children.Clear();
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
        camera ??= GetNode<Camera3D>("/root/Game/Camera3D");
    }

    public override void _Process(double delta)
    {
        auto_split();

        if (!built && !building && !is_split)
            gen_terrain();

        if (is_split && (built || building))
            unload_terrain();

        // Debug drawing (commented out in original)
        DebugDraw3D.DrawBoxAb(GlobalTransform * Vector3.One * -0.5f, GlobalTransform * Vector3.One * 0.5f);
    }
}
