using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class LaserAttack : AttackPattern
{
	[Export] private PackedScene warningScene;
	[Export] private PackedScene redLaserScene;
	[Export] private PackedScene yellowLaserScene;
	[Export] private PackedScene greenLaserScene;

	[Export] private int warningsToSpawn = 6;
	[Export] private float warningDuration = 0.5f;
	[Export] private float delayBeforeLaserSpawn = 0.1f;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.WidestEnemy);
		player.Spawn();

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		// ✅ Generate 3x3 grid positions inside battle box
		List<Vector2> grid = GenerateGrid(min, max);

		// Enable movement
		player.SetProcess(true);

		// Run attack waves
		await SpawnSingleWave(grid);
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);

		await SpawnDualWave(grid);
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);

		await SpawnTripleWave(grid);

		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
	}

	// --------------------------------------------------
	// GRID GENERATION
	// --------------------------------------------------
	private List<Vector2> GenerateGrid(Vector2 min, Vector2 max)
	{
		List<Vector2> grid = new();

		float width = max.X - min.X;
		float height = max.Y - min.Y;

		for (int r = 0; r < 3; r++)
		{
			for (int c = 0; c < 3; c++)
			{
				float x = min.X + (c + 0.5f) * (width / 3f);
				float y = min.Y + (r + 0.5f) * (height / 3f);
				grid.Add(new Vector2(x, y));
			}
		}

		return grid;
	}

	// --------------------------------------------------
	// SINGLE WAVE
	// --------------------------------------------------
	private async Task SpawnSingleWave(List<Vector2> grid)
	{
		var indices = GetRandomIndices(grid.Count, warningsToSpawn);

		var warnings = new List<Node2D>();

		foreach (int i in indices)
		{
			var w = warningScene.Instantiate<Node2D>();
			w.GlobalPosition = grid[i];
			AddChild(w);
			warnings.Add(w);
		}

		await ToSignal(GetTree().CreateTimer(warningDuration), SceneTreeTimer.SignalName.Timeout);

		foreach (var w in warnings)
			w.QueueFree();

		await ToSignal(GetTree().CreateTimer(delayBeforeLaserSpawn), SceneTreeTimer.SignalName.Timeout);

		foreach (int i in indices)
		{
			SpawnLaser(grid[i]);
		}
	}

	// --------------------------------------------------
	// DUAL WAVE
	// --------------------------------------------------
	private async Task SpawnDualWave(List<Vector2> grid)
	{
		var set1 = GetRandomIndices(grid.Count, warningsToSpawn);
		var set2 = GetRandomIndices(grid.Count, 7);

		await SpawnWarningSet(grid, set1, 0.5f);
		await SpawnWarningSet(grid, set2, 0.5f);

		await SpawnLaserSet(grid, set1);
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		await SpawnLaserSet(grid, set2);
	}

	// --------------------------------------------------
	// TRIPLE WAVE
	// --------------------------------------------------
	private async Task SpawnTripleWave(List<Vector2> grid)
	{
		var s1 = GetRandomIndices(grid.Count, warningsToSpawn);
		var s2 = GetRandomIndices(grid.Count, 7);
		var s3 = GetRandomIndices(grid.Count, 8);

		await SpawnWarningSet(grid, s1, 0.5f);
		await SpawnWarningSet(grid, s2, 0.5f);
		await SpawnWarningSet(grid, s3, 0.5f);

		await SpawnLaserSet(grid, s1);
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		await SpawnLaserSet(grid, s2);
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		await SpawnLaserSet(grid, s3);
	}

	// --------------------------------------------------
	// HELPERS
	// --------------------------------------------------
	private async Task SpawnWarningSet(List<Vector2> grid, List<int> indices, float duration)
	{
		var warnings = new List<Node2D>();

		foreach (int i in indices)
		{
			var w = warningScene.Instantiate<Node2D>();
			w.GlobalPosition = grid[i];
			AddChild(w);
			warnings.Add(w);
		}

		await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

		foreach (var w in warnings)
			w.QueueFree();
	}

	private async Task SpawnLaserSet(List<Vector2> grid, List<int> indices)
	{
		foreach (int i in indices)
		{
			SpawnLaser(grid[i]);
			await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void SpawnLaser(Vector2 pos)
	{
		PackedScene chosen = GetRandomLaser();
		if (chosen == null) return;

		var laser = chosen.Instantiate<Node2D>();
		laser.GlobalPosition = pos; // slight vertical offset like Unity
		AddChild(laser);
	}

	private PackedScene GetRandomLaser()
	{
		int r = GD.RandRange(0, 2);
		if (r == 0) return redLaserScene;
		if (r == 1) return yellowLaserScene;
		return greenLaserScene;
	}

	private List<int> GetRandomIndices(int max, int count)
	{
		List<int> result = new();

		while (result.Count < count)
		{
			int i = GD.RandRange(0, max - 1);
			if (!result.Contains(i))
				result.Add(i);
		}

		return result;
	}
}
