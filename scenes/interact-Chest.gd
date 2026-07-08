extends Area2D

var player_in_range = false
var opened = false

func _on_body_entered(body):
	if body.name == "Player":
		player_in_range = true


func _on_body_exited(body):
	if body.name == "Player":
		player_in_range = false
		
func _process(delta):
	if player_in_range and !opened:
		if Input.is_action_just_pressed("interact"):
			opened = true
			print("Chest opened!")