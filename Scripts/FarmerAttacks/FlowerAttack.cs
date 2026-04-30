using Godot;
using System;
using System.Threading.Tasks;

public partial class FlowerAttack : AttackPattern
{
	[Export] private float projectileTopSpeed = 30f;
	[Export] private PackedScene flower;
	[Export] private PackedScene parryFlower;

	// Delay between each bullet within a group
	[Export] private float bulletInterval = 0.1f;
	// Delay between full waves
	[Export] private float waveInterval = 0.55f;
	// How many times the two paths cross as they travel down the box
	[Export] private float crossings = 4f;

	private const float OutsideBox = 5f;
	private const int TotalWaves = 4;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.SlightlyLargerEnemy);
		player.Spawn();

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float centerX   = (min.X + max.X) / 2f;
		float amplitude = (max.X - min.X) / 2f + OutsideBox;
		float spawnY    = min.Y;
		float boxHeight = max.Y - min.Y;

		// traversalTime = how long a bullet takes to cross the full box height
		// The two columns are pi apart, so they cross once per full period.
		// Therefore: frequency = crossings / traversalTime
		float traversalTime = boxHeight / projectileTopSpeed;
		float frequency     = crossings / traversalTime;

		for (int wave = 0; wave < TotalWaves; wave++)
		{
			float initialPhase = wave * Mathf.Pi + Mathf.Pi / 2f;
			if (wave == 1) {
				SpawnFlowerParryBullet(parryFlower, new Vector2(centerX, spawnY-(wave*3f)), amplitude, frequency, initialPhase, false);
				SpawnFlowerParryBullet(parryFlower, new Vector2(centerX, spawnY-(wave*3f)), amplitude, frequency, initialPhase, true);
			}
			else {

			for (int i = 0; i < 3; i++)
			{
				SpawnFlowerBullet(flower, new Vector2(centerX, spawnY-(wave*3f)), amplitude, frequency, initialPhase, false);
				SpawnFlowerBullet(flower, new Vector2(centerX, spawnY-(wave*3f)), amplitude, frequency, initialPhase, true);

				await ToSignal(GetTree().CreateTimer(bulletInterval), "timeout");
			}
			}

			await ToSignal(GetTree().CreateTimer(waveInterval), "timeout");
		}

		await ToSignal(GetTree().CreateTimer(2f), "timeout");
	}

	private void SpawnFlowerBullet(PackedScene scene, Vector2 position, float amplitude, float frequency, float initialPhase, bool isRight)
	{
		FlowerBullet bullet = scene.Instantiate<FlowerBullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;
		bullet.Amplitude     = amplitude;
		bullet.Frequency     = frequency;
		bullet.DownSpeed     = projectileTopSpeed;
		bullet.IsRightColumn = isRight;
		bullet.InitializeFlower(position.X, initialPhase);
	}
	
	private void SpawnFlowerParryBullet(PackedScene scene, Vector2 position, float amplitude, float frequency, float initialPhase, bool isRight)
	{
		FlowerParryBullet bullet = scene.Instantiate<FlowerParryBullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;
		bullet.Amplitude     = amplitude;
		bullet.Frequency     = frequency;
		bullet.DownSpeed     = projectileTopSpeed;
		bullet.IsRightColumn = isRight;
		bullet.InitializeFlower(position.X, initialPhase);
	}
}
