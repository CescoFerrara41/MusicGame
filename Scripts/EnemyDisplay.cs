using Godot;
using System;
using System.Threading.Tasks;

public partial class EnemyDisplay : Area2D
{
	[Export] public string EnemyName = "Enemy";
	[Export] public int MaxHealth = 40;

	[Export] public ProgressBar HealthBar;
	[Export] public AnimatedSprite2D EnemySprite;
	[Signal] public delegate void DamagedEventHandler(int damage);
	
	[Export] public bool lastStand = false;
	public bool isLastStand = false;

	private int currentHealth;

	private BattleController battleController;

	public override void _Ready()
	{
		currentHealth = MaxHealth;

		if (HealthBar != null)
		{
			HealthBar.MaxValue = MaxHealth;
			HealthBar.Value = MaxHealth;
			HealthBar.Visible = false;
		}

		battleController = GetNode<BattleController>("../../BattleController");
		EnemySprite.Play();
	}

	public async Task TakeDamage(int damage)
{
	EmitSignal(SignalName.Damaged, damage);
	if (currentHealth - damage <= 1 && lastStand && !isLastStand) {
		currentHealth = 1;
		isLastStand = true;
	}
	else {
		currentHealth -= damage;
	}
	if (currentHealth < 0)
		currentHealth = 0;

	if (HealthBar != null)
	{
		HealthBar.Visible = true;

		var tween = CreateTween();
		tween.TweenProperty(
			HealthBar,
			"value",
			currentHealth,
			0.35f
		).SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);
		
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		HealthBar.Visible = false;
	}

	if (currentHealth <= 0)
	{
		Die();
	}
}

	private void Die()
	{
		GD.Print($"{EnemyName} defeated!");

		battleController.OnEnemyKilled(this);

		QueueFree();
	}
	
	public void HideHealth() {
		HealthBar.Visible = false;
	}
	
	public int GetCurrentHealth() {
		return currentHealth;
	}
}
