using Godot;

/// <summary>
/// A single selectable option. Each option is its own unique scene with its own
/// AnimatedSprite2D. No label or setup needed — the sprite IS the option.
///
///   GambleOption (Control)
///     └── AnimatedSprite2D   ← needs "idle", "hover", and "press" animations
/// </summary>
public partial class GambleOption : Control
{
	[Export] public AnimatedSprite2D Sprite { get; private set; }

	public override void _Ready()
	{
		Sprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		SetIdle();
	}

	public void SetIdle()  => Sprite?.Play("idle");
	public void SetHover() => Sprite?.Play("hover");
	public void SetPress() => Sprite?.Play("press");
}
