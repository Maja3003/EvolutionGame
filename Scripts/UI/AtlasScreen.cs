using Godot;
using Godot.Collections;

namespace EvoTap
{
	/// <summary>
	/// Ekran Atlasu — kolekcja wszystkich stworzeń.
	///
	/// Scena: res://Scenes/UI/AtlasScreen.tscn
	/// Struktura:
	///   AtlasScreen (CanvasLayer)
	///   └── MarginContainer
	///       └── VBoxContainer
	///           ├── TopBar
	///           │   ├── TitleLabel ("ATLAS EWOLUCJI")
	///           │   └── CloseButton (Button)
	///           ├── BiomeTabBar (HBoxContainer)    ← zakładki biomów
	///           └── GridContainer                  ← karty stworzeń
	/// </summary>
	public partial class AtlasScreen : CanvasLayer
	{
		[Export] private Button _closeButton;
		[Export] private HBoxContainer _biomeTabBar;
		[Export] private GridContainer _cardGrid;
		[Export] private PackedScene _atlasCardScene;    // res://Scenes/UI/AtlasCard.tscn

		private BiomeType _activeBiomeTab = BiomeType.Ocean;

		public override void _Ready()
		{
			_closeButton.Pressed += Close;

			// Utwórz zakładki biomów
			foreach (BiomeType biome in System.Enum.GetValues<BiomeType>())
			{
				var tabBtn = new Button();
				tabBtn.Text = GetBiomeEmoji(biome);
				tabBtn.TooltipText = biome.ToString();
				tabBtn.Pressed += () => SwitchTab(biome);

				// Zablokowane biomy są przyciemnione
				if (!BiomeManager.Instance.IsBiomeUnlocked(biome))
					tabBtn.Modulate = new Color(0.4f, 0.4f, 0.4f);

				_biomeTabBar.AddChild(tabBtn);
			}

			SwitchTab(GameManager.Instance.CurrentBiome);

			// Animacja wejścia
			Modulate = new Color(1, 1, 1, 0);
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", Colors.White, 0.2);
		}

		// ── ZAKŁADKI ───────────────────────────────────────────────────────

		private void SwitchTab(BiomeType biome)
		{
			if (!BiomeManager.Instance.IsBiomeUnlocked(biome)) return;

			_activeBiomeTab = biome;
			PopulateGrid(biome);
		}

		// ── SIATKA KART ────────────────────────────────────────────────────

		private void PopulateGrid(BiomeType biome)
		{
			// Wyczyść poprzednie karty
			foreach (Node child in _cardGrid.GetChildren())
				child.QueueFree();

			var biomeData = BiomeManager.Instance.GetBiomeData(biome);
			if (biomeData == null) return;

			foreach (var lineData in biomeData.CreatureLines)
			{
				// Karty liniowe (EVO1–EVO3)
				foreach (var stage in lineData.LinearStages)
				{
					string cardId = $"{lineData.LineId}_stage{stage.EvoLevel}";
					bool unlocked = SaveManager.Instance.HasAtlasCard(cardId)
									|| IsStageReachedInSave(lineData, stage.EvoLevel);

					SpawnCard(stage, unlocked, cardId);
				}

				// Karta ścieżki A
				if (lineData.PathAFinalStage != null)
				{
					string cardId = $"{lineData.LineId}_PathA";
					bool unlocked = SaveManager.Instance.HasAtlasCard(cardId);
					SpawnCard(lineData.PathAFinalStage, unlocked, cardId);
				}

				// Karta ścieżki B
				if (lineData.PathBFinalStage != null)
				{
					string cardId = $"{lineData.LineId}_PathB";
					bool unlocked = SaveManager.Instance.HasAtlasCard(cardId);
					SpawnCard(lineData.PathBFinalStage, unlocked, cardId);
				}
			}
		}

		private void SpawnCard(CreatureStageData stageData, bool unlocked, string cardId)
		{
			if (_atlasCardScene == null) return;

			var card = _atlasCardScene.Instantiate<AtlasCard>();
			_cardGrid.AddChild(card);
			card.Setup(stageData, unlocked);
		}

		// ── HELPERS ────────────────────────────────────────────────────────

		private bool IsStageReachedInSave(CreatureLineData lineData, int evoLevel)
		{
			var lineSave = SaveManager.Instance.GetCreatureLineSave(lineData.Biome, lineData.LineId);
			if (lineSave == null || !lineSave.IsUnlocked) return false;
			return lineSave.CurrentLinearStageIndex >= evoLevel - 1;
		}

		private static string GetBiomeEmoji(BiomeType biome) => biome switch
		{
			BiomeType.Ocean   => "🌊",
			BiomeType.Jungle  => "🌿",
			BiomeType.Volcano => "🌋",
			BiomeType.Tundra  => "❄️",
			BiomeType.Abyss   => "🌑",
			_ => "?"
		};

		private void Close()
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.15);
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}
}
