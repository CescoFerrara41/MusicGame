using Godot;
using System;
using System.Threading.Tasks;

public partial class TestAttack : AttackPattern
{
	
	[Export] private float projectileTopSpeed = 80f;
	
	[Export] private PackedScene bullet;
	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.SlightlyLargerEnemy);
		player.Spawn();
		Vector2 position = new Vector2(160, 150);
		SpawnParryProjectile(bullet, position, Vector2.Up);



		await ToSignal(GetTree().CreateTimer(3f), "timeout");
	}

	
	private void SpawnParryProjectile(PackedScene scene, Vector2 position, Vector2 direction) {
		AcceleratingParryBullet bullet = scene.Instantiate<AcceleratingParryBullet>();
		GetTree().CurrentScene.AddChild(bullet);
		bullet.ZIndex = 1;
		bullet.GlobalPosition = position;
		bullet.InitializeAccelerating(direction, projectileTopSpeed);
	}
}
