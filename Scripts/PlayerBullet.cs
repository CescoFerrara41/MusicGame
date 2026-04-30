using Godot;

public partial class PlayerBullet : Area2D
{
	[Export] private float speed = 60f;
	[Export] private float sineFrequency = 3f;   // how fast the wave oscillates
	[Export] private float sineAmplitude = 15f;  // keep low for subtle wave
	private int damage = 0;

	private float timeAlive = 0f;

	public override void _Ready()
	{
		AddToGroup("PlayerBullets");
		AreaEntered += OnAreaEntered;
	}

	public override void _Process(double delta)
	{
		timeAlive += (float)delta;

		float vertical = -speed * (float)delta;
		float horizontal = Mathf.Cos(timeAlive * sineFrequency) * sineAmplitude * (float)delta;

		GlobalPosition += new Vector2(horizontal, vertical);

		// Despawn if offscreen
		Vector2 screen = GetViewportRect().Size;
		if (GlobalPosition.Y < -50f || GlobalPosition.Y > screen.Y + 50f ||
			GlobalPosition.X < -50f || GlobalPosition.X > screen.X + 50f)
		{
			QueueFree();
		}
	}

	public int GetDamage()
	{
		return damage;
	}
	
	private void OnAreaEntered(Area2D area)
	{
		GD.Print("in zone");
		if (area.IsInGroup("BulletBreaker"))
		{
			QueueFree();
		}
	}
}
