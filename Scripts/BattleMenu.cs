using Godot;
using System.Threading.Tasks;

public partial class BattleMenu : Control
{
	[Export] public TextureRect FightIcon;
	[Export] public TextureRect ActIcon;
	[Export] public TextureRect ItemIcon;
	[Export] public TextureRect MercyIcon;

	[Export] public Texture2D FightNormal;
	[Export] public Texture2D FightSelected;

	[Export] public Texture2D ActNormal;
	[Export] public Texture2D ActSelected;

	[Export] public Texture2D ItemNormal;
	[Export] public Texture2D ItemSelected;

	[Export] public Texture2D MercyNormal;
	[Export] public Texture2D MercySelected;

	private bool interactable = false;
	private int currentIndex = 0;

	private TaskCompletionSource<int> selectionTask;

	public override void _Ready()
	{
		UpdateSelection();
	}

	public void SetInteractable(bool value)
	{
		interactable = value;
	}

	public async Task<int> WaitForSelection()
	{
		selectionTask = new TaskCompletionSource<int>();
		return await selectionTask.Task;
	}

	public override void _Process(double delta)
	{
		if (!interactable)
			return;

		if (Input.IsActionJustPressed("ui_right"))
		{
			currentIndex = (currentIndex + 1) % 4;
			UpdateSelection();
		}

		if (Input.IsActionJustPressed("ui_left"))
		{
			currentIndex = (currentIndex - 1 + 4) % 4;
			UpdateSelection();
		}

		if (Input.IsActionJustPressed("confirm"))
		{
			ConfirmSelection();
		}
	}

	private void ConfirmSelection()
	{
		selectionTask?.SetResult(currentIndex);
	}

	private void UpdateSelection()
	{
		FightIcon.Texture = currentIndex == 0 ? FightSelected : FightNormal;
		ActIcon.Texture = currentIndex == 1 ? ActSelected : ActNormal;
		ItemIcon.Texture = currentIndex == 2 ? ItemSelected : ItemNormal;
		MercyIcon.Texture = currentIndex == 3 ? MercySelected : MercyNormal;
	}
}
