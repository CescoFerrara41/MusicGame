using Godot;
using System;
using System.Threading.Tasks;

public partial class EggWallAttack : AttackPattern
{
	[Export] private PackedScene normalBulletScene;
	[Export] private PackedScene destroyableEggScene;

	private RandomNumberGenerator rng = new RandomNumberGenerator();

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		rng.Randomize();

		await box.SetMode(BattleBoxMode.TallEnemy);
		player.Spawn();
		player.GlobalPosition = new Vector2(160f, 130f);

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		player.TransformToBattleship();
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		int columns = 7;
		int waves = 6;
		float delayBetweenWaves = 2f;

		Vector2 innerMin = box.GetInnerMinBounds();
		Vector2 innerMax = box.GetInnerMaxBounds();
		float spawnY = innerMin.Y - 10f; // just above the box

		for (int w = 0; w < waves; w++)
		{
			int eggCol = rng.RandiRange(0, columns - 1);

			for (int c = 0; c < columns; c++)
			{
				float t = (columns == 1) ? 0f : (float)c / (columns - 1);
				float x = Mathf.Lerp(innerMin.X, innerMax.X, t);
				Vector2 spawnPos = new Vector2(x, spawnY);

				if (c == eggCol && destroyableEggScene != null)
				{
					SpawnEgg(spawnPos);
				}
				else
				{
					SpawnNormalBullet(spawnPos, Vector2.Down);
				}
			}

			await ToSignal(GetTree().CreateTimer(delayBetweenWaves), "timeout");
		}

		// Give the player time to deal with the last wave
		await ToSignal(GetTree().CreateTimer(3f), "timeout");
		player.ReturnToNormal();
	}

	
	private void SpawnNormalBullet(Vector2 position, Vector2 direction)
	{
		if (normalBulletScene == null) return;
		Bullet bullet = normalBulletScene.Instantiate<Bullet>();

		// 1. Add to scene FIRST
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;

		// 2. Set position
		bullet.GlobalPosition = position;

		// 3. Initialize movement
		bullet.Initialize(direction, 35f);
	}

	private void SpawnEgg(Vector2 position)
	{
		if (destroyableEggScene == null) return;
		var egg = destroyableEggScene.Instantiate<DestroyableEgg>();
		egg.GlobalPosition = position;
		GetTree().Root.AddChild(egg);
	}
}
