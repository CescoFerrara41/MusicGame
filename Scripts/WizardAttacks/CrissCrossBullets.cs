using Godot;
using System.Threading.Tasks;

public partial class CrissCrossBullets : AttackPattern
{
	[Export] private PackedScene bulletScene;
	[Export] private PackedScene parryBulletScene;
	
	private int bulletCount = 0;

	public override async Task Execute(
	PlayerSoul player,
	BattleBox box,
	EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		// Repeat pattern
		for (int i = 0; i < 4; i++)
		{
			SpawnVerticalWave(box);

			await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

			SpawnHorizontalWave(box);

			await ToSignal(GetTree().CreateTimer(0.8f), "timeout");
			
			SpawnVerticalWave2(box);
			
			await ToSignal(GetTree().CreateTimer(0.8f), "timeout");
			
			SpawnHorizontalWave2(box);
			
			await ToSignal(GetTree().CreateTimer(0.8f), "timeout");
		}
		
		await ToSignal(GetTree().CreateTimer(2f), "timeout");
	}

	private void SpawnBullet(BattleBox box)
{
	Vector2 min = box.GetInnerMinBounds();
	Vector2 max = box.GetInnerMaxBounds();

	float x = (float)GD.RandRange(min.X, max.X);

	Vector2 spawnPos = new Vector2(x, min.Y);
	Vector2 dir = Vector2.Down;

	SpawnBullet(spawnPos, dir);
	}
	
	private void SpawnBullet(Vector2 position, Vector2 direction)
	{
		bulletCount += 1;
		Bullet bullet;
		if (bulletCount % 10 == 0) {
			bullet = parryBulletScene.Instantiate<ParryBullet>();
			bulletCount += 1;
		}
		else {
			bullet = bulletScene.Instantiate<Bullet>();
		}


		// 1. Add to scene FIRST
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;

		// 2. Set position
		bullet.GlobalPosition = position;

		// 3. Initialize movement
		bullet.Initialize(direction, 35f);
	}
	
	

	private void SpawnVerticalWave(BattleBox box)
	{
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		int count = 3;
		
		float spreadStart = 0.1f;
		float spreadEnd = 0.9f;

		for (int i = 0; i < count; i++)
		{
			float normalizedT = i / (count-1f);
			float t = Mathf.Lerp(spreadStart, spreadEnd, normalizedT);
			float x = Mathf.Lerp(min.X, max.X, t);

			// Top → moving DOWN
			SpawnBullet(new Vector2(x, min.Y-15f), Vector2.Down);

			// Bottom → moving UP
			SpawnBullet(new Vector2(x, max.Y+15f), Vector2.Up);
		}
	}
	
	private void SpawnVerticalWave2(BattleBox box) {
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();
		
		int count = 2;
		float spreadStart = 0.25f;
		float spreadEnd = 0.75f;

		for (int i = 0; i < count; i++)
		{
			float normalizedT = i / (count-1f);
			float t = Mathf.Lerp(spreadStart, spreadEnd, normalizedT);
			float x = Mathf.Lerp(min.X, max.X, t);

			// Top → moving DOWN
			SpawnBullet(new Vector2(x, min.Y-15f), Vector2.Down);

			// Bottom → moving UP
			SpawnBullet(new Vector2(x, max.Y+15f), Vector2.Up);
		}
	}
	
	
	private void SpawnHorizontalWave(BattleBox box)
	{
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		int count = 3;
		
		float spreadStart = 0.1f;
		float spreadEnd = 0.9f;

		for (int i = 0; i < count; i++)
		{
			float normalizedT = i / (count-1f);
			float t = Mathf.Lerp(spreadStart, spreadEnd, normalizedT);
			float y = Mathf.Lerp(min.Y, max.Y, t);

			// Left → moving RIGHT
			SpawnBullet(new Vector2(min.X-15f, y), Vector2.Right);

			// Right → moving LEFT
			SpawnBullet(new Vector2(max.X+15f, y), Vector2.Left);
		}
	}
	
	private void SpawnHorizontalWave2(BattleBox box) {
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		int count = 2;
		
		float spreadStart = 0.25f;
		float spreadEnd = 0.75f;

		for (int i = 0; i < count; i++)
		{
			float normalizedT = i / (count-1f);
			float t = Mathf.Lerp(spreadStart, spreadEnd, normalizedT);
			float y = Mathf.Lerp(min.Y, max.Y, t);

			// Left → moving RIGHT
			SpawnBullet(new Vector2(min.X-15f, y), Vector2.Right);

			// Right → moving LEFT
			SpawnBullet(new Vector2(max.X+15f, y), Vector2.Left);
		}
	}
}
