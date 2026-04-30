using Godot;
using System.Threading.Tasks;

public partial class MovingGapColumns : AttackPattern
{
	[Export] private PackedScene bulletScene;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		float gapHeight = 23f;
		float oscSpeed = 1.2f;

		int rows = 20;
		float spawnInterval = 0.28f;
		float bulletSpeed = 25f;

		float totalDuration = 6f;
		float elapsed = 0f;

		while (elapsed < totalDuration)
		{
			// 🔹 Use your battle box bounds
			Vector2 min = box.GetInnerMinBounds();
			Vector2 max = box.GetInnerMaxBounds();

			float left = min.X;
			float right = max.X;
			float top = min.Y;
			float bottom = max.Y;

			float spawnX = left - 0.2f; // slightly outside for smoother entry

			// Time
			float time = Time.GetTicksMsec() / 1000.0f;

			// Oscillating safe path
			float centerNorm = (Mathf.Sin(time * oscSpeed) + 1f) / 2f;
			float safeY = Mathf.Lerp(
				top + gapHeight * 0.5f,
				bottom - gapHeight * 0.5f,
				centerNorm
			);

			for (int r = 0; r < rows; r++)
			{
				float tRow = (rows == 1) ? 0f : (float)r / (rows - 1);
				float y = Mathf.Lerp(top, bottom, tRow);

				// Skip gap
				if (Mathf.Abs(y - safeY) <= gapHeight * 0.5f)
					continue;

				SpawnBullet(new Vector2(spawnX, y), Vector2.Right, bulletSpeed);
			}

			await ToSignal(GetTree().CreateTimer(spawnInterval), "timeout");
			elapsed += spawnInterval;
		}

		await ToSignal(GetTree().CreateTimer(4f), "timeout");
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
		}
	}
}
