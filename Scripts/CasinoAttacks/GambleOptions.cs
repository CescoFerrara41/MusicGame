using Godot;
using System.Threading.Tasks;

/// <summary>
/// Scene hierarchy:
///
///   GambleOptions (Control)              ← this script
///     └── HBoxContainer
///           ├── Option10 (Control)       ← GambleOption.cs, sprite for 10%
///           ├── Option15 (Control)       ← GambleOption.cs, sprite for 15%
///           ├── Option20 (Control)       ← GambleOption.cs, sprite for 20%
///           └── Option25 (Control)       ← GambleOption.cs, sprite for 25%
///
/// For the coin flip, only Option10 and Option15 are used (index 0 and 1),
/// so you can reuse those slots or export a separate pair — see CoinOptions below.
/// </summary>
public partial class GambleOptions : Control
{
	// The 4 wager options — assign in the Inspector to your pre-placed nodes
	[Export] public GambleOption Option10;
	[Export] public GambleOption Option15;
	[Export] public GambleOption Option20;
	[Export] public GambleOption Option25;

	// The 2 coin options — can be separate nodes or a second HBoxContainer
	[Export] public GambleOption OptionHeads;
	[Export] public GambleOption OptionTails;

	[Export] public float StaggerDelay = 0.07f;
	[Export] public float PopDuration  = 0.12f;
	[Export] public float OutDuration  = 0.08f;

	private GambleOption[] activeOptions;
	private TaskCompletionSource<int> selectionTcs;
	private int selectedIndex  = 0;
	private bool acceptingInput = false;

	public override void _Ready()
	{
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
			selectionTcs.SetResult(selectedIndex);
	}

	// Call this for the wager phase — shows all 4 options
	public Task<int> WaitForWager()
	{
		return WaitForSelection(new[] { Option10, Option15, Option20, Option25 });
	}

	// Call this for the coin flip phase — shows just Heads and Tails
	public Task<int> WaitForCoinFlip()
	{
		return WaitForSelection(new[] { OptionHeads, OptionTails });
	}

	// ── Private ───────────────────────────────────────────────────────────────

	private async Task<int> WaitForSelection(GambleOption[] options)
	{
		selectionTcs   = new TaskCompletionSource<int>();
		selectedIndex  = 0;
		acceptingInput = false;
		activeOptions  = options;

		// Show and reset all options in this set
		foreach (var opt in activeOptions)
		{
			opt.Visible  = true;
			opt.Scale    = Vector2.Zero;
			opt.Modulate = new Color(1, 1, 1, 0);
			opt.SetIdle();
		}

		Visible = true;
		await AnimateIn();

		UpdateCursor();
		acceptingInput = true;

		int chosen = await selectionTcs.Task;

		acceptingInput = false;
		await AnimateOut();

		foreach (var opt in activeOptions)
			opt.Visible = false;

		Visible = false;
		return chosen;
	}

	private void MoveCursor(int direction)
	{
		activeOptions[selectedIndex].SetIdle();
		selectedIndex = Mathf.PosMod(selectedIndex + direction, activeOptions.Length);
		UpdateCursor();
	}

	private void UpdateCursor()
	{
		for (int i = 0; i < activeOptions.Length; i++)
		{
			if (i == selectedIndex)
				activeOptions[i].SetHover();
			else
				activeOptions[i].SetIdle();
		}
	}

	private async Task AnimateIn()
	{
		for (int i = 0; i < activeOptions.Length; i++)
		{
			var opt   = activeOptions[i];
			var tween = CreateTween();
			tween.SetParallel(true);

			tween.TweenProperty(opt, "modulate:a", 1.0f, PopDuration);
			tween.TweenProperty(opt, "scale", new Vector2(1.15f, 1.15f), PopDuration)
				 .SetEase(Tween.EaseType.Out);

			tween.SetParallel(false);
			tween.TweenProperty(opt, "scale", Vector2.One, PopDuration * 0.5f)
				 .SetEase(Tween.EaseType.In);

			if (i < activeOptions.Length - 1)
				await ToSignal(GetTree().CreateTimer(StaggerDelay), SceneTreeTimer.SignalName.Timeout);
		}

		await ToSignal(GetTree().CreateTimer(PopDuration * 1.5f), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task AnimateOut()
	{
		var tween = CreateTween();
		tween.SetParallel(true);

		foreach (var opt in activeOptions)
		{
			tween.TweenProperty(opt, "scale",      Vector2.Zero, OutDuration).SetEase(Tween.EaseType.In);
			tween.TweenProperty(opt, "modulate:a", 0.0f,         OutDuration);
		}

		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
