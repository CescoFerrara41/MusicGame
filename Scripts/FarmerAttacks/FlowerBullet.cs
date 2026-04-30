using Godot;

/// <summary>
/// A bullet that travels downward while oscillating horizontally
/// in a sine wave, creating a drifting/helix effect.
/// </summary>
public partial class FlowerBullet : Bullet
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
		time += (float)delta * Frequency;

	float phase = time + (IsRightColumn ? Mathf.Pi : 0f);
	float x = centerX + Amplitude * Mathf.Sin(phase);
	float y = GlobalPosition.Y + DownSpeed * (float)delta;

	GlobalPosition = new Vector2(x, y);
	Rotation = Amplitude * Frequency * Mathf.Cos(phase) * 0.01f; // tune 0.05f to taste
	}
}
