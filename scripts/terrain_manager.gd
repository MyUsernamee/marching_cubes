class_name TerrainManager
extends Node3D

@onready var camera = $/root/Game/Camera3D

const CHUNK_LIFETIME = 100

var chunks_dict = {};

func generate_chunk(pos):
    var new_chunk = MarchingCubes.new();
    add_child(new_chunk)
    new_chunk.create(pos * (MarchingCubes.VALUES - 1), func temp(x): return MarchingCubes.perlin(x / 5.0));
    return new_chunk;

func ensure_area_loaded(a, b):
    for x in range(a.x-2, b.x + 3):        
        for y in range(a.y-2, b.y + 3): 
            for z in range(a.z-2, b.z +3):        
                if chunks_dict.has(Vector3(x, y, z)):
                    chunks_dict[Vector3(x, y, z)][1] = Time.get_ticks_msec() + CHUNK_LIFETIME
                    continue;
                var chunk = generate_chunk(Vector3(x, y, z));
                var chunk_data =  [chunk, Time.get_ticks_msec(), Vector3(x, y, z)];
                chunks_dict[Vector3(x, y, z)] = chunk_data

func convert_world_to_chunk_space(a):
    return floor(a / (MarchingCubes.VALUES - 1))

func _ready() -> void:
    pass

func despawn_chunk(chunk):
    chunks_dict[chunk][0].hide()
    chunks_dict[chunk][0].call_deferred("queue_free")
    chunks_dict.erase(chunk)
    

func unload_chunks():
    for chunk in chunks_dict:
        if chunk.distance_to(convert_world_to_chunk_space(camera.global_position)) > 8:
            despawn_chunk(chunk)


func _process(delta: float) -> void:

    ensure_area_loaded(convert_world_to_chunk_space(camera.global_position) - Vector3.ONE, convert_world_to_chunk_space(camera.global_position) + Vector3.ONE)
