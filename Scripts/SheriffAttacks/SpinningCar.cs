using Godot;

/// <summary>
/// A spinning car that travels diagonally, bounces off one wall, and arrives
/// at a target lane at the player's Y position.
///
/// Initialize via InitializeSpinning() after adding to the scene.
/// Destroyed on player contact (via Bullet base class).
/// </summary>
public partial class SpinningCar : Bullet
{
	[Export] private float rotateSpeed = 5f; // radians/sec

	private Vector2 moveDirection;
	private float moveSpeed;
	private bool hasBounced = false;

	private float wallX;       // the wall to bounce off
	private float postBounceDirectionX; // X direction after bounce (flipped)

	// We override _Process entirely so disable base movement by not calling base
	public new void Initialize(Vector2 direction, float speed)
	{
		// Intentionally left empty — use InitializeSpinning instead
	}

	/// <summary>
	/// Called by DrivingAttack after AddChild.
	/// </summary>
	/// <param name="startPos">Spawn position at top of box</param>
	/// <param name="bounceWallX">X position of the wall to bounce off</param>
	/// <param name="targetX">Target lane X to arrive at (at player Y)</param>
	/// <param name="targetY">Player's locked Y position</param>
	/// <param name="speed">Movement speed</param>
	public void InitializeSpinning(Vector2 startPos, float bounceWallX, float targetX, float targetY, float speed)
	{
		GlobalPosition = startPos;
		moveSpeed      = speed;
		wallX          = bounceWallX;

		// --- Calculate bounce point via reflection ---
		// Reflect targetX across the wall to find where a straight line from
		// startPos would need to go to naturally "pass through" the wall and
		// arrive at target after bouncing.
		float reflectedTargetX = 2f * bounceWallX - targetX;
		Vector2 reflectedTarget = new Vector2(reflectedTargetX, targetY);

		// Direction from start to reflected target gives us the pre-bounce angle
		moveDirection = (reflectedTarget - startPos).Normalized();

		// After bouncing, X direction flips
		postBounceDirectionX = -moveDirection.X;

		// Set rotation to face movement direction (sprite faces up so offset -90°)
		Rotation = Mathf.Atan2(moveDirection.Y, moveDirection.X) - Mathf.Pi / 2f;
	}

	public override void _Process(double delta)
	{
		if (!hasBounced)
		{
			// Check if we've reached or passed the wall
			bool hitWall = moveDirection.X < 0
				? GlobalPosition.X <= wallX
				: GlobalPosition.X >= wallX;

			if (hitWall)
			{
				// Snap to wall and flip X direction
				GlobalPosition  = new Vector2(wallX, GlobalPosition.Y);
				moveDirection.X = postBounceDirectionX;
				hasBounced      = true;

				// Update rotation to face new direction
				Rotation = Mathf.Atan2(moveDirection.Y, moveDirection.X) - Mathf.Pi / 2f;
			}
		}

		GlobalPosition += moveDirection * moveSpeed * (float)delta;

		// Spin continuously
		Rotation += rotateSpeed * (float)delta;
	}
}
