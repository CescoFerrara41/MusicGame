using Godot;

public partial class UfoCowPull : Node2D
{
	[Export] private PackedScene cowScene;

	private Vector2 direction;
	private float speed;
	private BattleBox trackedBox;

	private CowBullet cowInstance;
	private Vector2 cowDirection;

	private enum State
	{
		Approaching,  // UFO and cow moving toward each other horizontally
		Pulling,      // UFO hovering, cow rising toward it
		Leaving,      // Cow absorbed, UFO continuing off screen
	}

	private State state = State.Approaching;

	public void Initialize(Vector2 moveDirection, float ufoSpeed, BattleBox box)
	{
		speed = ufoSpeed;
		trackedBox = box;

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		// Cow spawns at any random X across the full box width.
		// The UFO direction is then corrected to always travel toward the cow,
		// so the meeting point can be anywhere regardless of which side the UFO enters from.
		float cowX = (float)GD.RandRange(min.X, max.X);
		float cowY = max.Y + 5f;

		// Ensure UFO always moves toward the cow's X, not away from it
		direction = new Vector2(Mathf.Sign(cowX - GlobalPosition.X), 0f);

		if (cowScene != null)
		{
			cowInstance = cowScene.Instantiate<CowBullet>();
			GetTree().CurrentScene.AddChild(cowInstance);
			cowInstance.GlobalPosition = new Vector2(cowX, cowY);

			// Cow walks horizontally toward the UFO
			cowDirection = new Vector2(-Mathf.Sign(direction.X), 0f);
		}
		else
		{
			GD.PrintErr("[UFOCowPull] cowScene is not assigned.");
		}
	}

	public override void _Process(double delta)
	{
		switch (state)
		{
			case State.Approaching:
				Approach(delta);
				break;

			case State.Pulling:
				Pull(delta);
				break;

			case State.Leaving:
				Leave(delta);
				break;
		}
	}

	private void Approach(double delta)
	{
		// UFO moves horizontally
		GlobalPosition += direction * speed * (float)delta;

		// Cow walks toward the UFO on the X axis
		if (cowInstance != null && IsInstanceValid(cowInstance))
		{
			cowInstance.GlobalPosition += cowDirection * speed * (float)delta;

			// Once the cow is directly below the UFO, stop and begin pulling
			if (Mathf.Abs(cowInstance.GlobalPosition.X - GlobalPosition.X) <= 1f)
			{
				// Snap X so the cow rises in a straight line
				cowInstance.GlobalPosition = new Vector2(GlobalPosition.X, cowInstance.GlobalPosition.Y);
				GD.Print("[UFOCowPull] Aligned — beginning pull.");
				state = State.Pulling;
			}
		}

		// Safety: clean up if UFO exits bounds without ever aligning
		if (trackedBox != null)
		{
			Vector2 min = trackedBox.GetInnerMinBounds();
			Vector2 max = trackedBox.GetInnerMaxBounds();
			float x = GlobalPosition.X;
			if (x < min.X - 60f || x > max.X + 60f)
			{
				cowInstance?.QueueFree();
				QueueFree();
			}
		}
	}

	private void Pull(double delta)
	{
		if (cowInstance == null || !IsInstanceValid(cowInstance))
		{
			state = State.Leaving;
			return;
		}

		// Cow rises straight up toward the UFO
		cowInstance.GlobalPosition += Vector2.Up * speed * (float)delta;

		if (cowInstance.GlobalPosition.Y <= GlobalPosition.Y)
		{
			GD.Print("[UFOCowPull] Cow abducted — UFO leaving.");
			cowInstance.QueueFree();
			cowInstance = null;
			state = State.Leaving;
		}
	}

	private void Leave(double delta)
	{
		// UFO resumes its original direction and exits off screen
		GlobalPosition += direction * speed * (float)delta;

		if (trackedBox != null)
		{
			Vector2 min = trackedBox.GetInnerMinBounds();
			Vector2 max = trackedBox.GetInnerMaxBounds();
			float x = GlobalPosition.X;
			if (x < min.X - 60f || x > max.X + 60f)
			{
				QueueFree();
			}
		}
	}

	// Optional: connect UFO beam Area2D's area_entered signal to this in the editor
	public void OnBeamAreaEntered(Area2D area)
	{
		if (state == State.Pulling && area.IsInGroup("Cow"))
		{
			GD.Print("[UFOCowPull] Beam contact — cow abducted.");
			cowInstance?.QueueFree();
			cowInstance = null;
			state = State.Leaving;
		}
	}
}
