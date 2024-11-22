extends Button

func _ready():
	print("READYYYYYYYY")

func _input(event):
	if event is InputEventMouseButton:
		if event.pressed && get_rect().has_point(event.position):
			print("Custom Button logic: Button clicked!")
