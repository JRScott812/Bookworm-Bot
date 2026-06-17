using System.Collections.Generic;
using System.Numerics;

namespace Bookworm_Bot_Class
{
	public static class TileBoard
	{
		public static List<Tile> ApplyPlayedWord(
			IReadOnlyList<Tile> tiles,
			int usedMask,
			IReadOnlyList<Tile> replacements)
		{
			List<Tile> next = [];
			for (int index = 0; index < tiles.Count; index++)
			{
				if ((usedMask & (1 << index)) == 0)
				{
					next.Add(tiles[index]);
				}
			}

			next.AddRange(replacements);
			return next;
		}

		public static List<Tile> GetUsedTiles(IReadOnlyList<Tile> tiles, int usedMask)
		{
			List<Tile> used = [];
			for (int index = 0; index < tiles.Count; index++)
			{
				if ((usedMask & (1 << index)) != 0)
				{
					used.Add(tiles[index]);
				}
			}

			return used;
		}

		public static List<Tile> GetRemainingTiles(IReadOnlyList<Tile> tiles, int usedMask)
		{
			List<Tile> remaining = [];
			for (int index = 0; index < tiles.Count; index++)
			{
				if ((usedMask & (1 << index)) == 0)
				{
					remaining.Add(tiles[index]);
				}
			}

			return remaining;
		}

		public static int CountUsedTiles(int usedMask) => BitOperations.PopCount((uint)usedMask);
	}
}