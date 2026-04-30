using Godot;

public partial class LaneHitZone : Area2D
{
	[Export] public string ZoneType;
	

	public override void _Ready()
	{
		AreaEntered += Entered;
		AreaExited += Exited;
	}

	private void Entered(Area2D area)
	{
		if (area.GetParent() is RhythmNote note)
		{
			GD.Print("Entered zone: " + ZoneType);
			SetZone(note, true);
		}
	}

	private void Exited(Area2D area)
	{
		if (area.GetParent() is RhythmNote note)
		{
			SetZone(note, false);
		}
	}

	private void SetZone(RhythmNote note, bool value)
	{
		switch (ZoneType)
		{
			case "early": note.InEarly = value; break;
			case "good": note.InGood = value; break;
			case "perfect": note.InPerfect = value; break;
			case "late": note.InLate = value; break;
		}
	}
}
