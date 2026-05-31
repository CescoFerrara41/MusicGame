using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages a row of GambleOption nodes with a floating cursor sprite above them.
/// Each option is a unique scene with its own sprite, assigned in the Inspector.
///
/// Scene setup:
///   GambleOptions (Control, anchored to center of screen)
///     ├── HBoxContainer
///     └── CursorSprite (AnimatedSprite2D)  ← needs "idle" and "press" animations
///
/// In the Inspector, assign:
///   WagerOptionScenes  — 4 scenes, one each for 10%, 15%, 20%, 25%
///   CoinOptionScenes   — 2 scenes, one each for Heads, Tails
///   CursorSprite       — your cursor AnimatedSprite2D node
/// </summary>
public partial class GambleOptions : Control
{
	/// 4 scenes in order: 10%, 15%, 20%, 25%
	[Export] public Godot.Collections.Array<PackedScene> WagerOptionScenes = new();

	/// 2 scenes in order: Heads, Tails
	[Export] public Godot.Collections.Array<PackedScene> CoinOptionScenes = new();

	[Export] public AnimatedSprite2D CursorSprite;

	[Export] public float StaggerDelay  = 0.07f;
	[Export] public float PopDuration   = 0.12f;
	[Export] public float OutDuration   = 0.08f;
	[Export] public float CursorOffsetY = -24f;

	private HBoxContainer container;
	private List<GambleOption> activeOptions = new();

	private TaskCompletionSource<int> selectionTcs;
	private int  selectedIndex  = 0;
	private bool acceptingInput = false;

	public override void _Ready()
	{
		container = GetNodeOrNull<HBoxContainer>("HBoxContainer");
		if (container == null)
		{
			container = new HBoxContainer();
			container.AddThemeConstantOverride("separation", 16);
			AddChild(container);
		}

		if (CursorSprite != null)
			CursorSprite.Visible = false;

		Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (!acceptingInput || selectionTcs == null || selectionTcs.Task.IsCompleted)
			return;

		if (@event.IsActionPressed("ui_right"))
			MoveCursor(1);
		else if (@event.IsActionPressed("ui_left"))
			MoveCursor(-1);
		else if (@event is InputEventKey key && key.Pressed && !key.Echo
				 && key.Keycode == Key.Z)
			ConfirmSelection();
	}

	// ── Public API ────────────────────────────────────────────────────────────

	/// <summary>Returns 0–3 for 10%, 15%, 20%, 25%.</summary>
	public Task<int> WaitForWager() => WaitForSelection(WagerOptionScenes);

	/// <summary>Returns 0 for Heads, 1 for Tails.</summary>
	public Task<int> WaitForCoinFlip() => WaitForSelection(CoinOptionScenes);

	// ── Private helpers ───────────────────────────────────────────────────────

	private async Task<int> WaitForSelection(Godot.Collections.Array<PackedScene> scenes)
	{
		selectionTcs   = new TaskCompletionSource<int>();
		selectedIndex  = 0;
		acceptingInput = false;

		BuildOptions(scenes);
		Visible = true;

		await AnimateIn();

		if (CursorSprite != null)
		{
			CursorSprite.Visible = true;
			CursorSprite.Play("idle");
		}

		UpdateCursor();
		acceptingInput = true;

		int chosen = await selectionTcs.Task;

		acceptingInput = false;
		await AnimateOut();

		if (CursorSprite != null)
			CursorSprite.Visible = false;

		ClearOptions();
		Visible = false;

		return chosen;
	}

	private void MoveCursor(int direction)
	{
		activeOptions[selectedIndex].SetIdle();
		selectedIndex = Mathf.PosMod(selectedIndex + direction, activeOptions.Count);
		UpdateCursor();
	}

	private void UpdateCursor()
	{
		for (int i = 0; i < activeOptions.Count; i++)
		{
			if (i == selectedIndex)
				activeOptions[i].SetHover();
			else
				activeOptions[i].SetIdle();
		}

		SnapCursorToSelected();
	}

	private void SnapCursorToSelected()
	{
		if (CursorSprite == null || activeOptions.Count == 0)
			return;

		var option = activeOptions[selectedIndex];
		Vector2 optionCenter        = option.GlobalPosition + new Vector2(option.Size.X * 0.5f, 0f);
		CursorSprite.GlobalPosition = optionCenter + new Vector2(0f, CursorOffsetY);
	}

	private void ConfirmSelection()
	{
		if (selectionTcs == null || selectionTcs.Task.IsCompleted)
			return;

		CursorSprite?.Play("press");
		activeOptions[selectedIndex].SetPress();

		selectionTcs.SetResult(selectedIndex);
	}

	private void BuildOptions(Godot.Collections.Array<PackedScene> scenes)
	{
		foreach (var scene in scenes)
		{
			var option = scene.Instantiate<GambleOption>();
			container.AddChild(option);

			option.Scale    = Vector2.Zero;
			option.Modulate = new Color(1, 1, 1, 0);

			activeOptions.Add(option);
		}
	}

	private async Task AnimateIn()
	{
		for (int i = 0; i < activeOptions.Count; i++)
		{
			var option = activeOptions[i];
			var tween  = CreateTween();
			tween.SetParallel(true);

			tween.TweenProperty(option, "modulate:a", 1.0f, PopDuration);
			tween.TweenProperty(option, "scale", new Vector2(1.15f, 1.15f), PopDuration)
				 .SetEase(Tween.EaseType.Out);

			tween.SetParallel(false);
			tween.TweenProperty(option, "scale", Vector2.One, PopDuration * 0.5f)
				 .SetEase(Tween.EaseType.In);

			if (i < activeOptions.Count - 1)
				await ToSignal(GetTree().CreateTimer(StaggerDelay), SceneTreeTimer.SignalName.Timeout);
		}

		await ToSignal(GetTree().CreateTimer(PopDuration * 1.5f), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task AnimateOut()
	{
		var tween = CreateTween();
		tween.SetParallel(true);

		foreach (var option in activeOptions)
		{
			tween.TweenProperty(option, "scale",      Vector2.Zero, OutDuration).SetEase(Tween.EaseType.In);
			tween.TweenProperty(option, "modulate:a", 0.0f,         OutDuration);
		}

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	private void ClearOptions()
	{
		foreach (var option in activeOptions)
			option.QueueFree();

		activeOptions.Clear();
	}
}
