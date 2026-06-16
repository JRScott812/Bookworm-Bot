using System;
using System.Collections.Generic;

namespace Bookworm_Bot_Class
{
	internal static class LetterInput
	{
		public static List<Tile> Parse(string input)
		{
			List<Tile> tiles = [];

			for (int index = 0; index < input.Length; index++)
			{
				char letter = char.ToLowerInvariant(input[index]);
				if (!char.IsLetter(letter))
					continue;

				if (letter == 'q')
				{
					if (index + 1 < input.Length && char.ToLowerInvariant(input[index + 1]) == 'u')
						index++;

					tiles.Add(new Tile('q', ReadGem(input, ref index)));
					continue;
				}

				tiles.Add(new Tile(letter, ReadGem(input, ref index)));
			}

			return tiles;
		}

		private static GemType ReadGem(string input, ref int index)
		{
			if (index + 1 >= input.Length || input[index + 1] != '$')
				return GemType.None;

			index += 2;
			int start = index;

			while (index < input.Length && char.IsLetter(input[index]))
				index++;

			ReadOnlySpan<char> name = input.AsSpan(start, index - start);
			if (GemBonuses.TryParse(name, out GemType gem))
				return gem;

			index = start - 2;
			return GemType.None;
		}
	}
}
