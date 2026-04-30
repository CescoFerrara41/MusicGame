using Godot;
using System.Collections.Generic;

/// <summary>
/// BattleSelectScreen — displays a paginated list of battles (5 per page).
/// The player navigates with UI Up/Down, pages with UI Page Up/Down,
/// and confirms with the "interact" action (default: "ui_accept").
///
/// HOW TO ADD BATTLES:
///   Add entries to the _battles list below. Each entry is a (Display Name, Scene Path) pair.
///   The scene path must point to a valid .tscn file under res://.
///
/// HOW TO SET THE "INTERACT" ACTION:
///   By default this uses Godot's built-in "ui_accept" action (Enter / Space / Gamepad A).
///   If your project defines a custom action (e.g. "interact"), change InteractAction below.
/// </summary>
public partial class BattleSelectScreen : Control
{
	// -------------------------------------------------------------------------
	// Configuration — edit these to match your project
	// -------------------------------------------------------------------------

	/// <summary>The InputMap action name used to confirm/select a battle.</summary>
	private const string InteractAction = "interact"; // change to "interact" if you have a custom action

	/// <summary>How many battles to show per page.</summary>
	private const int ItemsPerPage = 5;

	/// <summary>
	/// Master list of battles.
	/// Format: (Display Name, Scene Path relative to res://)
	/// </summary>
	private readonly List<(string Name, string ScenePath)> _battles = new()
	{
		("Forest Ambush",      "res://Prefabs/Battles/ForestAmbush.tscn"),
		("Desert Siege",       "res://Battles/DesertSiege.tscn"),
		("Cave Crawl",         "res://Battles/CaveCrawl.tscn"),
		("Mountain Pass",      "res://Battles/MountainPass.tscn"),
		("Swamp Skirmish",     "res://Battles/SwampSkirmish.tscn"),
		("Castle Courtyard",   "res://Battles/CastleCourtyard.tscn"),
		("Frozen Tundra",      "res://Battles/FrozenTundra.tscn"),
		("Volcano Lair",       "res://Battles/VolcanoLair.tscn"),
		("Ancient Ruins",      "res://Battles/AncientRuins.tscn"),
		("Final Showdown",     "res://Battles/FinalShowdown.tscn"),
	};

	// -------------------------------------------------------------------------
	// Colors & style — tweak to match your pixel art palette
	// -------------------------------------------------------------------------
	private static readonly Color ColorBackground    = new(0.05f, 0.05f, 0.10f, 1f);
	private static readonly Color ColorPanelBg       = new(0.10f, 0.10f, 0.18f, 1f);
	private static readonly Color ColorPanelBorder   = new(0.25f, 0.25f, 0.45f, 1f);
	private static readonly Color ColorSelected      = new(0.90f, 0.75f, 0.20f, 1f);  // gold highlight
	private static readonly Color ColorSelectedBg    = new(0.20f, 0.16f, 0.04f, 1f);
	private static readonly Color ColorNormal        = new(0.78f, 0.78f, 0.88f, 1f);
	private static readonly Color ColorTitle         = new(0.90f, 0.75f, 0.20f, 1f);
	private static readonly Color ColorHint          = new(0.45f, 0.45f, 0.60f, 1f);
	private static readonly Color ColorDivider       = new(0.90f, 0.75f, 0.20f, 1f);
	private static readonly Color ColorCursor        = new(0.90f, 0.75f, 0.20f, 1f);

	// -------------------------------------------------------------------------
	// Runtime state
	// -------------------------------------------------------------------------
	private int _selectedIndex = 0;   // index within the full _battles list
	private int _pageStart     = 0;   // index of the first item shown on the current page

	// Built node references
	private VBoxContainer _battleList;
	private Label         _pageIndicator;

	// -------------------------------------------------------------------------
	// Godot lifecycle
	// -------------------------------------------------------------------------

	public override void _Ready()
	{
		// Build the entire UI in code so there are no missing-node errors
		// even if the .tscn was not imported correctly.
		BuildUI();
		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_down"))
		{
			Move(1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_up"))
		{
			Move(-1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_page_down"))
		{
			MovePage(1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("ui_page_up"))
		{
			MovePage(-1);
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InteractAction))
		{
			ConfirmSelection();
			GetViewport().SetInputAsHandled();
		}
	}

	// -------------------------------------------------------------------------
	// Navigation helpers
	// -------------------------------------------------------------------------

	private void Move(int delta)
	{
		_selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, _battles.Count - 1);

		// Scroll page window to keep selected item visible
		if (_selectedIndex < _pageStart)
			_pageStart = _selectedIndex;
		else if (_selectedIndex >= _pageStart + ItemsPerPage)
			_pageStart = _selectedIndex - ItemsPerPage + 1;

		Refresh();
	}

	private void MovePage(int delta)
	{
		int newPage = (CurrentPage() + delta);
		newPage = Mathf.Clamp(newPage, 0, TotalPages() - 1);
		_pageStart     = newPage * ItemsPerPage;
		_selectedIndex = Mathf.Clamp(_selectedIndex, _pageStart, Mathf.Min(_pageStart + ItemsPerPage - 1, _battles.Count - 1));
		Refresh();
	}

	private void ConfirmSelection()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _battles.Count)
			return;

		string scenePath = _battles[_selectedIndex].ScenePath;

		if (!ResourceLoader.Exists(scenePath))
		{
			GD.PrintErr($"[BattleSelectScreen] Scene not found: {scenePath}");
			return;
		}

		GetTree().ChangeSceneToFile(scenePath);
	}

	// -------------------------------------------------------------------------
	// UI refresh — rebuilds the visible battle rows
	// -------------------------------------------------------------------------

	private void Refresh()
	{
		// Clear old rows
		foreach (Node child in _battleList.GetChildren())
			child.QueueFree();

		int pageEnd = Mathf.Min(_pageStart + ItemsPerPage, _battles.Count);

		for (int i = _pageStart; i < pageEnd; i++)
		{
			bool isSelected = (i == _selectedIndex);
			_battleList.AddChild(BuildRow(_battles[i].Name, i + 1, isSelected));
		}

		// Page indicator
		int total   = TotalPages();
		int current = CurrentPage() + 1;
		_pageIndicator.Text = total > 1 ? $"Page {current} / {total}" : "";
	}

	// -------------------------------------------------------------------------
	// Row builder
	// -------------------------------------------------------------------------

	private Control BuildRow(string name, int number, bool selected)
	{
		// Outer panel (acts as the row background + border)
		var panel = new PanelContainer();
		panel.CustomMinimumSize = new Vector2(400, 52);

		var styleBox = new StyleBoxFlat();
		styleBox.BgColor           = selected ? ColorSelectedBg : ColorPanelBg;
		styleBox.BorderColor       = selected ? ColorSelected    : ColorPanelBorder;
		styleBox.SetBorderWidthAll(selected ? 2 : 1);
		styleBox.SetCornerRadiusAll(0); // sharp pixel corners
		styleBox.ContentMarginLeft   = 12;
		styleBox.ContentMarginRight  = 12;
		styleBox.ContentMarginTop    = 10;
		styleBox.ContentMarginBottom = 10;
		panel.AddThemeStyleboxOverride("panel", styleBox);

		// Inner horizontal layout: cursor | number | name
		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 10);

		// Cursor arrow
		var cursor = new Label();
		cursor.Text = selected ? "►" : "  ";
		cursor.AddThemeColorOverride("font_color", ColorCursor);
		cursor.AddThemeFontSizeOverride("font_size", 14);
		cursor.CustomMinimumSize = new Vector2(18, 0);
		hbox.AddChild(cursor);

		// Battle number
		var numLabel = new Label();
		numLabel.Text = $"{number:D2}.";
		numLabel.AddThemeColorOverride("font_color", selected ? ColorSelected : ColorHint);
		numLabel.AddThemeFontSizeOverride("font_size", 14);
		numLabel.CustomMinimumSize = new Vector2(32, 0);
		hbox.AddChild(numLabel);

		// Battle name
		var nameLabel = new Label();
		nameLabel.Text = name.ToUpper();
		nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		nameLabel.AddThemeColorOverride("font_color", selected ? ColorSelected : ColorNormal);
		nameLabel.AddThemeFontSizeOverride("font_size", selected ? 15 : 14);
		hbox.AddChild(nameLabel);

		panel.AddChild(hbox);
		return panel;
	}

	// -------------------------------------------------------------------------
	// UI construction (runs once in _Ready)
	// -------------------------------------------------------------------------

	private void BuildUI()
	{
		// Root fills the viewport
		AnchorRight  = 1; AnchorBottom = 1;
		OffsetRight  = 0; OffsetBottom = 0;

		// Dark background
		var bg = new ColorRect();
		bg.Color = ColorBackground;
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		// Centered outer VBox
		var outer = new VBoxContainer();
		outer.SetAnchorsPreset(LayoutPreset.Center);
		outer.OffsetLeft   = -210;
		outer.OffsetTop    = -240;
		outer.OffsetRight  =  210;
		outer.OffsetBottom =  240;
		outer.AddThemeConstantOverride("separation", 16);
		AddChild(outer);

		// ── Title ────────────────────────────────────────────────────────────
		var title = new Label();
		title.Text = "✦  CHOOSE YOUR BATTLE  ✦";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeColorOverride("font_color", ColorTitle);
		title.AddThemeFontSizeOverride("font_size", 20);
		outer.AddChild(title);

		// Divider
		var divider = new ColorRect();
		divider.Color = ColorDivider;
		divider.CustomMinimumSize = new Vector2(420, 2);
		outer.AddChild(divider);

		// ── Battle list ───────────────────────────────────────────────────────
		_battleList = new VBoxContainer();
		_battleList.CustomMinimumSize = new Vector2(420, 0);
		_battleList.AddThemeConstantOverride("separation", 6);
		outer.AddChild(_battleList);

		// Divider
		var divider2 = new ColorRect();
		divider2.Color = ColorDivider;
		divider2.CustomMinimumSize = new Vector2(420, 2);
		outer.AddChild(divider2);

		// ── Nav hints ────────────────────────────────────────────────────────
		var hints = new HBoxContainer();
		hints.Alignment = BoxContainer.AlignmentMode.Center;
		hints.AddThemeConstantOverride("separation", 24);
		outer.AddChild(hints);

		AddHint(hints, "↑↓", "Navigate");
		AddHint(hints, "PgUp/Dn", "Page");
		AddHint(hints, "Enter/Space", "Select");

		// ── Page indicator ───────────────────────────────────────────────────
		_pageIndicator = new Label();
		_pageIndicator.HorizontalAlignment = HorizontalAlignment.Center;
		_pageIndicator.AddThemeColorOverride("font_color", ColorHint);
		_pageIndicator.AddThemeFontSizeOverride("font_size", 11);
		outer.AddChild(_pageIndicator);
	}

	private static void AddHint(HBoxContainer parent, string key, string action)
	{
		var keyLabel = new Label();
		keyLabel.Text = $"[{key}]";
		keyLabel.AddThemeColorOverride("font_color", new Color(0.90f, 0.75f, 0.20f, 1f));
		keyLabel.AddThemeFontSizeOverride("font_size", 11);

		var actLabel = new Label();
		actLabel.Text = action;
		actLabel.AddThemeColorOverride("font_color", new Color(0.50f, 0.50f, 0.65f, 1f));
		actLabel.AddThemeFontSizeOverride("font_size", 11);

		parent.AddChild(keyLabel);
		parent.AddChild(actLabel);
	}

	// -------------------------------------------------------------------------
	// Utilities
	// -------------------------------------------------------------------------

	private int TotalPages()  => Mathf.CeilToInt((float)_battles.Count / ItemsPerPage);
	private int CurrentPage() => _pageStart / ItemsPerPage;
}
