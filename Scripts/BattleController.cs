using Godot;

public partial class BattleController : Node
{
	public BattleMenu Menu;
	public BattleNarrator Narrator;
	public EnemyManager EnemyManager;
	public AttackManager AttackManager;

	private TurnQueue queue;
	[Export] BattleBox battleBox;

	public override void _Ready()
	{
		Menu = GetNode<BattleMenu>("../BattleUI/BattleMenu");
		Narrator = GetNode<BattleNarrator>("../BattleUI/BattleNarrator");
		EnemyManager = GetNode<EnemyManager>("../EnemyManager");
		AttackManager = GetNode<AttackManager>("../AttackManager");

		queue = new TurnQueue();
		AddChild(queue);

		StartTurn();
	}

	private async void StartTurn()
	{
		queue.Clear();
		await battleBox.SetMode(BattleBoxMode.Dialogue);
		queue.AddStep(new MenuStep("Placeholder"));
		queue.AddStep(new PlayerAttackStep());
		//queue.AddStep(new EnemyDialogueStep("The enemy prepares an attack."));
		queue.AddStep(new EnemyAttackStep());

		await queue.RunQueue(this);

		StartTurn();
	}

	public void OnEnemyKilled(EnemyDisplay enemy)
	{
		GD.Print("Enemy defeated!");

		if (EnemyManager.GetEnemyCount() == 0)
		{
			Narrator.ShowMessage("You won!");
		}
	}
}
