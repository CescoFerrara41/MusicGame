using Godot;
using System.Threading.Tasks;

public partial class ParryBird : ParryBullet
{
	[Export] private float waveAmplitude = 1f;
	[Export] private float waveFrequency = 2f;
	[Export] private AnimatedSprite2D sprite;

	private float distanceTraveled = 0f;
	private bool wasParried = false;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		base._Ready();
		Parried += OnParried;
	}

	private void OnParried()
	{
		wasParried = true;
	}

	public override void _Process(double delta)
	{
		sprite.Play("Fly");

		Vector2 dir = GetDirection();
		float spd = GetSpeed();

		if (spd != 0f)
		{
			if (wasParried)
			{
				// Move straight at full parry speed, no wave
				GlobalPosition += dir * 400f * (float)delta;
			}
			else
			{
				// Wave movement
				Vector2 forwardMovement = dir * spd * (float)delta;
				distanceTraveled += forwardMovement.Length();

				Vector2 perpendicular = new Vector2(-dir.Y, dir.X);
				float waveOffset = Mathf.Sin(distanceTraveled * waveFrequency) * waveAmplitude;
				GlobalPosition += forwardMovement + perpendicular * waveOffset * (float)delta;
			}

			Pause();
			base._Process(delta);
			Resume();
		}
		else
		{
			base._Process(delta);
		}
	}
}
