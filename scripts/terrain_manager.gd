class_name TerrainManager
extends Node3D

@onready var camera = $/root/Game/Camera3D

var chunks = [];
var chunks_dict = {};

func generate_chunk(pos):
    var new_chunk = MarchingCubes.new();
    add_child(new_chunk)
    new_chunk.create(pos * (MarchingCubes.VALUES - 1), func temp(x): return MarchingCubes.perlin(x / 5.0));
    return new_chunk;

func ensure_area_loaded(a, b):
    for x in range(a.x, b.x + 1):        
        for y in range(a.y, b.y + 1): 
            for z in range(a.z, b.z + 1):        
                if chunks_dict.has(Vector3(x, y, z)):
                    continue;
                var chunk = generate_chunk(Vector3(x, y, z));
                chunks_dict[Vector3(x, y, z)] = chunk;
                chunks.append(chunk);

func convert_world_to_chunk_space(a):
    return floor(a / (MarchingCubes.VALUES - 1))

func _ready() -> void:
    pass

func _process(delta: float) -> void:

    ensure_area_loaded(convert_world_to_chunk_space(camera.global_position) - Vector3.ONE, convert_world_to_chunk_space(camera.global_position) + Vector3.ONE)
