using Godot;

public partial class CowBullet : Area2D
{
	[Export] private int damage = 1;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			GD.Print("[Cow] Hit player");

			if (area is PlayerSoul player)
			{
				bool damaged = player.TakeDamage(damage);

				// Make transparent on hit, matching Unity behaviour —
				// only if the player actually took damage (not invincible)
				if (damaged)
				{
					MakeTransparent();
				}
			}
		}
	}

	private void MakeTransparent()
	{
		// Fade out the cow's sprite so it visually disappears without immediately freeing it,
		// preserving any ongoing pull animation driven by UFOCowPull
		if (GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D") is AnimatedSprite2D sprite)
		{
			sprite.Modulate = new Color(sprite.Modulate, 0f);
		}
	}
}
