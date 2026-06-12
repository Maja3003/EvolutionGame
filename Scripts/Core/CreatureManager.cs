using Godot;
using System.Linq;

namespace EvoTap
{
	/// <summary>
	/// Zarządza stanem jednej linii ewolucji w aktywnym biomie.
	/// Umieść jako Node w scenie biomu, podaj LineData przez edytor.
	///
	/// Scena: res://Scenes/Creatures/CreatureLine.tscn
	/// </summary>
	public partial class CreatureManager : Node
	{
		[Export] public CreatureLineData LineData { get; set; }

		private CreatureLineSaveData _save;

		// ── SYGNAŁY ────────────────────────────────────────────────────────

		[Signal] public delegate void EvolutionReadyEventHandler();
		[Signal] public delegate void EvolvedEventHandler(int newStageIndex, bool isFinal);
		[Signal] public delegate void PathChoiceRequiredEventHandler();   // Pokaż pop-up wyboru
		[Signal] public delegate void LineUnlockedEventHandler();

		// ── INIT ───────────────────────────────────────────────────────────

		public override void _Ready()
		{
			if (LineData == null)
			{
				GD.PrintErr("[CreatureManager] Brak LineData!");
				return;
			}

			_save = SaveManager.Instance.GetCreatureLineSave(LineData.Biome, LineData.LineId);

			if (_save == null)
			{
				// Pierwsza inicjalizacja tej linii
				_save = new CreatureLineSaveData { LineId = LineData.LineId };

				// Linie darmowe (UnlockCost <= 0) są odblokowane od razu
				if (LineData.UnlockCost <= 0)
					_save.IsUnlocked = true;

				SaveManager.Instance.GetBiomeSave(LineData.Biome)?.CreatureLines.Add(_save);
			}

			RecalculateMultipliers();
		}

		// ── ODBLOKOWANIE ──────────────────────────────────────────────────

		public bool CanUnlock()
			=> !_save.IsUnlocked && GameManager.Instance.Dna >= LineData.UnlockCost;

		public void Unlock()
		{
			if (!CanUnlock()) return;

			// GameManager nie ma bezpośredniej metody SpendDna — 
			// modyfikujemy przez właściwość. Lub dodaj SpendDna() do GameManager.
			GameManager.Instance.SpendDna(LineData.UnlockCost);

			_save.IsUnlocked = true;
			SaveManager.Instance.Save();
			EmitSignal(SignalName.LineUnlocked);
			RecalculateMultipliers();
		}

		// ── EWOLUCJA ──────────────────────────────────────────────────────

		/// <summary>
		/// Koszt następnej ewolucji. Null = brak (max osiągnięty lub nie odblokowane).
		/// </summary>
		public double? NextEvolutionCost()
		{
			if (!_save.IsUnlocked || _save.IsFinalFormReached) return null;

			// Na etapie 2 (EVO3, indeks 2) — wymaga wyboru ścieżki przed podaniem kosztu
			if (_save.CurrentLinearStageIndex >= LineData.LinearStages.Count)
			{
				if (_save.ChosenPath == EvolutionPath.None) return null;

				var finalStage = _save.ChosenPath == EvolutionPath.PathA
					? LineData.PathAFinalStage
					: LineData.PathBFinalStage;
				return finalStage?.DnaCostToEvolve;
			}

			return LineData.LinearStages[_save.CurrentLinearStageIndex].DnaCostToEvolve;
		}

		public bool CanEvolve()
		{
			var cost = NextEvolutionCost();
			return cost.HasValue && GameManager.Instance.Dna >= cost.Value;
		}

		/// <summary>
		/// Próba ewolucji. Jeśli trzeba wybrać ścieżkę — emituje PathChoiceRequired i nie ewoluuje.
		/// </summary>
		public void TryEvolve()
		{
			if (!CanEvolve()) return;

			// Czy gracz jest na EVO3 i nie wybrał jeszcze ścieżki?
			bool atBranchPoint = _save.CurrentLinearStageIndex >= LineData.LinearStages.Count - 1
								  && _save.ChosenPath == EvolutionPath.None
								  && !_save.IsFinalFormReached;

			if (atBranchPoint)
			{
				EmitSignal(SignalName.PathChoiceRequired);
				return;
			}

			PerformEvolution();
		}

		/// <summary>
		/// Wywołaj po tym jak gracz wybrał ścieżkę w pop-upie.
		/// </summary>
		public void ChoosePath(EvolutionPath path)
		{
			if (path == EvolutionPath.None) return;
			_save.ChosenPath = path;
			SaveManager.Instance.Save();

			// Teraz ewoluuj do formy finalnej
			if (CanEvolve())
				PerformEvolution();
		}

		private void PerformEvolution()
		{
			var cost = NextEvolutionCost();
			if (!cost.HasValue) return;

			// Odejmij DNA — przez reflection lub lepiej przez dedicated metodę w GameManager
			SpendDna(cost.Value);

			bool isFinal = false;

			if (_save.CurrentLinearStageIndex < LineData.LinearStages.Count)
			{
				_save.CurrentLinearStageIndex++;
			}
			else
			{
				// Forma finalna
				_save.IsFinalFormReached = true;
				isFinal = true;

				// Dodaj kartę do Atlasu
				string cardId = $"{LineData.LineId}_{_save.ChosenPath}";
				SaveManager.Instance.AddAtlasCard(cardId);
				GameManager.Instance.EmitSignal(GameManager.SignalName.AtlasCardUnlocked, cardId);
			}

			SaveManager.Instance.Save();
			RecalculateMultipliers();
			EmitSignal(SignalName.Evolved, _save.CurrentLinearStageIndex, isFinal);
			GameManager.Instance.EmitSignal(
				GameManager.SignalName.CreatureEvolved, LineData.LineId, _save.CurrentLinearStageIndex);

			CheckBiomeCompletion();
		}

		// ── AKTUALNE DANE STAGE ───────────────────────────────────────────

		public CreatureStageData GetCurrentStageData()
		{
			if (!_save.IsUnlocked) return null;

			if (_save.IsFinalFormReached)
			{
				return _save.ChosenPath == EvolutionPath.PathA
					? LineData.PathAFinalStage
					: LineData.PathBFinalStage;
			}

			int idx = _save.CurrentLinearStageIndex;
			if (idx < LineData.LinearStages.Count)
				return LineData.LinearStages[idx];

			return null;
		}

		public int GetCurrentLinearStageIndex() => _save.CurrentLinearStageIndex;
		public bool IsUnlocked() => _save.IsUnlocked;
		public bool IsFinalFormReached() => _save.IsFinalFormReached;
		public EvolutionPath GetChosenPath() => _save.ChosenPath;

		// ── MNOŻNIKI ──────────────────────────────────────────────────────

		private void RecalculateMultipliers()
		{
			// Zbierz mnożniki ze wszystkich aktywnych stworków w biomie
			// (To uproszczone — pełna implementacja agreguje z BiomeManager)
			var stage = GetCurrentStageData();
			if (stage == null) return;

			// GameManager przechowuje sumy — tutaj tylko aktualizuj swój wkład
			// W pełnej implementacji BiomeManager agreguje wszystkie CreatureManagery
		}

		// ── POMOCNICZE ────────────────────────────────────────────────────

		private void SpendDna(double amount)
		{
			GameManager.Instance.SpendDna(amount); // już istnieje w GameManagerExtensions.cs
		}

		private void CheckBiomeCompletion()
		{
			var biomeSave = SaveManager.Instance.GetBiomeSave(LineData.Biome);
			if (biomeSave == null) return;

			bool allComplete = biomeSave.CreatureLines.Count >= 3
							   && biomeSave.CreatureLines.All(c => c.IsFinalFormReached);

			if (allComplete && !biomeSave.IsCompleted)
			{
				biomeSave.IsCompleted = true;
				SaveManager.Instance.Save();
				UnlockNextBiome();
			}
		}
		[Export] private PackedScene _biomeTransitionScene;

		private void UnlockNextBiome()
		{
			BiomeType next = LineData.Biome + 1;
			if (next > BiomeType.Abyss) return;

			var nextSave = SaveManager.Instance.GetBiomeSave(next);
			if (nextSave != null && !nextSave.IsUnlocked)
			{
				nextSave.IsUnlocked = true;
				SaveManager.Instance.Save();
				GD.Print($"[CreatureManager] Odblokowano biom: {next}!");
				BiomeTransitionScreen.Show(_biomeTransitionScene, next);
			}
		}
	}
}
