using Godot;

public class DialogueLine
{
	public string Text;
	public Texture2D Portrait;

	public DialogueLine(string text, Texture2D portrait = null)
	{
		Text = text;
		Portrait = portrait;
	}
}
