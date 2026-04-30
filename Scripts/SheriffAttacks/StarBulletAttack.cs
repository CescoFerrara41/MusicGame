using Godot;
using System.Threading.Tasks;

public partial class StarBulletAttack : AttackPattern
{
	[Export] private PackedScene starBulletScene;
	[Export] private float attackDuration = 12f;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.SlightlyLargerEnemy);
		player.Spawn();

		StarBullet star = starBulletScene.Instantiate<StarBullet>();
		GetTree().CurrentScene.AddChild(star);
		star.ZIndex = 1;
		star.Initialize(box);

		await ToSignal(GetTree().CreateTimer(attackDuration), "timeout");

		star?.QueueFree();

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
	}
}
