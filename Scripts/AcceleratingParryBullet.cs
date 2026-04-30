using Godot;
using System.Threading.Tasks;

public partial class AcceleratingParryBullet : ParryBullet
{
	[Export] private float startSpeed = 25f;
	[Export] private float acceleration = 80f;
	[Export] private float rotationOffsetDegrees = 0f;

	private float currentSpeed = 0f;
	private float maxSpeed = 0f;
	private Vector2 moveDirection;

	public void InitializeAccelerating(Vector2 direction, float topSpeed)
	{
		moveDirection = direction.Normalized();
		currentSpeed  = startSpeed;
		maxSpeed      = topSpeed;

		base.Initialize(direction, topSpeed);

		Rotation += Mathf.DegToRad(rotationOffsetDegrees);
	}

	public override void _Process(double delta)
	{
		if (!parried)
		{
			CheckParryInput();
		}

		if (IsParried())
		{
			base._Process(delta);
			return;
		}

		if (currentSpeed < maxSpeed)
			currentSpeed = Mathf.Min(currentSpeed + acceleration * (float)delta, maxSpeed);

		GlobalPosition += moveDirection * currentSpeed * (float)delta;
	}

	protected override async Task DoParry()
	{
		parried = true;
		canBeParried = false;

		EmitSignal(SignalName.Parried);

		EnemyDisplay target = enemyManager?.GetFirstAliveEnemy();
		if (target != null)
		{
			Vector2 dir = (target.GlobalPosition - GlobalPosition).Normalized();
			Initialize(dir, parrySpeed);
			Rotation += Mathf.DegToRad(90f);
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
}
