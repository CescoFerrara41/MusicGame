using Godot;
using System.Threading.Tasks;

public partial class EnemyAttackStep : BattleStep
{
	public override async Task Execute(BattleController battle)
	{
		battle.AttackManager.StartEnemyAttack();

		await battle.AttackManager.WaitForAttackEnd();
	}
}
