using Godot;

/// <summary>
/// A parryable bullet that travels downward while oscillating horizontally
/// in a sine wave, mirroring FlowerBullet's movement.
/// </summary>
public partial class FlowerParryBullet : ParryBullet
{
	public float Amplitude = 50f;
	public float Frequency = 1f;
	public float DownSpeed = 60f;
	public bool IsRightColumn = false;

	private float time = 0f;
	private float centerX;

	public void InitializeFlower(float startCenterX, float initialPhase)
	{
		centerX = startCenterX;
		time = initialPhase;
	}

	public override void _Process(double delta)
	{
		if (!parried)
		{
			// Flower sine-wave movement (skips base entirely)
			time += (float)delta * Frequency;
float phase = time + (IsRightColumn ? Mathf.Pi : 0f);
float x = centerX + Amplitude * Mathf.Sin(phase);
float y = GlobalPosition.Y + DownSpeed * (float)delta;

GlobalPosition = new Vector2(x, y);
Rotation = Amplitude * Frequency * Mathf.Cos(phase) * 0.01f;

			// Still need to check for parry input
			CheckParryInput();
		}
		else
		{
			// Post-parry: let Bullet handle velocity-based movement
			base._Process(delta);
		}
	}
}
