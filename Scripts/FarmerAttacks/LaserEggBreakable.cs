using Godot;
using System.Threading.Tasks;

public partial class LaserEggBreakable : Area2D
{
	[Export] private float spinSpeed = 360f;
	[Export] private PackedScene eggLaserScene;

	private int battleBoxHitCount = 0;
	private bool isStopped = false;
	private bool wallHitCooldown = false; // prevents counting the same wall crossing twice in one pass
	private PlayerSoul trackedPlayer;
	private BattleBox trackedBox;
	private bool isDying = false;
	private AnimatedSprite2D sprite;
	
	private Vector2 direction;
	private Vector2 startPosition;
	private float speed;
	private float savedSpeed;
	private bool paused = false;
	
	public override void _Ready()
	{
		AddToGroup("EnemyBullets");
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.Play("default");

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

	// Called by EggAttack after instantiation
	public void SetPlayerAndBox(PlayerSoul player, BattleBox box)
	{
		trackedPlayer = player;
		trackedBox = box;
	}

	public override void _Process(double delta)
	{
		if (isDying) return;
		if (!isStopped)
		{
			GlobalPosition += direction * speed * (float)delta;
			CheckWallHit();
		}

		// Spin the egg regardless of movement state
		RotationDegrees += spinSpeed * (float)delta;
	}

	private void CheckWallHit()
	{
		if (trackedBox == null) return;

		Vector2 min = trackedBox.GetInnerMinBounds();
		Vector2 max = trackedBox.GetInnerMaxBounds();
		Vector2 pos = GlobalPosition;

		bool outsideBounds = pos.X <= min.X || pos.X >= max.X || pos.Y <= min.Y || pos.Y >= max.Y;

		if (outsideBounds && !wallHitCooldown)
		{
			wallHitCooldown = true;
			battleBoxHitCount++;
			GD.Print($"[EggProjectile] Hit BattleBox wall #{battleBoxHitCount}");

			if (battleBoxHitCount >= 1)
			{
				isStopped = true;
				Pause();
				_ = FlashAndBreak();
			}
		}
		else if (!outsideBounds)
		{
			// Reset cooldown once the egg is back inside, ready to detect the next wall
			wallHitCooldown = false;
		}
	}

	
	private void OnAreaEntered(Area2D area)
	{
		if (isDying) return;

		if (area.IsInGroup("PlayerBullets"))
		{
			area.QueueFree();
			_ = FlashAndBreak();
		}
		else if (area.IsInGroup("PlayerSoul"))
		{
			// Handled by the PlayerSoul's own damage detection
		}
	}
	
	private async Task FlashAndBreak()
	{
		isDying = true;

		Color original = sprite.Modulate;
		Color red = new Color(1f, 0.2f, 0.2f, 1f);

		for (int i = 0; i < 2; i++)
		{
			sprite.Modulate = red;
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
			sprite.Modulate = original;
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		}

		SpawnLaser();
		QueueFree();
	}
	
	private void SpawnLaser()
	{
		if (eggLaserScene == null) return;
		var laser = eggLaserScene.Instantiate<Node2D>();
		laser.GlobalPosition = GlobalPosition;
		GetTree().Root.AddChild(laser);
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
}
