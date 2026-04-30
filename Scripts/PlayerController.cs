using Godot;
using System;

public partial class PlayerController : CharacterBody2D
{
	public static PlayerController Instance;
	private Npc currentNPC;
	private Area2D interactionArea;
	public bool CanMove = true;
	

	// -----------------------
	// Modes
	// -----------------------

	public enum PlayerMode
	{
		Overworld,
		Battle
	}

	public PlayerMode Mode = PlayerMode.Overworld;

	// -----------------------
	// Movement
	// -----------------------

	[Export] public float OverworldSpeed = 200f;
	[Export] public float BattleSpeed = 150f;
	[Export] public float SprintMultiplier = 2f;

	private Vector2 moveInput;

	// -----------------------
	// Shooting
	// -----------------------

	[Export] public PackedScene BattleshipBulletScene;
	[Export] public float FireCooldown = 0.5f;

	private float lastFireTime = 0f;
	public bool IsBattleship = false;

	// -----------------------
	// Health
	// -----------------------

	[Export] public int MaxHealth = 10;

	private int health;
	private bool isInvincible = false;

	[Export] public float InvincibilityDuration = 2f;
	[Export] public float FlashInterval = 0.1f;

	// -----------------------
	// Nodes
	// -----------------------

	private AnimatedSprite2D sprite;
	private AnimationPlayer animator;

	// -----------------------
	// Initialization
	// -----------------------

	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();

		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		animator = GetNode<AnimationPlayer>("AnimationPlayer");
		interactionArea = GetNode<Area2D>("InteractionArea");

		health = MaxHealth;
	}

	// -----------------------
	// Update
	// -----------------------

	public override void _Process(double delta)
	{
		moveInput = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
            "move_down"
		);

		if (Mode == PlayerMode.Battle)
		{
			HandleBattleInput();
		}
		
		if (Mode == PlayerMode.Overworld && Input.IsActionJustPressed("interact")) {
			CheckInteraction();
		}
	}
	
	private void CheckInteraction()
	{
		var bodies = interactionArea.GetOverlappingBodies();

		foreach (var body in bodies)
		{
			if (body is Npc npc)
			{
				if (!DialogueManager.Instance.Visible) {
					npc.StartDialogue();
				}
				return;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (Mode)
		{
			case PlayerMode.Overworld:
				HandleOverworldMovement();
				break;

			case PlayerMode.Battle:
				HandleBattleMovement();
				break;
		}
	}

	// -----------------------
	// Movement
	// -----------------------

	private void HandleOverworldMovement()
	{
		Velocity = moveInput * OverworldSpeed;
		if (!CanMove)
		{
			Velocity = Vector2.Zero;
		}
		UpdateAnimation();
		MoveAndSlide();
	}
	
	private void PlayAnimation(string anim)
	{
		if (sprite.Animation != anim)
			sprite.Play(anim);
	}
	
	private void UpdateAnimation()
	{
		if (Velocity == Vector2.Zero)
		{
			PlayAnimation("idle");
			return;
		}

		if (Mathf.Abs(moveInput.X) > Mathf.Abs(moveInput.Y))
		{
			if (moveInput.X > 0)
				PlayAnimation("walk_right");
			else
				PlayAnimation("walk_left");
		}
		else
		{
			if (moveInput.Y > 0)
				PlayAnimation("walk_down");
			else
				PlayAnimation("walk_up");
		}
	}

	private void HandleBattleMovement()
	{
		float speed = BattleSpeed;

		if (Input.IsActionPressed("sprint")) {
			speed *= SprintMultiplier;
		}

		Velocity = moveInput * speed;

		MoveAndSlide();
	}

	// -----------------------
	// Battle Actions
	// -----------------------

	private void HandleBattleInput()
	{
		if (!IsBattleship)
			return;

		if (Input.IsActionPressed("fire") &&
			Time.GetTicksMsec() > lastFireTime + FireCooldown * 1000)
		{
			FireBullet();
			lastFireTime = Time.GetTicksMsec();
		}
	}

	private void FireBullet()
	{
		if (BattleshipBulletScene == null)
			return;

		Node2D bullet = (Node2D)BattleshipBulletScene.Instantiate();

		bullet.Position = Position + new Vector2(0, -10);

		GetParent().AddChild(bullet);
	}

	// -----------------------
	// Health System
	// -----------------------

	public void TakeDamage(int damage)
	{
		if (isInvincible)
			return;

		health -= damage;

		if (health <= 0)
		{
			Die();
			return;
		}

		StartInvincibility();
	}

	private void Die()
	{
		GD.Print("Player died");
		QueueFree();
	}

	// -----------------------
	// Invincibility Frames
	// -----------------------

	private async void StartInvincibility()
	{
		isInvincible = true;

		float elapsed = 0f;

		while (elapsed < InvincibilityDuration)
		{
			sprite.Visible = !sprite.Visible;

			await ToSignal(
				GetTree().CreateTimer(FlashInterval),
                "timeout"
			);

			elapsed += FlashInterval;
		}

		sprite.Visible = true;
		isInvincible = false;
	}

	// -----------------------
	// Mode Switching
	// -----------------------

	public void EnterBattle()
	{
		Mode = PlayerMode.Battle;
		GD.Print("Entering battle mode");
	}

	public void ExitBattle()
	{
		Mode = PlayerMode.Overworld;
		GD.Print("Returning to overworld");
	}
}
