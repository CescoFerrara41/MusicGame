using Godot;
using System;

public partial class SeedBullet : Bullet
{
	[Export] private float spinSpeed = 360f;
	[Export] private PackedScene vineScene;
	
	private BattleBox trackedBox;

	private int battleBoxHitCount = 0;
	private bool isStopped = false;
	private bool wallHitCooldown = false; // prevents counting the same wall crossing twice in one pass
	
	public void SetBox(BattleBox box)
	{
		trackedBox = box;
	}
	
	public override void _Process(double delta)
	{
		if (!isStopped)
		{
			base._Process(delta);
			CheckWallHit();
		}

		// Spin the egg regardless of movement state
		RotationDegrees += spinSpeed * (float)delta;
	}
	
	private void CheckWallHit()
	{

		Vector2 min = trackedBox.GetInnerMinBounds();
		Vector2 max = trackedBox.GetInnerMaxBounds();
		Vector2 pos = GlobalPosition;

		bool outsideBounds = pos.X <= min.X || pos.X >= max.X || pos.Y <= min.Y || pos.Y >= max.Y;

		if (outsideBounds && !wallHitCooldown)
		{
			wallHitCooldown = true;
			battleBoxHitCount++;
			GD.Print($"[EggProjectile] Hit BattleBox wall #{battleBoxHitCount}");

			if (battleBoxHitCount >= 2)
			{
				isStopped = true;
				Pause();
				SpawnVine();
			}
		}
		else if (!outsideBounds)
		{
			// Reset cooldown once the egg is back inside, ready to detect the next wall
			wallHitCooldown = false;
		}
	}

	private async void SpawnVine()
	{

		var vine = vineScene.Instantiate<VineBullet>();
		GetTree().CurrentScene.AddChild(vine);
		vine.ZIndex = 1;
		vine.GlobalPosition = GlobalPosition;
		QueueFree();
		
	}
}
