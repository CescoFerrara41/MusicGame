using Godot;

public partial class Bullet : Area2D
{
	[Export] protected int damage = 1;

	private Vector2 direction;
	private float speed;
	private bool paused = false;
	private float savedSpeed = 0f;
	private Vector2 startPosition;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
	}

	public void Initialize(Vector2 shootDirection, float shootSpeed)
	{
		direction = shootDirection.Normalized();
		speed = shootSpeed;
		startPosition = GlobalPosition;

		float angle = Mathf.Atan2(direction.Y, direction.X);
		Rotation = angle;
	}

	public override void _Process(double delta)
	{
		if (!paused)
		{
			GlobalPosition += direction * speed * (float)delta;
		}
	}

	public void Pause()
	{
		if (!paused)
		{
			savedSpeed = speed;
			speed = 0f;
			paused = true;
		}
	}
	
	public float GetSpeed() {
		return speed;
	}

	public void Resume()
	{
		if (paused)
		{
			speed = savedSpeed;
			paused = false;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		// Player hit (only if NOT parried)
		if (area.IsInGroup("Player") && !(this is ParryBullet pb && pb.IsParried()))
		{
			if (area is PlayerSoul player)
			{
				if (player.TakeDamage(damage))
					QueueFree();
			}
		}

		// Enemy hit (ONLY if parried)
		if (area.IsInGroup("Enemy") && (this is ParryBullet pb2 && pb2.IsParried()))
		{
			if (area is EnemyDisplay enemy)
			{
				_ = enemy.TakeDamage(damage);
				QueueFree();
			}
		}

		if (area.IsInGroup("BulletBreaker") && !(this is ParryBullet pb3 && pb3.IsParried()))
		{
			QueueFree();
		}
	}

	public void SetDamage(int dmg)
	{
		damage = dmg;
	}

	public int GetDamage()
	{
		return damage;
	}

	public Vector2 GetStartPosition()
	{
		return startPosition;
	}

	public Vector2 GetDirection()
	{
		return direction;
	}
}
