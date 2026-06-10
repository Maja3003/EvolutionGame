using Godot;

namespace EvoTap
{
	/// <summary>
	/// Modal wyboru ścieżki ewolucji — pojawia się gdy stworzenie osiągnie EVO 3.
	///
	/// Scena: res://Scenes/UI/PathChoicePopup.tscn
	/// Struktura:
	///   PathChoicePopup (CanvasLayer)        ← Z-index 10, żeby był na wierzchu
	///   └── CenterContainer
	///       └── PanelContainer (ciemne tło z zaokrąglonymi rogami)
	///           └── VBoxContainer
	///               ├── TitleLabel (Label)          ← "Wybierz ścieżkę ewolucji!"
	///               ├── CreatureNameLabel (Label)   ← "Twój Wyvern jest gotowy..."
	///               ├── HBoxContainer
	///               │   ├── PathAButton (Button)    ← lewa ścieżka
	///               │   └── PathBButton (Button)    ← prawa ścieżka
	///               └── WarningLabel (Label)        ← "Ta decyzja jest permanentna"
	/// </summary>
	public partial class PathChoicePopup : CanvasLayer
	{
		[Export] private Label _titleLabel;
		[Export] private Label _creatureNameLabel;
		[Export] private Button _pathAButton;
		[Export] private Button _pathBButton;
		[Export] private Label _pathANameLabel;
		[Export] private Label _pathBNameLabel;
		[Export] private Label _pathADescLabel;
		[Export] private Label _pathBDescLabel;
		[Export] private TextureRect _pathAPreview;    // Podgląd sprite formy A
		[Export] private TextureRect _pathBPreview;    // Podgląd sprite formy B

		private CreatureLineData _lineData;
		private CreatureManager _manager;

		// ── SETUP ─────────────────────────────────────────────────────────

		public void Setup(CreatureLineData lineData, CreatureManager manager)
		{
			_lineData = lineData;
			_manager = manager;
		}

		public override void _Ready()
		{
			if (_lineData == null) return;

			// Tekst
			_titleLabel.Text = "Wybierz ścieżkę ewolucji!";
			_creatureNameLabel.Text = $"Twój {_lineData.LinearStages[^1]?.StageName ?? _lineData.LineName} jest gotowy na ostatnią mutację.";

			_pathANameLabel.Text = _lineData.PathAName;
			_pathBNameLabel.Text = _lineData.PathBName;
			_pathADescLabel.Text = _lineData.PathADescription;
			_pathBDescLabel.Text = _lineData.PathBDescription;

			// Podgląd sprite
			if (_pathAPreview != null && _lineData.PathAFinalStage?.Sprite != null)
				_pathAPreview.Texture = _lineData.PathAFinalStage.Sprite;

			if (_pathBPreview != null && _lineData.PathBFinalStage?.Sprite != null)
				_pathBPreview.Texture = _lineData.PathBFinalStage.Sprite;

			// Koszt
			double? costA = _lineData.PathAFinalStage?.DnaCostToEvolve;
			double? costB = _lineData.PathBFinalStage?.DnaCostToEvolve;

			_pathAButton.Text = $"✨ {_lineData.PathAName}\n{(costA.HasValue ? GameManager.FormatDna(costA.Value) + " DNA" : "")}";
			_pathBButton.Text = $"✨ {_lineData.PathBName}\n{(costB.HasValue ? GameManager.FormatDna(costB.Value) + " DNA" : "")}";

			// Przyciski
			_pathAButton.Pressed += () => Choose(EvolutionPath.PathA);
			_pathBButton.Pressed += () => Choose(EvolutionPath.PathB);

			// Animacja wejścia
			Modulate = new Color(1, 1, 1, 0);
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.25);
		}

		// Zamknij popup kliknięciem tła
		public override void _UnhandledInput(InputEvent @event)
		{
			// Nie pozwól zamknąć kliknięciem — gracz MUSI wybrać
		}

		private void Choose(EvolutionPath path)
		{
			_manager?.ChoosePath(path);
			Close();
		}

		private void Close()
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2);
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}
}
