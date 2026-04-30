using Godot;

public partial class RadialBullet : Area2D
{
	[Export] private int damage = 1;

	// Base travel speed (units/sec)
	private float baseSpeed;
	// Sine wave modulation: speed oscillates between baseSpeed * (1 - amplitude) and baseSpeed * (1 + amplitude)
	// Keep amplitude < 1 so speed never reaches zero
	[Export] private float sineAmplitude = 0.6f;
	// How many full speed cycles per second
	[Export] private float sineFrequency = 1.5f;

	private Vector2 direction;
	private float timeAlive = 0f;

	public void Initialize(Vector2 dir, float speed)
	{
		direction = dir.Normalized();
		baseSpeed = speed;

		// Rotate sprite to face direction (sprite is default facing right = 0 rad)
		Rotation = Mathf.Atan2(direction.Y, direction.X);
	}

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
	}

	public override void _Process(double delta)
	{
		timeAlive += (float)delta;

		// Sine wave oscillates between (1 - amplitude) and (1 + amplitude) of base speed
		float sineValue = Mathf.Sin(timeAlive * sineFrequency * Mathf.Tau);
		float currentSpeed = baseSpeed * (1f + sineAmplitude * sineValue);

		GlobalPosition += direction * currentSpeed * (float)delta;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			if (area is PlayerSoul player)
			{
				if (player.TakeDamage(damage))
					QueueFree();
			}
		}

		if (area.IsInGroup("BulletBreaker"))
		{
			QueueFree();
		}
	}

	public void SetDamage(int dmg)
	{
		damage = dmg;
	}
}
