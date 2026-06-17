using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public static class GemBoostCalculator
	{
		public static float SumGemBonus(IReadOnlyList<Tile> usedTiles, bool scimitarEquipped)
		{
			float total = 0f;
			foreach (Tile tile in usedTiles)
			{
				if (tile.Gem == GemType.None)
				{
					continue;
				}

				float gemBonus = tile.DamageBonus;
				if (scimitarEquipped)
				{
					gemBonus += 0.10f;
				}

				total += gemBonus;
			}

			return total;
		}

		public static int CountGemTiles(IReadOnlyList<Tile> usedTiles) => usedTiles.Count(tile => tile.Gem != GemType.None);
	}
}