using Godot;
using System;

public partial class HitBottom : AnimatedSprite2D
{
	[Export] private int lane;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("lane_" + lane)) {
			if (this.Animation != "Hit") {
				Play("Hit");
			}
		}
		else {
			if (Animation != "default") {
				Play("default");
			}
		}
	}
}
