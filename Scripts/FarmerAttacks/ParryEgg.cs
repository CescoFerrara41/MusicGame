using Godot;

public partial class ParryEgg : Bullet
{
	[Export] private float spinSpeed = 360f;
	[Export] private PackedScene birdProjectilePrefab;

	private int battleBoxHitCount = 0;
	private bool isStopped = false;
	private bool wallHitCooldown = false; // prevents counting the same wall crossing twice in one pass
	private PlayerSoul trackedPlayer;
	private BattleBox trackedBox;

	// Called by EggAttack after instantiation
	public void SetPlayerAndBox(PlayerSoul player, BattleBox box)
	{
		trackedPlayer = player;
		trackedBox = box;
	}

	public override void _Process(double delta)
	{
		if (!isStopped)
		{
			base._Process(delta);
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

			if (battleBoxHitCount >= 2)
			{
				isStopped = true;
				Pause();
				SpawnBirdProjectiles();
			}
		}
		else if (!outsideBounds)
		{
			// Reset cooldown once the egg is back inside, ready to detect the next wall
			wallHitCooldown = false;
		}
	}

	private async void SpawnBirdProjectiles()
	{
		if (trackedPlayer == null)
		{
			GD.PrintErr("[EggProjectile] No tracked player — cannot aim bird projectiles.");
			QueueFree();
			return;
		}

		Vector2 playerPos = trackedPlayer.GlobalPosition;
		float staggerDelay = 0.15f;
		float aimErrorRange = 15f; // degrees of random spread

		for (int i = 0; i < 6; i++)
		{
			await ToSignal(GetTree().CreateTimer(staggerDelay), "timeout");

			// Guard: egg may have been freed externally
			if (!IsInsideTree()) return;

			

			if (birdProjectilePrefab != null)
			{
				GD.Print($"[EggProjectile] Spawning bird projectile {i + 1}/6");
				var bird = birdProjectilePrefab.Instantiate<ParryBird>();
				GetTree().CurrentScene.AddChild(bird);
				bird.ZIndex = 1;
				bird.GlobalPosition = GlobalPosition;

				// Direction toward player with random angular error
				Vector2 directionToPlayer = (playerPos - GlobalPosition).Normalized();
				float randomAngle = Mathf.DegToRad((float)GD.RandRange(-aimErrorRange, aimErrorRange));
				float cos = Mathf.Cos(randomAngle);
				float sin = Mathf.Sin(randomAngle);
				Vector2 erroredDirection = new Vector2(
					directionToPlayer.X * cos - directionToPlayer.Y * sin,
					directionToPlayer.X * sin + directionToPlayer.Y * cos
				);

				bird.Initialize(erroredDirection, 40f);
			}
		}

		QueueFree();
	}
}
