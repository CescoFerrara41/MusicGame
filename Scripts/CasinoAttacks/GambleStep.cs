using Godot;
using System.Threading.Tasks;

public partial class GambleStep : BattleStep
{
	private string     message;
	private PlayerSoul player;
	private EnemyManager enemyManager;
	private BattleBox battleBox;

	public GambleStep(string msg, PlayerSoul playerSoul, EnemyManager em, BattleBox bb)
	{
		message = msg;
		player  = playerSoul;
		enemyManager = em;
		battleBox = bb;
	}

	public override async Task Execute(BattleController battle)
	{
		await battleBox.SetMode(BattleBoxMode.Dialogue);
		GambleOptions gambleOptions = battle.GambleOptions;

		// ── Phase 1: Intro ────────────────────────────────────────────────────
		battle.Narrator.ShowMessage(message);
		await Task.Delay(800);

		// ── Phase 2: Pick a wager ─────────────────────────────────────────────
		battle.Narrator.ShowMessage("How much health will you gamble?");

		float[] wagerPercents = { 0.10f, 0.15f, 0.20f, 0.25f };
		string[] wagerLabels  = { "10%", "15%", "20%", "25%" };

		int   wagerIndex  = await gambleOptions.WaitForWager();
		float wagerPct    = wagerPercents[wagerIndex];
		int   wagerAmount = Mathf.RoundToInt(player.GetMaxHealth() * wagerPct);

		battle.Narrator.ShowMessage(
			$"You gamble {wagerLabels[wagerIndex]} of your health ({wagerAmount} HP).");
		await Task.Delay(900);

		// ── Phase 3: Heads or tails ───────────────────────────────────────────
		battle.Narrator.ShowMessage("Heads or tails?");

		int coinChoice = await gambleOptions.WaitForCoinFlip();  // 0 = Heads, 1 = Tails

		// ── Phase 4: Resolve ──────────────────────────────────────────────────
		int    coinResult = GD.RandRange(0, 1);               // 0 = Heads, 1 = Tails
		bool   won        = (coinChoice == coinResult);
		string flipName   = (coinResult == 0) ? "Heads" : "Tails";

		if (won)
		{
			int enemyDamage = Mathf.RoundToInt(wagerAmount * 0.5f);
			battle.Narrator.ShowMessage($"{flipName}! You win! The enemy takes {enemyDamage} damage!");
			await Task.Delay(900);
			EnemyDisplay enemy = enemyManager.GetFirstAliveEnemy();
			enemy.TakeDamage(enemyDamage);
		}
		else
		{
			battle.Narrator.ShowMessage($"{flipName}! You lose! You take {wagerAmount} damage!");
			await Task.Delay(900);
			player.TakeDamage(wagerAmount);
		}

		await Task.Delay(700);
	}
}
