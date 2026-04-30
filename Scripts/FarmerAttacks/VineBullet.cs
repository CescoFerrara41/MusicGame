using Godot;
using System;
using System.Collections.Generic;

public partial class VineBullet : Area2D
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
	
	private void UpdateCollision()
	{
		if (collision.Shape is RectangleShape2D rect)
		{
			int frame = sprite.Frame;
			float height = 0f;
			Vector2 position;

			if (frame >= 0 && frame <= 1) {
				height = 12f;
				position = new Vector2(-1, -6);
			}
			
			else if (frame == 2) {
				height = 19f;
				position = new Vector2(-1, -9.5f);
			}
			
			else if (frame == 3) {
				height = 29f;
				position = new Vector2(-1, -14.5f);
			}
			else if (frame == 4) {
				height = 35f;
				position = new Vector2(-1, -17.5f);
			}
			else {
				height = 48f;
				position = new Vector2(-1, -24f);
			}
			
			rect.Size = new Vector2(rect.Size.X, height);
			collision.Position = position;
		}
	}

	public override void _Process(double delta)
	{
		UpdateCollision();
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
