using Godot;
using System;
using System.Linq;

namespace EvoTap
{
	/// <summary>
	/// Główny autoload singleton gry.
	/// Rejestruj jako AutoLoad: "GameManager" → res://Scripts/Core/GameManager.cs
	///
	/// Odpowiada za:
	///   - licznik DNA (globalny)
	///   - mechanikę klikania + pasywny dochód
	///   - mechaniki specjalne (Erupcja)
	///   - kalkulację offline DNA
	///   - emitowanie sygnałów do UI
	/// </summary>
	public partial class GameManager : Node
	{
		public static GameManager Instance { get; private set; }

		// ── SYGNAŁY ────────────────────────────────────────────────────────

		[Signal] public delegate void DnaChangedEventHandler(double newAmount);
		[Signal] public delegate void BiomeChangedEventHandler(BiomeType newBiome);
		[Signal] public delegate void CreatureEvolvedEventHandler(string lineId, int newStageIndex);
		[Signal] public delegate void AtlasCardUnlockedEventHandler(string cardId);
		[Signal] public delegate void EruptionReadyEventHandler();
		[Signal] public delegate void EruptionActivatedEventHandler(float duration);
		[Signal] public delegate void PrestigeAvailableEventHandler();

		// ── STAN ───────────────────────────────────────────────────────────
		
		[Export] private PackedScene _offlinePopupScene;

		private double _dna = 0;
		public double Dna
		{
			get => _dna;
			private set
			{
				_dna = Math.Max(0, value);
				SaveManager.Instance.CurrentSave.GlobalDna = _dna;
				EmitSignal(SignalName.DnaChanged, _dna);
			}
		}

		private BiomeType _currentBiome = BiomeType.Ocean;
		public BiomeType CurrentBiome => _currentBiome;

		// Cache aktywnego BiomeData (ładowany przez BiomeManager)
		public BiomeData ActiveBiomeData { get; set; }

		// Mnożniki z ewolucji (aktualizowane przez CreatureManager)
		public double ClickMultiplierTotal { get; set; } = 1.0;
		public double PassiveMultiplierTotal { get; set; } = 1.0;
		public double OfflineMultiplierTotal { get; set; } = 1.0;

		// Erupcja (Wulkan)
		private bool _eruptionReady = false;
		private bool _eruptionActive = false;
		private float _eruptionCooldownTimer = 0f;
		private float _eruptionActiveTimer = 0f;
		private int _eruptionClicksThisWindow = 0;
		private bool _eruptionWindowOpen = false;
		private float _eruptionWindowTimer = 0f;
		private const float EruptionWindowDuration = 4f;

		// Autosave
		private float _autosaveTimer = 0f;
		private const float AutosaveInterval = 30f;

		// Pasywny dochód
		private float _passiveTimer = 0f;
		private const float PassiveTickInterval = 1f; // co sekundę
		
		private double _lastOfflineEarned = 0;

		// ── INIT ───────────────────────────────────────────────────────────

		public override void _Ready()
		{
			Instance = this;

			var save = SaveManager.Instance.CurrentSave;
			_dna = save.GlobalDna;
			_currentBiome = Enum.Parse<BiomeType>(save.CurrentBiomeId);

			CalculateOfflineDna();
			long elapsed = GetOfflineElapsedSeconds();
			OfflineBonusPopup.ShowIfNeeded(_offlinePopupScene, _lastOfflineEarned, elapsed);
		}

		// ── GAME LOOP ─────────────────────────────────────────────────────

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			ProcessPassiveIncome(dt);
			ProcessEruption(dt);
			ProcessAutosave(dt);
		}

		// ── KLIKANIE ──────────────────────────────────────────────────────

		/// <summary>
		/// Wywołaj z UI przy każdym tapnięciu stworzenia.
		/// Zwraca ile DNA zdobyto za ten klik (do animacji floaty).
		/// </summary>
		public double OnCreatureClicked()
		{
			if (ActiveBiomeData == null) return 0;

			double baseDna = ActiveBiomeData.BaseDnaPerClick;
			double prestige = SaveManager.Instance.CurrentSave.PrestigeMultiplier;
			double gained = baseDna * ClickMultiplierTotal * prestige;

			// Erupcja aktywna → mnożnik
			if (_eruptionActive)
				gained *= ActiveBiomeData.EruptionMultiplier;

			Dna += gained;
			SaveManager.Instance.CurrentSave.TotalDnaAllTime += gained;

			// Licznik kliknięć dla Erupcji
			if (ActiveBiomeData.ClickMechanic == ClickMechanicType.Eruption)
				HandleEruptionClick();

			return gained;
		}

		// ── PASYWNY DOCHÓD ────────────────────────────────────────────────

		private void ProcessPassiveIncome(float dt)
		{
			if (ActiveBiomeData == null) return;
			if (ActiveBiomeData.BasePassiveDnaPerSecond <= 0) return;

			_passiveTimer += dt;
			if (_passiveTimer < PassiveTickInterval) return;
			_passiveTimer = 0f;

			double passive = ActiveBiomeData.BasePassiveDnaPerSecond
							 * PassiveMultiplierTotal
							 * SaveManager.Instance.CurrentSave.PrestigeMultiplier;

			if (_eruptionActive)
				passive *= ActiveBiomeData.EruptionMultiplier;

			Dna += passive;
			SaveManager.Instance.CurrentSave.TotalDnaAllTime += passive;
		}

		// ── ERUPCJA (Wulkan) ──────────────────────────────────────────────

		private void HandleEruptionClick()
		{
			if (_eruptionActive) return;

			if (!_eruptionWindowOpen)
			{
				// Sprawdź czy cooldown minął
				if (_eruptionCooldownTimer > 0) return;

				_eruptionWindowOpen = true;
				_eruptionWindowTimer = EruptionWindowDuration;
				_eruptionClicksThisWindow = 1;
				GD.Print("[Erupcja] Okno otwarte — klikaj szybko!");
			}
			else
			{
				_eruptionClicksThisWindow++;

				if (_eruptionClicksThisWindow >= ActiveBiomeData.EruptionClicksRequired)
				{
					ActivateEruption();
				}
			}
		}

		private void ActivateEruption()
		{
			_eruptionWindowOpen = false;
			_eruptionActive = true;
			_eruptionActiveTimer = ActiveBiomeData.EruptionDuration;
			_eruptionCooldownTimer = ActiveBiomeData.EruptionCooldown;
			EmitSignal(SignalName.EruptionActivated, ActiveBiomeData.EruptionDuration);
			GD.Print("[Erupcja] AKTYWNA!");
		}

		private void ProcessEruption(float dt)
		{
			if (_eruptionWindowOpen)
			{
				_eruptionWindowTimer -= dt;
				if (_eruptionWindowTimer <= 0)
				{
					_eruptionWindowOpen = false;
					_eruptionClicksThisWindow = 0;
				}
			}

			if (_eruptionActive)
			{
				_eruptionActiveTimer -= dt;
				if (_eruptionActiveTimer <= 0)
				{
					_eruptionActive = false;
					GD.Print("[Erupcja] zakończona.");
				}
			}

			if (_eruptionCooldownTimer > 0)
			{
				_eruptionCooldownTimer -= dt;
				if (_eruptionCooldownTimer <= 0 && !_eruptionActive)
				{
					_eruptionReady = true;
					EmitSignal(SignalName.EruptionReady);
				}
			}
		}

		// ── OFFLINE DNA ───────────────────────────────────────────────────

		private void CalculateOfflineDna()
		{
			if (ActiveBiomeData == null) return;
			if (ActiveBiomeData.BasePassiveDnaPerSecond <= 0) return;

			var save = SaveManager.Instance.CurrentSave;
			long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			long elapsed = now - save.LastSaveUnixTimestamp;

			if (elapsed <= 60) return; // Mniej niż minuta — ignoruj

			// Cap: max 8 godzin offline
			elapsed = Math.Min(elapsed, 8 * 3600);

			double offlineMultiplier = ActiveBiomeData.ClickMechanic == ClickMechanicType.OfflineBonus
				? OfflineMultiplierTotal * 2.0   // Tundra: podwójny bonus offline
				: OfflineMultiplierTotal;

			double earned = ActiveBiomeData.BasePassiveDnaPerSecond
							* offlineMultiplier
							* save.PrestigeMultiplier
							* elapsed;

			GD.Print($"[Offline] Czas nieobecności: {elapsed}s, zarobione DNA: {earned:F0}");

			_lastOfflineEarned = earned;
			Dna += earned;
			save.TotalDnaAllTime += earned;

			// Pokaż pop-up (sygnał odbiera UI)
			// EmitSignal(SignalName.OfflineBonusReceived, earned, elapsed);
		}

		// ── ZMIANA BIOMU ──────────────────────────────────────────────────

		public void SwitchBiome(BiomeType newBiome)
		{
			_currentBiome = newBiome;
			SaveManager.Instance.CurrentSave.CurrentBiomeId = newBiome.ToString();
			EmitSignal(SignalName.BiomeChanged, (int)newBiome);
			SaveManager.Instance.Save();
			GD.Print($"[GameManager] Zmiana biomu na: {newBiome}");
		}

		// ── PRESTIGE ──────────────────────────────────────────────────────

		/// <summary>
		/// Wywołaj gdy gracz potwierdzi Prestige (po ukończeniu Otchłani).
		/// </summary>
		public void ExecutePrestige()
		{
			SaveManager.Instance.ResetForPrestige();
			_dna = 0;
			_currentBiome = BiomeType.Ocean;
			ClickMultiplierTotal = 1.0;
			PassiveMultiplierTotal = 1.0;
			OfflineMultiplierTotal = 1.0;
			EmitSignal(SignalName.BiomeChanged, (int)BiomeType.Ocean);
			EmitSignal(SignalName.DnaChanged, 0.0);
		}

		// ── AUTOSAVE ──────────────────────────────────────────────────────

		private void ProcessAutosave(float dt)
		{
			_autosaveTimer += dt;
			if (_autosaveTimer >= AutosaveInterval)
			{
				_autosaveTimer = 0f;
				SaveManager.Instance.CurrentSave.GlobalDna = _dna;
				SaveManager.Instance.Save();
			}
		}

		// ── UTILS ─────────────────────────────────────────────────────────

		/// <summary>
		/// Formatuje liczby DNA do czytelnej postaci: 1.2K, 3.4M, 7.8B itd.
		/// </summary>
		public static string FormatDna(double value)
		{
			return value switch
			{
				>= 1_000_000_000_000 => $"{value / 1_000_000_000_000:F1}T",
				>= 1_000_000_000     => $"{value / 1_000_000_000:F1}B",
				>= 1_000_000         => $"{value / 1_000_000:F1}M",
				>= 1_000             => $"{value / 1_000:F1}K",
				_                    => $"{value:F0}"
			};
		}
	}
}
