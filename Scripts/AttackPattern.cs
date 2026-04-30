using Godot;
using System.Threading.Tasks;

public abstract partial class AttackPattern : Node
{
	public abstract Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	);
}
