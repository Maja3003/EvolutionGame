using Godot;

namespace EvoTap
{
	/// <summary>
	/// UI wskaźnika mechaniki Erupcji — widoczny tylko w Biomie Wulkanu.
	///
	/// Scena: res://Scenes/UI/EruptionIndicator.tscn
	/// Umieść go w HUD pod przyciskami nawigacji.
	///
	/// Struktura:
	///   EruptionIndicator (Control)
	///   └── VBoxContainer
	///       ├── HeaderLabel     "🌋 ERUPCJA"
	///       ├── ClickBar        (HBoxContainer)
	///       │   ├── ClickLabel  "3 / 10 kliknięć"
	///       │   └── ClickProgress (ProgressBar)
	///       ├── CooldownBar     (HBoxContainer)
	///       │   ├── CooldownLabel "Cooldown: 47s"
	///       │   └── CooldownProgress (ProgressBar)
	///       └── ActivePanel (PanelContainer)   ← widoczny gdy erupcja aktywna
	///           └── ActiveLabel  "🔥 ERUPCJA AKTYWNA! 3.0s"
	/// </summary>
	public partial class EruptionIndicator : Control
	{
		[Export] private Label _headerLabel;
		[Export] private Label _clickCountLabel;
		[Export] private ProgressBar _clickProgress;
		[Export] private Label _cooldownLabel;
		[Export] private ProgressBar _cooldownProgress;
		[Export] private PanelContainer _activePanel;
		[Export] private Label _activeLabel;

		// Stan lokalny — synchronizujemy z GameManager przez polling w _Process
		// (GameManager nie emituje szczegółowych sygnałów erupcji — można dodać)
		private float _displayedCooldown = 0f;
		private float _displayedActiveTimer = 0f;

		public override void _Ready()
		{
			// Pokaż tylko w Wulkanie
			GameManager.Instance.BiomeChanged += OnBiomeChanged;
			GameManager.Instance.EruptionReady += OnEruptionReady;
			GameManager.Instance.EruptionActivated += OnEruptionActivated;

			UpdateVisibility(GameManager.Instance.CurrentBiome);
		}

		public override void _Process(double delta)
		{
			if (!Visible) return;

			var biomeData = GameManager.Instance.ActiveBiomeData;
			if (biomeData == null || biomeData.ClickMechanic != ClickMechanicType.Eruption)
				return;

			// Pobieramy stan erupcji z GameManager przez publiczne właściwości
			// Dodaj te właściwości do GameManager (lub GameManagerExtensions):
			//   public bool EruptionWindowOpen { get; private set; }
			//   public int EruptionClicksThisWindow { get; private set; }
			//   public bool EruptionActive { get; private set; }
			//   public float EruptionActiveTimer { get; private set; }
			//   public float EruptionCooldownTimer { get; private set; }

			bool windowOpen   = GameManager.Instance.EruptionWindowOpen;
			int clicks        = GameManager.Instance.EruptionClicksThisWindow;
			bool active       = GameManager.Instance.EruptionActive;
			float activeTimer = GameManager.Instance.EruptionActiveTimer;
			float cooldown    = GameManager.Instance.EruptionCooldownTimer;
			int required      = biomeData.EruptionClicksRequired;
			float fullCooldown = biomeData.EruptionCooldown;

			// ── Panel aktywny ──
			if (_activePanel != null)
				_activePanel.Visible = active;

			if (active && _activeLabel != null)
				_activeLabel.Text = $"🔥 ERUPCJA AKTYWNA! {activeTimer:F1}s";

			// ── Pasek kliknięć (widoczny gdy okno otwarte lub czekamy) ──
			if (_clickCountLabel != null)
			{
				if (active)
					_clickCountLabel.Text = "";
				else if (windowOpen)
					_clickCountLabel.Text = $"Kliknij szybko! {clicks} / {required}";
				else if (cooldown > 0)
					_clickCountLabel.Text = "";
				else
					_clickCountLabel.Text = $"Kliknij {required}× szybko!";
			}

			if (_clickProgress != null)
			{
				_clickProgress.Visible = windowOpen && !active;
				_clickProgress.MaxValue = required;
				_clickProgress.Value = clicks;
			}

			// ── Pasek cooldown ──
			if (_cooldownLabel != null)
			{
				_cooldownLabel.Visible = cooldown > 0 && !active;
				if (cooldown > 0)
					_cooldownLabel.Text = $"Cooldown: {cooldown:F0}s";
			}

			if (_cooldownProgress != null)
			{
				_cooldownProgress.Visible = cooldown > 0 && !active;
				_cooldownProgress.MaxValue = fullCooldown;
				_cooldownProgress.Value = fullCooldown - cooldown; // odwrócony pasek
			}
		}

		// ── REAKCJE NA SYGNAŁY ────────────────────────────────────────────
		
		private void OnBiomeChanged(BiomeType biome)
		{
			UpdateVisibility(biome);
		}

		private void OnEruptionReady()
		{
			if (_headerLabel != null)
			{
				// Pulsujący kolor żeby zwrócić uwagę
				var tween = CreateTween().SetLoops(3);
				tween.TweenProperty(_headerLabel, "modulate", new Color(1f, 0.4f, 0f), 0.2f);
				tween.TweenProperty(_headerLabel, "modulate", Colors.White, 0.2f);
			}
		}

		private void OnEruptionActivated(float duration)
		{
			// Efekt podświetlenia całego panelu na czas erupcji
			if (_activePanel != null)
			{
				var tween = CreateTween();
				tween.TweenProperty(_activePanel, "modulate",
					new Color(1f, 0.6f, 0.1f, 1f), 0.1f);
				tween.TweenInterval(duration - 0.5f);
				tween.TweenProperty(_activePanel, "modulate", Colors.White, 0.5f);
			}
		}

		private void UpdateVisibility(BiomeType biome)
		{
			bool isVolcano = biome == BiomeType.Volcano;
			Visible = isVolcano;
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance == null) return;
			GameManager.Instance.BiomeChanged   -= OnBiomeChanged;
			GameManager.Instance.EruptionReady  -= OnEruptionReady;
			GameManager.Instance.EruptionActivated -= OnEruptionActivated;
		}
	}
}
