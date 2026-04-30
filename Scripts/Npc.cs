using Godot;

public partial class Npc : Node2D
{
	[Export] public string DialogueFile = "res://Dialogue/test_dialogue.json";

	public void StartDialogue()
	{
		DialogueManager.Instance.StartDialogue(DialogueFile);
	}
}
