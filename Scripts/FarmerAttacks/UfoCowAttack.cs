using Godot;
using System.Threading.Tasks;

public partial class UfoCowAttack : AttackPattern
{
	[Export] private PackedScene ufoCowPullScene;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.WideEnemy);
		player.Spawn();
		
		for (int i = 0; i<3; i++) {
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0f, 0.5f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0.25f, 0.75f)), "timeout");
		SpawnUFO(box);
		await ToSignal(GetTree().CreateTimer(GD.RandRange(0.25f, 0.75f)), "timeout");
		}

		// Clean up the battle box after 3s
		await ToSignal(GetTree().CreateTimer(3f), "timeout");
	}

	private void SpawnUFO(BattleBox box)
	{
		if (ufoCowPullScene == null)
		{
			GD.PrintErr("[UFOAttack] ufoCowPullScene is not assigned.");
			return;
		}

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();
		float offset = 15f;

		bool fromLeft = GD.RandRange(0, 1) == 0;
		Vector2 spawnPos;
		Vector2 direction;

		if (fromLeft)
		{
			spawnPos = new Vector2(min.X - offset, (float)GD.RandRange(min.Y, max.Y * 0.5f));
			direction = Vector2.Right;
		}
		else
		{
			spawnPos = new Vector2(max.X + offset, (float)GD.RandRange(min.Y, max.Y * 0.5f));
			direction = Vector2.Left;
		}

		var ufo = ufoCowPullScene.Instantiate<UfoCowPull>();
		GetTree().CurrentScene.AddChild(ufo);
		ufo.ZIndex = 1;
		ufo.GlobalPosition = spawnPos;
		ufo.Initialize(direction, 40f, box);
	}
}
