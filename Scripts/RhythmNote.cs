using Godot;

// A single tap note. Travels downward from spawn to the hit zone.
public partial class RhythmNote : Node2D
{
	public int Lane;
	[Export] public Sprite2D trail;

	private Vector2 velocity;
	private LaneController laneController;

	public bool InEarly;
	public bool InGood;
	public bool InPerfect;
	public bool InLate;

	public void Initialize(
		LaneController lane,
		int laneIndex,
		Vector2 spawnPos,
		Vector2 hitPos,
		float speed
	)
	{
		laneController = lane;
		Lane = laneIndex;

		GlobalPosition = spawnPos;

		Vector2 dir = (hitPos - spawnPos).Normalized();
		velocity = dir * speed;

		if (trail != null)
		{
			trail.Modulate = new Color(1, 1, 1, 0.5f);
			trail.Position = new Vector2(0, -3);
		}
	}

	public override void _Ready()
	{
		var area = GetNode<Area2D>("Area2D");
		area.AreaEntered += OnAreaEntered;
		area.AreaExited  += OnAreaExited;
	}

	public override void _Process(double delta)
	{
		GlobalPosition += velocity * (float)delta;
	}

	public void Hit()
	{
		laneController.RemoveNote(this);
		QueueFree();
	}

	public void Miss()
	{
		laneController.RemoveNote(this);
		QueueFree();
	}

	public float DistanceToHitZone()
	{
		if (laneController == null) return float.MaxValue;
		return GlobalPosition.DistanceTo(laneController.HitZone.GlobalPosition);
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area == laneController.PerfectZone) InPerfect = true;
		if (area == laneController.GoodZone)    InGood    = true;
		if (area == laneController.EarlyZone)   InEarly   = true;
		if (area == laneController.LateZone)    InLate    = true;
		if (area == laneController.GoodZoneLate) InGood   = true;
	}

	private void OnAreaExited(Area2D area)
	{
		if (area == laneController.PerfectZone) InPerfect = false;
		if (area == laneController.GoodZone)    InGood    = false;
		if (area == laneController.EarlyZone)   InEarly   = false;
		if (area == laneController.LateZone)    InLate    = false;
		if (area == laneController.GoodZoneLate) InGood   = false;
	}
}
