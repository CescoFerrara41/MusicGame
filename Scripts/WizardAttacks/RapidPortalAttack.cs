using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class RapidPortalAttack : AttackPattern
{
	[Export] private PackedScene portalScene;
	[Export] private PackedScene bulletScene;
	[Export] private PackedScene parryBulletScene;
	
	private bool attackEnded = false;
	private int lastKnownHealth = -1;

	private enum Side { Left, Right, Top, Bottom }

	public override async Task Execute(
	PlayerSoul player,
	BattleBox box,
	EnemyManager enemyManager
)
{
	await box.SetMode(BattleBoxMode.Enemy);
	player.Spawn();

	attackEnded = false;
	lastKnownHealth = player.GetCurrentHealth();
	player.HealthChanged += OnPlayerHealthChanged;
	foreach (var enemy in enemyManager.GetAllEnemies())
	{
		enemy.Damaged += OnEnemyDamaged;
	}

	Vector2 min = box.GetInnerMinBounds();
	Vector2 max = box.GetInnerMaxBounds();

	float leftX = min.X - 7f;
	float rightX = max.X + 7f;
	float topY = min.Y - 7f;
	float bottomY = max.Y + 7f;

	float midY = (topY + bottomY) / 2f;

	Side entrySide = Side.Right;
	float entryCoord = midY;

	int chains = 5;

	for (int i = 0; i < chains; i++)
	{
		if (attackEnded) break;

		// -------------------
		// PICK EXIT SIDE
		// -------------------
		List<Side> choices = new() { Side.Left, Side.Right, Side.Top, Side.Bottom };
		choices.Remove(entrySide);
		Side exitSide = choices[GD.RandRange(0, choices.Count - 1)];

		PackedScene currentBulletScene = (i == 2) ? parryBulletScene : bulletScene;

		Vector2 exitPos = Vector2.Zero;
		Vector2 spawnDir = Vector2.Down;
		bool spawnAlongX = true;

		float margin = 8f;

		// -------------------
		// EXIT PORTAL POSITION
		// -------------------
		if (exitSide == Side.Top)
		{
			float x = (float)GD.RandRange(leftX + margin, rightX - margin);
			exitPos = new Vector2(x, topY - margin);
			spawnDir = Vector2.Down;
			spawnAlongX = true;
		}
		else if (exitSide == Side.Bottom)
		{
			float x = (float)GD.RandRange(leftX + margin, rightX - margin);
			exitPos = new Vector2(x, bottomY + margin);
			spawnDir = Vector2.Up;
			spawnAlongX = true;
		}
		else if (exitSide == Side.Left)
		{
			float y = (float)GD.RandRange(topY + margin, bottomY - margin);
			exitPos = new Vector2(leftX - margin, y);
			spawnDir = Vector2.Right;
			spawnAlongX = false;
		}
		else // Right
		{
			float y = (float)GD.RandRange(topY + margin, bottomY - margin);
			exitPos = new Vector2(rightX + margin, y);
			spawnDir = Vector2.Left;
			spawnAlongX = false;
		}

		// -------------------
		// ENTRY PORTAL POSITION
		// -------------------
		Vector2 entryPos = entrySide switch
		{
			Side.Right => new Vector2(rightX, entryCoord),
			Side.Left => new Vector2(leftX + 4f, entryCoord),
			Side.Top => new Vector2(entryCoord, topY + 4f),
			Side.Bottom => new Vector2(entryCoord, bottomY),
			_ => Vector2.Zero
		};

		// -------------------
		// SPAWN ENTRY PORTAL
		// -------------------
		var entryPortal = portalScene.Instantiate<Portal>();
		entryPortal.GlobalPosition = entryPos;
		AddChild(entryPortal);

		entryPortal.bulletScene = currentBulletScene;
		entryPortal.entryAlongY = (entrySide == Side.Left || entrySide == Side.Right);
		entryPortal.spawnAlongX = spawnAlongX;
		entryPortal.exitDirection = spawnDir;

		entryPortal.Rotation = GetEntryRotation(
			entrySide switch
			{
				Side.Right => Vector2.Right,
				Side.Left => Vector2.Left,
				Side.Top => Vector2.Up,
				Side.Bottom => Vector2.Down,
				_ => Vector2.Right
			}
		);

		// delay before exit portal appears
		await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
		if (attackEnded) break;

		// -------------------
		// SPAWN EXIT PORTAL
		// -------------------
		var exitPortal = portalScene.Instantiate<Portal>();
		exitPortal.GlobalPosition = exitPos;
		AddChild(exitPortal);

		exitPortal.Rotation = GetExitRotation(spawnDir);

		entryPortal.exitPortal = exitPortal;

		// -------------------
		// FIRST BULLET
		// -------------------
		if (i == 0)
		{
			SpawnBullet(new Vector2(leftX, entryCoord), Vector2.Right);
		}

		// -------------------
		// SAFE WAIT FOR RELAY
		// -------------------
		while (!entryPortal.hasRelayed && !attackEnded)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (attackEnded) break;

		// -------------------
		// DETERMINE NEXT ENTRY
		// -------------------
		entrySide = DirectionToSide(spawnDir);

		if (entrySide == Side.Left || entrySide == Side.Right)
			entryCoord = exitPortal.GlobalPosition.Y;
		else
			entryCoord = exitPortal.GlobalPosition.X;

		// cleanup
		entryPortal.QueueFree();
		exitPortal.QueueFree();

		await ToSignal(GetTree().CreateTimer(0.12f), SceneTreeTimer.SignalName.Timeout);
		if (attackEnded) break;
	}

	attackEnded = true;

	// clean up portals
	foreach (var child in GetChildren())
	{
		if (child is Portal p)
			p.QueueFree();
	}

	// disconnect signal
	player.HealthChanged -= OnPlayerHealthChanged;
	foreach (var enemy in enemyManager.GetAllEnemies())
	{
		enemy.Damaged -= OnEnemyDamaged;
	}

	await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
}

	private float GetEntryRotation(Vector2 incomingDir)
{
	return Mathf.Atan2(incomingDir.Y, incomingDir.X) - Mathf.Pi / 2f;
}

private float GetExitRotation(Vector2 outgoingDir)
{
	Vector2 facing = -outgoingDir;
	return Mathf.Atan2(facing.Y, facing.X) - Mathf.Pi / 2f;
}

	private Side DirectionToSide(Vector2 dir)
	{
		if (dir == Vector2.Down) return Side.Bottom;
		if (dir == Vector2.Up) return Side.Top;
		if (dir == Vector2.Right) return Side.Right;
		return Side.Left;
	}

	private void SpawnBullet(Vector2 pos, Vector2 dir)
	{
		var bullet = bulletScene.Instantiate<Bullet>();
		bullet.GlobalPosition = pos;
		bullet.Initialize(dir, 80f);
		AddChild(bullet);
	}
	
	private void OnPlayerHealthChanged(int current, int max)
	{
		if (lastKnownHealth == -1)
		{
			lastKnownHealth = current;
			return;
		}

		if (current < lastKnownHealth)
		{
			attackEnded = true;
		}

		lastKnownHealth = current;
	}
	
	
	private void OnEnemyDamaged(int damage)
	{
		attackEnded = true;
	}
}
