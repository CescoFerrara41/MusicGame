using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class EnemyManager : Node
{
	private List<EnemyDisplay> enemies = new();

	private int selectedIndex = 0;
	private TaskCompletionSource<EnemyDisplay> targetTask;

	private BattleNarrator narrator;

	public override void _Ready()
	{
		narrator = GetNode<BattleNarrator>("../BattleUI/BattleNarrator");

		foreach (Node child in GetChildren())
		{
			if (child is EnemyDisplay enemy)
				enemies.Add(enemy);
		}
	}

	public int GetEnemyCount()
	{
		return enemies.Count;
	}

	public async Task<EnemyDisplay> WaitForTarget()
{
	if (enemies.Count == 0)
		return null;

	selectedIndex = 0;
	ShowEnemyList();

	targetTask = new TaskCompletionSource<EnemyDisplay>();

	// Wait for confirm key to be released first
	while (Input.IsActionPressed("confirm"))
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	while (!targetTask.Task.IsCompleted)
	{
		HandleInput();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	return await targetTask.Task;
}

	private void HandleInput()
	{
		if (Input.IsActionJustPressed("ui_down"))
		{
			selectedIndex = (selectedIndex + 1) % enemies.Count;
			ShowEnemyList();
		}

		if (Input.IsActionJustPressed("ui_up"))
		{
			selectedIndex = (selectedIndex - 1 + enemies.Count) % enemies.Count;
			ShowEnemyList();
		}

		if (Input.IsActionJustPressed("confirm"))
		{
			targetTask.SetResult(enemies[selectedIndex]);
			narrator.ShowMessage("");
		}
	}

	private void ShowEnemyList()
	{
		string display = "";

		for (int i = 0; i < enemies.Count; i++)
		{
			if (i == selectedIndex)
				display += "> " + enemies[i].EnemyName + "\n";
			else
				display += "  " + enemies[i].EnemyName + "\n";
		}

		narrator.ShowMessage(display, 0);
	}
	
	public void HideAllEnemyHealth() {
		for (int i = 0; i < enemies.Count; i++) {
			enemies[i].HideHealth();
		}
	}
	
	public EnemyDisplay GetFirstAliveEnemy()
	{
		foreach (Node child in GetChildren())
		{
			if (child is EnemyDisplay enemy)
				return enemy;
		}

		return null;
	}
	
	public List<EnemyDisplay> GetAllEnemies()
	{
		return enemies;
	}
}
