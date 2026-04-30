using Godot;
using System.Threading.Tasks;

public abstract partial class BattleStep : Node
{
	public abstract Task Execute(BattleController battle);
}
