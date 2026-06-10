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
	///   ├── CreatureSprite (AnimatedSprite2D)  ← podmeniasz tekstury przez CreatureLineUI
	///   └── FloatingTextSpawner (Node2D)       ← punkt spawnu latającego tekstu
	/// </summary>
	public partial class CreatureClickArea : Area2D
	{
		[Export] private AnimatedSprite2D _creatureSprite;
		[Export] private Node2D _floatingTextSpawner;
		[Export] private PackedScene _floatingTextScene;   // res://Scenes/UI/FloatingDnaText.tscn

		// Animacja "bounce" przy kliknięciu
		private Vector2 _baseScale = Vector2.One;
		private bool _isBouncing = false;
		private float _bounceTimer = 0f;
		private const float BounceDuration = 0.12f;
		private const float BounceScale = 1.15f;

		public override void _Ready()
		{
			InputPickable = true;
			InputEvent += OnInputEvent;
			_baseScale = Scale;
		}

		public override void _Process(double delta)
		{
			if (!_isBouncing) return;

			_bounceTimer += (float)delta;
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
								  + new Vector2(GD.RandRange(-30f, 30f), 0);
			text.SetText($"+{GameManager.FormatDna(amount)}");
		}

		/// <summary>
		/// Wywołaj z CreatureLineUI gdy zmienia się forma ewolucji.
		/// </summary>
		public void SetCreatureAnimation(string animationName)
		{
			if (_creatureSprite == null) return;
			if (_creatureSprite.SpriteFrames?.HasAnimation(animationName) == true)
				_creatureSprite.Play(animationName);
		}
	}
}
