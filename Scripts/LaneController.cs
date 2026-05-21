using Godot;
using System.Collections.Generic;

public partial class LaneController : Node2D
{
	[Export] public int     LaneIndex;
	[Export] public Node2D  SpawnPoint;
	[Export] public HitZone HitZone;

	// Press zones — head (BottomCap) is judged against these
	[Export] public Area2D EarlyZone;
	[Export] public Area2D GoodZone;
	[Export] public Area2D PerfectZone;
	[Export] public Area2D LateZone;
	[Export] public Area2D GoodZoneLate;

	// Release zones — tail (TopCap) is judged against these
	[Export] public Area2D ReleaseEarlyZone;
	[Export] public Area2D ReleaseGoodZone;
	[Export] public Area2D ReleaseGoodZoneLate;
	[Export] public Area2D ReleasePerfectZone;
	[Export] public Area2D ReleaseLateZone;

	// Notes that scroll past this without being hit are misses
	[Export] public Area2D CleanupZone;

	[Export] public PackedScene NoteScene;
	[Export] public PackedScene HoldNoteScene;

	[Export] public float NoteSpeed = 450f;

	private RhythmManager    manager;
	private List<RhythmNote> notes       = new();
	private List<HoldNote>   activeHolds = new();

	public override void _Ready()
	{
		manager = GetParent<RhythmManager>();
		CleanupZone.AreaEntered += OnCleanupEntered;
	}

	// -------------------------------------------------------------------------
	// Spawning
	// -------------------------------------------------------------------------

	public void SpawnNote(float delay, bool isHold, float holdDuration)
	{
		if (isHold) SpawnHoldAsync(delay, holdDuration);
		else        SpawnTapAsync(delay);
	}

	private async void SpawnTapAsync(float delay)
	{
		await ToSignal(GetTree().CreateTimer(delay), "timeout");

		RhythmNote note = NoteScene.Instantiate<RhythmNote>();
		
		AddChild(note);
		note.Initialize(this, LaneIndex, SpawnPoint.GlobalPosition, HitZone.GlobalPosition, NoteSpeed);
		note.ZIndex = 0;
		note.ZAsRelative = true;
		notes.Add(note);
	}

	private async void SpawnHoldAsync(float delay, float holdDuration)
	{
		await ToSignal(GetTree().CreateTimer(delay), "timeout");

		HoldNote note = HoldNoteScene.Instantiate<HoldNote>();
		
		AddChild(note); // _Ready runs here
		note.Initialize(this, SpawnPoint.GlobalPosition, HitZone.GlobalPosition, NoteSpeed, holdDuration);
		note.ZIndex = 0;
		note.ZAsRelative = true;
		activeHolds.Add(note);
	}

	// -------------------------------------------------------------------------
	// Input routing
	// -------------------------------------------------------------------------

	public void HandleCorrectPress()
	{
		// Priority: un-started hold notes
		foreach (var hold in activeHolds)
		{
			if (!hold.IsStarted)
			{
				hold.TryStartHold();
				return;
			}
		}

		// Tap notes
		RhythmNote note = GetPriorityNote();
		if (note == null) return;

		if (note.InPerfect)
		{
			manager.RegisterPerfect();
			note.Hit();
			HitZone.PlayHit();
		}
		else if (note.InGood)
		{
			manager.RegisterGood();
			note.Hit();
			HitZone.PlayHit();
		}
		else
		{
			manager.RegisterMiss();
		}
	}

	public void HandleWrongPress()
	{
		if (HasHittableNote())
			manager.RegisterMiss();
	}

	public bool HasHittableNote()
	{
		foreach (var hold in activeHolds)
			if (!hold.IsStarted) return true;

		RhythmNote note = GetPriorityNote();
		if (note == null) return false;
		return note.InPerfect || note.InGood || note.InEarly || note.InLate;
	}

	// -------------------------------------------------------------------------
	// Cleanup zone
	// -------------------------------------------------------------------------

	private void OnCleanupEntered(Area2D area)
	{
		// The area's immediate parent is the note node in both cases.
		Node parent = area.GetParent();

		if (parent is RhythmNote note)
		{
			manager.RegisterMiss();
			note.Miss();
			return;
		}

		if (parent is HoldNote hold)
		{
			// Only miss un-started holds. Started holds have their head
			// disabled (monitoring = false) so this branch won't fire for
			// them — but guard anyway for safety.
			if (!hold.IsStarted)
				hold.Miss();
		}
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	public void RemoveNote(RhythmNote note) => notes.Remove(note);
	public void RemoveHold(HoldNote hold)   => activeHolds.Remove(hold);
	public void NotifyHoldStarted(HoldNote hold) { }

	public float GetTravelTime()
	{
		return SpawnPoint.GlobalPosition.DistanceTo(HitZone.GlobalPosition) / NoteSpeed;
	}

	private RhythmNote GetPriorityNote()
	{
		RhythmNote best     = null;
		float      bestDist = float.MaxValue;
		foreach (var n in notes)
		{
			float d = n.DistanceToHitZone();
			if (d < bestDist) { bestDist = d; best = n; }
		}
		return best;
	}
}
