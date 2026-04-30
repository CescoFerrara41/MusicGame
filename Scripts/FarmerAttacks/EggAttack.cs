using Godot;
using System.Threading.Tasks;

public partial class EggAttack : AttackPattern
{
	[Export] private PackedScene eggProjectileScene;
	[Export] private PackedScene parryEggProjectileScene;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();


		// Spawn egg from a random border of the battlebox, aimed inward
		SpawnEggProjectile(player, box);

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		
		SpawnParryEggProjectile(player, box);

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		
		SpawnEggProjectile(player, box);

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		

		// Clean up the battle box after 3s
		await ToSignal(GetTree().CreateTimer(3f), "timeout");
		await box.SetMode(BattleBoxMode.Enemy);
	}
	
	private void SpawnParryEggProjectile(PlayerSoul player, BattleBox box) {
		if (parryEggProjectileScene == null)
		{
			GD.PrintErr("[EggAttack] parryEggProjectileScene is not assigned.");
			return;
		}

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();
		Vector2 center = (min + max) / 2f;
		float offset = 15f; // spawn just outside the wall, same convention as CrissCrossBullets

		// Pick a random side: 0 = top, 1 = bottom, 2 = left, 3 = right
		int side = (int)GD.RandRange(0, 4);
		Vector2 spawnPos;
		Vector2 inwardDir;

		switch (side)
		{
			case 0: // Top — enter moving downward
				spawnPos = new Vector2((float)GD.RandRange(min.X, max.X), min.Y - offset);
				inwardDir = new Vector2(0f, 1f);
				break;
			case 1: // Bottom — enter moving upward
				spawnPos = new Vector2((float)GD.RandRange(min.X, max.X), max.Y + offset);
				inwardDir = new Vector2(0f, -1f);
				break;
			case 2: // Left — enter moving right
				spawnPos = new Vector2(min.X - offset, (float)GD.RandRange(min.Y, max.Y));
				inwardDir = new Vector2(1f, 0f);
				break;
			default: // Right — enter moving left
				spawnPos = new Vector2(max.X + offset, (float)GD.RandRange(min.Y, max.Y));
				inwardDir = new Vector2(-1f, 0f);
				break;
		}

		// Tilt the direction slightly toward the box center so it always
		// crosses the interior rather than grazing a corner
		Vector2 toCenter = (center - spawnPos).Normalized();
		Vector2 direction = (inwardDir + toCenter * 0.3f).Normalized();

		var egg = parryEggProjectileScene.Instantiate<ParryEgg>();
		GetTree().CurrentScene.AddChild(egg);
		egg.ZIndex = 1;
		egg.GlobalPosition = spawnPos;
		egg.Initialize(direction, 40f);
		egg.SetPlayerAndBox(player, box);
	}

	private void SpawnEggProjectile(PlayerSoul player, BattleBox box)
	{
		if (eggProjectileScene == null)
		{
			GD.PrintErr("[EggAttack] eggProjectileScene is not assigned.");
			return;
		}

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();
		Vector2 center = (min + max) / 2f;
		float offset = 15f; // spawn just outside the wall, same convention as CrissCrossBullets

		// Pick a random side: 0 = top, 1 = bottom, 2 = left, 3 = right
		int side = (int)GD.RandRange(0, 4);
		Vector2 spawnPos;
		Vector2 inwardDir;

		switch (side)
		{
			case 0: // Top — enter moving downward
				spawnPos = new Vector2((float)GD.RandRange(min.X, max.X), min.Y - offset);
				inwardDir = new Vector2(0f, 1f);
				break;
			case 1: // Bottom — enter moving upward
				spawnPos = new Vector2((float)GD.RandRange(min.X, max.X), max.Y + offset);
				inwardDir = new Vector2(0f, -1f);
				break;
			case 2: // Left — enter moving right
				spawnPos = new Vector2(min.X - offset, (float)GD.RandRange(min.Y, max.Y));
				inwardDir = new Vector2(1f, 0f);
				break;
			default: // Right — enter moving left
				spawnPos = new Vector2(max.X + offset, (float)GD.RandRange(min.Y, max.Y));
				inwardDir = new Vector2(-1f, 0f);
				break;
		}

		// Tilt the direction slightly toward the box center so it always
		// crosses the interior rather than grazing a corner
		Vector2 toCenter = (center - spawnPos).Normalized();
		Vector2 direction = (inwardDir + toCenter * 0.3f).Normalized();

		var egg = eggProjectileScene.Instantiate<EggProjectile>();
		GetTree().CurrentScene.AddChild(egg);
		egg.ZIndex = 1;
		egg.GlobalPosition = spawnPos;
		egg.Initialize(direction, 40f);
		egg.SetPlayerAndBox(player, box);
	}
}
