using Godot;

public partial class ElectrifiedWall : Area2D
{
	[Export] private int   damageAmount   = 1;
	[Export] private float damageTickRate = 0.15f;

	private PlayerSoul      player;
	private BattleBox       box;
	private TaserProbe.Wall wall;

	private bool  isPlayerInside = false;
	private float lastDamageTime = 0f;

	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (sprite != null)
			sprite.Play("default");

		AreaEntered += OnAreaEntered;
		AreaExited  += OnAreaExited;
	}

	public void SetTarget(PlayerSoul targetPlayer, BattleBox battleBox, TaserProbe.Wall anchoredWall)
	{
		player = targetPlayer;
		box    = battleBox;
		wall   = anchoredWall;
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(box)) return;

		FollowWall();

		if (!isPlayerInside || !IsInstanceValid(player)) return;

		float now = Time.GetTicksMsec() / 1000f;
		if (now - lastDamageTime < damageTickRate) return;

		player.TakeDamage(damageAmount, ignoreInvincibility: true);
		lastDamageTime = now;
	}

	private void FollowWall()
	{
		Vector2 min    = box.GetInnerMinBounds();
		Vector2 max    = box.GetInnerMaxBounds();
		Vector2 center = box.GlobalPosition;
		float   boxW   = max.X - min.X;
		float   boxH   = max.Y - min.Y;

		switch (wall)
		{
			case TaserProbe.Wall.Top:
				GlobalPosition = new Vector2(center.X, min.Y);
				break;

			case TaserProbe.Wall.Bottom:
				GlobalPosition = new Vector2(center.X, max.Y + 1f);
				break;

			case TaserProbe.Wall.Left:
				GlobalPosition = new Vector2(min.X, center.Y);
				break;

			case TaserProbe.Wall.Right:
				GlobalPosition = new Vector2(max.X + 1f, center.Y);
				break;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is PlayerSoul) isPlayerInside = true;
	}

	private void OnAreaExited(Area2D area)
	{
		if (area is PlayerSoul) isPlayerInside = false;
	}
}
