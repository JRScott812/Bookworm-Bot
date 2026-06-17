using System;
using System.IO;
using System.Text.Json;

namespace Bookworm_Bot_Class
{
	public sealed class LoadoutSettingsData
	{
		public string? EnemyId { get; set; }
		public string Weakness { get; set; } = WordCategory.None.ToString();
		public float Multiplier { get; set; } = 3f;
		public int LexLevel { get; set; } = 1;
		public bool PowerUpActive { get; set; }
		public string Slot1 { get; set; } = TreasureId.None.ToString();
		public string Slot2 { get; set; } = TreasureId.None.ToString();
		public string Slot3 { get; set; } = TreasureId.None.ToString();
	}

	public static class LoadoutSettingsStore
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

		public static string SettingsDirectory =>
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bookworm Bot");

		public static string SettingsPath => Path.Combine(SettingsDirectory, "loadout.json");

		public static void Save(LoadoutSettingsData data)
		{
			try
			{
				Directory.CreateDirectory(SettingsDirectory);
				File.WriteAllText(SettingsPath, JsonSerializer.Serialize(data, JsonOptions));
			}
			catch
			{
				// Persistence is best-effort for unpackaged desktop apps.
			}
		}

		public static void Save(
			string? enemyId,
			WordCategory weakness,
			float multiplier,
			int lexLevel,
			bool powerUpActive,
			TreasureId slot1,
			TreasureId slot2,
			TreasureId slot3) =>
			Save(new LoadoutSettingsData
			{
				EnemyId = enemyId,
				Weakness = weakness.ToString(),
				Multiplier = multiplier,
				LexLevel = lexLevel,
				PowerUpActive = powerUpActive,
				Slot1 = slot1.ToString(),
				Slot2 = slot2.ToString(),
				Slot3 = slot3.ToString()
			});

		public static bool TryLoad(out LoadoutSettingsData data)
		{
			data = new LoadoutSettingsData();
			try
			{
				if (!File.Exists(SettingsPath))
				{
					return false;
				}

				LoadoutSettingsData? loaded = JsonSerializer.Deserialize<LoadoutSettingsData>(File.ReadAllText(SettingsPath));
				if (loaded is null)
				{
					return false;
				}

				data = loaded;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static AbilityProfile BuildProfile(LoadoutSettingsData? data = null)
		{
			data ??= TryLoad(out LoadoutSettingsData loaded) ? loaded : new LoadoutSettingsData();

			Loadout loadout = new()
			{
				Slot1 = ParseTreasure(data.Slot1),
				Slot2 = ParseTreasure(data.Slot2),
				Slot3 = ParseTreasure(data.Slot3)
			};
			loadout.Normalize();

			AbilityProfile profile = new()
			{
				Loadout = loadout,
				Session = new SessionContext
				{
					LexLevel = data.LexLevel is >= 1 and <= 42 ? data.LexLevel : 1,
					PowerUpActive = data.PowerUpActive
				}
			};

			if (!string.IsNullOrWhiteSpace(data.EnemyId)
				&& EnemyCatalog.TryGet(data.EnemyId, out EnemyDefinition enemy))
			{
				profile.Fight = EnemyCatalog.ToFightContext(enemy);
				profile.EnemyWeakness = WordCategory.None;
			}
			else
			{
				profile.Fight = null;
				profile.EnemyWeakness = Enum.TryParse(data.Weakness, out WordCategory weakness)
					? weakness
					: WordCategory.None;
				profile.EnemyWeaknessMultiplier = data.Multiplier > 0f ? data.Multiplier : 3f;
			}

			return profile;
		}

		private static TreasureId ParseTreasure(string? text) =>
			string.IsNullOrWhiteSpace(text) || !Enum.TryParse(text, out TreasureId treasure)
				? TreasureId.None
				: treasure;
	}
}
