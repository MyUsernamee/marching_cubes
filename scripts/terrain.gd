@tool
extends MeshInstance3D
class_name MarchingCubes

const VALUES = 8;

var value_array : Array[float] = [];

var surface_array = []
var verts: PackedVector3Array;
var indicies: PackedInt32Array;
var normals: PackedVector3Array;
var uvs: PackedVector2Array;

class MeshChunk:
	var verts: Array[Vector3];
	var uvs: Array[Vector2];
	var normals: Array[Vector3];
	var indicies: Array[int];

func add_mesh_chunk(chunk, position):
	var start_index = indicies.size()
	for vert in range(chunk.verts.size()):
		verts.push_back(chunk.verts[vert] + position);
		uvs.push_back(chunk.uvs[vert]);
		normals.push_back(chunk.normals[vert]);
		indicies.push_back(chunk.indicies[vert] + start_index);


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
	return value_array[x + y * VALUES + z * VALUES * VALUES];

func set_value(x, y, z, value):
	value_array[x + y * VALUES + z * VALUES * VALUES] = value;

func gen_mesh():

	for i in range(VALUES - 1):
		for j in range(VALUES - 1):
			for k in range(1, VALUES):
				# Get data
				
				var values = [
					get_value(i, j, k),
					get_value(i + 1, j, k),
					get_value(i, j + 1, k),
					get_value(i + 1, j + 1, k),
					get_value(i, j, k - 1),
					get_value(i + 1, j, k - 1),
					get_value(i, j + 1, k - 1),
					get_value(i + 1, j + 1, k - 1),
				]

				var mesh_chunk = generate_cube(values, .5)
				add_mesh_chunk(mesh_chunk, Vector3(i, j, k))

# Called when the node enters the scene tree for the first time.
func _ready() -> void:

	value_array.resize(VALUES ** 3)

	for i in range(VALUES ** 3):
		value_array[i] = 0.0;

	# Set value to be distance from center 16, 16, 16 / 32
	for x in range(VALUES):
		for y in range(VALUES):
			for z in range(VALUES):
				var center = Vector3(VALUES / 2, VALUES / 2, VALUES / 2)
				var position = Vector3(x, y, z)
				var distance = center.distance_to(position)
				set_value(x, y, z, 1.0 - (distance / (VALUES / 2)))
	
	mesh.clear_surfaces();

	if Engine.is_editor_hint():
		print("HELLO!");


	surface_array.resize(Mesh.ARRAY_MAX)

	verts = PackedVector3Array()
	uvs = PackedVector2Array()
	normals = PackedVector3Array()
	indicies = PackedInt32Array()

	surface_array[Mesh.ARRAY_VERTEX] = verts
	surface_array[Mesh.ARRAY_NORMAL] = normals
	surface_array[Mesh.ARRAY_TEX_UV] = uvs
	surface_array[Mesh.ARRAY_INDEX] = indicies


	print(verts);
	print(normals);
	print(uvs);
	print(indicies);
	print(verts.size());

	gen_mesh();
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, surface_array)
	mesh.regen_normal_maps();


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:

	if Engine.is_editor_hint() and false:

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
