using Godot;
using System.Threading.Tasks;

/// <summary>
/// Attach to a Node2D scene that has an AnimatedSprite2D child.
/// Animations expected:
///   "default"  — idle state
///   "activate" — flashes red twice (plays once, then returns to default)
///
/// Rotation by wall (sprite defaults to facing down = Top wall):
///   Top    →   0°
///   Bottom → 180°
///   Left   → -90°
///   Right  →  90°
/// </summary>
public partial class TaserProbe : Node2D
{
	public enum Wall { Top, Bottom, Left, Right }

	[Export] public Wall AnchoredWall = Wall.Top;

	// How far outside the box the probe spawns before sliding in.
	[Export] private float entryOffset = 40f;

	// How long the slide-in tween takes.
	[Export] private float entryDuration = 0.4f;

	[Signal] public delegate void ProbeActivatedEventHandler(int wallIndex);

	private AnimatedSprite2D sprite;
	private bool isActivating = false;

	// Set after Initialize() — used to track the box every frame.
	private BattleBox box;
	private bool followBox = false;

	public override void _Ready()
	{
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (sprite != null)
			sprite.Play("default");
	}

	public override void _Process(double delta)
	{
		if (!followBox || !IsInstanceValid(box)) return;

		// Snap to the correct wall position every frame so the probe
		// moves in lockstep with the box during tweens.
		GlobalPosition = GetWallPosition();
	}

	/// <summary>
	/// Called by TaserAttack after setting AnchoredWall.
	/// Rotates the probe, offsets it outside the box, then tweens it to the wall.
	/// Returns a Task that completes when the probe has snapped into place.
	/// </summary>
	public async Task Initialize(Vector2 wallPos, BattleBox battleBox)
	{
		box = battleBox;

		// Apply rotation now that AnchoredWall is set
		RotationDegrees = AnchoredWall switch
		{
			Wall.Top    =>   0f,
			Wall.Bottom => 180f,
			Wall.Left   => -90f,
			Wall.Right  =>  90f,
			_           =>   0f
		};

		// Spawn offset — push the probe outward from its wall before sliding in
		Vector2 offsetDir = AnchoredWall switch
		{
			Wall.Top    => new Vector2(0f, -1f),
			Wall.Bottom => new Vector2(0f,  1f),
			Wall.Left   => new Vector2(-1f, 0f),
			Wall.Right  => new Vector2( 1f, 0f),
			_           => Vector2.Zero
		};

		GlobalPosition = wallPos + offsetDir * entryOffset;

		// Slide into position
		var tween = CreateTween();
		tween.TweenProperty(this, "global_position", wallPos, entryDuration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);

		// Start following the box every frame now that we've connected
		followBox = true;
	}

	/// <summary>
	/// Plays the "activate" animation once, waits for it to finish,
	/// returns to "default", then emits ProbeActivated.
	/// </summary>
	public async Task Activate()
	{
		if (isActivating) return;
		isActivating = true;

		if (sprite != null)
		{
			sprite.Play("activate");
			await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
			sprite.Play("default");
		}

		EmitSignal(SignalName.ProbeActivated, (int)AnchoredWall);

		isActivating = false;
	}

	// Returns the correct wall-center position based on current box bounds.
	private Vector2 GetWallPosition()
	{
		Vector2 min    = box.GetInnerMinBounds();
		Vector2 max    = box.GetInnerMaxBounds();
		Vector2 center = box.GlobalPosition;

		return AnchoredWall switch
		{
			Wall.Top    => new Vector2(center.X, min.Y),
			Wall.Bottom => new Vector2(center.X, max.Y),
			Wall.Left   => new Vector2(min.X,    center.Y),
			Wall.Right  => new Vector2(max.X,    center.Y),
			_           => GlobalPosition
		};
	}

	public Vector2 GetMoveDirection()
	{
		return AnchoredWall switch
		{
			Wall.Top    => new Vector2(0f, -1f),
			Wall.Bottom => new Vector2(0f,  1f),
			Wall.Left   => new Vector2(-1f, 0f),
			Wall.Right  => new Vector2( 1f, 0f),
			_           => Vector2.Zero
		};
	}
}
