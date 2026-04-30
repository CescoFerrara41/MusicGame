using Godot;

// JudgementDisplay — a single shared Node2D placed at the center of the screen.
//
// Scene tree expected:
//   JudgementDisplay (Node2D)
//     AnimatedSprite2D   <- node named "AnimatedSprite2D"
//
// The AnimatedSprite2D should have three animations:
//   "perfect"  — shown on a perfect hit
//   "good"     — shown on a good hit
//   "miss"     — shown on a miss
//
// Each call to Show() interrupts any in-progress animation and restarts fresh.

public partial class JudgementDisplay : Node2D
{
	[Export] public float FloatDistance = 40f;  // pixels to drift upward
	[Export] public float Duration      = 0.6f; // seconds for the full animation

	private AnimatedSprite2D sprite;
	private Tween            activeTween;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.Modulate = new Color(1, 1, 1, 0f); // start invisible
	}

	public void ShowPerfect() => Show("perfect");
	public void ShowGood()    => Show("good");
	public void ShowMiss()    => Show("miss");

	private void Show(string animation)
	{
		// Kill any tween that's already running so overlapping hits don't fight.
		activeTween?.Kill();

		// Reset position and opacity instantly before starting the new animation.
		sprite.Position = Vector2.Zero;
		sprite.Modulate = new Color(1, 1, 1, 1f);
		sprite.Play(animation);

		activeTween = CreateTween().SetParallel(true);

		// Float upward (negative Y = up in Godot 2D).
		activeTween.TweenProperty(
			sprite, "position",
			new Vector2(0f, -FloatDistance),
			Duration
		).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

		// Fade out — start fading halfway through so it's readable at first.
		activeTween.TweenProperty(
			sprite, "modulate:a",
			0f,
			Duration * 0.5f
		).SetDelay(Duration * 0.5f)
		 .SetTrans(Tween.TransitionType.Linear);
	}
}
