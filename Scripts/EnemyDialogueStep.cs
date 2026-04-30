using Godot;
using System.Threading.Tasks;

public partial class EnemyDialogueStep : BattleStep
{
	private string message;

	public EnemyDialogueStep(string msg)
	{
		message = msg;
	}

	public override async Task Execute(BattleController battle)
	{
		battle.Narrator.ShowMessage(message);

		await ToSignal(
			battle.GetTree().CreateTimer(2),
            "timeout"
		);
	}
}
