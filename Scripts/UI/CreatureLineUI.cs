using Godot;

namespace EvoTap
{
	/// <summary>
	/// Panel UI jednej linii ewolucji — przycisk odblokowania, pasek postępu, przycisk ewolucji.
	///
	/// Scena: res://Scenes/UI/CreatureLineUI.tscn
	/// Struktura:
	///   CreatureLineUI (PanelContainer)
	///   ├── VBoxContainer
	///   │   ├── CreatureNameLabel (Label)
	///   │   ├── StageLabel (Label)            ← "EVO 2 / 4"
	///   │   ├── ProgressBar (ProgressBar)     ← DNA do następnej ewolucji
	///   │   ├── DnaCostLabel (Label)          ← "250 / 1000 DNA"
	///   │   ├── EvolveButton (Button)         ← "EWOLUUJ!"
	///   │   └── UnlockButton (Button)         ← widoczny gdy zablokowane
	/// </summary>
	public partial class CreatureLineUI : PanelContainer
	{
		[Export] public CreatureLineData LineData { get; set; }

		[Export] private Label _creatureNameLabel;
		[Export] private Label _stageLabel;
		[Export] private ProgressBar _progressBar;
		[Export] private Label _dnaCostLabel;
		[Export] private Button _evolveButton;
		[Export] private Button _unlockButton;

		// Scena pop-upu wyboru ścieżki
		[Export] private PackedScene _pathChoicePopupScene;

		// Referencja do clickArea — żeby podmienić sprite po ewolucji
		[Export] private CreatureClickArea _clickArea;

		private CreatureManager _manager;

		public override void _Ready()
		{
			// Znajdź CreatureManager dla tej linii w drzewie sceny
			// (zakłada się że CreatureManager jest rodzicem lub rodzeństwem)
			_manager = GetParent().FindChild("CreatureManager_" + LineData?.LineId, true, false) as CreatureManager
					   ?? GetParent().GetNodeOrNull<CreatureManager>("CreatureManager");

			if (_manager == null)
			{
				GD.PrintErr($"[CreatureLineUI] Brak CreatureManagera dla linii: {LineData?.LineId}");
				return;
			}

			// Podłącz sygnały managera
			_manager.LineUnlocked    += Refresh;
			_manager.Evolved         += OnEvolved;
			_manager.PathChoiceRequired += ShowPathChoicePopup;

			// Podłącz sygnał DNA (żeby odświeżać przycisk w czasie rzeczywistym)
			GameManager.Instance.DnaChanged += _ => RefreshButtons();

			// Przyciski
			_evolveButton.Pressed += () => _manager.TryEvolve();
			_unlockButton.Pressed += () => _manager.Unlock();

			Refresh();
		}

		// ── ODŚWIEŻANIE ────────────────────────────────────────────────────

		private void Refresh()
		{
			if (_manager == null) return;

			bool unlocked = _manager.IsUnlocked();

			// Przełącz widoczność przycisków
			_unlockButton.Visible = !unlocked;
			_evolveButton.Visible = unlocked && !_manager.IsFinalFormReached();

			if (!unlocked)
			{
				_creatureNameLabel.Text = LineData.LineName;
				_stageLabel.Text = "Zablokowane";
				_dnaCostLabel.Text = $"Odblokuj za {GameManager.FormatDna(LineData.UnlockCost)} DNA";
				_progressBar.Value = 0;
				_unlockButton.Text = $"🔓 {GameManager.FormatDna(LineData.UnlockCost)} DNA";
				RefreshButtons();
				return;
			}

			var stage = _manager.GetCurrentStageData();
			if (stage == null) return;

			_creatureNameLabel.Text = stage.StageName;

			if (_manager.IsFinalFormReached())
			{
				_stageLabel.Text = "✨ Forma Finalna";
				_dnaCostLabel.Text = "Odblokowana!";
				_progressBar.Value = 100;

				// Aktualizuj sprite do formy finalnej
				_clickArea?.SetCreatureAnimation(stage.StageName);
				return;
			}

			int currentIdx = _manager.GetCurrentLinearStageIndex();
			int totalStages = LineData.LinearStages.Count + 1; // +1 za formę finalną
			_stageLabel.Text = $"EVO {currentIdx + 1} / {totalStages}";

			// Pasek postępu DNA
			double? cost = _manager.NextEvolutionCost();
			if (cost.HasValue && cost.Value > 0)
			{
				double dna = GameManager.Instance.Dna;
				double pct = (dna / cost.Value) * 100.0;
				_progressBar.Value = Mathf.Clamp((float)pct, 0f, 100f);
				_dnaCostLabel.Text = $"{GameManager.FormatDna(dna)} / {GameManager.FormatDna(cost.Value)} DNA";
			}

			_clickArea?.SetCreatureAnimation(stage.StageName);
			RefreshButtons();
		}

		private void RefreshButtons()
		{
			if (_manager == null) return;

			_unlockButton.Disabled = !_manager.CanUnlock();
			_evolveButton.Disabled = !_manager.CanEvolve();

			// Kolor przycisku: zielony gdy gotowy, szary gdy nie
			if (!_manager.IsFinalFormReached())
			{
				_evolveButton.SelfModulate = _manager.CanEvolve()
					? new Color(0.3f, 1f, 0.5f)
					: new Color(0.6f, 0.6f, 0.6f);
			}
		}

		private void OnEvolved(int newStageIndex, bool isFinal)
		{
			// Mała animacja celebracji — możesz dodać Tween
			var tween = CreateTween();
			tween.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.1);
			tween.TweenProperty(this, "scale", Vector2.One, 0.15);

			Refresh();
		}

		// ── POP-UP WYBORU ŚCIEŻKI ──────────────────────────────────────────

		private void ShowPathChoicePopup()
		{
			if (_pathChoicePopupScene == null)
			{
				GD.PrintErr("[CreatureLineUI] Brak PathChoicePopupScene!");
				return;
			}

			var popup = _pathChoicePopupScene.Instantiate<PathChoicePopup>();
			GetTree().Root.AddChild(popup);
			popup.Setup(LineData, _manager);
		}

		public override void _Process(double delta)
		{
			// Aktualizuj pasek postępu w czasie rzeczywistym (bez sygnału)
			if (_manager?.IsUnlocked() == true && !_manager.IsFinalFormReached())
			{
				double? cost = _manager.NextEvolutionCost();
				if (cost.HasValue && cost.Value > 0)
				{
					double pct = (GameManager.Instance.Dna / cost.Value) * 100.0;
					_progressBar.Value = Mathf.Clamp((float)pct, 0f, 100f);
					_dnaCostLabel.Text = $"{GameManager.FormatDna(GameManager.Instance.Dna)} / {GameManager.FormatDna(cost.Value)} DNA";
					RefreshButtons();
				}
			}
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance != null)
				GameManager.Instance.DnaChanged -= _ => RefreshButtons();
		}
	}
}
