using Godot;

namespace EvoTap
{
	/// <summary>
	/// Jeden etap ewolucji stworzenia.
	/// Tworzysz go jako .tres w edytorze Godota i wypełniasz dane.
	/// </summary>
	[GlobalClass]
	public partial class CreatureStageData : Resource
	{
		[Export] public string StageName { get; set; } = "";
		[Export] public string ScientificDescription { get; set; } = ""; // Humorystyczny opis do Atlasu
		[Export] public Texture2D Sprite { get; set; }
		[Export] public Texture2D SilhouetteSprite { get; set; }       // Szara sylwetka dla zablokowanych
		[Export] public int EvoLevel { get; set; } = 1;                // 1-4
		[Export] public EvolutionPath Path { get; set; } = EvolutionPath.None;

		// Koszt ewolucji do TEGO etapu
		[Export] public double DnaCostToEvolve { get; set; } = 100;

		// Bonusy które daje ta forma
		[Export] public double ClickMultiplier { get; set; } = 1.0;
		[Export] public double PassiveMultiplier { get; set; } = 1.0;
		[Export] public double OfflineMultiplier { get; set; } = 1.0;

		// Unlock hint wyświetlany w Atlasie gdy zablokowane
		[Export][Multiline] public string UnlockHint { get; set; } = "";
	}
}
