using Godot;

namespace EvoTap
{
	/// <summary>
	/// Pojedyncza karta w Atlasie.
	///
	/// Scena: res://Scenes/UI/AtlasCard.tscn
	/// Struktura:
	///   AtlasCard (PanelContainer)
	///   └── VBoxContainer
	///       ├── CreatureImage (TextureRect)     ← sprite lub sylwetka
	///       ├── CreatureNameLabel (Label)
	///       ├── DescriptionLabel (Label)        ← humorystyczny opis lub hint
	///       └── StatusLabel (Label)             ← "✓ Odblokowane" lub "🔒 Hint"
	///
	/// Rozmiar karty w GridContainer: min_size = (140, 180)
	/// </summary>
	public partial class AtlasCard : PanelContainer
	{
		[Export] private TextureRect _creatureImage;
		[Export] private Label _creatureNameLabel;
		[Export] private Label _descriptionLabel;
		[Export] private Label _statusLabel;

		// Stylebox — przyciemniony dla locked
		[Export] private StyleBoxFlat _lockedStyle;
		[Export] private StyleBoxFlat _unlockedStyle;

		public void Setup(CreatureStageData stageData, bool unlocked)
		{
			if (stageData == null) return;

			if (unlocked)
			{
				// Pełne dane
				_creatureImage.Texture = stageData.Sprite;
				_creatureImage.Modulate = Colors.White;
				_creatureNameLabel.Text = stageData.StageName;
				_descriptionLabel.Text = stageData.ScientificDescription;
				_statusLabel.Text = "✓ Odblokowane";
				_statusLabel.Modulate = new Color(0.3f, 1f, 0.5f);

				if (_unlockedStyle != null)
					AddThemeStyleboxOverride("panel", _unlockedStyle);
			}
			else
			{
				// Sylwetka + hint
				_creatureImage.Texture = stageData.SilhouetteSprite ?? stageData.Sprite;
				_creatureImage.Modulate = new Color(0.15f, 0.15f, 0.15f, 1f);
				_creatureNameLabel.Text = "???";
				_descriptionLabel.Text = stageData.UnlockHint;
				_statusLabel.Text = "🔒 Zablokowane";
				_statusLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);

				if (_lockedStyle != null)
					AddThemeStyleboxOverride("panel", _lockedStyle);
			}
		}
	}
}
