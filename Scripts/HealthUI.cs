using Godot;
using System.Threading.Tasks;

public partial class HealthUI : Control
{
	private TextureProgressBar frontBar;
	private TextureProgressBar backBar;
	[Export] public RichTextLabel healthText;
	[Export] public int maxHealth = 20;
	[Export] public int currentHealth = 20;

	[Export] private float lagDelay = 0.2f;
	[Export] private float lagSpeed = 0.5f;

	public override void _Ready()
	{
		frontBar = GetNode<TextureProgressBar>("FrontBar");
		backBar = GetNode<TextureProgressBar>("BackBar");

		
		var player = GetNode<PlayerSoul>("../../PlayerSoul");
		maxHealth = player.GetMaxHealth();
		currentHealth = player.GetCurrentHealth();
		GD.Print(maxHealth);
		GD.Print(currentHealth);

		player.HealthChanged += OnHealthChanged;
		SetHealth(currentHealth, maxHealth);
	}
	
	private async void OnHealthChanged(int current, int max)
	{
		await SetHealth(current, max);
	}

	public async Task SetHealth(int current, int max)
	{
		frontBar.MaxValue = max;
		backBar.MaxValue = max;

		float oldValue = (float)frontBar.Value;

		// FRONT BAR: instantly drops
		frontBar.Value = current;
		healthText.Text = $"{current} / {max}";

		UpdateColor();

		// Wait before lag bar starts moving
		await ToSignal(GetTree().CreateTimer(lagDelay), "timeout");

		// BACK BAR: smoothly follows
		var tween = CreateTween();
		tween.TweenProperty(
			backBar,
			"value",
			current,
			lagSpeed
		);

		await ToSignal(tween, "finished");
	}

	private void UpdateColor()
	{
		float percent = (float)(frontBar.Value / frontBar.MaxValue);

		Color color;

		if (percent > 0.5f)
			color = new Color(0, 1, 0); // green
		else if (percent > 0.2f)
			color = new Color(1, 1, 0); // yellow
		else
			color = new Color(1, 0, 0); // red

		frontBar.Modulate = color;
	}
}
