using Godot;
using System.Threading.Tasks;

/// <summary>
/// Final attack of the alien fight. Spawns the AlienBoss node, which:
///   • Moves erratically across the top of the BattleBox
///   • Shoots aimed eggs (regular + parry) at the player
///   • Triggers periodic UfoCowPull events
/// 
/// The attack ends when the boss has been hit 10 times by PlayerBullets.
/// 
/// Setup in the editor:
///   • alienBossScene  – PackedScene of the AlienBoss node
/// </summary>
public partial class AlienFinalAttack : AttackPattern
{
	[Export] private PackedScene alienBossScene;

	public override async Task Execute(
		PlayerSoul    player,
		BattleBox     box,
		EnemyManager  enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.TallEnemy);
		player.Spawn();
		player.GlobalPosition = new Vector2(160f, 130f);

		if (alienBossScene == null)
		{
			GD.PrintErr("[AlienFinalAttack] alienBossScene is not assigned.");
			return;
		}
		await ToSignal(GetTree().CreateTimer(1f), "timeout");
		player.TransformToBattleship();
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		// ── Spawn the boss ────────────────────────────────────────────────────
		var boss = alienBossScene.Instantiate<AlienBoss>();
		GetTree().CurrentScene.AddChild(boss);
		boss.ZIndex = 2; // render above bullets

		boss.Initialize(player, box);

		// ── Wait until the boss emits AlienDefeated ───────────────────────────
		GD.Print("[AlienFinalAttack] Boss spawned — waiting for defeat.");
		await ToSignal(boss, AlienBoss.SignalName.AlienDefeated);
		GD.Print("[AlienFinalAttack] Boss defeated — cleaning up.");

		// Small pause so the player can see the boss die before the box closes
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
		player.ReturnToNormal();

		await box.SetMode(BattleBoxMode.Enemy);
	}
}
