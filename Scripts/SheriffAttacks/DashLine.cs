using Godot;

/// <summary>
/// Attach to your dash line sprite scene.
/// Moves downward at a set speed and frees itself when it exits the bottom of the box.
/// </summary>
public partial class DashLine : Node2D
{
	private float speed;
	private float despawnY;

	public void Initialize(float scrollSpeed, float bottomY)
	{
		speed    = scrollSpeed;
		despawnY = bottomY;
	}

	public override void _Process(double delta)
	{
		GlobalPosition += Vector2.Down * speed * (float)delta;

		if (GlobalPosition.Y > despawnY)
			QueueFree();
	}
}
