using Godot;
using System.Collections.Generic;

public partial class BattleSelect : Control
{
	[Export] private VBoxContainer battleList;
	[Export] private TextureRect cursor;

	private List<BattleData> battles = new();

	private int currentPage = 0;
	private int selectedIndex = 0;

	private const int PAGE_SIZE = 5;

	public override void _Ready()
	{
		battles.Add(new BattleData("WizardBattle", "res://Prefabs/Battles/WizardBattle.tscn"));
		battles.Add(new BattleData("Battle 2", "res://Battles/Battle2.tscn"));
		battles.Add(new BattleData("Battle 3", "res://Battles/Battle3.tscn"));
		battles.Add(new BattleData("Battle 4", "res://Battles/Battle4.tscn"));
		battles.Add(new BattleData("Battle 5", "res://Battles/Battle5.tscn"));
		battles.Add(new BattleData("Battle 6", "res://Battles/Battle6.tscn"));
		battles.Add(new BattleData("Battle 7", "res://Battles/Battle7.tscn"));

		RefreshList();
		CallDeferred(nameof(UpdateCursor));
	}

	public override void _Process(double delta)
	{
		HandleInput();
	}

	private void HandleInput()
	{
		if (Input.IsActionJustPressed("ui_down"))
			MoveSelection(1);

		if (Input.IsActionJustPressed("ui_up"))
			MoveSelection(-1);

		if (Input.IsActionJustPressed("interact"))
			SelectBattle();
	}

	private void MoveSelection(int direction)
	{
		int start = currentPage * PAGE_SIZE;
		int itemsOnPage = Mathf.Min(PAGE_SIZE, battles.Count - start);

		selectedIndex += direction;

		// DOWN → next page
		if (selectedIndex >= itemsOnPage)
		{
			if ((currentPage + 1) * PAGE_SIZE < battles.Count)
			{
				currentPage++;
				selectedIndex = 0;
				RefreshList();
			}
			else
			{
				selectedIndex = itemsOnPage - 1;
			}
		}
		// UP → previous page
		else if (selectedIndex < 0)
		{
			if (currentPage > 0)
			{
				currentPage--;

				int prevStart = currentPage * PAGE_SIZE;
				int prevCount = Mathf.Min(PAGE_SIZE, battles.Count - prevStart);

				selectedIndex = prevCount - 1;
				RefreshList();
			}
			else
			{
				selectedIndex = 0;
			}
		}

		CallDeferred(nameof(UpdateCursor));
	}

	private void RefreshList()
	{
		foreach (Node child in battleList.GetChildren())
			child.QueueFree();

		int start = currentPage * PAGE_SIZE;

		for (int i = 0; i < PAGE_SIZE; i++)
		{
			int index = start + i;
			if (index >= battles.Count)
				break;

			Label label = new Label();
			label.Text = battles[index].Name;
			battleList.AddChild(label);
		}

		// 🔴 CRITICAL: clamp AFTER rebuilding
		selectedIndex = Mathf.Clamp(selectedIndex, 0, battleList.GetChildCount() - 1);
		CallDeferred(nameof(UpdateCursor));
	}

	private void UpdateCursor()
{
	if (battleList.GetChildCount() == 0)
		return;

	var selected = battleList.GetChild<Control>(selectedIndex);

	Vector2 pos = selected.Position;

	cursor.Position = new Vector2(
		pos.X - cursor.Size.X + 10f,
		pos.Y + (selected.Size.Y - cursor.Size.Y) * 0.5f
	);
}

	private void SelectBattle()
	{
		int globalIndex = currentPage * PAGE_SIZE + selectedIndex;

		if (globalIndex >= battles.Count)
			return;

		GetTree().ChangeSceneToFile(battles[globalIndex].ScenePath);
	}
}
