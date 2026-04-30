using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class RhythmManager : Node2D
{
	[Export] public LaneController[]  Lanes;
	[Export] public AudioStreamPlayer  MusicPlayer;
	[Export] public JudgementDisplay  Judgement;

	[Export] public float BPM             = 120f;
	[Export] public float SequenceDuration = 20f;  // total seconds of notes to spawn

	// --- Double note settings ---
	[Export] public bool  AllowDoubleNotes  = true;
	[Export] public float DoubleNoteChance  = 0.2f;

	// --- Hold note settings ---
	[Export] public bool  AllowHoldNotes  = true;
	[Export] public float HoldNoteChance  = 0.3f;
	[Export] public int   MinHoldBeats    = 1;
	[Export] public int   MaxHoldBeats    = 3;

	private float SecondsPerBeat => 60f / BPM;

	// Scoring
	private int totalNotes;
	private int perfectHits;
	private int goodHits;
	private int misses;

	private bool  running;
	private float timer;
	private float sequenceStartTime;

	private float lastSongTime = 0f;
	private int   loopCount    = 0;
	private float songLength;

	private TaskCompletionSource<bool> sequenceTask;

	// -------------------------------------------------------------------------
	// Pattern data
	// -------------------------------------------------------------------------

	private class NoteEvent
	{
		public int   Lane;
		public bool  IsHold;
		public float HoldDuration;
		public float HitTime;      // seconds from sequence start when head reaches hit zone
	}

	public override void _Ready()
	{
		SetLaneVisibility(false);
		songLength = (float)MusicPlayer.Stream.GetLength();
	}

	// -------------------------------------------------------------------------
	// Public API
	// -------------------------------------------------------------------------

	public Task StartRhythmSequence()
	{
		sequenceTask = new TaskCompletionSource<bool>();

		perfectHits = 0;
		goodHits    = 0;
		misses      = 0;
		totalNotes  = 0;
		timer       = 0f;
		loopCount   = 0;
		lastSongTime = 0f;
		running     = true;

		float maxTravel      = GetMaxTravelTime();
		float spb            = SecondsPerBeat;
		int   laneCount      = Lanes.Length;
		int   beatCount      = Mathf.CeilToInt(SequenceDuration / spb);

		// We'll track, per lane, the earliest beat at which a new note may start
		// so that hold notes don't overlap subsequent notes in the same lane.
		int[] laneNextFreeBeat = new int[laneCount];

		var events = new List<NoteEvent>();

		for (int beat = 0; beat < beatCount; beat++)
		{
			float hitTime = beat * spb + maxTravel;

			bool tryDouble = AllowDoubleNotes && GD.Randf() < DoubleNoteChance;
			bool tryHold   = AllowHoldNotes   && GD.Randf() < HoldNoteChance;

			// Pick primary lane — must be free on this beat.
			int primaryLane = PickFreeLane(laneNextFreeBeat, beat, laneCount, -1);
			if (primaryLane == -1) continue; // all lanes busy, skip beat

			int holdBeats = 0;
			float holdDuration = 0f;

			if (tryHold)
			{
				holdBeats    = GD.RandRange(MinHoldBeats, MaxHoldBeats);
				holdDuration = holdBeats * spb;
			}

			events.Add(new NoteEvent
			{
				Lane         = primaryLane,
				IsHold       = tryHold,
				HoldDuration = holdDuration,
				HitTime      = hitTime
			});
			// Hold notes are judged twice (head press + tail release),
			// so they count as 2 towards the denominator.
			totalNotes += tryHold ? 2 : 1;

			// Mark this lane as busy for the duration of the hold (+1 beat buffer).
			laneNextFreeBeat[primaryLane] = beat + holdBeats + 1;

			// Optionally add a second simultaneous note in a different lane.
			if (tryDouble && !tryHold) // doubles on hold beats get messy, skip
			{
				int secondLane = PickFreeLane(laneNextFreeBeat, beat, laneCount, primaryLane);
				if (secondLane != -1)
				{
					events.Add(new NoteEvent
					{
						Lane         = secondLane,
						IsHold       = false,
						HoldDuration = 0f,
						HitTime      = hitTime
					});
					totalNotes++;
					laneNextFreeBeat[secondLane] = beat + 1;
				}
			}
		}

		// sequenceStartTime is "what song position corresponds to beat 0 hitting the zone".
		sequenceStartTime = MusicPlayer.GetPlaybackPosition() - maxTravel;

		// Schedule every note.
		foreach (var ev in events)
		{
			float travelTime = Lanes[ev.Lane].GetTravelTime();
			float spawnDelay = ev.HitTime - travelTime;

			Lanes[ev.Lane].SpawnNote(spawnDelay, ev.IsHold, ev.HoldDuration);
		}

		return sequenceTask.Task;
	}

	public float GetAccuracy()
	{
		if (totalNotes == 0) return 0f;
		return (perfectHits * 1.0f + goodHits * 0.6f) / totalNotes;
	}

	// -------------------------------------------------------------------------
	// Per-frame
	// -------------------------------------------------------------------------

	public override void _Process(double delta)
	{
		if (!running) return;

		// Handle song looping for accurate elapsed time.
		float songTime = MusicPlayer.GetPlaybackPosition();
		if (songTime < lastSongTime) loopCount++;
		lastSongTime = songTime;

		float absoluteTime = songTime + loopCount * songLength;
		timer = absoluteTime - sequenceStartTime;

		// Route lane input.
		for (int i = 0; i < Lanes.Length; i++)
		{
			if (Input.IsActionJustPressed("lane_" + i))
				RouteLanePress(i);
		}

		// End ~3 s after last note should have appeared.
		if (timer >= SequenceDuration + 3f)
			EndSequence();
	}

	private void RouteLanePress(int pressedLane)
	{
		// Collect all lanes that currently have something hittable.
		var activeLanes = new List<int>();
		for (int i = 0; i < Lanes.Length; i++)
			if (Lanes[i].HasHittableNote())
				activeLanes.Add(i);

		if (activeLanes.Contains(pressedLane))
		{
			Lanes[pressedLane].HandleCorrectPress();
		}
		else
		{
			// Wrong lane — penalise every lane that had something hittable.
			foreach (int lane in activeLanes)
				Lanes[lane].HandleWrongPress();
		}
	}

	// -------------------------------------------------------------------------
	// Scoring callbacks (called by notes / lanes)
	// -------------------------------------------------------------------------

	public void RegisterPerfect() { perfectHits++; Judgement?.ShowPerfect(); }
	public void RegisterGood()    { goodHits++;    Judgement?.ShowGood();    }
	public void RegisterMiss()    { misses++;      Judgement?.ShowMiss();    }

	// -------------------------------------------------------------------------
	// Visibility / fade helpers
	// -------------------------------------------------------------------------

	public async Task FadeIn()
	{
		SetLaneVisibility(true);
		Modulate = new Color(1, 1, 1, 0f);
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 1f, 0.5f);
		await ToSignal(tween, "finished");
	}

	public async Task FadeOut()
	{
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0f, 0.5f);
		await ToSignal(tween, "finished");
	}

	private void SetLaneVisibility(bool visible)
	{
		foreach (var lane in Lanes)
			lane.Visible = visible;
		Visible = visible;
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private float GetMaxTravelTime()
	{
		float max = 0f;
		foreach (var lane in Lanes)
		{
			float t = lane.GetTravelTime();
			if (t > max) max = t;
		}
		return max;
	}

	// Returns a lane index that is free at `beat` and is not `excludeLane`.
	// Returns -1 if no free lane exists.
	private static int PickFreeLane(int[] laneNextFreeBeat, int beat, int laneCount, int excludeLane)
	{
		// Collect free candidates.
		var candidates = new List<int>();
		for (int i = 0; i < laneCount; i++)
		{
			if (i == excludeLane) continue;
			if (laneNextFreeBeat[i] <= beat) candidates.Add(i);
		}

		if (candidates.Count == 0) return -1;

		return candidates[GD.RandRange(0, candidates.Count - 1)];
	}

	private void EndSequence()
	{
		running = false;
		GD.Print($"Sequence complete — accuracy: {GetAccuracy():P1}  " +
				 $"(P:{perfectHits} G:{goodHits} M:{misses} / {totalNotes} notes)");
		sequenceTask?.SetResult(true);
	}
}
