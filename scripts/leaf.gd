
class_name Leaf
extends Node3D

var children = [];
var is_split = false;
var level = 0;

var terrain;

var camera;

func gen_fun(x):
	return MarchingCubes.perlin(x / 5.0);

func get_center():
	return global_position 

func get_cell_scale():
	return floor(scale.x / MarchingCubes.VALUES)

static func get_target_scale(distance):
	return floor(pow(distance / (MarchingCubes.VALUES * 4), 2.0));

# Returns if point is inside the leaf
func is_inside(x):
	var local_position = to_local(x)
	return abs(local_position.x) <= 0.5 and abs(local_position.y) <= 0.5 and abs(local_position.z) <= 0.5
	
func should_split():
	return is_inside(camera.global_position) and level < 8

func should_combine():
	return not is_inside(camera.global_position) and is_split and not get_parent().should_combine()

func split():

	if terrain:
		terrain.queue_free();

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
	if terrain or level != 8:
		return;

	terrain = MarchingCubes.new();
	add_child(terrain)
	terrain.create(gen_fun)


func combine():

	# Delete children if we have them.
	for child in children:
		child.queue_free()

	children.clear();

	is_split = false
	# gen_terrain();

func auto_split():
	if should_split() and not is_split:
		split()

	if should_combine() and is_split:
		combine();

func _ready() -> void:
	if not camera:
		camera = $/root/Game/Camera3D


func _process(delta: float) -> void:
	
	if not terrain and not is_split:
		gen_terrain()
		pass

	auto_split()

	# Draw outlines of self
	DebugDraw3D.draw_box_ab(global_position - global_basis * Vector3.ONE * 0.5, global_position + global_basis * Vector3.ONE * 0.5,)
	
	
