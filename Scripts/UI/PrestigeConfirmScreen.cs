using Godot;

namespace EvoTap
{
	/// <summary>
	/// Ekran potwierdzenia Prestige — ostrzeżenie + przycisk "Tak, zaczynam od nowa".
	///
	/// Scena: res://Scenes/UI/PrestigeConfirmScreen.tscn
	/// Struktura:
	///   PrestigeConfirmScreen (CanvasLayer)    ← Z-index 15 (najwyższy)
	///   └── CenterContainer
	///       └── PanelContainer
	///           └── VBoxContainer
	///               ├── TitleLabel       "⭐ NOWY CYKL"
	///               ├── CycleLabel       "Cykl #1 → Cykl #2"
	///               ├── BonusLabel       "Permanentny bonus: ×2 DNA"
	///               ├── WarningLabel     "Twoje DNA zostanie zresetowane..."
	///               ├── AtlasKeepLabel   "✓ Atlas pozostaje"
	///               ├── HSpacer
	///               ├── ConfirmButton    "🌑 ZACZNIJ NOWY CYKL"
	///               └── CancelButton    "Anuluj"
	/// </summary>
	public partial class PrestigeConfirmScreen : CanvasLayer
	{
		[Export] private Label _titleLabel;
		[Export] private Label _cycleLabel;
		[Export] private Label _bonusLabel;
		[Export] private Label _warningLabel;
		[Export] private Label _atlasKeepLabel;
		[Export] private Button _confirmButton;
		[Export] private Button _cancelButton;

		public override void _Ready()
		{
			var save = SaveManager.Instance.CurrentSave;
			int currentCycle = save.PrestigeCycle;
			double nextMultiplier = save.PrestigeMultiplier * 2.0;

			if (_titleLabel    != null) _titleLabel.Text    = "⭐ NOWY CYKL";
			if (_cycleLabel    != null) _cycleLabel.Text    = $"Cykl #{currentCycle + 1} → Cykl #{currentCycle + 2}";
			if (_bonusLabel    != null) _bonusLabel.Text    = $"Permanentny bonus DNA: ×{nextMultiplier:F0}";
			if (_warningLabel  != null) _warningLabel.Text  =
				"Uwaga: Wszystkie zebrania DNA i ewolucje zostaną zresetowane.\nZaczynasz od nowa z Oceanem.";
			if (_atlasKeepLabel != null) _atlasKeepLabel.Text =
				"✓  Karty Atlasu zostają na zawsze\n✓  Nowe karty Cyklu do odblokowania";

			_confirmButton.Pressed += OnConfirm;
			_cancelButton.Pressed  += Close;

			// Animacja wejścia — delikatne pojawienie
			GetChild<Control>(0).Modulate = new Color(1, 1, 1, 0);
			Scale    = new Vector2(0.92f, 0.92f);
			var tween = CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "modulate", Colors.White, 0.25f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(this, "scale", Vector2.One, 0.25f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}

		private void OnConfirm()
		{
			// Animacja potwierdzenia przed resetem
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.3f);
			tween.TweenCallback(Callable.From(ExecutePrestige));
		}

		private void ExecutePrestige()
		{
			GameManager.Instance.ExecutePrestige();
			QueueFree();
			// Opcjonalnie: załaduj scenę powitania nowego cyklu
			// GetTree().ChangeSceneToFile("res://Scenes/NewCycleIntro.tscn");
		}

		private void Close()
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.18f);
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}
}
