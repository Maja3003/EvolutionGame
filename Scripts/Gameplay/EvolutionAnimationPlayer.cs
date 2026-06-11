using Godot;

namespace EvoTap
{
	/// <summary>
	/// Odtwarza animację ewolucji: zoom kamery, flash ekranu, podmiana sprite'a stworzenia.
	///
	/// Scena: res://Scenes/UI/EvolutionAnimationPlayer.tscn
	/// Struktura:
	///   EvolutionAnimationPlayer (CanvasLayer)   ← Z-index 5
	///   ├── FlashRect (ColorRect)                ← pełnoekranowy biały prostokąt
	///   └── EvoLabel (Label)                     ← "EWOLUCJA!" z dużą czcionką
	///
	/// Użycie — wywołaj z CreatureLineUI.OnEvolved():
	///   EvolutionAnimationPlayer.Instance.PlayEvolution(newSprite, isFinal);
	/// </summary>
	public partial class EvolutionAnimationPlayer : CanvasLayer
	{
		public static EvolutionAnimationPlayer Instance { get; private set; }

		[Export] private ColorRect _flashRect;
		[Export] private Label _evoLabel;

		// Referencja do kamery sceny gry — opcjonalna
		[Export] private Camera2D _gameCamera;

		private Vector2 _cameraBaseZoom = Vector2.One;
		private bool _isPlaying = false;

		// Sygnał — odbiera CreatureLineUI żeby po animacji odświeżyć sprite
		[Signal] public delegate void AnimationFinishedEventHandler();

		public override void _Ready()
		{
			Instance = this;
			Visible = false;

			if (_gameCamera != null)
				_cameraBaseZoom = _gameCamera.Zoom;
		}

		// ── PUBLICZNE API ─────────────────────────────────────────────────

		/// <summary>
		/// Odtwarza pełną sekwencję animacji ewolucji.
		/// </summary>
		/// <param name="isFinalForm">Jeśli true — efekty mocniejsze (finalna forma).</param>
		public void PlayEvolution(bool isFinalForm = false)
		{
			if (_isPlaying) return;
			_isPlaying = true;
			Visible = true;

			string labelText = isFinalForm ? "✨ FORMA FINALNA!" : "🧬 EWOLUCJA!";
			Color labelColor = isFinalForm ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 1f, 0.6f);

			if (_evoLabel != null)
			{
				_evoLabel.Text = labelText;
				_evoLabel.AddThemeColorOverride("font_color", labelColor);
				_evoLabel.Scale = Vector2.Zero;
				_evoLabel.Visible = true;
			}

			if (_flashRect != null)
			{
				_flashRect.Color = new Color(1, 1, 1, 0);
				_flashRect.Visible = true;
			}

			var tween = CreateTween();
			tween.SetParallel(false);

			// 1. Zoom kamery w górę
			if (_gameCamera != null)
			{
				tween.TweenProperty(_gameCamera, "zoom",
					_cameraBaseZoom * (isFinalForm ? 1.35f : 1.2f), 0.18f)
					.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			}

			// 2. Flash ekranu
			tween.SetParallel(true);
			if (_flashRect != null)
			{
				tween.TweenProperty(_flashRect, "color",
					new Color(1, 1, 1, isFinalForm ? 0.6f : 0.35f), 0.1f);
			}

			// 3. Label "EWOLUCJA!" się pojawia
			if (_evoLabel != null)
			{
				tween.TweenProperty(_evoLabel, "scale", Vector2.One * 1.15f, 0.15f)
					.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			}

			// -- pauza na peak --
			tween.SetParallel(false);
			tween.TweenInterval(isFinalForm ? 0.55f : 0.35f);

			// 4. Fade out wszystkiego
			tween.SetParallel(true);
			if (_flashRect != null)
				tween.TweenProperty(_flashRect, "color", new Color(1, 1, 1, 0), 0.25f);

			if (_evoLabel != null)
			{
				tween.TweenProperty(_evoLabel, "scale", Vector2.Zero, 0.2f)
					.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
			}

			// 5. Kamera wraca
			if (_gameCamera != null)
			{
				tween.TweenProperty(_gameCamera, "zoom", _cameraBaseZoom, 0.3f)
					.SetTrans(Tween.TransitionType.Spring).SetEase(Tween.EaseType.Out);
			}

			tween.SetParallel(false);
			tween.TweenCallback(Callable.From(OnAnimationDone));
		}

		// ── PRYWATNE ──────────────────────────────────────────────────────

		private void OnAnimationDone()
		{
			_isPlaying = false;
			Visible = false;
			EmitSignal(SignalName.AnimationFinished);
		}
	}
}
