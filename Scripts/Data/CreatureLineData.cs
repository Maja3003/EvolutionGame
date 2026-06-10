using Godot;
using Godot.Collections;

namespace EvoTap
{
	/// <summary>
	/// Pełna linia ewolucji jednego stworzenia (np. Salamandra → Wyvern).
	/// Zawiera etapy 1-2 liniowe, potem dwa rozgałęzienia (PathA / PathB).
	///
	/// Struktura:
	///   Stage[0] = EVO 1 (startowe)
	///   Stage[1] = EVO 2
	///   Stage[2] = EVO 3 (przed wyborem – wyświetlasz pop-up wyboru)
	///   PathAStage = forma finalna ścieżki A
	///   PathBStage = forma finalna ścieżki B
	/// </summary>
	[GlobalClass]
	public partial class CreatureLineData : Resource
	{
		[Export] public string LineId { get; set; } = "";          // np. "ocean_neonfish"
		[Export] public string LineName { get; set; } = "";        // np. "Linia Neonki"
		[Export] public BiomeType Biome { get; set; } = BiomeType.Ocean;

		// Koszt pierwszego odblokowania tej linii (w DNA biomu)
		[Export] public double UnlockCost { get; set; } = 500;

		// Etapy liniowe: indeks 0 = EVO1, 1 = EVO2, 2 = EVO3 (pre-branch)
		[Export] public Array<CreatureStageData> LinearStages { get; set; } = new();

		// Formy finalne po rozgałęzieniu
		[Export] public CreatureStageData PathAFinalStage { get; set; }
		[Export] public CreatureStageData PathBFinalStage { get; set; }

		// Opis wyboru pokazywany w pop-upie
		[Export] public string PathAName { get; set; } = "Ścieżka A";
		[Export] public string PathBName { get; set; } = "Ścieżka B";
		[Export][Multiline] public string PathADescription { get; set; } = "";
		[Export][Multiline] public string PathBDescription { get; set; } = "";
	}
}
