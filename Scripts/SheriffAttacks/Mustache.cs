using Godot;
using System.Threading.Tasks;

public enum MustacheSide { Left, Right }

public partial class Mustache : Area2D
{
	[Export] public MustacheSide Side = MustacheSide.Left;
	[Export] private float bobSpeed = 40f;
	[Export] private float detectionThreshold = 5f;
	[Export] private float holdDuration = 0.4f;
	[Export] private int damage = 1;

	[Export] private AnimatedSprite2D sprite;
	[Export] private AnimationPlayer animPlayer;
	[Export] private CollisionShape2D hitbox;

	private BattleBox box;
	private PlayerSoul player;

	private float bobDir = 1f;
	private bool isStriking = false;
	private bool isRetracting = false;
	private float restX;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		//sprite.FlipH = (Side == MustacheSide.Right);

		// Make the collision shape unique to this instance so that AnimationPlayer
		// keyframes on one mustache don't affect the other's shared shape resource
		hitbox.Shape = hitbox.Shape.Duplicate() as Shape2D;
	}

	public void Initialize(BattleBox battleBox, PlayerSoul playerSoul)
	{
		box    = battleBox;
		player = playerSoul;
	}

	public void SetRestX(float x)
	{
		restX = x;
		GlobalPosition = new Vector2(restX, GlobalPosition.Y);
	}

	public void SetInitialBobDirection(float dir)
	{
		bobDir = Mathf.Sign(dir);
	}

	public override void _Process(double delta)
	{
		if (box == null || player == null) return;
		if (isStriking) return;

		Bob(delta);
		CheckAlignment();
	}

	private void Bob(double delta)
	{
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		Vector2 pos = GlobalPosition;
		pos.Y += bobDir * bobSpeed * (float)delta;

		if      (pos.Y >= max.Y) { pos.Y = max.Y; bobDir = -1f; }
		else if (pos.Y <= min.Y) { pos.Y = min.Y; bobDir =  1f; }

		GlobalPosition = pos;
	}

	private void CheckAlignment()
	{
		if (player == null || !player.Visible) return;

		float yDiff = Mathf.Abs(GlobalPosition.Y - player.GlobalPosition.Y);
		if (yDiff > detectionThreshold) return;

		bool playerOnLeft = player.GlobalPosition.X < box.GlobalPosition.X;
		if (Side == MustacheSide.Left  && !playerOnLeft) return;
		if (Side == MustacheSide.Right &&  playerOnLeft) return;

		_ = Strike();
	}

	private async Task Strike()
	{
		if (isStriking) return;
		isStriking = true;
		isRetracting = false;

		animPlayer.Play("strike");
		await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);

		await ToSignal(GetTree().CreateTimer(holdDuration), "timeout");

		isRetracting = true;
		animPlayer.PlayBackwards("strike");
		await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);

		isRetracting = false;
		isStriking = false;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (!isStriking) return;
		//if (isRetracting) return;

		if (area.IsInGroup("Player") && area is PlayerSoul soul)
		{
			soul.TakeDamage(damage);
		}
	}
}
