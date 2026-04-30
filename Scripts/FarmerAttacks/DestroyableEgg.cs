using Godot;
using System.Threading.Tasks;

public partial class DestroyableEgg : Area2D
{
	[Export] private float speed = 80f;
	[Export] private float spinSpeed = 180f;   // degrees per second
	[Export] private int damage = 1;
	[Export] private PackedScene eggLaserScene;

	private bool isDying = false;
	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		AddToGroup("EnemyBullets");
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.Play("default");

		AreaEntered += OnAreaEntered;
	}

	public override void _Process(double delta)
	{
		if (isDying) return;

		// Move downward
		GlobalPosition += new Vector2(0, speed * (float)delta);

		// Spin the sprite
		sprite.RotationDegrees += spinSpeed * (float)delta;

		// Despawn if offscreen
		Vector2 screen = GetViewportRect().Size;
		if (GlobalPosition.Y > screen.Y + 50f)
		{
			QueueFree();
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (isDying) return;

		if (area.IsInGroup("PlayerBullets"))
		{
			area.QueueFree();
			_ = FlashAndBreak();
		}
		else if (area.IsInGroup("PlayerSoul"))
		{
			// Handled by the PlayerSoul's own damage detection
		}
	}

	private async Task FlashAndBreak()
	{
		isDying = true;

		Color original = sprite.Modulate;
		Color red = new Color(1f, 0.2f, 0.2f, 1f);

		for (int i = 0; i < 2; i++)
		{
			sprite.Modulate = red;
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
			sprite.Modulate = original;
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		}

		SpawnLaser();
		QueueFree();
	}

	private void SpawnLaser()
	{
		if (eggLaserScene == null) return;
		var laser = eggLaserScene.Instantiate<Node2D>();
		laser.GlobalPosition = GlobalPosition;
		GetTree().Root.AddChild(laser);
	}

	public int GetDamage() => damage;
}
