using Godot;
using System;

public partial class BattleNarrator : Node
{
	[Export] public RichTextLabel DialogueText;
	[Export] public float TypeSpeed = 0.03f;

	private string lastMessage = "";
	private int typingId = 0;

	// -------------------------
	// Show Message
	// -------------------------

	public async void ShowMessage(string message, float typeSpeed = 0.03f, int hold = 0)
	{
		if (hold == 1)
			lastMessage = message;

		TypeSpeed = typeSpeed;

		typingId++;
		int currentTyping = typingId;

		DialogueText.Text = message;
		DialogueText.VisibleCharacters = 0;

		for (int i = 0; i < message.Length; i++)
		{
			if (currentTyping != typingId)
				return;

			DialogueText.VisibleCharacters++;

			if (TypeSpeed > 0)
			{
				await ToSignal(
					GetTree().CreateTimer(TypeSpeed),
					SceneTreeTimer.SignalName.Timeout
				);
			}
		}
	}

	// -------------------------
	// Show Last Message
	// -------------------------

	public void ShowLastMessage(float typeSpeed = 0.03f)
	{
		ShowMessage(lastMessage, typeSpeed);
	}

	// -------------------------
	// Instantly Finish Typing
	// -------------------------

	public void FinishTyping()
	{
		DialogueText.VisibleCharacters = DialogueText.Text.Length;
	}
}
