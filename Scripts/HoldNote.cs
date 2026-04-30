using Godot;

// Notes scroll TOP → BOTTOM.
// SpawnPoint is at the top, HitZone and CleanupZone are below.
//
// Visual layout (local Y axis, Y increases downward):
//
//   TopCap    at (0, -bodyLength)   <- TAIL (release here) — starts above screen
//   Body      from (0, -bodyLength) to (0, 0)
//   BottomCap at (0, 0)             <- HEAD (press here)  — starts at SpawnPoint
//
// On initialize:   GlobalPosition = SpawnPoint  → head is exactly at spawn.
// On press:        Head Area2D is disabled so it can never reach CleanupZone.
// On hold complete: entire node is freed when the tail passes the release zone.

public partial class HoldNote : Node2D
{
	private LaneController lane;
	private RhythmManager  manager;

	private float speed;
	private float holdDuration;
	private float holdTimer = 0f;

	private bool started   = false;
	private bool completed = false;

	// HEAD (BottomCap) zone flags — judged on press
	private bool headInEarly, headInGood, headInPerfect, headInLate;

	// TAIL (TopCap) zone flags — judged on release
	private bool tailInEarly, tailInGood, tailInPerfect, tailInLate;

	private Area2D        topCap;     // tail
	private NinePatchRect body;
	private Area2D        bottomCap;  // head

	public bool IsStarted => started;

	public override void _Ready()
	{
		topCap    = GetNode<Area2D>("TopCap");
		body      = GetNode<NinePatchRect>("Body");
		bottomCap = GetNode<Area2D>("BottomCap");

		// HEAD = BottomCap, judged against press zones
		bottomCap.AreaEntered += OnHeadEntered;
		bottomCap.AreaExited  += OnHeadExited;

		// TAIL = TopCap, judged against release zones
		topCap.AreaEntered += OnTailEntered;
		topCap.AreaExited  += OnTailExited;
	}

	// Call AFTER AddChild so _Ready has already run.
	public void Initialize(
		LaneController lane,
		Vector2 spawnPos,
		Vector2 hitPos,
		float speed,
		float holdDuration
	)
	{
		this.lane         = lane;
		this.manager      = lane.GetParent<RhythmManager>();
		this.speed        = speed;
		this.holdDuration = holdDuration;

		float bodyLength = speed * holdDuration;

		// HEAD (BottomCap) at local (0, 0) — will be placed at SpawnPoint.
		bottomCap.Position = new Vector2(0f, 0f);

		// Body grows upward from the head.
		// NinePatchRect grows downward by default, so we offset it up by bodyLength.
		body.Size     = new Vector2(body.Size.X, bodyLength);
		body.Position = new Vector2(-15f, -bodyLength);

		// TAIL (TopCap) sits at the top of the body.
		topCap.Position = new Vector2(0f, -bodyLength);

		// Place the whole node so the head is exactly at the spawn point.
		GlobalPosition = spawnPos;
	}

	public override void _Process(double delta)
	{
		GlobalPosition += new Vector2(0f, speed * (float)delta);

		if (!started) return;

		bool stillHeld = Input.IsActionPressed("lane_" + lane.LaneIndex);
		if (!stillHeld)
		{
			TryRelease();
			return;
		}

		holdTimer += (float)delta;

		// Auto-complete once enough time has passed for the tail to come through.
		if (holdTimer >= holdDuration + 0.2f)
		{
			RegisterTailJudgement();
			Finish();
		}
	}

	// Called by LaneController when the player presses this lane.
	public void TryStartHold()
	{
		if (started) return;

		if (headInPerfect)
		{
			manager.RegisterPerfect();
			StartHold();
		}
		else if (headInGood)
		{
			manager.RegisterGood();
			StartHold();
		}
		else if (headInEarly || headInLate)
		{
			Miss();
		}
		// If head hasn't reached a zone yet, do nothing — too early.
	}

	private void StartHold()
	{
		started = true;

		// Disable the head area so it cannot trigger the CleanupZone.
		// The player has already pressed it — it has served its purpose.
		bottomCap.SetDeferred("monitoring", false);
		bottomCap.SetDeferred("monitorable", false);

		lane.NotifyHoldStarted(this);
		lane.HitZone.PlayHold();
	}

	private void TryRelease()
	{
		if (completed) return;
		RegisterTailJudgement();
		Finish();
	}

	private void RegisterTailJudgement()
	{
		if (tailInPerfect) {
			manager.RegisterPerfect();
			lane.HitZone.PlayHit();
		}
		else if (tailInGood) {
			manager.RegisterGood();
			lane.HitZone.PlayHit();
		}
		else {
			manager.RegisterMiss();
			lane.HitZone.StopHold();
			GD.Print("Stop hold");
		}
	}

	private void Finish()
	{
		completed = true;
		lane.RemoveHold(this);
		QueueFree();
	}

	public void Miss()
	{
		if (completed) return;
		completed = true;
		lane.HitZone.StopHold();
		manager.RegisterMiss();
		lane.RemoveHold(this);
		QueueFree();
	}

	// HEAD (BottomCap) — press zones
	private void OnHeadEntered(Area2D area)
	{
		if (area == lane.PerfectZone) headInPerfect = true;
		if (area == lane.GoodZone)    headInGood    = true;
		if (area == lane.EarlyZone)   headInEarly   = true;
		if (area == lane.LateZone)    headInLate    = true;
	}

	private void OnHeadExited(Area2D area)
	{
		if (area == lane.PerfectZone) headInPerfect = false;
		if (area == lane.GoodZone)    headInGood    = false;
		if (area == lane.EarlyZone)   headInEarly   = false;
		if (area == lane.LateZone)    headInLate    = false;
	}

	// TAIL (TopCap) — release zones
	private void OnTailEntered(Area2D area)
	{
		if (area == lane.ReleasePerfectZone) tailInPerfect = true;
		if (area == lane.ReleaseGoodZone)    tailInGood    = true;
		if (area == lane.ReleaseEarlyZone)   tailInEarly   = true;
		if (area == lane.ReleaseLateZone)    tailInLate    = true;
	}

	private void OnTailExited(Area2D area)
	{
		if (area == lane.ReleasePerfectZone) tailInPerfect = false;
		if (area == lane.ReleaseGoodZone)    tailInGood    = false;
		if (area == lane.ReleaseEarlyZone)   tailInEarly   = false;
		if (area == lane.ReleaseLateZone)    tailInLate    = false;
	}
}
