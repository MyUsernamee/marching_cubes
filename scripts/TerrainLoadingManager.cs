
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using static Godot.GD;


public partial class TerrainLoadingManager : Node
{

    static List<List<Node>> queued_loads; // Tree of loads
    static HashSet<Node3D> in_queue;
    public static void ensure_level(int level)
    {
        if (level < queued_loads.Count)
            return;

        for (int i = queued_loads.Count; i <= level; i++)
        {
            queued_loads.Add(new List<Node>());
        }
    }

    public static void queue_build(Node chunk, int level)
    {
        ensure_level(level);

        if (in_queue.Contains(chunk))
            return;

        queued_loads[level].Add(chunk);

    }

    public static void build_chunk(int build_count)
    {
        var start_time = Time.GetTicksUsec();

        for (int level_index = queued_loads.Count - 1; level_index >= 0; level_index--)
        {
            var level = queued_loads[level_index];
            while (level.Count > 0)
            {

                Leaf chunk = (Leaf)level.Last();
                level.RemoveAt(level.Count - 1);
                in_queue.Remove(chunk);

                if (chunk == null || !chunk.IsInsideTree() || !chunk.building)
                    continue;

                chunk.Call("build_terrain");
                if (Time.GetTicksUsec() - start_time > 10000)
                    return;
            }
        }

    }

    public override void _Ready()
    {
        queued_loads = new List<List<Node>>();
        in_queue = new HashSet<Node3D>();
    }

    public override void _Process(double _d)
    {
        build_chunk(16);
    }

}