using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TaserAttack : AttackPattern
{
	// ── Exported scenes ──────────────────────────────────────────────────────
	[Export] private PackedScene taserProbeScene;

	// Assign your horizontal electrified wall scene (used for top + bottom).
	[Export] private PackedScene horizontalWallScene;

	// Assign your vertical electrified wall scene (used for left + right).
	[Export] private PackedScene verticalWallScene;

	// ── Timing ───────────────────────────────────────────────────────────────
	[Export] private float attackDuration      = 12f;
	[Export] private float minTimeBetweenFlash =  2f;
	[Export] private float maxTimeBetweenFlash =  4f;

	// ── Box-move settings ────────────────────────────────────────────────────
	[Export] private float boxMoveDist     = 30f;
	[Export] private float boxMoveDuration = 0.35f;

	// ── Internal state ───────────────────────────────────────────────────────
	private TaserProbe[]      probes = new TaserProbe[4];      // Top=0, Bottom=1, Left=2, Right=3
	private ElectrifiedWall[] walls  = new ElectrifiedWall[4]; // same indexing

	private bool attackRunning = false;

	// ── Wall axis groups (never flash both members of the same group) ────────
	// Group 0: Top / Bottom  (Y axis)
	// Group 1: Left / Right  (X axis)
	private static readonly int[] WallGroup = { 0, 0, 1, 1 };

	// ─────────────────────────────────────────────────────────────────────────
	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager)
	{
		await box.SetMode(BattleBoxMode.SmallerEnemy);
		player.Spawn();

		// ── 1. Instantiate probes and slide them all in simultaneously ────────
		await SpawnProbes(box);

		// ── 2. Walls become electrified only after all probes have connected ──
		SpawnElectrifiedWalls(player, box);

		// ── 3. Run the flash-and-move loop ────────────────────────────────────
		attackRunning = true;
		_ = FlashLoop(box);

		await ToSignal(GetTree().CreateTimer(attackDuration), "timeout");

		// ── 4. Clean up ───────────────────────────────────────────────────────
		attackRunning = false;

		foreach (var probe in probes)
			if (IsInstanceValid(probe)) probe.QueueFree();

		foreach (var wall in walls)
			if (IsInstanceValid(wall)) wall.QueueFree();

		await box.SetMode(BattleBoxMode.Enemy);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Probe spawning — all four slide in at the same time, awaits all of them
	// ─────────────────────────────────────────────────────────────────────────
	private async Task SpawnProbes(BattleBox box)
	{
		if (taserProbeScene == null)
		{
			GD.PrintErr("[TaserAttack] taserProbeScene is not assigned.");
			return;
		}

		Vector2 min    = box.GetInnerMinBounds();
		Vector2 max    = box.GetInnerMaxBounds();
		Vector2 center = box.GlobalPosition;

		Vector2[] wallPositions =
		{
			new Vector2(center.X, min.Y),    // Top
			new Vector2(center.X, max.Y),    // Bottom
			new Vector2(min.X,    center.Y), // Left
			new Vector2(max.X,    center.Y), // Right
		};

		TaserProbe.Wall[] wallEnums =
		{
			TaserProbe.Wall.Top, TaserProbe.Wall.Bottom,
			TaserProbe.Wall.Left, TaserProbe.Wall.Right
		};

		List<Task> entryTasks = new();
		for (int i = 0; i < 4; i++)
		{
			var probe = taserProbeScene.Instantiate<TaserProbe>();
			GetTree().CurrentScene.AddChild(probe);
			probe.ZIndex       = -1;
			probe.AnchoredWall = wallEnums[i];
			probes[i]          = probe;

			// Pass box so the probe can follow it every frame after connecting
			entryTasks.Add(probe.Initialize(wallPositions[i], box));
		}

		foreach (var t in entryTasks)
			await t;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Electrified wall spawning — called only after probes have connected
	// Horizontal scene → Top (0) and Bottom (1)
	// Vertical scene   → Left (2) and Right (3)
	// ─────────────────────────────────────────────────────────────────────────
	private void SpawnElectrifiedWalls(PlayerSoul player, BattleBox box)
	{
		TaserProbe.Wall[] wallEnums =
		{
			TaserProbe.Wall.Top, TaserProbe.Wall.Bottom,
			TaserProbe.Wall.Left, TaserProbe.Wall.Right
		};

		for (int i = 0; i < 4; i++)
		{
			bool        isHorizontal = i < 2;
			PackedScene scene        = isHorizontal ? horizontalWallScene : verticalWallScene;

			if (scene == null)
			{
				GD.PrintErr($"[TaserAttack] {(isHorizontal ? "horizontalWallScene" : "verticalWallScene")} is not assigned.");
				continue;
			}

			var wall = scene.Instantiate<ElectrifiedWall>();
			GetTree().CurrentScene.AddChild(wall);
			wall.ZIndex = 3;
			wall.SetTarget(player, box, wallEnums[i]);
			walls[i] = wall;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Flash + move loop
	// ─────────────────────────────────────────────────────────────────────────
	private async Task FlashLoop(BattleBox box)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		while (attackRunning)
		{
			float wait = (float)GD.RandRange(minTimeBetweenFlash, maxTimeBetweenFlash);
			await ToSignal(GetTree().CreateTimer(wait), "timeout");

			if (!attackRunning) break;

			int       count  = rng.RandiRange(1, 2);
			List<int> chosen = PickProbes(count, rng);

			List<Task> flashTasks = new();
			foreach (int idx in chosen)
				if (IsInstanceValid(probes[idx]))
					flashTasks.Add(probes[idx].Activate());

			foreach (var t in flashTasks)
				await t;

			if (!attackRunning) break;

			Vector2 moveDir = Vector2.Zero;
			foreach (int idx in chosen)
				moveDir += ((TaserProbe.Wall)idx) switch
				{
					TaserProbe.Wall.Top    => new Vector2(0f, -1f),
					TaserProbe.Wall.Bottom => new Vector2(0f,  1f),
					TaserProbe.Wall.Left   => new Vector2(-1f, 0f),
					TaserProbe.Wall.Right  => new Vector2( 1f, 0f),
					_                      => Vector2.Zero
				};

			if (moveDir != Vector2.Zero)
				await MoveBox(box, moveDir.Normalized());
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Probe selection — never picks Top+Bottom or Left+Right simultaneously
	// ─────────────────────────────────────────────────────────────────────────
	private List<int> PickProbes(int count, RandomNumberGenerator rng)
	{
		List<int> pool   = new() { 0, 1, 2, 3 };
		List<int> chosen = new();

		for (int i = pool.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			(pool[i], pool[j]) = (pool[j], pool[i]);
		}

		HashSet<int> usedGroups = new();

		foreach (int idx in pool)
		{
			if (chosen.Count >= count) break;
			int group = WallGroup[idx];
			if (usedGroups.Contains(group)) continue;
			chosen.Add(idx);
			usedGroups.Add(group);
		}

		return chosen;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Ease the BattleBox position
	// ─────────────────────────────────────────────────────────────────────────
	private async Task MoveBox(BattleBox box, Vector2 direction)
	{
		Vector2 targetPos = box.Position + direction * boxMoveDist;

		var tween = box.CreateTween();
		tween.TweenProperty(box, "position", targetPos, boxMoveDuration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);

		// No manual probe repositioning needed — probes follow via _Process
	}

}
