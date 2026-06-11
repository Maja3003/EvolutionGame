using Godot;

namespace EvoTap
{
	/// <summary>
	/// Rozszerzenie GameManager — eksponuje stan Erupcji jako publiczne właściwości read-only.
	/// Potrzebne przez EruptionIndicator.cs do pollowania stanu bez reflection.
	///
	/// To jest partial class — Godot automatycznie scali z GameManager.cs.
	/// Plik: res://Scripts/Core/GameManagerEruptionState.cs
	/// </summary>
	public partial class GameManager
	{
		// ── Publiczne właściwości stanu Erupcji ────────────────────────────
		// (pola prywatne zdefiniowane w GameManager.cs)

		/// <summary>Czy okno klikania Erupcji jest aktualnie otwarte.</summary>
		public bool EruptionWindowOpen => _eruptionWindowOpen;

		/// <summary>Ile kliknięć zebrano w obecnym oknie Erupcji.</summary>
		public int EruptionClicksThisWindow => _eruptionClicksThisWindow;

		/// <summary>Czy Erupcja jest aktualnie aktywna (mnożnik DNA działa).</summary>
		public bool EruptionActive => _eruptionActive;

		/// <summary>Ile sekund zostało do końca aktywnej Erupcji (0 gdy nieaktywna).</summary>
		public float EruptionActiveTimer => _eruptionActiveTimer;

		/// <summary>Ile sekund zostało na cooldownie Erupcji (0 gdy gotowa).</summary>
		public float EruptionCooldownTimer => _eruptionCooldownTimer;

		/// <summary>Czy Erupcja jest gotowa do aktywacji (cooldown minął).</summary>
		public bool IsEruptionReady => _eruptionReady;

		// ── Poprawka: eksponuj LastSaveTimestamp do OfflineBonusPopup ─────

		/// <summary>
		/// Czas offline od ostatniego zapisu w sekundach.
		/// Liczy w momencie wywołania.
		/// </summary>
		public long GetOfflineElapsedSeconds()
		{
			long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			return System.Math.Max(0, now - SaveManager.Instance.CurrentSave.LastSaveUnixTimestamp);
		}
	}
}
