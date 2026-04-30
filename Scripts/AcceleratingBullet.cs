using Godot;

/// <summary>
/// A bullet that accelerates from a start speed up to a max speed.
/// Create separate scenes and configure startSpeed, acceleration, and
/// rotationOffset per scene in the Inspector.
/// Destroyed on player contact (inherited from Bullet).
/// </summary>
public partial class AcceleratingBullet : Bullet
{
	[Export] private float startSpeed = 25f;
	[Export] private float acceleration = 80f;
	// Set this in the Inspector to match your sprite's facing direction.
	// e.g. -90 if your sprite faces upward, 0 if it faces right (Godot default).
	[Export] private float rotationOffsetDegrees = 0f;

	private float currentSpeed = 0f;
	private float maxSpeed = 0f;
	private Vector2 moveDirection;

	/// <summary>
	/// Call this instead of base Initialize().
	/// topSpeed is passed in from StarBullet so all variants share the same ceiling.
	/// </summary>
	public void InitializeAccelerating(Vector2 direction, float topSpeed)
	{
		moveDirection = direction.Normalized();
		currentSpeed  = startSpeed;
		maxSpeed      = topSpeed;

		// Call base to set rotation; speed value doesn't matter since we override movement
		base.Initialize(direction, topSpeed);

		// Apply the per-scene offset to correct for sprite facing direction
		Rotation += Mathf.DegToRad(rotationOffsetDegrees);
	}

	public override void _Process(double delta)
	{
		if (currentSpeed < maxSpeed)
			currentSpeed = Mathf.Min(currentSpeed + acceleration * (float)delta, maxSpeed);

		GlobalPosition += moveDirection * currentSpeed * (float)delta;
	}
}
