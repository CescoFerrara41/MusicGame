using Godot;

/// <summary>
/// Attach this to each of the 4 pre-placed option nodes in your GambleOptions scene.
/// Each one owns its own AnimatedSprite2D with "idle" and "hover" animations.
///
///   Option10  (Control) ← GambleOption.cs, unique sprite for 10%
///   Option15  (Control) ← GambleOption.cs, unique sprite for 15%
///   Option20  (Control) ← GambleOption.cs, unique sprite for 20%
///   Option25  (Control) ← GambleOption.cs, unique sprite for 25%
/// </summary>
public partial class GambleOption : Control
{
	[Export] public AnimatedSprite2D Sprite { get; private set; }

	public override void _Ready()
	{
		Sprite ??= GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		SetIdle();
	}

	public void SetHover() => Sprite?.Play("hover");
	public void SetIdle()  => Sprite?.Play("idle");
}
