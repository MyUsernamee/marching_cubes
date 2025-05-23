extends CharacterBody3D


const SPEED = 5.0
const JUMP_VELOCITY = 4.5

@onready var camera = get_node("./Camera3D");
@onready var main_leaf = $/root/Game/Leaf;

var mouse_delta = Vector2.ZERO;
@export var sensitivity = 0.01;

func rotate_upwards():

	var wish_up = global_position.normalized();
	var current_up = global_position * Vector3.UP

	# Get the angle between up and  our current up
	var angle = acos((current_up).dot(wish_up))

	# Get the rotation axis
	var axis = current_up.cross(wish_up);

	if axis == Vector3.ZERO:
		return;


	rotate(axis.normalized(), angle);

func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		mouse_delta = event.relative;


func _physics_process(delta: float) -> void:
	# Add the gravity.
	if not is_on_floor():
		velocity += get_gravity().y * position.normalized() * delta

	rotate_upwards();

	rotate_object_local(Vector3.UP, -mouse_delta.x * sensitivity);
	camera.rotate_object_local(Vector3.RIGHT, -mouse_delta.y * sensitivity);

	# Handle jump.
	if Input.is_action_just_pressed("jump") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "up", "down")
	var direction := (transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

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

			main_leaf.set_value(result['position'], main_leaf.get_value(result['position']) + delta);

	move_and_slide()
	mouse_delta = Vector2.ZERO
