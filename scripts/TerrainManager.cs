using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class TerrainManager : Node3D
{
    private Camera3D _camera;

    private const ulong ChunkLifetimeMs = 100;          // milliseconds
    private readonly Dictionary<Vector3I, ChunkData> _chunks = new();

    // --------------------------------------------------------------------
    //  Helpers
    // --------------------------------------------------------------------
    private static Vector3I WorldToChunkCoords(Vector3 worldPos)
    {
        // Assumes each chunk spans (TerrainChunk.GetCount() - 1) world units
        float size = 1.0f;
        return (Vector3I)(worldPos / size).Floor();
    }

    private TerrainChunk GenerateChunk(Vector3I coords)
    {
        var chunk = new TerrainChunk();
        AddChild(chunk);

        var scale = Vector3.One;
        chunk.Scale = scale;

        // Center the chunk inside its grid cell
        chunk.GlobalPosition = (Vector3)coords * (scale) + scale / 2f;

        // Kick off generation
        chunk.create(Callable.From<Vector3, float>(TerrainChunk._generation_function));   // assuming static delegate

        return chunk;
    }

    private void EnsureAreaLoaded(Vector3I a, Vector3I b)
    {
        for (int x = a.X - 2; x <= b.X + 2; x++)
            for (int y = a.Y - 2; y <= b.Y + 2; y++)
                for (int z = a.Z - 2; z <= b.Z + 2; z++)
                {
                    var key = new Vector3I(x, y, z);

                    // Refresh existing chunk
                    if (_chunks.TryGetValue(key, out var data))
                    {
                        data.DespawnTime = Time.GetTicksMsec() + ChunkLifetimeMs;
                        _chunks[key] = data;          // structs are copied
                        continue;
                    }

                    // Spawn new chunk
                    var chunk = GenerateChunk(key);
                    _chunks[key] = new ChunkData(chunk, Time.GetTicksMsec() + ChunkLifetimeMs, key);
                }
    }

    private void DespawnChunk(Vector3I key)
    {
        var data = _chunks[key];
        data.Chunk.Hide();
        data.Chunk.QueueFree();
        _chunks.Remove(key);
    }

    private void UnloadChunks()
    {
        // Iterate over a copy to avoid modifying during enumeration
        var keys = new List<Vector3I>(_chunks.Keys);
        foreach (var key in keys)
        {
            var chunkPos = _chunks[key].Chunk.GlobalPosition;
            if (chunkPos.DistanceTo(_camera.GlobalPosition) > 16f)
                DespawnChunk(key);
        }
    }

    // --------------------------------------------------------------------
    //  Godot callbacks
    // --------------------------------------------------------------------
    public override void _Ready()
    {
        _camera ??= GetNode<Camera3D>("/root/Game/Camera3D");
    }

    public override void _Process(double delta)
    {

        var camChunk = WorldToChunkCoords(_camera.GlobalPosition);
        EnsureAreaLoaded(camChunk - Vector3I.One, camChunk + Vector3I.One);
    }

    // --------------------------------------------------------------------
    //  Internal data structure
    // --------------------------------------------------------------------
    private struct ChunkData
    {
        public TerrainChunk Chunk;
        public ulong DespawnTime;
        public Vector3I Index;

        public ChunkData(TerrainChunk chunk, ulong despawnTime, Vector3I index)
        {
            Chunk = chunk;
            DespawnTime = despawnTime;
            Index = index;
        }
    }
}
