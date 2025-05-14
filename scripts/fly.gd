extends Camera3D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:

	var move_direction = Input.get_vector("left", "right", "back", "forward");
	global_position += Vector3(move_direction.x, 0.0, -move_direction.y) * global_basis;

	
