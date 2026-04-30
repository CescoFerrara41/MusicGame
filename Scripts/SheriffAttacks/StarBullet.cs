using Godot;

/// <summary>
/// The main star body. Attach to an Area2D scene.
/// - Enters from above the battle box at an angle
/// - Bounces off all 4 inner walls, firing projectiles on each bounce
/// - Rotates continuously
/// - Does NOT get destroyed on player contact, but deals damage
/// </summary>
public partial class StarBullet : Area2D
{
	[Export] private float moveSpeed = 80f;
	[Export] private float rotateSpeed = 1.2f;
	[Export] private int damage = 1;
	[Export] private float projectileTopSpeed = 120f;

	[Export] private PackedScene bigBulletScene;
	[Export] private PackedScene mediumBulletScene;
	[Export] private PackedScene smallBulletScene;
	
	[Export] private PackedScene bigParryBulletScene;
	[Export] private PackedScene mediumParryBulletScene;
	[Export] private PackedScene smallParryBulletScene;

	private BattleBox box;
	private Vector2 velocity;
	private bool insideBox = false;

	private const int PointCount = 5;
	private const float BaseAngleOffset = -Mathf.Pi / 2f;

	public void Initialize(BattleBox battleBox)
	{
		box = battleBox;

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float spawnX = Mathf.Lerp(min.X, max.X, (float)GD.RandRange(0.2, 0.8));
		float spawnY = min.Y - 40f;
		GlobalPosition = new Vector2(spawnX, spawnY);

		float horizontalLean = (float)GD.RandRange(-0.5, 0.5);
		velocity = new Vector2(horizontalLean, 1f).Normalized() * moveSpeed;

		AreaEntered += OnAreaEntered;
	}

	public override void _Process(double delta)
	{
		if (box == null) return;

		Rotate(rotateSpeed * (float)delta);
		Move(delta);
	}

	private void Move(double delta)
	{
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		GlobalPosition += velocity * (float)delta;

		if (!insideBox)
		{
			if (GlobalPosition.Y >= min.Y)
				insideBox = true;
			return;
		}

		bool bounced = false;

		if (GlobalPosition.X <= min.X)
		{
			GlobalPosition = new Vector2(min.X, GlobalPosition.Y);
			velocity.X = Mathf.Abs(velocity.X);
			bounced = true;
		}
		else if (GlobalPosition.X >= max.X)
		{
			GlobalPosition = new Vector2(max.X, GlobalPosition.Y);
			velocity.X = -Mathf.Abs(velocity.X);
			bounced = true;
		}

		if (GlobalPosition.Y <= min.Y)
		{
			GlobalPosition = new Vector2(GlobalPosition.X, min.Y);
			velocity.Y = Mathf.Abs(velocity.Y);
			bounced = true;
		}
		else if (GlobalPosition.Y >= max.Y)
		{
			GlobalPosition = new Vector2(GlobalPosition.X, max.Y);
			velocity.Y = -Mathf.Abs(velocity.Y);
			bounced = true;
		}

		if (bounced)
			FireFromAllPoints();
	}

	private void FireFromAllPoints()
	{
		for (int i = 0; i < PointCount; i++)
		{
			float pointAngle = BaseAngleOffset + (i * Mathf.Tau / PointCount) + Rotation;
			Vector2 pointDir = new Vector2(Mathf.Cos(pointAngle), Mathf.Sin(pointAngle));
			Vector2 spawnPos = GlobalPosition + pointDir * 12f;
			
			if (i == 1) {
				SpawnParryProjectile(bigParryBulletScene, spawnPos, pointDir);
				SpawnParryProjectile(mediumParryBulletScene, spawnPos, pointDir);
				SpawnParryProjectile(smallParryBulletScene, spawnPos, pointDir);
			}
			else {
				SpawnProjectile(bigBulletScene,    spawnPos, pointDir);
				SpawnProjectile(mediumBulletScene, spawnPos, pointDir);
				SpawnProjectile(smallBulletScene,  spawnPos, pointDir);
			}
		}
	}
	
	private void SpawnParryProjectile(PackedScene scene, Vector2 position, Vector2 direction) {
		AcceleratingParryBullet bullet = scene.Instantiate<AcceleratingParryBullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;
		bullet.InitializeAccelerating(direction, projectileTopSpeed);
	}

	private void SpawnProjectile(PackedScene scene, Vector2 position, Vector2 direction)
	{
		AcceleratingBullet bullet = scene.Instantiate<AcceleratingBullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;
		bullet.InitializeAccelerating(direction, projectileTopSpeed);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player") && area is PlayerSoul soul)
		{
			soul.TakeDamage(damage);
		}
	}
}
