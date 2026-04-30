using Godot;
using System.Collections.Generic;

public partial class EggBasicLaser : Area2D
{
	[Export] private int damageAmount = 1;
	[Export] private float damageTickRate = 0.1f;
	[Export] private float lifetime = 0.3f;

	private float lastDamageTime = 0f;

	// Track players inside the laser
	private HashSet<PlayerSoul> playersInside = new();
	
	private AnimatedSprite2D sprite;
	private CollisionShape2D collision;

	public override void _Ready()
	{
		// Auto destroy
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		
		
		if (sprite != null) {
			sprite.Play();
			
			sprite.AnimationFinished += OnAnimationFinished;
		}

		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
	}
	
	private void OnAnimationFinished() {
		QueueFree();
	}


	private void OnAreaEntered(Area2D area)
	{
		if (area is PlayerSoul player)
		{
			playersInside.Add(player);
		}
	}

	private void OnAreaExited(Area2D area)
	{
		if (area is PlayerSoul player)
		{
			playersInside.Remove(player);
		}
	}

	public override void _Process(double delta)
	{
		if (playersInside.Count == 0) return;

		float time = Time.GetTicksMsec() / 1000f;

		if (time - lastDamageTime < damageTickRate)
			return;

		foreach (var player in playersInside)
		{
			// 🔥 IGNORE INVINCIBILITY (you must support this in PlayerSoul)
			player.TakeDamage(damageAmount, ignoreInvincibility: true);
		}

		lastDamageTime = time;
	}
}
