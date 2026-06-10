using Godot;
using Godot.Collections;

namespace EvoTap
{
	/// <summary>
	/// Dane jednego biomu. Tworzysz jako .tres w edytorze.
	/// </summary>
	[GlobalClass]
	public partial class BiomeData : Resource
	{
		[Export] public BiomeType BiomeType { get; set; }
		[Export] public string BiomeName { get; set; } = "";
		[Export] public string BiomeDescription { get; set; } = "";
		[Export] public Texture2D BackgroundTexture { get; set; }
		[Export] public Color AccentColor { get; set; } = Colors.Cyan;
		[Export] public AudioStream AmbientMusic { get; set; }

		// 3 linie stworzeń przypisane do biomu
		[Export] public Array<CreatureLineData> CreatureLines { get; set; } = new();

		// Mechanika klikania aktywna w tym biomie
		[Export] public ClickMechanicType ClickMechanic { get; set; } = ClickMechanicType.Basic;

		// Bazowy DNA na klik (skaluje się z ulepszeniami)
		[Export] public double BaseDnaPerClick { get; set; } = 1.0;

		// Pasywny dochód DNA/sek (0 jeśli nie ma)
		[Export] public double BasePassiveDnaPerSecond { get; set; } = 0.0;

		// Czas cooldown Erupcji w sekundach (tylko Wulkan)
		[Export] public float EruptionCooldown { get; set; } = 60f;
		[Export] public int EruptionClicksRequired { get; set; } = 10;
		[Export] public double EruptionMultiplier { get; set; } = 5.0;
		[Export] public float EruptionDuration { get; set; } = 3f;
	}
}
