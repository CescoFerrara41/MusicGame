using Godot;

public partial class CameraFollow2D : Camera2D
{
	[Export] public NodePath TargetPath;       // Assign the player node in Inspector
	[Export] public float SmoothSpeed = 5f;    // How fast the camera follows
	[Export] public Rect2 CameraBounds = new Rect2(0, 0, 1280, 720); // Optional bounds
	[Export] public bool StopFollow = false;   // Can stop following for cutscenes

	private Node2D target;

	public override void _Ready()
	{
		// Get target node
		if (TargetPath != null)
			target = GetNode<Node2D>(TargetPath);
	}

	public override void _Process(double delta)
	{
		if (target == null || StopFollow)
			return;

		// Smooth follow using lerp
		Vector2 targetPos = target.GlobalPosition;
		Vector2 newPos = GlobalPosition.Lerp(targetPos, SmoothSpeed * (float)delta);

		// Clamp camera to bounds
		Vector2 halfSize = GetViewportRect().Size * 0.5f;

		float minX = CameraBounds.Position.X + halfSize.X;
		float maxX = CameraBounds.Position.X + CameraBounds.Size.X - halfSize.X;
		float minY = CameraBounds.Position.Y + halfSize.Y;
		float maxY = CameraBounds.Position.Y + CameraBounds.Size.Y - halfSize.Y;

		// If bounds smaller than viewport, center
		if (minX > maxX) { minX = maxX = CameraBounds.Position.X + CameraBounds.Size.X * 0.5f; }
		if (minY > maxY) { minY = maxY = CameraBounds.Position.Y + CameraBounds.Size.Y * 0.5f; }

		newPos.X = Mathf.Clamp(newPos.X, minX, maxX);
		newPos.Y = Mathf.Clamp(newPos.Y, minY, maxY);

		GlobalPosition = newPos;
	}

	// Optional: move camera instantly to a position
	public void MoveToPosition(Vector2 newPos)
	{
		GlobalPosition = newPos;
	}

	// Optional: stop/resume following the target
	public void SetFollowEnabled(bool enable)
	{
		StopFollow = !enable;
	}
}
