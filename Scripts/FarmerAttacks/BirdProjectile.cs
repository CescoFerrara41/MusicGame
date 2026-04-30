using Godot;

public partial class BirdProjectile : Bullet
{
	[Export] private float waveAmplitude = 1f;
	[Export] private float waveFrequency = 2f;
	[Export] private AnimatedSprite2D sprite;

	private float distanceTraveled = 0f;

	// Initialize is not virtual on Bullet, so we call base to set up
	// direction/speed normally, then read them back via the public getters.
	// No reflection needed — GetDirection() and GetSpeed() give us what we need.
	
	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		base._Ready();
	}

	public override void _Process(double delta)
	{
		sprite.Play("Fly");
		// Do NOT call base._Process() — we handle all movement here so we
		// can apply the wave offset on top of forward movement.
		if (GetSpeed() == 0f) return;

		Vector2 dir = GetDirection();
		float spd = GetSpeed();

		// Forward movement
		Vector2 forwardMovement = dir * spd * (float)delta;
		distanceTraveled += forwardMovement.Length();

		// Perpendicular wave offset
		Vector2 perpendicular = new Vector2(-dir.Y, dir.X);
		float waveOffset = Mathf.Sin(distanceTraveled * waveFrequency) * waveAmplitude;
		Vector2 totalMovement = forwardMovement + perpendicular * waveOffset * (float)delta;

		GlobalPosition += totalMovement;
	}
}
