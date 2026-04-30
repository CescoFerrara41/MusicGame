using Godot;
using System.Threading.Tasks;

public partial class ParryBullet : Bullet
{
	[Export] protected int parryDamage = 1;
	[Export] protected float parrySpeed = 400f;

	protected bool canBeParried = false;
	protected bool parried = false;

	protected EnemyManager enemyManager;
	private PlayerSoul playerSoul;

	protected Area2D parryArea;
	[Signal] public delegate void ParriedEventHandler();

	public override void _Ready()
	{
		base._Ready();

		parryArea = GetNode<Area2D>("ParryArea");

		parryArea.AreaEntered += OnParryAreaEntered;
		parryArea.AreaExited += OnParryAreaExited;
		enemyManager = GetNode<EnemyManager>("../EnemyManager");
		playerSoul = GetNode<PlayerSoul>("../PlayerSoul");
	}

	public void SetEnemyManager(EnemyManager em)
	{
		enemyManager = em;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (!parried)
		{
			CheckParryInput();
		}
	}

	private void OnParryAreaEntered(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			canBeParried = true;
			Modulate = new Color(1f, 1f, 0.5f);
		}
	}

	private void OnParryAreaExited(Area2D area)
	{
		if (area.IsInGroup("Player"))
		{
			canBeParried = false;
			if (!parried)
				Modulate = new Color(1f, 1f, 1f);
		}
	}

	protected void CheckParryInput()
	{
		if (playerSoul == null) return;
		if (playerSoul.IsInvincible()) return;

		if (canBeParried && Input.IsActionJustPressed("interact"))
		{
			_ = DoParry();
		}
	}

	protected virtual async Task DoParry()
	{
		parried = true;
		canBeParried = false;

		EmitSignal(SignalName.Parried);

		EnemyDisplay target = enemyManager?.GetFirstAliveEnemy();
		if (target != null)
		{
			Vector2 dir = (target.GlobalPosition - GlobalPosition).Normalized();
			Initialize(dir, parrySpeed);
		}
		else
		{
			Initialize(Vector2.Up, parrySpeed);
		}

		SetDamage(parryDamage);

		Modulate = new Color(0.5f, 1f, 1f);
		parryArea.Monitoring = false;

		Engine.TimeScale = 0.05f;
		await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
		Engine.TimeScale = 1f;
	}

	public bool IsParried()
	{
		return parried;
	}
}
