
class_name Leaf
extends Node3D

var children = [];
var is_split = false;
var level = 0;
var has_terrain = false;

const WIGGLE_ROOM = 0.5

var terrain;

var camera;

func gen_fun(x):
	return -x.distance_to(Vector3.ZERO) + 4000 + 40.0 * MarchingCubes.perlin(x / 5.0);

func get_center():
	return global_position 

func get_cell_scale():
	return floor(scale.x / MarchingCubes.COUNT)

func get_world_size():
	return global_basis* Vector3.ONE

# Returns if point is inside the leaf
func is_inside(x, wiggle):
	var local_position = to_local(x)
	var max = 0.5 + wiggle
	return abs(local_position.x) <= max and abs(local_position.y) <= max and abs(local_position.z) <= max
	
func should_split():
	return is_inside(camera.global_position, WIGGLE_ROOM) and (get_world_size().x > MarchingCubes.COUNT);


func should_combine():
	return not is_inside(camera.global_position, WIGGLE_ROOM) and is_split and get_parent() is Leaf and not get_parent().should_combine()

func split():

	for x in range(2):
		for y in range(2):
			for z in range(2):
				var child = Leaf.new()
				child.camera = camera
				child.level = level + 1
				add_child(child)
				child.scale = 0.5 * Vector3.ONE
				child.position = Vector3(x - 0.5, y - 0.5, z - 0.5) * 0.5
				children.append(child)
	is_split = true

func gen_terrain():
	if has_terrain:
		return;

	terrain = MarchingCubes.new();
	add_child(terrain)
	terrain.create(gen_fun)
	terrain.sorting_offset = get_world_size().x * 100.0;
	has_terrain = true

func unload_terrain():
	if not has_terrain:
		return;

	terrain.queue_free();
	has_terrain = false


func combine():

	# Delete children if we have them.
	for child in children:
		child.queue_free()

	children.clear();

	is_split = false
	# gen_terrain()

func auto_split():
	if should_split() and not is_split:
		split()

	if should_combine() and is_split:
		combine();

func _ready() -> void:
	if not camera:
		camera = $/root/Game/Camera3D


func _process(delta: float) -> void:
	
	if not has_terrain and not is_split:
		gen_terrain()

	auto_split()

	return;
	# Draw outlines of self
	DebugDraw3D.draw_box_ab(global_position - global_basis * Vector3.ONE * 0.5, global_position + global_basis * Vector3.ONE * 0.5,)
	
	
