using Godot;

public partial class HitZone : Node2D
{
	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	}

	public void PlayHit()
	{
		sprite.Play("hit");
	}

	// Loops while a hold note is being held.
	public void PlayHold()
	{
		sprite.Play("sparkle"); // rename to match your animation name
	}

	public void StopHold()
	{
		sprite.Play("default");
	}
}
