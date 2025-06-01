using static Godot.GD;
using Godot;
using System.Linq;
using System.Diagnostics;

[Tool]
[GlobalClass]
public partial class OctreeNode : Node3D
{

    public const float MIN_SIZE = 1.0f;
    

    [Export]
    Callable sdf;

    OctreeNode[] children;
    bool has_children = false;
    float value; // If we are childless, this is our value.

    float get_size()
    {
        return (GlobalBasis * Vector3.One).Length();
    }

    void split()
    {


        for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
                for (int z = 0; z <= 1; z++)
                {
                    var local_position = new Vector3(x, y, z) * 0.5f - Vector3.One * 0.25f;
                    OctreeNode child = new OctreeNode(sdf);
                    AddChild(child);
                    child.Position = local_position;
                    child.Scale = Vector3.One * 0.5f;
                    children[x + y * 2  + z * 4] = (child);
                }

        has_children = true;
    }

    void combine()
    {
        if (!has_children)
            return;
        foreach (var child in children)
        {
            if (child ==null)
                continue;
            child.QueueFree();
        }
        has_children = false;
    }

    bool should_be_split()
    {
        return Mathf.Abs(value) < get_size() && get_size() > MIN_SIZE;
    }

    /// <summary>
    /// Fills the octree given the sdf function
    /// </summary>
    public void fill_values()
    {

        if (has_children && !should_be_split())
            combine(); // Unload the children if we have them;

        // Sample the SDF at this point
        value = (float)sdf.Call(GlobalPosition);

        if (should_be_split() && !has_children)
        {
            split();

            foreach (var child in children)
            {
                if (child == null)
                    continue;
                child.fill_values();
            }
        }


    }

    public OctreeNode(Callable _sdf)
    {
        children = new OctreeNode[8];
        has_children = false;
        sdf = _sdf;
    }

    public OctreeNode()
    {
        children = new OctreeNode[8];
        has_children = false;
        sdf = Callable.From((Vector3 p) =>
        {
            return p.Length() - 10.0;
        });
    }

    public void draw_debug()
    {
        if (!has_children && Mathf.Abs(value) < get_size())
            DebugDraw3D.DrawBoxAb(GlobalTransform * (Vector3.One * 0.5f), GlobalTransform * (Vector3.One * -0.5f), null, null, true);

    }

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            // fill_values();
            // draw_debug();
        }
    }

}