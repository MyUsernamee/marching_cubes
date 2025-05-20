
using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using static Godot.GD;

public partial class TerrainLoadingManager : Node
{

    static Array<Array<Node>> queued_loads; // Tree of loads
    public static void ensure_level(int level)
    {
        if (level < queued_loads.Count)
            return;

        for (int i = queued_loads.Count; i <= level; i++)
        {
            queued_loads.Add(new Array<Node>());
        }
    }

    public static void queue_build(Node chunk, int level)
    {
        ensure_level(level);

        queued_loads[level].Add(chunk);

    }

    public static void build_chunk(int build_count)
    {
        var start_time = Time.GetTicksUsec();

        for (int level_index = queued_loads.Count - 1; level_index >= 0; level_index--)
        {
            var level = queued_loads[level_index];
            if (level.Count == 0)
                continue;

            var chunk = level[level.Count - 1];
            level.RemoveAt(level.Count - 1);

            if (chunk == null || !chunk.IsInsideTree())
                continue;

            chunk.Call("build_terrain");
            if (Time.GetTicksUsec() - start_time > 10000)
                return;
        }

    }

    public override void _Ready()
    {
        queued_loads = new Array<Array<Node>>();
    }

    public override void _Process(double _d)
    {
            build_chunk(16);
    }

}