@tool
extends MeshInstance3D
class_name MarchingCubes

const COUNT = 8;
const VALUES = COUNT + 1;

const last_update = 0;

@export var debug = false;

var mutex = Mutex.new();
var task;
var fill_task

var is_ready = false;


var value_array: Array[float] = [];
@export var needs_update = false;

var surface_array = []
var verts: PackedVector3Array;
var indicies: PackedInt32Array;
var normals: PackedVector3Array;
var uvs: PackedVector2Array;

var generation_function: Callable;

static func frac(x):
	return x - floor(x);

static func _hash(n):
	return frac(sin(n) * 43758.5453);
	
static func perlin(x):
	var p: Vector3 = Vector3(floor(x.x), floor(x.y), floor(x.z));
	var f: Vector3 = Vector3(frac(x.x), frac(x.y), frac(x.z));
	
	f = f * f * (Vector3.ONE * 3.0 - 2.0 * f);
	var n = p.x + p.y * 57.0 + 113.0 * p.z;
	
	return lerp(lerp(lerp(_hash(n + 0.0), _hash(n + 1.0), clamp(f.x, 0.0, 1.0)),
			lerp(_hash(n + 57.0), _hash(n + 58.0), clamp(f.x, 0.0, 1.0)), clamp(f.y, 0.0, 1.0)),
			lerp(lerp(_hash(n + 113.0), _hash(n + 114.0), clamp(f.x, 0.0, 1.0)),
			lerp(_hash(n + 170.0), _hash(n + 171.0), clamp(f.x, 0.0, 1.0)), clamp(f.y, 0.0, 1.0)), clamp(f.z, 0.0, 1.0));

func get_value(x, y, z):
	if x < 0 or x >= VALUES or y < 0 or y >= VALUES or z < 0 or z >= VALUES:
		return 0.0
	var temp = value_array[x + y * VALUES + z * VALUES * VALUES];
	return temp

func set_value(x, y, z, value):
	value_array[x  + y  * VALUES + z * VALUES * VALUES] = value;
	needs_update = true;

func get_intersection(p, d):
	var a = get_value(p.x, p.y, p.z)
	var b = get_value(p.x + d.x, p.y + d.y, p.z + d.z)

	if a * b >= 0:
		return null;

	var mid_point = -a / (b - a); # Gets the 0 point
	

	if mid_point < 0 or mid_point > 1:
		return null

	return p * (1.0 - mid_point) + (p + d) * (mid_point)

const directions = [Vector3.RIGHT, Vector3.UP, Vector3.BACK]

func orth_directions(direction):
	match direction:
		Vector3.RIGHT:
			return [Vector3.UP, Vector3.BACK]
		Vector3.UP:
			return [Vector3.BACK, Vector3.RIGHT]
		Vector3.BACK:
			return [Vector3.RIGHT, Vector3.UP]

func convert_pos_to_index(x, y, z):
	return int(x) + int(y) * VALUES + int(z) * VALUES ** 2


func convert_to_index(p):
	return convert_pos_to_index(p.x, p.y, p.z)

func get_normal(p):
	var normal = Vector3.ZERO
	var offsets = [
		Vector3(-1, 0, 0), Vector3(1, 0, 0),
		Vector3(0, -1, 0), Vector3(0, 1, 0),
		Vector3(0, 0, -1), Vector3(0, 0, 1)
	]

	for offset in offsets:
		var neighbor_pos = p + offset
		var diff = get_value(neighbor_pos.x, neighbor_pos.y, neighbor_pos.z) - get_value(p.x, p.y, p.z)
		normal += offset * diff

	return normal.normalized()

func quadize_mesh():
	verts.resize(VALUES ** 3)
	normals.resize(VALUES ** 3)
	var _mutex = Mutex.new();

	var _task = WorkerThreadPool.add_group_task(func temp(_i):
		var x = _i % VALUES
		var y = floor(_i / VALUES) % VALUES ;
		var z = floor(_i / VALUES ** 2);

		normals.set(convert_pos_to_index(x, y, z), get_normal(Vector3(x, y, z)))
		var count = 0
		var average = Vector3.ZERO;
		var flatten = Vector3.ZERO

		for i in range(2):
			for j in range(2):
				for k in range(2):
					var direction = Vector3(i, j, k)
					var _p = Vector3(x, y, z)

					if i < COUNT and j < COUNT and k < COUNT:
						var a = get_value(x, y, z);
						var b = get_value(x + direction.x, y + direction.y, z + direction.z)

						average += get_intersection(_p, direction)
						count += 1

					if i + j + k == 1 and x * y * z > 0 and i < COUNT and j < COUNT and k < COUNT:
						var a = get_value(x, y, z);
						var b = get_value(x + direction.x, y + direction.y, z + direction.z)

						var orth = orth_directions(direction)
						var right = _p - orth[0]
						var up = _p - orth[1]
						var back = _p - orth[0] - orth[1]

						mutex.lock()
						if a > b:
							indicies.append(convert_to_index(_p))
							indicies.append(convert_to_index(right))
							indicies.append(convert_to_index(up))
							indicies.append(convert_to_index(right))
							indicies.append(convert_to_index(back))
							indicies.append(convert_to_index(up))
						else:
							indicies.append(convert_to_index(_p))
							indicies.append(convert_to_index(up))
							indicies.append(convert_to_index(right))
							indicies.append(convert_to_index(up))
							indicies.append(convert_to_index(back))
							indicies.append(convert_to_index(right))
						mutex.unlock();
		if count != 0:
			average /= count

			# If I am at the edge so our position on x  y or z == count 
			# flatten us to that face
			if x == 0:
				average.x = 0
			if y == 0:
				average.y = 0
			if z == 0:
				average.z = 0

			verts.set(convert_pos_to_index(x, y, z), (average) / COUNT - Vector3.ONE * 0.5)
		, VALUES ** 3)

	WorkerThreadPool.wait_for_group_task_completion(_task)

	uvs.resize(verts.size())

	
func gen_mesh():
	var start = Time.get_ticks_usec()
	quadize_mesh()
	print(start - Time.get_ticks_usec())

func regen_mesh():
	verts = PackedVector3Array()
	uvs = PackedVector2Array()
	normals = PackedVector3Array()
	indicies = PackedInt32Array()

	surface_array[Mesh.ARRAY_VERTEX] = verts
	surface_array[Mesh.ARRAY_NORMAL] = normals
	surface_array[Mesh.ARRAY_TEX_UV] = uvs
	surface_array[Mesh.ARRAY_INDEX] = indicies
	
	gen_mesh();

	if verts.size() != 0:
		mesh.clear_surfaces();
		mesh.call_deferred("add_surface_from_arrays", Mesh.PRIMITIVE_TRIANGLES, surface_array)
	

func fill_generation(_transform):
	# Set value to be distance from center 16, 16, 16 / 32
	var group_task = WorkerThreadPool.add_group_task(func temp(index):
		var x = index % VALUES;
		var y = floor(index / VALUES) % VALUES 
		var z = floor(index / (VALUES ** 2) )
		var _position = _transform * (Vector3(x, y, z) / COUNT - 0.5 * Vector3.ONE);
		set_value(x, y, z, generation_function.call(_position))
	, VALUES ** 3, -1, );


	WorkerThreadPool.wait_for_group_task_completion(group_task);


func threaded_fill():
	fill_generation(global_transform);

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	mesh = ArrayMesh.new();

	value_array.resize(VALUES ** 3)
	for i in range(VALUES ** 3):
		value_array[i] = 0.0;

	surface_array.resize(Mesh.ARRAY_MAX)

	is_ready = true;

	if Engine.is_editor_hint():
		generation_function = func temp(x: Vector3): return (x.length()) - 1.00
		fill_generation(global_transform);
		regen_mesh();

func create(gen_fun: Callable):
	generation_function = gen_fun;
	threaded_fill()

func _exit_tree() -> void:
	if fill_task:
		WorkerThreadPool.wait_for_task_completion(fill_task)
	if task:
		WorkerThreadPool.wait_for_task_completion(task)


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	if Engine.is_editor_hint():
		if needs_update:
			fill_generation(global_transform);

		if debug:
			# Draw every point
			for x in range(COUNT):
				for y in range(COUNT):
					for z in range(COUNT):
						var p = Vector3(x, y, z) / COUNT - Vector3.ONE * 0.5
						var value = get_value(x, y, z)
						var color = Color(value, 0.0, 0.0, 1.0)
						DebugDraw3D.draw_sphere(global_transform * p, 0.01, color);

	if needs_update and is_ready:
		needs_update = false;
		# if not task or WorkerThreadPool.is_task_completed(task):
			# task = WorkerThreadPool.add_task(regen_mesh, true,);
		regen_mesh();
