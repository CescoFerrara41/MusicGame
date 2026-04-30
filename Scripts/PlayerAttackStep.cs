using Godot;
using System.Threading.Tasks;

public partial class PlayerAttackStep : BattleStep
{
	public override async Task Execute(BattleController battle)
	{
		EnemyDisplay target = await battle.EnemyManager.WaitForTarget();

		// run rhythm system
		await battle.AttackManager.StartRhythmSequence();

		float accuracy = battle.AttackManager.GetAccuracy();

		int baseDamage = 10;

		int damage = Mathf.RoundToInt(baseDamage * accuracy);

		GD.Print("Damage: " + damage);

		await target.TakeDamage(damage);
	}
}
