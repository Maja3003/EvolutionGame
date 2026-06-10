using Godot;

namespace EvoTap
{
	/// <summary>
	/// Latający tekst "+1.2K" który pojawia się po kliknięciu.
	///
	/// Scena: res://Scenes/UI/FloatingDnaText.tscn
	/// Struktura:
	///   FloatingDnaText (Node2D)
	///   └── Label (Label)
	///
	/// Ustaw w scenie:
	///   - Label: font size ~24, bold, kolor biały z cieniem
	///   - Node2D: Z-index wysoki (np. 10)
	/// </summary>
	public partial class FloatingDnaText : Node2D
	{
		[Export] private Label _label;

		private float _lifetime = 0f;
		private const float MaxLifetime = 1.1f;
		private const float RiseSpeed = 90f;       // piksele/sek w górę
		private const float FadeStartAt = 0.6f;    // kiedy zaczyna zanikać (% lifetime)

		public void SetText(string text)
		{
			if (_label != null)
				_label.Text = text;
		}

		public override void _Process(double delta)
		{
			_lifetime += (float)delta;

			// Przesuń w górę
			Position += Vector2.Up * RiseSpeed * (float)delta;

			// Zanikanie
			float progress = _lifetime / MaxLifetime;
			if (progress > FadeStartAt)
			{
				float fade = 1f - (progress - FadeStartAt) / (1f - FadeStartAt);
				Modulate = new Color(1, 1, 1, Mathf.Clamp(fade, 0f, 1f));
			}

			// Usuń po wygaśnięciu
			if (_lifetime >= MaxLifetime)
				QueueFree();
		}
	}
}
