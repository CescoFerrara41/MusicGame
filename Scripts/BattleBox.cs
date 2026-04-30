using Godot;
using System.Threading.Tasks;

public enum BattleBoxMode
	{
		Hidden,
		Dialogue,
		Attack,
		Enemy,
		WideEnemy,
		WidestEnemy,
		TallEnemy,
		SlightlyLargerEnemy,
		CarThreeLanes,
		SmallerEnemy,
		CarFiveLanes
	}

public partial class BattleBox : Node2D
{
	[Export] private Sprite2D top;
	[Export] private Sprite2D bottom;
	[Export] private Sprite2D left;
	[Export] private Sprite2D right;

	[Export] private Sprite2D topLeft;
	[Export] private Sprite2D topRight;
	[Export] private Sprite2D bottomLeft;
	[Export] private Sprite2D bottomRight;

	[Export] private CollisionShape2D collisionShape;

	[Export] private float resizeDuration = 0.35f;
	[Export] private Vector2 targetSize = new Vector2(50, 50);
	
	[Export] private Vector2 dialogueSize = new Vector2(300, 54);
	[Export] private Vector2 attackSize = new Vector2(125, 25);
	[Export] private Vector2 enemySize = new Vector2(50, 50);
	[Export] private Vector2 widerEnemySize = new Vector2(80, 60);
	[Export] private Vector2 widestEnemySize = new Vector2(96, 96);
	[Export] private Vector2 tallEnemySize = new Vector2 (50, 100);
	[Export] private Vector2 slightlyLargerEnemySize = new Vector2(65, 65);
	[Export] private Vector2 carThreeLanesSize = new Vector2(70, 100);
	[Export] private Vector2 smallerEnemySize = new Vector2(25, 25);
	[Export] private Vector2 carFiveLanesSize = new Vector2(90, 100);

	[Export] private Vector2 dialoguePosition = new Vector2(160, 118);
	[Export] private Vector2 attackPosition = new Vector2(160f, 116.5f);
	[Export] private Vector2 enemyPosition = new Vector2(160, 119);
	[Export] private Vector2 widerEnemyPosition = new Vector2(160, 114);
	[Export] private Vector2 widestEnemyPosition = new Vector2(160, 90);
	[Export] private Vector2 tallEnemyPosition = new Vector2(160, 90);
	[Export] private Vector2 slightlyLargerEnemyPosition = new Vector2(160, 112);
	[Export] private Vector2 carThreeLanesPosition = new Vector2(160, 109);
	[Export] private Vector2 smallerEnemyPosition = new Vector2(160, 106);
	[Export] private Vector2 carFiveLanesPosition = new Vector2(160, 109);
	
	[Export] private Sprite2D blackBackground;
	
	[Export] private Area2D cleanupTop;
	[Export] private Area2D cleanupBottom;
	[Export] private Area2D cleanupLeft;
	[Export] private Area2D cleanupRight;

	[Export] private float cleanupOffset = 30f; // distance outside box

	private Vector2 currentSize = Vector2.Zero;
	private float thickness = 2;


	public override void _Ready()
	{
		//Visible = false;

		RectangleShape2D rect = collisionShape.Shape as RectangleShape2D;
		rect.Size = Vector2.Zero;

		UpdateBox();
	}
	
	public Vector2 GetInnerMinBounds()
{
	float halfW = currentSize.X / 2f;
	float halfH = currentSize.Y / 2f;

	return GlobalPosition + new Vector2(-halfW, -halfH+1);
}

public Vector2 GetInnerMaxBounds()
{
	float halfW = currentSize.X / 2f;
	float halfH = currentSize.Y / 2f;

	return GlobalPosition + new Vector2(halfW, halfH);
}
	
	public float GetWallThickness()
	{
		return thickness;
	}

	public async Task Resize(Vector2 newSize)
	{
		Vector2 startSize = currentSize;

		var tween = CreateTween();

		tween.TweenMethod(
			Callable.From<float>(t =>
			{
				currentSize = startSize.Lerp(newSize, t);
				UpdateBox();
			}),
			0f,
			1f,
			resizeDuration
		).SetTrans(Tween.TransitionType.Sine)
		 .SetEase(Tween.EaseType.InOut);

		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public async Task ShowBox()
	{
		Visible = true;
		await Resize(targetSize);
	}

	public async Task HideBox()
	{
		await Resize(Vector2.Zero);
		Visible = false;
	}

	private void UpdateBox()
	{
		float width = currentSize.X;
		float height = currentSize.Y;

		float halfW = width / 2f;
		float halfH = height / 2f;

		float halfT = thickness / 2f;

		// WALL POSITIONS
		top.Position = new Vector2(0, -halfH - halfT+1);
		bottom.Position = new Vector2(0, halfH + halfT);
		left.Position = new Vector2(-halfW - halfT, 0);
		right.Position = new Vector2(halfW + halfT, 0);

		// WALL SIZES
		top.Scale = new Vector2(width, thickness);
		bottom.Scale = new Vector2(width, thickness);
		left.Scale = new Vector2(thickness, height);
		right.Scale = new Vector2(thickness, height);

		// CORNERS
		topLeft.Position = new Vector2(-halfW - halfT, -halfH - halfT+1);
		topRight.Position = new Vector2(halfW + halfT, -halfH - halfT+1);

		bottomLeft.Position = new Vector2(-halfW - halfT, halfH + halfT);
		bottomRight.Position = new Vector2(halfW + halfT, halfH + halfT);

		// CORNER SIZE
		topLeft.Scale = new Vector2(thickness, thickness);
		topRight.Scale = new Vector2(thickness, thickness);
		bottomLeft.Scale = new Vector2(thickness, thickness);
		bottomRight.Scale = new Vector2(thickness, thickness);
		
		blackBackground.Scale = currentSize;
		blackBackground.Position = new Vector2(halfW + halfT-1, halfH + halfT);

		// COLLISION
		RectangleShape2D rect = collisionShape.Shape as RectangleShape2D;
		rect.Size = currentSize;
		
		UpdateCleanupZones(halfW, halfH);
	}
	
	
	public async Task SetMode(BattleBoxMode mode)
	{
		Vector2 targetPos = Position;
		Vector2 size = currentSize;

		switch (mode)
		{
			case BattleBoxMode.Hidden:
				await HideBox();
				return;

			case BattleBoxMode.Dialogue:
				targetPos = dialoguePosition;
				size = dialogueSize;
				break;

			case BattleBoxMode.Attack:
				targetPos = attackPosition;
				size = attackSize;
				break;

			case BattleBoxMode.Enemy:
				targetPos = enemyPosition;
				size = enemySize;
				break;

			case BattleBoxMode.WideEnemy:
				targetPos = widerEnemyPosition;
				size = widerEnemySize;
				break;
				
			case BattleBoxMode.WidestEnemy:
				targetPos = widestEnemyPosition;
				size = widestEnemySize;
				break;
				
			case BattleBoxMode.TallEnemy:
				targetPos = tallEnemyPosition;
				size = tallEnemySize;
				break;
			
			case BattleBoxMode.SlightlyLargerEnemy:
				targetPos = slightlyLargerEnemyPosition;
				size = slightlyLargerEnemySize;
				break;
			
			case BattleBoxMode.CarThreeLanes:
				targetPos = carThreeLanesPosition;
				size = carThreeLanesSize;
				break;
				
			case BattleBoxMode.SmallerEnemy:
				targetPos = smallerEnemyPosition;
				size = smallerEnemySize;
				break;
				
			case BattleBoxMode.CarFiveLanes:
				targetPos = carFiveLanesPosition;
				size = carFiveLanesSize;
				break;
		}

		Visible = true;

		var tween = CreateTween();
		tween.TweenProperty(this, "position", targetPos, resizeDuration);

		await Resize(size);
	}
	
	private void UpdateCleanupZones(float halfW, float halfH)
	{
		cleanupTop.Modulate = new Color(1, 0, 0, 1f);
		cleanupLeft.Modulate = new Color(1, 0, 0, 1f);
		cleanupRight.Modulate = new Color(1, 0, 0, 1f);
		cleanupBottom.Modulate = new Color(1, 0, 0, 1f);
		float offset = cleanupOffset;

		// TOP
		cleanupTop.Position = new Vector2(0, -halfH - offset);
		SetZoneSize(cleanupTop, new Vector2(currentSize.X + 100, 10));

		// BOTTOM
		cleanupBottom.Position = new Vector2(0, halfH + offset);
		SetZoneSize(cleanupBottom, new Vector2(currentSize.X + 100, 10));

		// LEFT
		cleanupLeft.Position = new Vector2(-halfW - offset, 0);
		SetZoneSize(cleanupLeft, new Vector2(10, currentSize.Y + 100));

		// RIGHT
		cleanupRight.Position = new Vector2(halfW + offset, 0);
		SetZoneSize(cleanupRight, new Vector2(10, currentSize.Y + 100));
	}
	
	private void SetZoneSize(Area2D area, Vector2 size)
	{
		var shape = area.GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
		shape.Size = size;
	}
}
