using Godot;
using System.Threading.Tasks;

/// <summary>
/// The alien boss that hovers at the top of the BattleBox.
/// – Moves horizontally in an erratic pattern (random bursts of speed/direction).
/// – Can be hit by PlayerBullets; after 10 hits the attack ends.
/// – Periodically drops eggs aimed at the player (regular and parry variants).
/// – Periodically triggers a UfoCowPull to abduct a cow from the bottom of the box.
/// 
/// Wire up in the editor:
///   • alienSprite         – AnimatedSprite2D child
///   • hitArea             – Area2D child in group "EnemyHittable" (or connect AreaEntered manually)
///   • eggProjectileScene  – EggProjectile PackedScene
///   • parryEggScene       – ParryEgg PackedScene
///   • ufoCowPullScene     – UfoCowPull PackedScene
/// </summary>
public partial class AlienBoss : Area2D
{
	// ── Exports ────────────────────────────────────────────────────────────────
	[Export] private AnimatedSprite2D alienSprite;

	[Export] private PackedScene eggProjectileScene;
	[Export] private PackedScene parryEggScene;
	[Export] private PackedScene ufoCowPullScene;
	[Export] private PackedScene laserEggScene;

	[Export] private int   maxHits          = 10;
	[Export] private float minSpeed         = 30f;
	[Export] private float maxSpeed         = 90f;
	[Export] private float minBurstDuration = 0.4f;  // seconds before picking a new direction
	[Export] private float maxBurstDuration = 1.2f;
	[Export] private float topMargin        = 12f;   // pixels below the top wall to hover at
	[Export] private float eggInterval      = 2.2f;  // seconds between egg drops
	[Export] private float cowPullInterval  = 5f;    // seconds between cow-pull events

	// ── State ──────────────────────────────────────────────────────────────────
	private int   hitCount      = 0;
	private bool  isDead        = false;

	private PlayerSoul trackedPlayer;
	private BattleBox  trackedBox;

	// Movement burst state
	private float currentSpeed     = 60f;
	private float currentDirection = 1f;   // +1 = right, -1 = left
	private float burstTimer        = 0f;
	private float burstDuration     = 0.8f;

	// Routine timers
	private float eggTimer     = 0f;
	private float cowPullTimer = 0f;

	// How many eggs have been dropped since the last cow pull — used to alternate
	// between regular and parry eggs so neither dominates
	private int eggsSinceLastParry = 0;

	// ── Public API (called by AlienFinalAttack) ────────────────────────────────
	[Signal] public delegate void AlienDefeatedEventHandler();

	public void Initialize(PlayerSoul player, BattleBox box)
	{
		trackedPlayer = player;
		trackedBox    = box;

		// Start at a random horizontal position just inside the top of the box
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();
		GlobalPosition = new Vector2(
			(float)GD.RandRange(min.X + 10f, max.X - 10f),
			min.Y + topMargin
		);

		PickNewBurst();

		// Connect hit detection
		AreaEntered += OnHitAreaEntered;

		if (alienSprite != null)
			alienSprite.Play("default");
	}

	// ── Godot loop ─────────────────────────────────────────────────────────────
	public override void _Process(double delta)
	{
		if (isDead || trackedBox == null) return;

		UpdateMovement(delta);
		UpdateEggTimer(delta);
		UpdateCowPullTimer(delta);
	}

	// ── Movement ───────────────────────────────────────────────────────────────
	private void UpdateMovement(double delta)
	{
		Vector2 min = trackedBox.GetInnerMinBounds();
		Vector2 max = trackedBox.GetInnerMaxBounds();

		// Tick the burst timer; pick a new random direction/speed when it expires
		burstTimer -= (float)delta;
		if (burstTimer <= 0f)
			PickNewBurst();

		// Move horizontally
		GlobalPosition += new Vector2(currentDirection * currentSpeed * (float)delta, 0f);

		// Clamp to box and bounce if we hit a wall mid-burst
		float x = GlobalPosition.X;
		if (x <= min.X + 6f)
		{
			x = min.X + 6f;
			currentDirection = 1f;   // bounce right
			burstTimer = 0f;         // force a new burst on next frame
		}
		else if (x >= max.X - 6f)
		{
			x = max.X - 6f;
			currentDirection = -1f;  // bounce left
			burstTimer = 0f;
		}

		// Lock Y to just below the top wall
		GlobalPosition = new Vector2(x, min.Y + topMargin);
	}

	private void PickNewBurst()
	{
		// Random speed
		currentSpeed = (float)GD.RandRange(minSpeed, maxSpeed);

		// Bias: 70 % chance to keep going the same direction so movement
		// feels purposeful but still erratic, 30 % chance to reverse
		if (GD.Randf() < 0.3f)
			currentDirection = -currentDirection;

		burstDuration = (float)GD.RandRange(minBurstDuration, maxBurstDuration);
		burstTimer    = burstDuration;
	}

	// ── Egg shooting ───────────────────────────────────────────────────────────
	private void UpdateEggTimer(double delta)
	{
		eggTimer -= (float)delta;
		if (eggTimer <= 0f)
		{
			eggTimer = eggInterval + (float)GD.RandRange(-0.3f, 0.3f); // slight jitter
			DropEgg();
		}
	}

	private void DropEgg()
	{
		if (trackedPlayer == null) return;

		// Alternate: drop a parry egg every 3rd egg
		bool spawnParry = (eggsSinceLastParry >= 2);
		eggsSinceLastParry = spawnParry ? 0 : eggsSinceLastParry + 1;

		if (spawnParry)
			SpawnLaserEgg();
		else
			SpawnRegularEgg();
	}

	private void SpawnRegularEgg()
	{
		if (eggProjectileScene == null)
		{
			GD.PrintErr("[AlienBoss] eggProjectileScene is not assigned.");
			return;
		}

		Vector2 targetPos   = trackedPlayer.GlobalPosition;
		Vector2 direction   = AimedDirection(targetPos);

		var egg = eggProjectileScene.Instantiate<EggProjectileBreakable>();
		GetTree().CurrentScene.AddChild(egg);
		egg.ZIndex         = 1;
		egg.GlobalPosition = GlobalPosition;
		egg.Initialize(direction, 45f);
		egg.SetPlayerAndBox(trackedPlayer, trackedBox);
	}
	
	private void SpawnLaserEgg() {
		if (laserEggScene == null)
		{
			GD.PrintErr("[AlienBoss] eggProjectileScene is not assigned.");
			return;
		}

		Vector2 targetPos   = trackedPlayer.GlobalPosition;
		Vector2 direction   = AimedDirection(targetPos);

		var egg = laserEggScene.Instantiate<LaserEggBreakable>();
		GetTree().CurrentScene.AddChild(egg);
		egg.ZIndex         = 1;
		egg.GlobalPosition = GlobalPosition;
		egg.Initialize(direction, 45f);
		egg.SetPlayerAndBox(trackedPlayer, trackedBox);
	}

	private void SpawnParryEgg()
	{
		if (parryEggScene == null)
		{
			GD.PrintErr("[AlienBoss] parryEggScene is not assigned.");
			return;
		}

		Vector2 targetPos = trackedPlayer.GlobalPosition;
		Vector2 direction = AimedDirection(targetPos);

		var egg = parryEggScene.Instantiate<ParryEggBreakable>();
		GetTree().CurrentScene.AddChild(egg);
		egg.ZIndex         = 1;
		egg.GlobalPosition = GlobalPosition;
		egg.Initialize(direction, 40f);
		egg.SetPlayerAndBox(trackedPlayer, trackedBox);
	}

	/// <summary>
	/// Returns a direction from the boss toward <paramref name="targetPos"/>
	/// with a small random angular error so eggs aren't perfectly accurate.
	/// </summary>
	private Vector2 AimedDirection(Vector2 targetPos)
	{
		Vector2 raw        = (targetPos - GlobalPosition).Normalized();
		float   errorDeg   = (float)GD.RandRange(-12f, 12f);
		float   errorRad   = Mathf.DegToRad(errorDeg);
		float   cos        = Mathf.Cos(errorRad);
		float   sin        = Mathf.Sin(errorRad);
		return new Vector2(raw.X * cos - raw.Y * sin, raw.X * sin + raw.Y * cos);
	}

	// ── Cow pull ───────────────────────────────────────────────────────────────
	private void UpdateCowPullTimer(double delta)
	{
		cowPullTimer -= (float)delta;
		if (cowPullTimer <= 0f)
		{
			cowPullTimer = cowPullInterval + (float)GD.RandRange(-0.5f, 0.5f);
			TriggerCowPull();
		}
	}

	private void TriggerCowPull()
	{
		if (ufoCowPullScene == null)
		{
			GD.PrintErr("[AlienBoss] ufoCowPullScene is not assigned.");
			return;
		}

		if (trackedBox == null) return;

		Vector2 min = trackedBox.GetInnerMinBounds();
		Vector2 max = trackedBox.GetInnerMaxBounds();

		// Decide which side the UFO enters from (left or right)
		bool enterFromLeft = GD.Randf() < 0.5f;
		float spawnX       = enterFromLeft ? min.X : max.X;
		float spawnY       = min.Y - 10f;   // just below the alien

		var pull = ufoCowPullScene.Instantiate<UfoCowPull>();
		GetTree().CurrentScene.AddChild(pull);
		pull.ZIndex         = 1;
		pull.GlobalPosition = new Vector2(spawnX, spawnY);

		Vector2 moveDir = enterFromLeft ? Vector2.Right : Vector2.Left;
		pull.Initialize(moveDir, 55f, trackedBox);

		GD.Print("[AlienBoss] Cow pull triggered.");
	}

	// ── Hit detection ──────────────────────────────────────────────────────────
	private void OnHitAreaEntered(Area2D area)
	{
		if (isDead) return;
		if (!area.IsInGroup("PlayerBullets")) return;

		area.QueueFree();
		hitCount++;
		GD.Print($"[AlienBoss] Hit {hitCount}/{maxHits}");

		_ = FlashOnHit();

		if (hitCount >= maxHits)
			_ = Die();
	}

	private async Task FlashOnHit()
	{
		if (alienSprite == null) return;

		Color original = alienSprite.Modulate;
		alienSprite.Modulate = new Color(1f, 0.2f, 0.2f, 1f);
		await ToSignal(GetTree().CreateTimer(0.12f), "timeout");

		if (IsInsideTree() && alienSprite != null)
			alienSprite.Modulate = original;
	}

	private async Task Die()
	{
		isDead = true;

		GD.Print("[AlienBoss] Defeated!");

		// Play death animation if one exists, otherwise just flash and fade
		if (alienSprite != null && alienSprite.SpriteFrames.HasAnimation("death"))
		{
			alienSprite.Play("death");
			await ToSignal(alienSprite, AnimatedSprite2D.SignalName.AnimationFinished);
		}
		else
		{
			// Simple fade-out
			for (int i = 0; i < 5; i++)
			{
				if (!IsInsideTree()) break;
				alienSprite.Modulate = new Color(1f, 1f, 1f, 1f - i * 0.2f);
				await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			}
		}

		EmitSignal(SignalName.AlienDefeated);
		QueueFree();
	}
}
