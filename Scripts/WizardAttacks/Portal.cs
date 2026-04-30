using Godot;
public partial class Portal : Area2D
{
	[Export] public float targetSpawnY = 1.7f;
	[Export] public float targetSpawnX = 0f;
	[Export] public bool spawnAlongX = true;
	[Export] public PackedScene bulletScene;
	[Export] public float spawnSpeed = 3.5f;
	[Export] public int spawnDamage = 1;
	[Export] public Vector2 spawnDirection = Vector2.Down;
	[Export] public float entryHalfHeight = 1f;
	[Export] public float entryHalfWidth = 1f;
	[Export] public bool entryAlongY = true;
	[Export] public Node2D exitPortal;
	[Export] public float exitHalfWidth = 1.5f;
	[Signal] public delegate void RelayedEventHandler();
	
	public Vector2 exitDirection;
	public bool hasRelayed = false;

	public override void _Ready()
	{
		var sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();
		AreaEntered += OnAreaEntered;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is not Bullet incoming) return;
		int dmg = incoming.GetDamage();
		float incomingSpeed = incoming.GetSpeed(); // ✅ saved but now actually used below
		Vector2 entryPos = incoming.GlobalPosition;
		incoming.QueueFree();

		if (bulletScene == null || exitPortal == null) return;

		// 🔹 Compute normalized entry position
		float norm = 0.5f;
		if (entryAlongY)
		{
			float localY = entryPos.Y - GlobalPosition.Y;
			norm = entryHalfHeight > 0f
				? Mathf.Clamp((localY + entryHalfHeight) / (2f * entryHalfHeight), 0f, 1f)
				: 0.5f;
		}
		else
		{
			float localX = entryPos.X - GlobalPosition.X;
			norm = entryHalfWidth > 0f
				? Mathf.Clamp((localX + entryHalfWidth) / (2f * entryHalfWidth), 0f, 1f)
				: 0.5f;
		}

		// 🔹 Compute spawn position
		Vector2 spawnPos;
		if (spawnAlongX)
		{
			float exitCenterX = exitPortal.GlobalPosition.X;
			// ✅ Use entryHalfHeight to mirror wall size onto the exit axis
			float exitSpread = entryHalfHeight;
			float spawnX = Mathf.Lerp(exitCenterX - exitSpread, exitCenterX + exitSpread, norm);
			spawnPos = new Vector2(spawnX, exitPortal.GlobalPosition.Y + 0.5f);
		}
		else
		{
			float exitCenterY = exitPortal.GlobalPosition.Y;
			// ✅ Use entryHalfWidth to mirror wall size onto the exit axis
			float exitSpread = entryHalfWidth;
			float spawnY = Mathf.Lerp(exitCenterY - exitSpread, exitCenterY + exitSpread, norm);
			spawnPos = new Vector2(exitPortal.GlobalPosition.X + 0.5f, spawnY);
		}

		// 🔹 Spawn bullet
		var nb = bulletScene.Instantiate<Node2D>();
		nb.ZIndex = 1;
		var sprite = nb.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();

		if (nb is Bullet nbScript)
		{
			nbScript.SetDamage(dmg);
			nbScript.Initialize(exitDirection, incomingSpeed); // ✅ exitDirection + incomingSpeed
			hasRelayed = true;
			EmitSignal(SignalName.Relayed);
		}
		

		nb.GlobalPosition = spawnPos;
		GetTree().CurrentScene.CallDeferred("add_child", nb);
		nb.CallDeferred(Node2D.MethodName.SetGlobalPosition, spawnPos);
	}
}
