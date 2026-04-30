using Godot;
using System;
using System.Collections.Generic;

public partial class DialogueManager : CanvasLayer
{
	public static DialogueManager Instance;

	[Export] public Label NameLabel;
	[Export] public RichTextLabel DialogueText;
	[Export] public TextureRect Portrait;
	[Export] public VBoxContainer ChoicesContainer;
	[Export] public PackedScene ChoiceButtonScene;

	private Godot.Collections.Dictionary dialogue;
	private string currentNode;

	private bool typing = false;

	public override void _Ready()
	{
		Instance = this;
		Hide();
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			if (typing)
			{
				DialogueText.VisibleCharacters = DialogueText.Text.Length;
				typing = false;
			}
			else
			{
				ContinueDialogue();
			}
		}
	}

	// -------------------------
	// Start Dialogue
	// -------------------------

	public void StartDialogue(string jsonPath)
	{
		string jsonText = FileAccess.GetFileAsString(jsonPath);
		var parsed = Json.ParseString(jsonText);

		if (parsed.VariantType == Variant.Type.Nil) 
		{
			GD.PrintErr("Failed to parse JSON: " + jsonPath);
			return;
		}
		dialogue = (Godot.Collections.Dictionary)parsed;

		currentNode = "start";

		Show();
		PlayerController.Instance.CanMove = false;

		ShowNode();
	}

	// -------------------------
	// Show Dialogue Node
	// -------------------------

	private async void ShowNode()
	{
		var node = dialogue[currentNode].AsGodotDictionary();

		DialogueText.Text = node["text"].AsString();
		DialogueText.VisibleCharacters = 0;

		NameLabel.Text = node.ContainsKey("name") ? node["name"].AsString() : "";

		if (node.ContainsKey("portrait"))
		{
			Portrait.Texture = GD.Load<Texture2D>(node["portrait"].AsString());
			Portrait.Visible = true;
		}
		else
		{
			Portrait.Visible = false;
		}

		typing = true;

		for (int i = 0; i < DialogueText.Text.Length; i++)
		{
			DialogueText.VisibleCharacters++;

			await ToSignal(GetTree().CreateTimer(0.02f), "timeout");

			if (!typing)
				break;
		}

		typing = false;

		if (node.ContainsKey("choices"))
		{
			ShowChoices(node["choices"].AsGodotArray());
		}
	}

	// -------------------------
	// Continue Dialogue
	// -------------------------

	private void ContinueDialogue()
{
	if (!dialogue.ContainsKey(currentNode))
	{
		GD.PrintErr("Dialogue node not found: " + currentNode);
		EndDialogue();
		return;
	}

	var node = dialogue[currentNode].AsGodotDictionary();

	// If typing is still happening, skip typewriter (this should be handled in _Process)
	if (typing)
		return;

	// If node has choices, don't automatically continue
	if (node.ContainsKey("choices"))
		return;

	// Go to next node if it exists
	if (node.ContainsKey("next"))
	{
		currentNode = node["next"].AsString();
		ShowNode();
		return;
	}

	// Otherwise end dialogue
	EndDialogue();
}

	// -------------------------
	// Choices
	// -------------------------

	private void ShowChoices(Godot.Collections.Array choices)
	{
		ChoicesContainer.Show();

		foreach (Node child in ChoicesContainer.GetChildren())
			child.QueueFree();

		foreach (var choice in choices)
		{
			var data = choice.AsGodotDictionary();

			Button btn = ChoiceButtonScene.Instantiate<Button>();

			btn.Text = data["text"].AsString();

			string nextNode = data["next"].AsString();

			btn.Pressed += () =>
			{
				ChoicesContainer.Hide();
				currentNode = nextNode;
				ShowNode();
			};

			ChoicesContainer.AddChild(btn);
		}
	}

	// -------------------------
	// End Dialogue
	// -------------------------

	private void EndDialogue()
	{
		PlayerController.Instance.CanMove = true;
		Hide();
	}
}
