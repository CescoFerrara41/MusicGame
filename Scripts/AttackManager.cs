using Godot;
using System.Threading.Tasks;

public partial class AttackManager : Node
{
	private TaskCompletionSource<bool> attackFinished;

	private RhythmManager rhythmManager;
	private BattleBox battleBox;
	private PlayerSoul playerSoul;
	private EnemyManager enemyManager;
	[Export] private EnemyDisplay mainEnemy;
	
	[Export] private PackedScene[] attacks1;
	[Export] private PackedScene[] attacks2;
	[Export] private PackedScene[] finalAttack;
	[Export] private PackedScene[] testAttacks;
	[Export] private AttackPattern currentAttack;

	public override void _Ready()
	{
		rhythmManager = GetNode<RhythmManager>("../RhythmManager");
		battleBox = GetNode<BattleBox>("../BattleBox");
		playerSoul = GetNode<PlayerSoul>("../PlayerSoul");
		enemyManager = GetNode<EnemyManager>("../EnemyManager");
	}

	// ENEMY ATTACK
	public void StartEnemyAttack()
	{
		attackFinished = new TaskCompletionSource<bool>();
		if (mainEnemy.GetCurrentHealth() == 1) {
			currentAttack = GetFinalAttack();
		}
		else if ((float)mainEnemy.GetCurrentHealth() / mainEnemy.MaxHealth <= 0.50f) {
			currentAttack = GetRandomAttack2();
		}
		else {
			currentAttack = GetRandomAttack();
		}
		GD.Print("Enemy attack started");
		
		// comment this out when not testing
		//currentAttack = testAttacks[0].Instantiate<AttackPattern>();

		RunAttack();
	}
	
	public AttackPattern GetRandomAttack()
{
	int index = GD.RandRange(0, attacks1.Length - 1);
	return attacks1[index].Instantiate<AttackPattern>();
}

	public AttackPattern GetRandomAttack2() {
		int randomNum = GD.RandRange(0, 1);
		if (randomNum == 0) {
			int index = GD.RandRange(0, attacks1.Length - 1);
			return attacks1[index].Instantiate<AttackPattern>();
		}
		else {
			int index = GD.RandRange(0, attacks2.Length-1);
			return attacks2[index].Instantiate<AttackPattern>();
		}
	}
	
	public AttackPattern GetFinalAttack() {
		return finalAttack[0].Instantiate<AttackPattern>();
	}

	private async void RunAttack()
	{
		enemyManager.HideAllEnemyHealth();

		// Switch box to enemy arena mode
		// await battleBox.SetMode(BattleBoxMode.Enemy);

		//playerSoul.Spawn();

		// Placeholder enemy attack duration
		AddChild(currentAttack);
		await currentAttack.Execute(playerSoul, battleBox, enemyManager);
		currentAttack.QueueFree();

		playerSoul.Despawn();

		// Return box to dialogue mode after attack
		await battleBox.SetMode(BattleBoxMode.Dialogue);

		GD.Print("Attack finished");

		attackFinished?.SetResult(true);
	}

	public async Task WaitForAttackEnd()
	{
		if (attackFinished != null)
			await attackFinished.Task;
	}

	// PLAYER RHYTHM ATTACK
	public async Task StartRhythmSequence()
	{
		GD.Print("Rhythm attack started");

		// Switch box to attack hit zone
		await battleBox.SetMode(BattleBoxMode.Attack);
		await rhythmManager.FadeIn();

		await rhythmManager.StartRhythmSequence();
		await rhythmManager.FadeOut();

		// Return to dialogue box after attack
		//await battleBox.SetMode(BattleBoxMode.Dialogue);
	}

	public float GetAccuracy()
	{
		return rhythmManager.GetAccuracy();
	}
}
