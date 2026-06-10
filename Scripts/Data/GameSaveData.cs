using System.Collections.Generic;

namespace EvoTap
{
	/// <summary>
	/// Stan jednej linii ewolucji — zapisywany do JSON.
	/// </summary>
	public class CreatureLineSaveData
	{
		public string LineId { get; set; } = "";
		public bool IsUnlocked { get; set; } = false;
		public int CurrentLinearStageIndex { get; set; } = 0;  // 0 = EVO1, 1 = EVO2, 2 = EVO3
		public EvolutionPath ChosenPath { get; set; } = EvolutionPath.None;
		public bool IsFinalFormReached { get; set; } = false;
		public double CurrentDna { get; set; } = 0;            // DNA zgromadzone do następnej ewolucji
	}

	/// <summary>
	/// Stan jednego biomu.
	/// </summary>
	public class BiomeSaveData
	{
		public BiomeType BiomeType { get; set; }
		public bool IsUnlocked { get; set; } = false;
		public bool IsCompleted { get; set; } = false;          // Wszystkie linie = forma finalna
		public double TotalDnaCollected { get; set; } = 0;
		public List<CreatureLineSaveData> CreatureLines { get; set; } = new();
	}

	/// <summary>
	/// Globalny stan gry — cały zapis.
	/// </summary>
	public class GameSaveData
	{
		public int PrestigeCycle { get; set; } = 0;             // Ile razy gracz zrobił Prestige
		public double PrestigeMultiplier { get; set; } = 1.0;   // 1.0 = brak, 2.0 po pierwszym Prestige
		public double GlobalDna { get; set; } = 0;              // Aktualne DNA (globalne)
		public double TotalDnaAllTime { get; set; } = 0;
		public string CurrentBiomeId { get; set; } = "Ocean";
		public long LastSaveUnixTimestamp { get; set; } = 0;    // Do liczenia offline DNA
		public List<BiomeSaveData> Biomes { get; set; } = new();

		// Atlas — które karty są odblokowane (LineId_PathSuffix np. "ocean_neonfish_A")
		public List<string> UnlockedAtlasCards { get; set; } = new();

		// Ustawienia
		public bool AdsRemoved { get; set; } = false;
		public float MusicVolume { get; set; } = 0.8f;
		public float SfxVolume { get; set; } = 1.0f;
	}
}
