using Godot;
using System;
using System.Threading.Tasks;

public partial class SeedAttack : AttackPattern
{
	[Export] private PackedScene seedScene;
	private BattleBox battleBox;


	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		battleBox = box;
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");


		Vector2 innerMin = box.GetInnerMinBounds();
		Vector2 innerMax = box.GetInnerMaxBounds();
		float spawnY = innerMin.Y - 10f; // just above the box

		float duration = 6f;              // total rain time
		float spawnInterval = 1f;     // how often rain ticks

		float elapsed = 0f;

		while (elapsed < duration)
		{
			// Spawn 1 or 2 seeds
			int count = (int)GD.RandRange(1, 3); // 1 or 2

			for (int i = 0; i < count; i++)
			{
				float x = (float)GD.RandRange(innerMin.X, innerMax.X);
				Vector2 spawnPos = new Vector2(x, spawnY);

				SpawnSeedBullet(spawnPos, Vector2.Down);
			}

			await ToSignal(GetTree().CreateTimer(spawnInterval), "timeout");
			elapsed += spawnInterval;
		}

		// cooldown after rain
		await ToSignal(GetTree().CreateTimer(2f), "timeout");
	}

	
	private void SpawnSeedBullet(Vector2 position, Vector2 direction)
	{
		if (seedScene == null) return;
		SeedBullet bullet = seedScene.Instantiate<SeedBullet>();

		// 1. Add to scene FIRST
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;

		// 2. Set position
		bullet.GlobalPosition = position;

		// 3. Initialize movement
		bullet.Initialize(direction, 35f);
		bullet.SetBox(battleBox);
	}
	

}
