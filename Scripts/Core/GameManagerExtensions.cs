using Godot;
using System;

namespace EvoTap
{
	// Partial class — rozszerzenie GameManager.cs o brakujące metody publiczne.
	// Trzymaj w tym samym namespace, Godot scali partial klasy automatycznie.
	public partial class GameManager
	{
		/// <summary>
		/// Odejmuje DNA. Bezpieczna metoda — nie zejdzie poniżej 0.
		/// Zwraca true jeśli transakcja się powiodła.
		/// </summary>
		public bool SpendDna(double amount)
		{
			if (_dna < amount) return false;
			Dna -= amount;
			return true;
		}

		/// <summary>
		/// Dodaje DNA (np. z reklam nagrodowych lub prestige bonusu).
		/// </summary>
		public void AddDna(double amount)
		{
			Dna += amount;
			SaveManager.Instance.CurrentSave.TotalDnaAllTime += amount;
		}

		/// <summary>
		/// DNA na sekundę (pasywne) — dla UI w HUD.
		/// </summary>
		public double GetPassiveDnaPerSecond()
		{
			if (ActiveBiomeData == null) return 0;
			return ActiveBiomeData.BasePassiveDnaPerSecond
				   * PassiveMultiplierTotal
				   * SaveManager.Instance.CurrentSave.PrestigeMultiplier;
		}

		/// <summary>
		/// DNA na klik — dla UI w HUD.
		/// </summary>
		public double GetDnaPerClick()
		{
			if (ActiveBiomeData == null) return 0;
			return ActiveBiomeData.BaseDnaPerClick
				   * ClickMultiplierTotal
				   * SaveManager.Instance.CurrentSave.PrestigeMultiplier;
		}

		/// <summary>
		/// Czy Prestige jest dostępny (gracz ukończył Otchłań).
		/// </summary>
		public bool IsPrestigeAvailable()
		{
			var abyss = SaveManager.Instance.GetBiomeSave(BiomeType.Abyss);
			return abyss?.IsCompleted == true;
		}
	}
}
