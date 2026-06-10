using Godot;

namespace EvoTap
{
	/// <summary>
	/// Ekran wyboru biomu — lista 5 biomów, zablokowane są przyciemnione z info jak odblokować.
	///
	/// Scena: res://Scenes/UI/BiomeSelectScreen.tscn
	/// Struktura:
	///   BiomeSelectScreen (CanvasLayer)
	///   └── MarginContainer
	///       └── VBoxContainer
	///           ├── TitleLabel
	///           ├── CloseButton
	///           └── BiomeList (VBoxContainer)   ← tutaj generujemy rzędy
	/// </summary>
	public partial class BiomeSelectScreen : CanvasLayer
	{
		[Export] private Button _closeButton;
		[Export] private VBoxContainer _biomeList;
		[Export] private PackedScene _biomeRowScene;  // res://Scenes/UI/BiomeRow.tscn

		public override void _Ready()
		{
			_closeButton.Pressed += Close;
			PopulateBiomeList();

			Modulate = new Color(1, 1, 1, 0);
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", Colors.White, 0.2);
		}

		private void PopulateBiomeList()
		{
			foreach (BiomeType biome in System.Enum.GetValues<BiomeType>())
			{
				var biomeData = BiomeManager.Instance.GetBiomeData(biome);
				if (biomeData == null) continue;

				bool unlocked  = BiomeManager.Instance.IsBiomeUnlocked(biome);
				bool completed = BiomeManager.Instance.IsBiomeCompleted(biome);
				bool isCurrent = biome == GameManager.Instance.CurrentBiome;

				var row = new BiomeRow();
				_biomeList.AddChild(row);
				row.Setup(biomeData, unlocked, completed, isCurrent, () => SelectBiome(biome));
			}
		}

		private void SelectBiome(BiomeType biome)
		{
			if (!BiomeManager.Instance.IsBiomeUnlocked(biome)) return;

			GameManager.Instance.SwitchBiome(biome);
			BiomeManager.Instance.LoadBiome(biome);
			Close();
		}

		private void Close()
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.15);
			tween.TweenCallback(Callable.From(QueueFree));
		}
	}

	// ── Inline helper — BiomeRow ────────────────────────────────────────────
	// Możesz też zrobić to jako osobna scena .tscn; tutaj jako prosty HBoxContainer.

	public partial class BiomeRow : HBoxContainer
	{
		private System.Action _onSelect;

		public void Setup(BiomeData data, bool unlocked, bool completed, bool isCurrent, System.Action onSelect)
		{
			_onSelect = onSelect;

			// Emoji / ikona biomu
			var icon = new Label { Text = GetBiomeEmoji(data.BiomeType) };
			icon.CustomMinimumSize = new Vector2(40, 0);
			AddChild(icon);

			// Nazwa i opis
			var textBox = new VBoxContainer();
			textBox.SizeFlagsHorizontal = SizeFlags.Expand;

			var nameLabel = new Label
			{
				Text = data.BiomeName,
				HorizontalAlignment = HorizontalAlignment.Left
			};

			var descLabel = new Label
			{
				Text = unlocked ? data.BiomeDescription : UnlockHint(data.BiomeType),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			descLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			descLabel.AddThemeFontSizeOverride("font_size", 12);

			textBox.AddChild(nameLabel);
			textBox.AddChild(descLabel);
			AddChild(textBox);

			// Status / przycisk
			var btn = new Button();
			if (!unlocked)
			{
				btn.Text = "🔒";
				btn.Disabled = true;
				Modulate = new Color(0.5f, 0.5f, 0.5f);
			}
			else if (isCurrent)
			{
				btn.Text = "▶ Aktywny";
				btn.Disabled = true;
				nameLabel.AddThemeColorOverride("font_color", data.AccentColor);
			}
			else if (completed)
			{
				btn.Text = "✓ Ukończony";
				btn.Pressed += () => _onSelect?.Invoke();
			}
			else
			{
				btn.Text = "Przejdź →";
				btn.Pressed += () => _onSelect?.Invoke();
			}

			AddChild(btn);
		}

		private static string GetBiomeEmoji(BiomeType b) => b switch
		{
			BiomeType.Ocean   => "🌊",
			BiomeType.Jungle  => "🌿",
			BiomeType.Volcano => "🌋",
			BiomeType.Tundra  => "❄️",
			BiomeType.Abyss   => "🌑",
			_ => "?"
		};

		private static string UnlockHint(BiomeType b) => b switch
		{
			BiomeType.Jungle  => "Ukończ Neonowy Ocean",
			BiomeType.Volcano => "Ukończ Pradawną Dżunglę",
			BiomeType.Tundra  => "Ukończ Wulkaniczne Piekło",
			BiomeType.Abyss   => "Ukończ Wieczną Tundrę",
			_ => "?"
		};
	}
}
