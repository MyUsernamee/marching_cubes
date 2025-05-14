@tool
extends MeshInstance3D
class_name MarchingCubes

const VALUES = 8;

var mutex = Mutex.new();
var task;

var is_ready = false;

var value_array : Array[float] = [];
var needs_update = false;

var surface_array = []
var verts: PackedVector3Array;
var indicies: PackedInt32Array;
var normals: PackedVector3Array;
var uvs: PackedVector2Array;

var generation_function: Callable;

class MeshChunk:
	var verts: Array[Vector3];
	var uvs: Array[Vector2];
	var normals: Array[Vector3];
	var indicies: Array[int];

static func frac(x):
	return x - floor(x);

static func _hash(n): 
	return frac(sin(n)*43758.5453);
	
static func perlin(x): 
	var p: Vector3 = Vector3(floor(x.x), floor(x.y), floor(x.z));
	var f: Vector3 = Vector3(frac(x.x), frac(x.y), frac(x.z));
	
	f = f*f*(Vector3.ONE * 3.0-2.0*f);
	var n = p.x + p.y*57.0 + 113.0*p.z;
	
	return lerp(lerp(lerp( _hash(n+0.0), _hash(n+1.0), clamp(f.x, 0.0, 1.0)),
			lerp( _hash(n+57.0), _hash(n+58.0),clamp(f.x, 0.0, 1.0)),clamp(f.y, 0.0, 1.0)),
			lerp(lerp( _hash(n+113.0), _hash(n+114.0),clamp(f.x, 0.0, 1.0)),
			lerp( _hash(n+170.0), _hash(n+171.0),clamp(f.x, 0.0, 1.0)),clamp(f.y, 0.0, 1.0)),clamp(f.z, 0.0, 1.0));


func add_mesh_chunk(chunk, _position):
	var start_index = indicies.size()
	mutex.lock()
	for vert in range(chunk.verts.size()):
		verts.push_back(chunk.verts[vert] + _position);
		uvs.push_back(chunk.uvs[vert]);
		normals.push_back(chunk.normals[vert]);
		indicies.push_back(chunk.indicies[vert] + start_index);
	mutex.unlock();

func convert_values_to_lookup_index(values: Array, surface_level: float) -> int:
	var lookup_index = 0;

	for i in range(values.size()):
		var value = values[i];
		if value > surface_level:
			lookup_index += 1 << i;

	return lookup_index;

func get_edge_indicies(index) -> Array:
	return Tables.EdgeVertexIndices[index];

# Returns two arrays, one with the verticies  
func generate_cube(values: Array, surface_level: float) -> MeshChunk:
	var mesh_chunk = MeshChunk.new();

	# First we convert the values to a lookup index
	var lookup_index = convert_values_to_lookup_index(values, surface_level);
	var lut_data = Tables.TriangleTable[lookup_index];

	for i in range(0, lut_data.size(), 3):
		if lut_data[i] == -1:
			break ; # Generated all triangles

		var verticies = []
		for j in range(3):
			var edge_verticies_index = get_edge_indicies(lut_data[i + j]);
			var a = values[edge_verticies_index[0]];
			var b = values[edge_verticies_index[1]];
			
			var distance = (surface_level - a) / (b - a);

			var weighted_position = Tables.VertexPositions[edge_verticies_index[0]] * (1.0 - distance); 
			weighted_position += Tables.VertexPositions[edge_verticies_index[1]] * (distance);

			verticies.append(weighted_position);
			mesh_chunk.verts.append(weighted_position);
			mesh_chunk.indicies.append(mesh_chunk.indicies.size());
			mesh_chunk.uvs.append(Vector2.ZERO);

		var normal = (verticies[0] - verticies[1]).cross(verticies[2] - verticies[1]).normalized();
		# Calculate normals
		for j in range(3):
			mesh_chunk.normals.append(normal);


	# Now we are going to generate some triangles
	return mesh_chunk;

func get_value(x, y, z):
	mutex.lock()
	var temp = value_array[x + y * VALUES + z * VALUES * VALUES];
	mutex.unlock()
	return temp

func set_value(x, y, z, value):
	mutex.lock()
	value_array[x + y * VALUES + z * VALUES * VALUES] = value;
	needs_update = true;
	mutex.unlock()

func gen_mesh():

	for i in range(VALUES - 1):
		for j in range(VALUES - 1):
			for k in range(1, VALUES):
				# Get data

				var values = []

				for n_k in range(0, -2, -1):
					for n_j in range(0, 2):
						for n_i in range(0, 2):
							if i + n_i < 0 or i + n_i >= VALUES or j + n_j < 0 or j + n_j >= VALUES or k + n_k < 0 or k + n_k >= VALUES:
								values.append(0.0)
								continue;
							values.append(get_value(i + n_i, j + n_j, k + n_k)) # Get values needed for generating a mesh for a single cell.

				var mesh_chunk = generate_cube(values, .5)
				
				add_mesh_chunk(mesh_chunk, Vector3(i, j, k))
				

func regen_mesh():
	
	mesh.clear_surfaces();

	verts = PackedVector3Array()
	uvs = PackedVector2Array()
	normals = PackedVector3Array()
	indicies = PackedInt32Array()

	surface_array[Mesh.ARRAY_VERTEX] = verts
	surface_array[Mesh.ARRAY_NORMAL] = normals
	surface_array[Mesh.ARRAY_TEX_UV] = uvs
	surface_array[Mesh.ARRAY_INDEX] = indicies
	
	gen_mesh();
	
	mesh.call_deferred("add_surface_from_arrays", Mesh.PRIMITIVE_TRIANGLES, surface_array)
	

func fill_generation():
	needs_update = true;
	# Set value to be distance from center 16, 16, 16 / 32
	
	for x in range(VALUES):
		for y in range(VALUES):
			for z in range(VALUES):
				var _position = Vector3(x, y, z) * scale + global_position;
				set_value(x, y, z, generation_function.call(_position))


# Called when the node enters the scene tree for the first time.
func _ready() -> void:

	mesh = ArrayMesh.new();

	value_array.resize(VALUES ** 3)
	for i in range(VALUES ** 3):
		value_array[i] = 0.0;

	surface_array.resize(Mesh.ARRAY_MAX)
	is_ready = true;

func create(_position, gen_fun: Callable, _scale = Vector3.ONE):
	global_position = _position;
	generation_function =  gen_fun;
	scale = _scale;

	fill_generation();

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:

	DebugDraw3D.draw_sphere(global_position, 0.1, Color(1.0, 0.0, 0.0, 1.0));

	if needs_update and is_ready:
		needs_update = false;
		if task and not WorkerThreadPool.is_task_completed(task):
			WorkerThreadPool.wait_for_task_completion(task);
		task = WorkerThreadPool.add_task(regen_mesh);

	return;

	# Draw every point
	for x in range(VALUES):
		for y in range(VALUES):
			for z in range(VALUES):
				var position = Vector3(x, y, z)
				var value = get_value(x, y, z)
				DebugDraw3D.draw_box_ab(position, position + Vector3.ONE, Vector3.UP);
				if value > 0.5:
					var color = Color(value, 0.0, 0.0, 0.0)
					DebugDraw3D.draw_sphere(position, 0.1, color);
