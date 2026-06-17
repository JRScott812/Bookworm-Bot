using System;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_GUI
{
	internal static class LoadoutSettings
	{
		public static void Save(
			string? enemyId,
			WordCategory weakness,
			float multiplier,
			int lexLevel,
			bool powerUpActive,
			TreasureId slot1,
			TreasureId slot2,
			TreasureId slot3) =>
			LoadoutSettingsStore.Save(enemyId, weakness, multiplier, lexLevel, powerUpActive, slot1, slot2, slot3);

		public static bool TryLoad(
			out string? enemyId,
			out WordCategory weakness,
			out float multiplier,
			out int lexLevel,
			out bool powerUpActive,
			out TreasureId slot1,
			out TreasureId slot2,
			out TreasureId slot3)
		{
			enemyId = null;
			weakness = WordCategory.None;
			multiplier = 3f;
			lexLevel = 1;
			powerUpActive = false;
			slot1 = TreasureId.None;
			slot2 = TreasureId.None;
			slot3 = TreasureId.None;

			if (!LoadoutSettingsStore.TryLoad(out LoadoutSettingsData data))
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(data.EnemyId))
			{
				enemyId = data.EnemyId;
			}

			if (Enum.TryParse(data.Weakness, out WordCategory parsedWeakness))
			{
				weakness = parsedWeakness;
			}

			if (data.Multiplier > 0f)
			{
				multiplier = data.Multiplier;
			}

			if (data.LexLevel is >= 1 and <= 42)
			{
				lexLevel = data.LexLevel;
			}

			powerUpActive = data.PowerUpActive;
			slot1 = ParseTreasure(data.Slot1);
			slot2 = ParseTreasure(data.Slot2);
			slot3 = ParseTreasure(data.Slot3);
			return true;
		}

		private static TreasureId ParseTreasure(string? text) =>
			string.IsNullOrWhiteSpace(text) || !Enum.TryParse(text, out TreasureId treasure)
				? TreasureId.None
				: treasure;
	}
}
