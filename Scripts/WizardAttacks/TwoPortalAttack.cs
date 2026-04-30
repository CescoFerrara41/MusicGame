using Godot;
using System.Threading.Tasks;

public partial class TwoPortalAttack : AttackPattern
{
	[Export] private PackedScene bulletScene;
	[Export] private PackedScene portalScene; // Single portal scene, normally faces upward

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.WideEnemy);
		player.Spawn();

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float leftX = min.X - 0.5f;
		float rightX = max.X + 0.5f;

		float topY = min.Y;
		float bottomY = max.Y;
		float midY = Mathf.Lerp(topY, bottomY, 0.5f);

		// =========================
		// 🔹 SPAWN PORTALS
		// =========================

		// Entry portals on the right wall, rotated to face LEFT (bullets enter from right)
		// Portal faces up by default → rotate -90° (Pi/2) to face left
		var rightTopPortal = portalScene.Instantiate<Node2D>();
		var rightBottomPortal = portalScene.Instantiate<Node2D>();

		GetTree().CurrentScene.AddChild(rightTopPortal);
		GetTree().CurrentScene.AddChild(rightBottomPortal);

		rightTopPortal.GlobalPosition = new Vector2(rightX+5f, (topY + midY) / 2f);
		rightBottomPortal.GlobalPosition = new Vector2(rightX+5f, (midY + bottomY) / 2f);

		rightTopPortal.Rotation = -Mathf.Pi / 2f;    // faces left (bullets travel right → enter here)
		rightBottomPortal.Rotation = -Mathf.Pi / 2f;

		// Exit portals on the top wall, rotated to face DOWN (bullets exit downward)
		// Portal faces up by default → rotate 180° (Pi) to face down
		var topVisualA = portalScene.Instantiate<Node2D>();
		var topVisualB = portalScene.Instantiate<Node2D>();

		GetTree().CurrentScene.AddChild(topVisualA);
		GetTree().CurrentScene.AddChild(topVisualB);

		float portalY = topY - 0.5f;
		float leftPortalX  = Mathf.Lerp(min.X, max.X, 0.25f);
		float rightPortalX = Mathf.Lerp(min.X, max.X, 0.75f);

		topVisualA.GlobalPosition = new Vector2(leftPortalX-3f,  portalY-5f);
		topVisualB.GlobalPosition = new Vector2(rightPortalX, portalY-5f);
		
		rightTopPortal.ZIndex = 1;
		rightBottomPortal.ZIndex = 1;
		topVisualA.ZIndex = 1;
		topVisualB.ZIndex = 1;

		topVisualA.Rotation = Mathf.Pi;  // faces down
		topVisualB.Rotation = Mathf.Pi;  // faces down

		// =========================
		// 🔥 GET PORTAL SCRIPTS
		// =========================

		var topRelay    = rightTopPortal    as Portal;
		var bottomRelay = rightBottomPortal as Portal;

		if (topRelay == null || bottomRelay == null)
		{
			GD.PrintErr("Portal script missing on portal scene!");
			return;
		}

		// =========================
		// 🔹 CONFIGURE PORTALS
		// =========================

		topRelay.bulletScene    = bulletScene;
		bottomRelay.bulletScene = bulletScene;

		// Bullets exit from the top and travel downward
		topRelay.exitDirection    = Vector2.Down;
		bottomRelay.exitDirection = Vector2.Down;

		// Exit speed (can tweak)
		topRelay.spawnSpeed    = 3.5f;
		bottomRelay.spawnSpeed = 3.5f;

		// The entry portal spans half the box height each
		topRelay.entryHalfHeight    = Mathf.Abs(topY - midY) / 2f;
		bottomRelay.entryHalfHeight = Mathf.Abs(midY - bottomY) / 2f;

		// Link entry → exit
		topRelay.exitPortal    = topVisualA;
		bottomRelay.exitPortal = topVisualB;

		// Exit portal half-width (used for remapping bullet X position)
		topRelay.exitHalfWidth    = 1.5f;
		bottomRelay.exitHalfWidth = 1.5f;

		// =========================
		// 🔹 WALL PARAMETERS
		// =========================

		int buffers     = 2;
		int columns     = 10;
		int rows        = 6;
		float columnDelay = 0.08f;
		float bufferDelay = 0.5f;
		float bulletSpeed = 40f;

		// =========================
		// 🔹 TOP HALF WALLS
		// =========================

		for (int b = 0; b < buffers; b++)
		{
			for (int c = 0; c < columns; c++)
			{
				for (int r = 0; r < rows; r++)
				{
					float tr = (rows == 1) ? 0f : (float)r / (rows - 1);
					// tr=0 → topY edge, tr=1 → midY edge
					// Portal will remap this relative position to the exit portal's width
					float y = Mathf.Lerp(topY + 5f, midY - 5f, tr);
					SpawnBullet(new Vector2(leftX, y), Vector2.Right, bulletSpeed);
				}

				await ToSignal(GetTree().CreateTimer(columnDelay), "timeout");
			}

			await ToSignal(GetTree().CreateTimer(bufferDelay), "timeout");
		}

		await ToSignal(GetTree().CreateTimer(0.25f), "timeout");

		// =========================
		// 🔹 BOTTOM HALF WALLS
		// =========================

		for (int b = 0; b < buffers; b++)
		{
			for (int c = 0; c < columns; c++)
			{
				for (int r = 0; r < rows; r++)
				{
					float tr = (rows == 1) ? 0f : (float)r / (rows - 1);
					float y = Mathf.Lerp(midY + 5f, bottomY - 5f, tr);
					SpawnBullet(new Vector2(leftX, y), Vector2.Right, bulletSpeed);
				}

				await ToSignal(GetTree().CreateTimer(columnDelay), "timeout");
			}

			await ToSignal(GetTree().CreateTimer(bufferDelay), "timeout");
		}

		await ToSignal(GetTree().CreateTimer(2f), "timeout");

		// =========================
		// 🔹 CLEANUP
		// =========================

		rightTopPortal.QueueFree();
		rightBottomPortal.QueueFree();
		topVisualA.QueueFree();
		topVisualB.QueueFree();

		await ToSignal(GetTree().CreateTimer(2f), "timeout");
	}

	// =========================
	// 🔹 BULLET SPAWNER
	// =========================

	private void SpawnBullet(Vector2 position, Vector2 direction, float speed)
	{
		var bullet = bulletScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;

		var sprite = bullet.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();

		if (bullet is Bullet b)
		{
			b.Initialize(direction, speed);
		}
	}
}
