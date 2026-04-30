using Godot;

public partial class ScopeMarker : Node2D
{
	// How fast the scope lerps toward the player
	[Export] private float trackingSpeed = 4f;

	private PlayerSoul target;
	private bool tracking = false;

	public override void _Process(double delta)
	{
		if (!tracking || target == null) return;

		GlobalPosition = GlobalPosition.Lerp(
			target.GlobalPosition,
			trackingSpeed * (float)delta
		);
	}

	public void StartTracking(PlayerSoul player)
	{
		target = player;
		tracking = true;

		// Return to idle animation if it was flashing
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null)
			sprite.Play("default");
	}

	public void StopTracking()
	{
		tracking = false;
	}

	public void PlayFlash()
	{
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (sprite != null)
			sprite.Play("flash");
	}
}
