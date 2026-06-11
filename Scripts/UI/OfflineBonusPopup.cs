using Godot;

namespace EvoTap
{
	/// <summary>
	/// Popup wyświetlany po powrocie do gry z informacją o bonusie offline.
	///
	/// Scena: res://Scenes/UI/OfflineBonusPopup.tscn
	/// Struktura:
	///   OfflineBonusPopup (CanvasLayer)            ← Z-index 8
	///   └── CenterContainer
	///       └── PanelContainer
	///           └── VBoxContainer
	///               ├── TitleLabel      "Witaj z powrotem!"
	///               ├── TimeLabel       "Byłeś offline: 3 godz. 22 min."
	///               ├── DnaLabel        "Zebrano: +1.4M DNA"
	///               ├── WatchAdButton   "Obejrzyj reklamę → ×2 bonus"  (opcjonalny)
	///               └── CloseButton     "Odbierz"
	///
	/// Wywołanie z GameManager._Ready() po CalculateOfflineDna():
	///   OfflineBonusPopup.ShowIfNeeded(earnedDna, elapsedSeconds);
	/// </summary>
	public partial class OfflineBonusPopup : CanvasLayer
	{
		[Export] private Label _titleLabel;
		[Export] private Label _timeLabel;
		[Export] private Label _dnaLabel;
		[Export] private Button _watchAdButton;    // Opcjonalny — reklama za ×2
		[Export] private Button _closeButton;
		[Export] private AnimationPlayer _animPlayer;  // Opcjonalne konfetti itp.

		private double _earnedDna;
		private bool _adDoubleUsed = false;

		// ── FABRYKA ────────────────────────────────────────────────────────

		/// <summary>
		/// Tworzy i pokazuje popup jeśli DNA > 0.
		/// Wywołaj po CalculateOfflineDna().
		/// </summary>
		public static void ShowIfNeeded(PackedScene popupScene, double earnedDna, long elapsedSeconds)
		{
			if (earnedDna <= 0 || elapsedSeconds < 60) return;
			if (popupScene == null) return;

			var popup = popupScene.Instantiate<OfflineBonusPopup>();
			// Dodaj do root żeby był nad wszystkim
			((SceneTree)Engine.GetMainLoop()).Root.AddChild(popup);

			popup.Setup(earnedDna, elapsedSeconds);
		}

		// ── SETUP ──────────────────────────────────────────────────────────

		public void Setup(double earnedDna, long elapsedSeconds)
		{
			_earnedDna = earnedDna;

			// Formatuj czas
			long hours   = elapsedSeconds / 3600;
			long minutes = (elapsedSeconds % 3600) / 60;
			string timeStr = hours > 0
				? $"{hours} godz. {minutes} min."
				: $"{minutes} min.";

			if (_titleLabel != null) _titleLabel.Text = "👋 Witaj z powrotem!";
			if (_timeLabel  != null) _timeLabel.Text  = $"Byłeś offline: {timeStr}";
			if (_dnaLabel   != null) _dnaLabel.Text   = $"+{GameManager.FormatDna(earnedDna)} 🧬 DNA";

			// Ukryj przycisk reklamy jeśli reklamy usunięte lub brak AdManagera
			bool adsEnabled = !SaveManager.Instance.CurrentSave.AdsRemoved
							  && AdManager.Instance != null
							  && AdManager.Instance.IsRewardedAdReady();

			if (_watchAdButton != null)
				_watchAdButton.Visible = adsEnabled;
		}

		public override void _Ready()
		{
			if (_closeButton  != null) _closeButton.Pressed  += Collect;
			if (_watchAdButton != null) _watchAdButton.Pressed += WatchAdForDouble;

			// Animacja wejścia
			GetChild<Control>(0).Modulate = new Color(1, 1, 1, 0);
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", Colors.White, 0.3f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

			_animPlayer?.Play("show");
		}

		// ── AKCJE ──────────────────────────────────────────────────────────

		private void Collect()
		{
			// DNA już zostało dodane przez GameManager.CalculateOfflineDna()
			// Tutaj tylko zamykamy popup
			Close();
		}

		private void WatchAdForDouble()
		{
			if (_adDoubleUsed) return;

			AdManager.Instance?.ShowRewardedAd(OnAdRewarded);
		}

		private void OnAdRewarded()
		{
			if (_adDoubleUsed) return;
			_adDoubleUsed = true;

			// Dodaj jeszcze raz tyle samo DNA (= podwaja bonus)
			GameManager.Instance.AddDna(_earnedDna);

			if (_dnaLabel != null)
				_dnaLabel.Text = $"+{GameManager.FormatDna(_earnedDna * 2)} 🧬 DNA (×2 bonus!)";

			if (_watchAdButton != null)
				_watchAdButton.Disabled = true;

			GD.Print("[OfflineBonusPopup] Podwojono bonus offline przez reklamę.");
		}

		private void Close()
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2f);
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}
}
