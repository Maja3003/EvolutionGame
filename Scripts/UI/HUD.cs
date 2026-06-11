using Godot;

namespace EvoTap
{
	/// <summary>
	/// Główny HUD gry — wyświetla DNA, DPS, przyciski nawigacji.
	///
	/// Scena: res://Scenes/UI/HUD.tscn
	/// Struktura sceny:
	///   HUD (Control)
	///   ├── TopBar (HBoxContainer)
	///   │   ├── DnaLabel (Label)          ← aktualne DNA
	///   │   └── DpsLabel (Label)          ← DNA/sek
	///   ├── BiomeName (Label)
	///   ├── AtlasButton (Button)
	///   ├── BiomeSelectButton (Button)
	///   └── PrestigeButton (Button)       ← widoczny tylko gdy dostępne
	/// </summary>
	public partial class HUD : CanvasLayer
	{
		[Export] private Label _dnaLabel;
		[Export] private Label _dpsLabel;
		[Export] private Label _biomeNameLabel;
		[Export] private Button _atlasButton;
		[Export] private Button _biomeSelectButton;
		[Export] private Button _prestigeButton;

		// Sceny do załadowania przez przyciski
		[Export] private PackedScene _atlasScene;
		[Export] private PackedScene _biomeSelectScene;
		[Export] private PackedScene _prestigeConfirmScene;

		public override void _Ready()
		{
			// Podłącz sygnały GameManagera
			GameManager.Instance.DnaChanged += OnDnaChanged;
			GameManager.Instance.BiomeChanged += OnBiomeChanged;
			GameManager.Instance.PrestigeAvailable += OnPrestigeAvailable;

			// Przyciski
			_atlasButton.Pressed       += () => OpenScene(_atlasScene);
			_biomeSelectButton.Pressed += () => OpenScene(_biomeSelectScene);
			_prestigeButton.Pressed    += () => OpenScene(_prestigeConfirmScene);

			// Init
			OnDnaChanged(GameManager.Instance.Dna);
			OnBiomeChanged(GameManager.Instance.CurrentBiome);
			_prestigeButton.Visible = GameManager.Instance.IsPrestigeAvailable();
		}

		public override void _Process(double delta)
		{
			// Aktualizuj DPS co klatkę (wystarczy co 0.5 sek — optymalizacja niżej)
			if (_dpsLabel != null)
			{
				double dps = GameManager.Instance.GetPassiveDnaPerSecond();
				_dpsLabel.Text = dps > 0
					? $"+ {GameManager.FormatDna(dps)}/sek"
					: "";
			}
		}

		private void OnDnaChanged(double newAmount)
		{
			if (_dnaLabel != null)
				_dnaLabel.Text = $"🧬 {GameManager.FormatDna(newAmount)}";
		}

		private void OnBiomeChanged(BiomeType biome)
{
	var data = BiomeManager.Instance?.GetBiomeData(biome);
	if (_biomeNameLabel != null)
		_biomeNameLabel.Text = data?.BiomeName ?? biome.ToString();
}

		private void OnPrestigeAvailable()
		{
			if (_prestigeButton != null)
				_prestigeButton.Visible = true;
		}

		private void OpenScene(PackedScene scene)
		{
			if (scene == null) return;
			var instance = scene.Instantiate();
			GetTree().Root.AddChild(instance);
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.DnaChanged -= OnDnaChanged;
				GameManager.Instance.BiomeChanged -= OnBiomeChanged;
				GameManager.Instance.PrestigeAvailable -= OnPrestigeAvailable;
			}
		}
	}
}
