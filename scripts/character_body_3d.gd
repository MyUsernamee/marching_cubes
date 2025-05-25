extends CharacterBody3D


const SPEED = 5.0
const JUMP_VELOCITY = 10.0

@onready var camera = get_node("./Camera3D");
@onready var main_leaf = $/root/Game/Leaf;

var mouse_delta = Vector2.ZERO;
@export var sensitivity = 0.01;

func rotate_upwards():

	var wish_up = global_position.normalized();
	var current_up = global_basis.y

	up_direction = global_basis.y;

	# Get the angle between up and  our current up
	var angle = acos((current_up).dot(wish_up))

	# Get the rotation axis
	var axis = current_up.cross(wish_up);

	if axis == Vector3.ZERO or angle < 0.01:
		return;

	rotate(axis.normalized(), angle);

func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		mouse_delta = event.relative;


func _physics_process(delta: float) -> void:
	rotate_upwards();
	# Add the gravity.
	if not is_on_floor():
		velocity += get_gravity().y * position.normalized() * delta


	rotate_object_local(Vector3.UP, -mouse_delta.x * sensitivity);
	camera.rotate_object_local(Vector3.RIGHT, -mouse_delta.y * sensitivity);
	velocity = global_basis.inverse() * velocity;

	# Handle jump.
	if Input.is_action_just_pressed("jump"):
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "up", "down")
	var direction := Vector3(input_dir.x, 0, input_dir.y).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	velocity = global_basis * velocity;

	if Input.is_action_pressed("fire"):
		# Cast a ray and get which chunk we are looking at
		var state = get_world_3d().direct_space_state;

		var from = camera.global_position
		var to = camera.global_position + camera.global_transform.basis.z * -1000.0
		var query = PhysicsRayQueryParameters3D.create(from, to, 0b01)
		var result = state.intersect_ray(query);

		if not result.is_empty():
			var collider = result["collider"];
			var terrain : TerrainChunk = collider.get_parent();

			var _temp: Leaf = terrain.get_parent().get_node_containing_point(result['position'] + Vector3.BACK * 4.0, null);
			DebugDraw3D.draw_box_ab(_temp.global_transform * (Vector3.ONE * -0.5), _temp.global_transform * (Vector3.ONE * 0.5));

			main_leaf.set_value(result['position'], main_leaf.get_value(result['position']) + delta);

	move_and_slide()
	mouse_delta = Vector2.ZERO
