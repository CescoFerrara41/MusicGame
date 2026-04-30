using Godot;
using System.Threading.Tasks;

public partial class PlayerSoul : Area2D
{
	[Export] private float speed = 40f;
	[Export] private BattleBox battleBox;
	[Export] private int maxHealth = 20;
	private bool invincible = false;
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	private bool hasTransformedBefore = false;
	private bool hasTransformedCarBefore = false;
	[Export] private PackedScene[] bulletScenes = new PackedScene[3]; // assign your 3 bullet scenes in the Inspector
	private RandomNumberGenerator rng = new RandomNumberGenerator();
	private bool isBattleship = false;
	private bool isCar = false;

	private int currentHealth = 20;

	private AnimatedSprite2D soulSprite;
	private float soulHalfWidth;
	private float soulHalfHeight;

	public override void _Ready()
	{
		Visible = false;

		soulSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		var texture = soulSprite.SpriteFrames.GetFrameTexture(soulSprite.Animation, 0);  // 👈 Changed
		//Vector2 texSize = texture.GetSize();
		Vector2 texSize = new Vector2(11f, 9f);
		//Vector2 texSize = soulSprite.GetRect().Size;

		soulHalfWidth = (texSize.X * soulSprite.Scale.X) / 2f;
		soulHalfHeight = (texSize.Y * soulSprite.Scale.Y) / 2f;
		rng.Randomize();

	}

	public void Spawn()
	{
		Visible = true;
		GlobalPosition = battleBox.GlobalPosition;
	}

	public void Despawn()
	{
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible) return;

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		GlobalPosition += input * speed * (float)delta;

		ClampInsideBox();
		if (Input.IsActionJustPressed("interact"))
		{
			ShootRandomBullet();
		}
	}

	private void ClampInsideBox()
	{
		Vector2 min = battleBox.GetInnerMinBounds();
		Vector2 max = battleBox.GetInnerMaxBounds();

		Vector2 pos = GlobalPosition;

		pos.X = Mathf.Clamp(pos.X, min.X + soulHalfWidth, max.X - soulHalfWidth);
		pos.Y = Mathf.Clamp(pos.Y, min.Y + soulHalfHeight, max.Y - soulHalfHeight);

		GlobalPosition = pos;
	}

	// 🔥 THIS is what your bullet will call
	public bool TakeDamage(int damage, bool ignoreInvincibility = false)
{
	if (invincible && !ignoreInvincibility) return false;

	currentHealth -= damage;
	
	EmitSignal(SignalName.HealthChanged, currentHealth, maxHealth);

	GD.Print("Player took damage: ", damage);

	if (currentHealth <= 0)
	{
		GD.Print("Player dead");
	}

	invincible = true;

	// Start async flicker effect, but do NOT await here
	_ = FlickerAlphaEffect();

	// Start invincibility timer (also async)
	_ = InvincibilityTimer();

	return true;
}

private async Task InvincibilityTimer()
{
	await ToSignal(GetTree().CreateTimer(0.6f), "timeout");
	invincible = false;

	// Ensure sprite is fully visible
	Color finalColor = soulSprite.Modulate;
	finalColor.A = 1f;
	soulSprite.Modulate = finalColor;
}

private async Task FlickerAlphaEffect()
{
	float flickerInterval = 0.05f;
	float lowAlpha = 0.3f;
	float highAlpha = 1f;
	bool toggle = false;

	while (invincible)
	{
		toggle = !toggle;
		float alpha = toggle ? lowAlpha : highAlpha;

		Color c = soulSprite.Modulate;
		c.A = alpha;
		soulSprite.Modulate = c;

		await ToSignal(GetTree().CreateTimer(flickerInterval), "timeout");
	}

	// Ensure fully visible at the end
	Color finalColor = soulSprite.Modulate;
	finalColor.A = 1f;
	soulSprite.Modulate = finalColor;
}

public int GetMaxHealth() {
	return maxHealth;
}

public int GetCurrentHealth() {
	return currentHealth;
}

public bool IsInvincible()
{
	return invincible;
}

public void TransformToBattleship()
{
	isBattleship = true;
	if (hasTransformedBefore)
	{
		soulSprite.Play("StaticShip");
	}
	else
	{
		soulSprite.Play("Battleship");
		hasTransformedBefore = true;
	}
}

public void TransformToCar() {
	isCar = true;
	if (hasTransformedCarBefore) {
		soulSprite.Play("StaticCar");
	}
	else {
		soulSprite.Play("Car");
		hasTransformedCarBefore = true;
	}
}

public void ReturnToNormal() {
	isBattleship = false;
	isCar = false;
	soulSprite.Play("default");
}

private void ShootRandomBullet()
{
	if (bulletScenes == null || bulletScenes.Length == 0 || !isBattleship) return;
	int index = rng.RandiRange(0, bulletScenes.Length - 1);
	PackedScene chosenScene = bulletScenes[index];
	if (chosenScene == null) return;

	PlayerBullet bullet = chosenScene.Instantiate<PlayerBullet>();
	GD.Print("bullet spawned");
	//bullet.ZIndex = 10;
	
	GetTree().Root.AddChild(bullet);

	// Spawn slightly above the soul
	bullet.GlobalPosition = GlobalPosition + new Vector2(0, -soulHalfHeight - 8f);

	
}

}
