using Godot;
using System.Threading.Tasks;

public partial class ScopeAttack : AttackPattern
{
	[Export] private PackedScene scopeScene;
	[Export] private PackedScene scopeBulletScene;

	// How long the scope tracks the player before locking (seconds)
	[Export] private float trackingDuration = 1.5f;
	// How long the flash animation plays before firing
	[Export] private float flashDuration = 0.8f;
	// How long after firing before tracking resumes
	[Export] private float postFirePause = 0.3f;

	[Export] private float bulletSpeed = 60f;

	private const int BurstCount = 3;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		// Spawn the scope
		ScopeMarker scope = scopeScene.Instantiate<ScopeMarker>();
		GetTree().CurrentScene.AddChild(scope);
		scope.ZIndex = 2;
		scope.GlobalPosition = box.GlobalPosition;

		for (int burst = 0; burst < BurstCount; burst++)
		{
			// --- Tracking phase ---
			scope.StartTracking(player);
			await ToSignal(GetTree().CreateTimer(trackingDuration), "timeout");

			// --- Lock and flash ---
			scope.StopTracking();
			scope.PlayFlash();
			await ToSignal(GetTree().CreateTimer(flashDuration), "timeout");

			// --- Fire 8 bullets radially ---
			FireRadialBurst(scope.GlobalPosition);

			await ToSignal(GetTree().CreateTimer(postFirePause), "timeout");
		}

		// Clean up scope and wait for bullets to clear
		scope.QueueFree();
		await ToSignal(GetTree().CreateTimer(2f), "timeout");
	}

	private void FireRadialBurst(Vector2 origin)
	{
		int bulletCount = 8;
		for (int i = 0; i < bulletCount; i++)
		{
			float angle = i * (Mathf.Tau / bulletCount);
			Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

			RadialBullet bullet = scopeBulletScene.Instantiate<RadialBullet>();
			GetTree().CurrentScene.AddChild(bullet);
			bullet.ZIndex = 1;
			bullet.GlobalPosition = origin;
			bullet.Initialize(direction, bulletSpeed);
		}
	}
}
