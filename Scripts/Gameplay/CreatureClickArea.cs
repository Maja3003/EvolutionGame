using Godot;

namespace EvoTap
{
	/// <summary>
	/// Główny klikalny obszar stworzenia na ekranie gry.
	///
	/// Scena: res://Scenes/Creatures/CreatureClickArea.tscn
	/// Struktura:
	///   CreatureClickArea (Area2D)
	///   ├── CollisionShape2D
	///   ├── CreatureSprite (Sprite2D)          ← podmieniana przez CreatureLineUI
	///   └── FloatingTextSpawner (Node2D)       ← punkt spawnu latającego tekstu
	/// </summary>
	public partial class CreatureClickArea : Area2D
	{
		[Export] private NeonowaRyba _neonowaRyba;
		[Export] private Sprite2D _creatureSprite;
		[Export] private Node2D _floatingTextSpawner;
		[Export] private PackedScene _floatingTextScene;   // res://Scenes/UI/FloatingDnaText.tscn

		// Animacja "bounce" przy kliknięciu
		private Vector2 _baseScale = Vector2.One;
		private bool _isBouncing = false;
		private float _bounceTimer = 0f;
		private const float BounceDuration = 0.12f;
		private const float BounceScale = 1.15f;

		// Docelowa szerokość stworzenia na ekranie (px) — sprite skalowany proporcjonalnie
		private const float TargetSpriteWidth = 220f;

		// Ciągła animacja "idle" — lekkie kołysanie/unoszenie całego stworzenia
		private float _idleTime = 0f;
		private const float IdleBobAmplitude = 6f;
		private const float IdleBobSpeed = 1.5f;
		private const float IdleSwayAmplitude = 4f;
		private const float IdleSwaySpeed = 0.9f;
		private const float IdleRotationAmplitude = 0.03f; // radiany

		// Animacja "podpłynięcia" co sporo kliknięć
		private Vector2 _basePosition;
		private bool _isSwimming = false;
		private int _clicksSinceSwim = 0;
		private int _clicksUntilSwim = 0;
		private const int MinClicksForSwim = 25;
		private const int MaxClicksForSwim = 40;
		private const float SwimHeight = 180f;

		public override void _Ready()
		{
			InputPickable = true;
			InputEvent += OnInputEvent;
			_baseScale = Scale;
			_basePosition = Position;
			_neonowaRyba?.PlayIdle();
			_clicksUntilSwim = (int)GD.RandRange(MinClicksForSwim, MaxClicksForSwim);
		}

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			ProcessIdleAnimation(dt);
			ProcessBounce(dt);
		}

		// ── IDLE ANIMACJA ─────────────────────────────────────────────────

		private void ProcessIdleAnimation(float dt)
		{
			// Animację obsługuje NeonowaRyba AnimationPlayer
		}

		// ── BOUNCE PRZY KLIKNIĘCIU ────────────────────────────────────────

		private void ProcessBounce(float dt)
		{
			if (!_isBouncing) return;

			_bounceTimer += dt;
			float t = _bounceTimer / BounceDuration;

			if (t >= 1f)
			{
				Scale = _baseScale;
				_isBouncing = false;
				return;
			}

			// Ease out bounce: rośnie szybko, wraca powoli
			float scaleFactor = t < 0.5f
				? Mathf.Lerp(1f, BounceScale, t * 2f)
				: Mathf.Lerp(BounceScale, 1f, (t - 0.5f) * 2f);

			Scale = _baseScale * scaleFactor;
		}

		// ── PODPŁYNIĘCIE CO KILKA KLIKNIĘĆ ────────────────────────────────

		private void TriggerSwim()
		{
			if (_isSwimming) return;
			_isSwimming = true;

			float xDrift = (float)GD.RandRange(-40.0, 40.0);

			var tween = CreateTween();
			tween.SetTrans(Tween.TransitionType.Sine);

			tween.TweenProperty(this, "position", _basePosition + new Vector2(xDrift, -SwimHeight), 0.5f)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(this, "position", _basePosition, 0.6f)
				.SetEase(Tween.EaseType.In);

			tween.TweenCallback(Callable.From(() => _isSwimming = false));
		}

		private void OnInputEvent(Node viewport, InputEvent inputEvent, long shapeIdx)
		{
			if (inputEvent is not InputEventScreenTouch touch || !touch.Pressed)
				if (inputEvent is not InputEventMouseButton mouse || !mouse.Pressed)
					return;

			HandleClick();
		}

		private void HandleClick()
		{
			double gained = GameManager.Instance.OnCreatureClicked();
			if (gained <= 0) return;

			TriggerBounce();
			SpawnFloatingText(gained);

			// Animacja ryby przy kliknięciu
			_neonowaRyba?.PlaySpecial1();  // rozbłysk neonowy

			_clicksSinceSwim++;
			if (_clicksSinceSwim >= _clicksUntilSwim)
			{
				_clicksSinceSwim = 0;
				_clicksUntilSwim = (int)GD.RandRange(MinClicksForSwim, MaxClicksForSwim);
				TriggerSwim();
				_neonowaRyba?.PlayMove();  // pływanie przy swimie
			}
		}

		private void TriggerBounce()
		{
			_isBouncing = true;
			_bounceTimer = 0f;
		}

		private void SpawnFloatingText(double amount)
		{
			if (_floatingTextScene == null || _floatingTextSpawner == null) return;

			var text = _floatingTextScene.Instantiate<FloatingDnaText>();
			_floatingTextSpawner.GetParent().AddChild(text);
			text.GlobalPosition = _floatingTextSpawner.GlobalPosition
					  + new Vector2((float)GD.RandRange(-30.0, 30.0), 0);
			text.SetText($"+{GameManager.FormatDna(amount)}");
		}

		/// <summary>
		/// Wywołaj z CreatureLineUI gdy zmienia się forma ewolucji.
		/// </summary>
		public void SetCreatureSprite(Texture2D texture)
		{
			if (_creatureSprite == null || texture == null) return;
			_creatureSprite.Texture = texture;

			float width = texture.GetWidth();
			if (width > 0)
			{
				float scale = TargetSpriteWidth / width;
				_creatureSprite.Scale = new Vector2(scale, scale);
			}
		}
	}
}
