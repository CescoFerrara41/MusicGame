using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// A driving-style attack where:
/// - The battle box has 3 evenly spaced lanes (0 = left, 1 = center, 2 = right)
/// - The player is locked to the bottom of the box and snaps between lanes
/// - Two columns of scrolling dash lines divide the lanes
/// - Obstacles are spawned on a fully scripted timeline via SpawnAt()
/// - Spinning cars travel diagonally, bounce off one wall, and arrive at a target lane
/// </summary>
public partial class DrivingAttack2 : AttackPattern
{
	[Export] private float attackDuration = 11f;
	[Export] private float playerBottomOffset = 15f;
	[Export] private float snapDuration = 0.08f;

	// Dash lines
	[Export] private PackedScene dashLineScene;
	[Export] private float scrollSpeed = 120f;
	[Export] private float spawnInterval = 0.35f;

	// Obstacles
	[Export] private PackedScene spikeTrapScene;
	[Export] private PackedScene carScene;
	[Export] private PackedScene spinningCarScene;
	[Export] private float carSpeedMultiplier = 0.6f;

	private PlayerSoul player;
	private BattleBox box;

	private int currentLane = 1;
	private bool isTweening = false;
	private float[] laneXPositions = new float[3];
	private float lockedY;

	private float spawnTimer = 0f;
	private float elapsedTime = 0f;
	private bool active = false;

	private HashSet<int> firedEvents = new HashSet<int>();

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		this.player = player;
		this.box    = box;

		await box.SetMode(BattleBoxMode.CarThreeLanes);
		player.Spawn();

		SetupLanes();

		currentLane = 1;
		player.GlobalPosition = new Vector2(laneXPositions[currentLane], lockedY);

		player.SetProcess(false);
		player.TransformToCar();
		await ToSignal(GetTree().CreateTimer(1f), "timeout");

		elapsedTime = 0f;
		spawnTimer  = spawnInterval;
		firedEvents.Clear();

		active = true;

		await ToSignal(GetTree().CreateTimer(attackDuration), "timeout");

		active = false;

		player.SetProcess(true);

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		player.ReturnToNormal();
	}

	private void SetupLanes()
	{
		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float boxWidth = max.X - min.X;

		laneXPositions[0] = min.X + boxWidth * (1f / 6f);
		laneXPositions[1] = min.X + boxWidth * (3f / 6f);
		laneXPositions[2] = min.X + boxWidth * (5f / 6f);

		lockedY = max.Y - playerBottomOffset;
	}

	public override void _Process(double delta)
	{
		if (!active || player == null || box == null) return;

		elapsedTime += (float)delta;

		HandleInput();
		LockPlayerY();
		HandleDashLineSpawning(delta);
		HandleTimeline();
	}

	private void LockPlayerY()
	{
		Vector2 pos = player.GlobalPosition;
		pos.Y = lockedY;
		player.GlobalPosition = pos;
	}

	private void HandleInput()
	{
		if (isTweening) return;

		if (Input.IsActionJustPressed("ui_left") && currentLane > 0)
		{
			currentLane--;
			_ = SnapToLane(currentLane);
		}
		else if (Input.IsActionJustPressed("ui_right") && currentLane < 2)
		{
			currentLane++;
			_ = SnapToLane(currentLane);
		}
	}

	private async Task SnapToLane(int lane)
	{
		isTweening = true;

		var tween = CreateTween();
		tween.TweenProperty(player, "global_position:x", laneXPositions[lane], snapDuration)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);

		isTweening = false;
	}

	// -------------------------------------------------------------------------
	// Dash lines
	// -------------------------------------------------------------------------
	private void HandleDashLineSpawning(double delta)
	{
		spawnTimer += (float)delta;
		if (spawnTimer < spawnInterval) return;
		spawnTimer = 0f;

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float boxWidth  = max.X - min.X;
		float divider1X = min.X + boxWidth * (2f / 6f);
		float divider2X = min.X + boxWidth * (4f / 6f);

		SpawnDashLine(divider1X, min.Y, max.Y);
		SpawnDashLine(divider2X, min.Y, max.Y);
	}

	private void SpawnDashLine(float x, float topY, float bottomY)
	{
		if (dashLineScene == null) return;

		DashLine dash = dashLineScene.Instantiate<DashLine>();
		GetTree().CurrentScene.AddChild(dash);
		dash.ZIndex = 0;
		dash.GlobalPosition = new Vector2(x, topY);
		dash.Initialize(scrollSpeed, bottomY);
	}

	// -------------------------------------------------------------------------
	// Scripted timeline
	// -------------------------------------------------------------------------
	private void HandleTimeline()
	{
		SpawnSpinningCarAt(id: 0, time: 1.5f, speed: 100f, startLane: 0, endLane: 1, bounceWall: SpinningCarWall.Right);
		SpawnSpinningCarAt(id: 1, time: 2.5f, speed: 100f, startLane: 2, endLane: 1, bounceWall: SpinningCarWall.Left);
		SpawnAt(id: 2,  time: 3.5f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0, 2 });
		SpawnAt(id: 3,  time: 4f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 1 });
		SpawnAt(id: 4,  time: 4f,    speed: scrollSpeed-20f,                        scene: carScene, lanes: new[] { 0 });
		SpawnSpinningCarAt(id: 5, time: 4.25f, speed: 90f, startLane: 2, endLane: 1, bounceWall: SpinningCarWall.Left);
		SpawnSpinningCarAt(id: 6, time: 5f, speed: 90f, startLane: 0, endLane: 2, bounceWall: SpinningCarWall.Right);
		SpawnSpinningCarAt(id: 7, time: 5f, speed: 90f, startLane: 2, endLane: 0, bounceWall: SpinningCarWall.Left);
		SpawnAt(id: 8,  time: 6f,    speed: scrollSpeed-20f,                        scene: carScene, lanes: new[] { 1 });
		SpawnSpinningCarAt(id: 9, time: 6.5f, speed: 90f, startLane: 2, endLane: 1, bounceWall: SpinningCarWall.Left);
		SpawnAt(id: 10,  time: 6.5f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0 });
		SpawnAt(id: 11,  time: 7f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0 });
		SpawnAt(id: 12,  time: 7.5f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 1 });
		SpawnAt(id: 13,  time: 7.5f,    speed: 30f,                        scene: carScene, lanes: new[] { 1 });
		SpawnSpinningCarAt(id: 14, time: 7.5f, speed: 90f, startLane: 2, endLane: 0, bounceWall: SpinningCarWall.Left);
		SpawnSpinningCarAt(id: 14, time: 8f, speed: 90f, startLane: 0, endLane: 2, bounceWall: SpinningCarWall.Right);
		SpawnSpinningCarAt(id: 15, time: 9f, speed: 200f, startLane: 2, endLane: 0, bounceWall: SpinningCarWall.Left);
		
		//SpawnAt(id: 0,  time: 2f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 1 });
		//SpawnAt(id: 1,  time: 3f,    speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0, 2 });
		//SpawnAt(id: 2,  time: 4f,    speed: scrollSpeed * carSpeedMultiplier,   scene: carScene,       lanes: new[] { 2 });
		//SpawnAt(id: 3,  time: 4f,    speed: scrollSpeed * carSpeedMultiplier - 2f, scene: carScene,    lanes: new[] { 1 });
		//SpawnAt(id: 4,  time: 4.75f, speed: scrollSpeed * carSpeedMultiplier,   scene: carScene,       lanes: new[] { 0, 1 });
		//SpawnAt(id: 5,  time: 5.75f, speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0, 2 });
		//SpawnAt(id: 6,  time: 6.25f, speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 1 });
		//SpawnAt(id: 7,  time: 6.75f, speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0, 2 });
		//SpawnAt(id: 8,  time: 6.5f,  speed: 72f - 10f,                         scene: carScene,       lanes: new[] { 1 });
		//SpawnAt(id: 9,  time: 7.5f,  speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0 });
		//SpawnAt(id: 10, time: 7.6f,  speed: scrollSpeed - 40f,                  scene: carScene,       lanes: new[] { 1 });
		//SpawnAt(id: 11, time: 7.7f,  speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 2 });
		//SpawnAt(id: 12, time: 8.4f,  speed: scrollSpeed,                        scene: spikeTrapScene, lanes: new[] { 0 });

		// Spinning car example:
		//SpawnSpinningCarAt(id: 20, time: 5f, speed: 100f, startLane: 0, endLane: 2, bounceWall: SpinningCarWall.Right);
	}

	// -------------------------------------------------------------------------
	// SpawnAt — fires a regular obstacle event once at the given time
	// -------------------------------------------------------------------------
	private void SpawnAt(int id, float time, float speed, PackedScene scene, int[] lanes)
	{
		if (firedEvents.Contains(id)) return;
		if (elapsedTime < time) return;

		firedEvents.Add(id);
		SpawnObstaclesInLanes(scene, speed, lanes);
	}

	private void SpawnObstaclesInLanes(PackedScene scene, float speed, int[] lanes)
	{
		if (scene == null) return;

		Vector2 min = box.GetInnerMinBounds();

		foreach (int lane in lanes)
		{
			if (lane < 0 || lane > 2) continue;

			Bullet obstacle = scene.Instantiate<Bullet>();
			GetTree().CurrentScene.AddChild(obstacle);
			obstacle.ZIndex = 1;
			obstacle.GlobalPosition = new Vector2(laneXPositions[lane], min.Y);
			obstacle.Initialize(Vector2.Down, speed);
			obstacle.Rotation = 0f;
		}
	}

	// -------------------------------------------------------------------------
	// SpawnSpinningCarAt — fires a spinning car event once at the given time.
	// The car spawns at startLane, bounces off the specified wall, and arrives
	// at endLane at the player's locked Y position.
	//
	// bounceWall: SpinningCarWall.Left or SpinningCarWall.Right
	// -------------------------------------------------------------------------
	public enum SpinningCarWall { Left, Right }

	private void SpawnSpinningCarAt(int id, float time, float speed, int startLane, int endLane, SpinningCarWall bounceWall)
	{
		if (firedEvents.Contains(id)) return;
		if (elapsedTime < time) return;

		firedEvents.Add(id);

		if (spinningCarScene == null) return;

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float bounceWallX = bounceWall == SpinningCarWall.Left ? min.X : max.X;
		Vector2 startPos  = new Vector2(laneXPositions[startLane], min.Y);
		float targetX     = laneXPositions[endLane];

		SpinningCar car = spinningCarScene.Instantiate<SpinningCar>();
		GetTree().CurrentScene.AddChild(car);
		car.ZIndex = 1;
		car.InitializeSpinning(startPos, bounceWallX, targetX, lockedY, speed);
	}
}
