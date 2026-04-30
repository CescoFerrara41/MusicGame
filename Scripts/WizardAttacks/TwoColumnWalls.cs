using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class TwoColumnWalls : AttackPattern
{
	[Export] private PackedScene bulletScene;
	[Export] private PackedScene bulletSceneLeft;
	[Export] private PackedScene trafficLightScene;

	private List<Bullet> activeBullets = new();

	private bool isPaused = false; // 🔥 controls BOTH bullets + spawning

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float left = min.X;
		float right = max.X;
		float top = min.Y;
		float bottom = max.Y;

		int columns = 5;
		int rows = 8;

		float columnTravelDistance = 1.0f;
		float bulletSpeed = 40f;

		float delayBetweenColumns = 0.6f + (columnTravelDistance / bulletSpeed);
		var trafficLight = trafficLightScene.Instantiate<Node2D>();
		Vector2 lightPosition = new Vector2(((right-left)/2f) + left, min.Y+6f);
		GetTree().CurrentScene.AddChild(trafficLight);
		var lightSprite = trafficLight.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		lightSprite.Frame = 0;
		trafficLight.ZIndex = 1;

		trafficLight.GlobalPosition = lightPosition;

		// 🔥 Start pause routine in parallel
		_ = PauseAllBulletsRoutine(columns, delayBetweenColumns, 2f, lightSprite);;

		for (int c = 0; c < columns; c++)
		{
			// 🚫 WAIT if paused (prevents new columns spawning)
			while (isPaused)
			{
				await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
			}

			float spawnXLeft = left - 0.2f;

			for (int r = 0; r < rows; r++)
			{
				float tRow = (rows == 1) ? 0f : (float)r / (rows - 1);
				float y = Mathf.Lerp(top, Mathf.Lerp(top, bottom - 10f, 0.5f), tRow);

				SpawnBullet(new Vector2(spawnXLeft, y), Vector2.Right, bulletSpeed);
			}

			float spawnXRight = right + 0.2f;

			for (int r2 = 0; r2 < rows; r2++)
			{
				float tRow2 = (rows == 1) ? 0f : (float)r2 / (rows - 1);
				float yb = Mathf.Lerp(
					Mathf.Lerp(top + 10f, bottom, 0.5f),
					bottom,
					tRow2
				);

				SpawnBulletLeft(new Vector2(spawnXRight, yb), Vector2.Left, bulletSpeed);
			}

			// ⏳ Wait between columns (also pause-aware)
			float waited = 0f;
			while (waited < delayBetweenColumns)
			{
				if (!isPaused)
				{
					await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
					waited += 0.01f;
				}
				else
				{
					await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
				}
			}
		}
		
		await ToSignal(GetTree().CreateTimer(3f), "timeout");
		trafficLight.QueueFree();
	}

	// 🔥 FULL pause system (bullets + spawning)
	private async Task PauseAllBulletsRoutine(
		int columns,
		float delayBetweenColumns,
		float pauseDuration,
		AnimatedSprite2D lightSprite
	)
	{
		float totalSpawnTime = columns * delayBetweenColumns;
		float pauseStartTime = totalSpawnTime / 2f;

		// ⏳ Wait until 1 second BEFORE pause → turn yellow
		await ToSignal(GetTree().CreateTimer(Mathf.Max(0f, pauseStartTime - 1f)), "timeout");

		if (IsInstanceValid(lightSprite))
			lightSprite.Frame = 1; // 🟡 Yellow

		// ⏳ Wait remaining time until pause
		await ToSignal(GetTree().CreateTimer(1f), "timeout");

		// ⏸ Pause starts → RED
		isPaused = true;

		if (IsInstanceValid(lightSprite))
			lightSprite.Frame = 2; // 🔴 Red

		foreach (var bullet in activeBullets)
		{
			if (!IsInstanceValid(bullet)) continue;

			bullet.Pause();

			var sprite = bullet.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			sprite?.Pause();
		}

		// ⏳ Stay paused
		await ToSignal(GetTree().CreateTimer(pauseDuration), "timeout");

		// ▶ Resume → GREEN
		isPaused = false;

		if (IsInstanceValid(lightSprite))
			lightSprite.Frame = 0; // 🟢 Green

		foreach (var bullet in activeBullets)
		{
			if (!IsInstanceValid(bullet)) continue;

			bullet.Resume();

			var sprite = bullet.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
			sprite?.Play();
		}
	}

	private void SpawnBullet(Vector2 position, Vector2 direction, float speed)
	{
		var bullet = bulletScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;

		bullet.GlobalPosition = position;

		var sprite = bullet.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();

		if (bullet is Bullet b)
		{
			b.Initialize(direction, speed);
			activeBullets.Add(b);
		}
	}

	private void SpawnBulletLeft(Vector2 position, Vector2 direction, float speed)
	{
		var bullet = bulletSceneLeft.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;

		bullet.GlobalPosition = position;

		var sprite = bullet.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();

		if (bullet is Bullet b)
		{
			b.Initialize(direction, speed);
			b.Rotation = 0f;
			activeBullets.Add(b);
		}
	}
}
