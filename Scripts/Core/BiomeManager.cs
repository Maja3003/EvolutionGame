using Godot;
using Godot.Collections;
using System.Linq;

namespace EvoTap
{
	/// <summary>
	/// Autoload singleton zarządzający biomami.
	/// Rejestruj jako AutoLoad: "BiomeManager" → res://Scripts/Biomes/BiomeManager.cs
	///
	/// Ładuje wszystkie BiomeData z Resources/Biomes/ i obsługuje przełączanie biomów.
	/// </summary>
	public partial class BiomeManager : Node
	{
		public static BiomeManager Instance { get; private set; }

		// Wypełnij w edytorze Godota — przeciągnij 5 .tres biomów
		[Export] public Array<BiomeData> AllBiomes { get; set; } = new();

		private BiomeData _activeBiome;
		public BiomeData ActiveBiome => _activeBiome;

		// ── INIT ───────────────────────────────────────────────────────────

		public override void _Ready()
		{
			Instance = this;

			// Załaduj aktywny biom z zapisu
			var savedBiome = GameManager.Instance.CurrentBiome;
			LoadBiome(savedBiome);
		}

		// ── ŁADOWANIE BIOMU ───────────────────────────────────────────────

		public void LoadBiome(BiomeType biomeType)
		{
			_activeBiome = AllBiomes.FirstOrDefault(b => b.BiomeType == biomeType);

			if (_activeBiome == null)
			{
				GD.PrintErr($"[BiomeManager] Nie znaleziono danych dla biomu: {biomeType}");
				return;
			}

			GameManager.Instance.ActiveBiomeData = _activeBiome;
			RecalculateGlobalMultipliers();

			GD.Print($"[BiomeManager] Załadowano biom: {_activeBiome.BiomeName}");
		}

		// ── MNOŻNIKI GLOBALNE ─────────────────────────────────────────────

		/// <summary>
		/// Sumuje mnożniki ze wszystkich odblokowanych stworzeń aktywnego biomu.
		/// Wywołuj po każdej ewolucji.
		/// </summary>
		public void RecalculateGlobalMultipliers()
		{
			if (_activeBiome == null) return;

			double clickMult = 1.0;
			double passiveMult = 1.0;
			double offlineMult = 1.0;

			var biomeSave = SaveManager.Instance.GetBiomeSave(_activeBiome.BiomeType);
			if (biomeSave == null) return;

			foreach (var lineData in _activeBiome.CreatureLines)
			{
				var lineSave = biomeSave.CreatureLines.Find(c => c.LineId == lineData.LineId);
				if (lineSave == null || !lineSave.IsUnlocked) continue;

				var stageData = GetCurrentStageData(lineData, lineSave);
				if (stageData == null) continue;

				clickMult   += stageData.ClickMultiplier - 1.0;   // Dodajemy bonus (1.0 = brak bonusu)
				passiveMult += stageData.PassiveMultiplier - 1.0;
				offlineMult += stageData.OfflineMultiplier - 1.0;
			}

			GameManager.Instance.ClickMultiplierTotal   = clickMult;
			GameManager.Instance.PassiveMultiplierTotal = passiveMult;
			GameManager.Instance.OfflineMultiplierTotal = offlineMult;

			GD.Print($"[BiomeManager] Mnożniki: klik={clickMult:F2}×, pasyw={passiveMult:F2}×, offline={offlineMult:F2}×");
		}

		// ── HELPERS ────────────────────────────────────────────────────────

		private CreatureStageData GetCurrentStageData(CreatureLineData lineData, CreatureLineSaveData lineSave)
		{
			if (lineSave.IsFinalFormReached)
			{
				return lineSave.ChosenPath == EvolutionPath.PathA
					? lineData.PathAFinalStage
					: lineData.PathBFinalStage;
			}

			int idx = lineSave.CurrentLinearStageIndex;
			if (idx < lineData.LinearStages.Count)
				return lineData.LinearStages[idx];

			return null;
		}

		public BiomeData GetBiomeData(BiomeType type)
			=> AllBiomes.FirstOrDefault(b => b.BiomeType == type);

		public bool IsBiomeUnlocked(BiomeType type)
			=> SaveManager.Instance.GetBiomeSave(type)?.IsUnlocked == true;

		public bool IsBiomeCompleted(BiomeType type)
			=> SaveManager.Instance.GetBiomeSave(type)?.IsCompleted == true;
	}
}
