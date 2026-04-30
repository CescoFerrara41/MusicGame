using Godot;
using System.Threading.Tasks;

public partial class MenuStep : BattleStep
{
	private string message;

	public MenuStep(string msg)
	{
		message = msg;
	}

	
	public override async Task Execute(BattleController battle)
	{
		battle.Menu.Visible = true;
		battle.Narrator.ShowMessage(message);
		battle.Menu.SetInteractable(true);

		await battle.Menu.WaitForSelection();

		battle.Menu.SetInteractable(false);
		//battle.Menu.Visible = false;
	}
}
