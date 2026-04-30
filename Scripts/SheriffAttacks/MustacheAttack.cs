using Godot;
using System.Threading.Tasks;

public partial class MustacheAttack : AttackPattern
{
	[Export] private PackedScene leftMustacheScene;
	[Export] private PackedScene rightMustacheScene;
	[Export] private float attackDuration = 8f;
	[Export] private float startNormalizedY = 0.3f;

	public override async Task Execute(
		PlayerSoul player,
		BattleBox box,
		EnemyManager enemyManager
	)
	{
		await box.SetMode(BattleBoxMode.Enemy);
		player.Spawn();

		Mustache leftMustache  = SpawnMustache(leftMustacheScene,  box, player, MustacheSide.Left);
		Mustache rightMustache = SpawnMustache(rightMustacheScene, box, player, MustacheSide.Right);

		await ToSignal(GetTree().CreateTimer(attackDuration), "timeout");

		leftMustache?.QueueFree();
		rightMustache?.QueueFree();

		await ToSignal(GetTree().CreateTimer(1f), "timeout");
	}

	private Mustache SpawnMustache(PackedScene scene, BattleBox box, PlayerSoul player, MustacheSide side)
	{
		Mustache mustache = scene.Instantiate<Mustache>();
		mustache.Side = side;

		GetTree().CurrentScene.AddChild(mustache);
		mustache.ZIndex = 1;

		Vector2 min = box.GetInnerMinBounds();
		Vector2 max = box.GetInnerMaxBounds();

		float wallX  = side == MustacheSide.Left ? min.X-40f : max.X+40f;
		float startY = Mathf.Lerp(min.Y, max.Y, startNormalizedY);

		mustache.GlobalPosition = new Vector2(wallX, startY);
		mustache.SetRestX(wallX);
		mustache.Initialize(box, player);

		if (side == MustacheSide.Right)
			mustache.SetInitialBobDirection(-1f);

		return mustache;
	}
}
