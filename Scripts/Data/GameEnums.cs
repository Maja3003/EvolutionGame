namespace EvoTap
{
	public enum BiomeType
	{
		Ocean = 0,
		Jungle = 1,
		Volcano = 2,
		Tundra = 3,
		Abyss = 4
	}

	public enum CreatureState
	{
		Locked,
		Unlocked,
		Evolving,
		MaxEvolution
	}

	public enum EvolutionPath
	{
		None,       // Etapy 1-2 (liniowe)
		PathA,      // Wybór A na EVO 3
		PathB       // Wybór B na EVO 3
	}

	public enum ClickMechanicType
	{
		Basic,          // Ocean: klik = DNA
		Passive,        // Dżungla: klik + pasywny co 3 sek
		Eruption,       // Wulkan: klik + pasywny + combo Erupcja
		OfflineBonus,   // Tundra: klik + pasywny + duży bonus offline
		Prestige        // Otchłań: wszystko + Prestige reset
	}
}
