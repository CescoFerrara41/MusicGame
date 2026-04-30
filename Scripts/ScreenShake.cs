using Godot;

public partial class ScreenShake : Camera2D
{
	private float shakeStrength = 0;
	private float decay = 10f;

	public override void _Process(double delta)
	{
		if (shakeStrength > 0)
		{
			Offset = new Vector2(
				(float)GD.RandRange(-shakeStrength, shakeStrength),
				(float)GD.RandRange(-shakeStrength, shakeStrength)
			);

			shakeStrength -= decay * (float)delta;
		}
		else
		{
			shakeStrength = 0;
			Offset = Vector2.Zero;
		}
	}

	public void Shake(float strength)
	{
		shakeStrength = strength;
	}
}
