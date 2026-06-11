using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvoTap
{
	/// <summary>
	/// Autoload singleton. Obsługuje zapis/odczyt stanu gry do user://save.json
	/// Rejestruj jako AutoLoad w Project Settings: "SaveManager" → res://Scripts/Core/SaveManager.cs
	/// </summary>
	public partial class SaveManager : Node
	{
		private const string SavePath = "user://save.json";
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			Converters = { new JsonStringEnumConverter() }
		};

		public static SaveManager Instance { get; private set; }

		public GameSaveData CurrentSave { get; private set; } = new();

		public override void _Ready()
		{
			Instance = this;
			Load();
		}

		// ── ZAPIS ──────────────────────────────────────────────────────────

		public void Save()
		{
			CurrentSave.LastSaveUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			try
			{
				string json = JsonSerializer.Serialize(CurrentSave, JsonOptions);
				using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
				if (file != null)
					file.StoreString(json);
			}
			catch (Exception e)
			{
				GD.PrintErr($"[SaveManager] Błąd zapisu: {e.Message}");
			}
		}

		// ── ODCZYT ─────────────────────────────────────────────────────────

		public void Load()
		{
			if (!FileAccess.FileExists(SavePath))
			{
				CurrentSave = CreateNewSave();
				Save();
				return;
			}

			try
			{
				using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
				if (file == null)
				{
					CurrentSave = CreateNewSave();
					return;
				}

				string json = file.GetAsText();
				CurrentSave = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions) ?? CreateNewSave();
			}
			catch (Exception e)
			{
				GD.PrintErr($"[SaveManager] Błąd odczytu: {e.Message}. Tworzę nowy zapis.");
				CurrentSave = CreateNewSave();
				Save();
			}
		}

		// ── RESET / PRESTIGE ───────────────────────────────────────────────

		public void ResetForPrestige()
		{
			int newCycle = CurrentSave.PrestigeCycle + 1;
			double newMultiplier = CurrentSave.PrestigeMultiplier * 2.0;
			var atlasCards = CurrentSave.UnlockedAtlasCards; // Karty zostają!
			bool adsRemoved = CurrentSave.AdsRemoved;

			CurrentSave = CreateNewSave();
			CurrentSave.PrestigeCycle = newCycle;
			CurrentSave.PrestigeMultiplier = newMultiplier;
			CurrentSave.UnlockedAtlasCards = atlasCards;
			CurrentSave.AdsRemoved = adsRemoved;

			Save();
			GD.Print($"[SaveManager] Prestige! Cykl: {newCycle}, Mnożnik: {newMultiplier}×");
		}

		// ── HELPERS ────────────────────────────────────────────────────────

		private static GameSaveData CreateNewSave()
		{
			var save = new GameSaveData
			{
				CurrentBiomeId = BiomeType.Ocean.ToString(),
				LastSaveUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			// Inicjalizuj biomy — tylko Ocean odblokowany
			foreach (BiomeType biome in Enum.GetValues<BiomeType>())
			{
				save.Biomes.Add(new BiomeSaveData
				{
					BiomeType = biome,
					IsUnlocked = biome == BiomeType.Ocean
				});
			}

			return save;
		}

		public BiomeSaveData GetBiomeSave(BiomeType biome)
		{
			return CurrentSave.Biomes.Find(b => b.BiomeType == biome);
		}

		public CreatureLineSaveData GetCreatureLineSave(BiomeType biome, string lineId)
		{
			var biomeSave = GetBiomeSave(biome);
			return biomeSave?.CreatureLines.Find(c => c.LineId == lineId);
		}

		public void AddAtlasCard(string cardId)
		{
			if (!CurrentSave.UnlockedAtlasCards.Contains(cardId))
			{
				CurrentSave.UnlockedAtlasCards.Add(cardId);
				Save();
			}
		}

		public bool HasAtlasCard(string cardId) =>
			CurrentSave.UnlockedAtlasCards.Contains(cardId);
	}
}
