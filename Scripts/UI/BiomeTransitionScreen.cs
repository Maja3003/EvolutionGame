using Godot;

namespace EvoTap
{
	/// <summary>
	/// Animowany ekran przejścia biomu — pojawia się po odblokowaniu nowego biomu.
	/// Zmieniające się tło + tekst "Witaj w [NazwaBiomu]".
	///
	/// Scena: res://Scenes/UI/BiomeTransitionScreen.tscn
	/// Struktura:
	///   BiomeTransitionScreen (CanvasLayer)    ← Z-index 20 (nad wszystkim)
	///   └── Control (pełny ekran)
	///       ├── BgRect (ColorRect)             ← tło nowego biomu
	///       ├── VBoxContainer (wyśrodkowane)
	///       │   ├── BiomeEmojiLabel (Label)    ← duże emoji biomu
	///       │   ├── BiomeNameLabel  (Label)    ← nazwa biomu
	///       │   ├── BiomeDescLabel  (Label)    ← krótki opis
	///       │   └── CreatureRow     (HBoxContainer) ← 3 sylwetki nowych stworzeń
	///       └── TapToContiueLabel (Label)      ← "Dotknij żeby kontynuować"
	///
	/// Wywołanie (z CreatureManager.UnlockNextBiome()):
	///   BiomeTransitionScreen.Show(scene, BiomeType.Jungle);
	/// </summary>
	public partial class BiomeTransitionScreen : CanvasLayer
	{
		[Export] private ColorRect _bgRect;
		[Export] private Label _biomeEmojiLabel;
		[Export] private Label _biomeNameLabel;
		[Export] private Label _biomeDescLabel;
		[Export] private HBoxContainer _creatureRow;
		[Export] private Label _tapLabel;

		private bool _canClose = false;

		// ── FABRYKA ────────────────────────────────────────────────────────

		public static void Show(PackedScene scene, BiomeType newBiome)
		{
			if (scene == null) return;

			var screen = scene.Instantiate<BiomeTransitionScreen>();
		((SceneTree)Engine.GetMainLoop()).Root.AddChild(screen);

			screen.Setup(newBiome);
		}

		// ── SETUP ──────────────────────────────────────────────────────────

		public void Setup(BiomeType newBiome)
		{
			var biomeData = BiomeManager.Instance.GetBiomeData(newBiome);
			if (biomeData == null) return;

			// Tło w kolorze biomu
			if (_bgRect != null)
				_bgRect.Color = biomeData.AccentColor with { A = 0.85f };

			// Tekst
			if (_biomeEmojiLabel != null) _biomeEmojiLabel.Text = GetBiomeEmoji(newBiome);
			if (_biomeNameLabel  != null) _biomeNameLabel.Text  = biomeData.BiomeName;
			if (_biomeDescLabel  != null) _biomeDescLabel.Text  = biomeData.BiomeDescription;

			// Sylwetki 3 nowych stworzeń
			if (_creatureRow != null)
			{
				foreach (var line in biomeData.CreatureLines)
				{
					var stage = line.LinearStages.Count > 0 ? line.LinearStages[0] : null;
					if (stage == null) continue;

					var img = new TextureRect
					{
						Texture         = stage.SilhouetteSprite ?? stage.Sprite,
						Modulate        = new Color(0.2f, 0.2f, 0.2f, 0.6f),
						ExpandMode      = TextureRect.ExpandModeEnum.FitWidth,
						StretchMode     = TextureRect.StretchModeEnum.KeepAspectCentered,
						CustomMinimumSize = new Vector2(80, 80)
					};
					_creatureRow.AddChild(img);
				}
			}

			if (_tapLabel != null)
			{
				_tapLabel.Text = "Dotknij żeby kontynuować";
				_tapLabel.Modulate = new Color(1, 1, 1, 0);
			}

			PlayEntrance();
		}

		// ── ANIMACJA ───────────────────────────────────────────────────────

		private void PlayEntrance()
		{
			// Start niewidoczny
			GetChild<Control>(0).Modulate = new Color(1, 1, 1, 0);

			var tween = CreateTween();
			tween.SetParallel(false);

			// 1. Fade in ekranu
			tween.TweenProperty(GetChild<Control>(0), "modulate", Colors.White, 0.5f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

			// 2. Chwila zachwytu
			tween.TweenInterval(1.0f);

			// 3. Pokaż "dotknij żeby kontynuować" z pulsowaniem
			tween.TweenCallback(Callable.From(() =>
			{
				_canClose = true;
				if (_tapLabel == null) return;

				var pulseTween = CreateTween().SetLoops(0);
				pulseTween.TweenProperty(_tapLabel, "modulate", Colors.White, 0.6f);
				pulseTween.TweenProperty(_tapLabel, "modulate",
					new Color(1, 1, 1, 0.3f), 0.6f);
			}));
		}

		// ── INPUT ──────────────────────────────────────────────────────────

		public override void _UnhandledInput(InputEvent @event)
		{
			if (!_canClose) return;

			bool tapped = @event is InputEventScreenTouch t && t.Pressed
						  || @event is InputEventMouseButton m && m.Pressed;

			if (!tapped) return;

			GetViewport().SetInputAsHandled();
			Close();
		}

		private void Close()
		{
			_canClose = false;
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.4f)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
			tween.TweenCallback(Callable.From(QueueFree));
		}

		// ── HELPERS ────────────────────────────────────────────────────────

		private static string GetBiomeEmoji(BiomeType b) => b switch
		{
			BiomeType.Ocean   => "🌊",
			BiomeType.Jungle  => "🌿",
			BiomeType.Volcano => "🌋",
			BiomeType.Tundra  => "❄️",
			BiomeType.Abyss   => "🌑",
			_ => "🌍"
		};
	}
}
