using System;
using System.Collections.Generic;

namespace Bookworm_Bot_Class
{
	public sealed class AbilityProfile
	{
		public Loadout Loadout { get; set; } = new();
		public SessionContext Session { get; set; } = new();
		public FightContext? Fight { get; set; }
		public WordCategory EnemyWeakness { get; set; }
		public float EnemyWeaknessMultiplier { get; set; } = 3f;

		public float CalculateDamage(
			string word,
			WordCategory categories,
			float gemBonus = 0f,
			IReadOnlyList<Tile>? usedTilesInOrder = null)
		{
			if (usedTilesInOrder is { Count: > 0 })
			{
				return DamageCalculator.CalculateDamage(this, word, categories, usedTilesInOrder);
			}

			List<Tile> syntheticTiles = BuildSyntheticTiles(word);
			if (gemBonus > 0f)
			{
				ApplySyntheticGemBonus(syntheticTiles, gemBonus);
			}

			return DamageCalculator.CalculateDamage(this, word, categories, syntheticTiles);
		}

		public IReadOnlyList<string> DescribeBonuses(
			string word,
			WordCategory categories,
			float gemBonus = 0f,
			IReadOnlyList<Tile>? usedTilesInOrder = null)
		{
			if (usedTilesInOrder is { Count: > 0 })
			{
				return DamageCalculator.DescribeBonuses(this, word, categories, usedTilesInOrder);
			}

			List<Tile> syntheticTiles = BuildSyntheticTiles(word);
			if (gemBonus > 0f)
			{
				ApplySyntheticGemBonus(syntheticTiles, gemBonus);
			}

			return DamageCalculator.DescribeBonuses(this, word, categories, syntheticTiles);
		}

		public static float GetBaseDamage(string word) => GetBaseDamageFromLength(CalculateAdjustedLength(word));

		public static float GetBaseDamageFromLength(int adjustedLength)
		{
			return adjustedLength < Solver.MinWordLength
				? 0f
				: adjustedLength < BaseDamageByAdjustedLength.Length
				? BaseDamageByAdjustedLength[adjustedLength]
				: BaseDamageByAdjustedLength[^1];
		}

		public static int CalculateAdjustedLength(string word) => CalculateAdjustedLength(word, loadout: null);

		public static int CalculateAdjustedLength(string word, Loadout? loadout) => CalculateAdjustedLengthCore(word, loadout, usedTilesInOrder: null);

		public static int CalculateAdjustedLength(
					string word,
					IReadOnlyList<Tile> usedTilesInOrder,
					Loadout loadout) => CalculateAdjustedLengthCore(word, loadout, usedTilesInOrder);

		private static int CalculateAdjustedLengthCore(
			string word,
			Loadout? loadout,
			IReadOnlyList<Tile>? usedTilesInOrder)
		{
			float total = 0f;
			int tileIndex = 0;
			for (int index = 0; index < word.Length; index++)
			{
				if (word[index] == 'q' && index + 1 < word.Length && word[index + 1] == 'u')
				{
					total += 2.75f;
					if (usedTilesInOrder is not null)
					{
						tileIndex++;
					}

					index++;
					continue;
				}

				if (usedTilesInOrder is not null && tileIndex < usedTilesInOrder.Count)
				{
					Tile tile = usedTilesInOrder[tileIndex];
					if (tile.ContributesToDamage)
					{
						total += GetTreasureLetterWeight(tile.Letter, loadout);
					}

					tileIndex++;
				}
				else
				{
					total += GetTreasureLetterWeight(word[index], loadout);
				}
			}

			return (int)Math.Ceiling(total);
		}

		private static float GetTreasureLetterWeight(char letter, Loadout? loadout)
		{
			letter = char.ToLowerInvariant(letter);
			if (loadout is not null)
			{
				if (loadout.Has(TreasureId.ArchOfXyzzy) && letter is 'x' or 'y' or 'z')
				{
					return 3f;
				}

				if (loadout.Has(TreasureId.BowOfZyx) && letter is 'x' or 'y' or 'z')
				{
					return 2.5f;
				}

				if (loadout.Has(TreasureId.WoodenParrot) && letter == 'r')
				{
					return 2f;
				}
			}

			return GetLetterWeight(letter);
		}

		private static float GetLetterWeight(char letter)
		{
			return char.ToLowerInvariant(letter) switch
			{
				'a' or 'd' or 'e' or 'g' or 'i' or 'l' or 'n' or 'o' or 'r' or 's' or 't' or 'u' => 1f,
				'b' or 'c' or 'f' or 'h' or 'm' or 'p' => 1.25f,
				'v' or 'w' or 'y' => 1.5f,
				'j' or 'k' or 'q' => 1.75f,
				'x' or 'z' => 2f,
				_ => throw new ArgumentException($"Character '{letter}' is not a valid letter.", nameof(letter))
			};
		}

		private static List<Tile> BuildSyntheticTiles(string word)
		{
			List<Tile> tiles = [];
			for (int index = 0; index < word.Length; index++)
			{
				if (word[index] == 'q' && index + 1 < word.Length && word[index + 1] == 'u')
				{
					tiles.Add(new Tile('q'));
					index++;
					continue;
				}

				tiles.Add(new Tile(word[index]));
			}

			return tiles;
		}

		private static void ApplySyntheticGemBonus(List<Tile> tiles, float gemBonus)
		{
			if (tiles.Count == 0)
			{
				return;
			}

			tiles[0] = new Tile(tiles[0].Letter, GemType.Ruby);
			float remaining = gemBonus - tiles[0].DamageBonus;
			for (int index = 1; index < tiles.Count && remaining > 0.01f; index++)
			{
				tiles[index] = new Tile(tiles[index].Letter, GemType.Amethyst);
				remaining -= tiles[index].DamageBonus;
			}
		}

		private static readonly float[] BaseDamageByAdjustedLength =
		[
			0f, 0f, 0f,
			0.5f, 0.75f, 1f, 1.5f, 2f, 2.75f, 3.5f, 4.5f, 5.5f, 6.75f, 8f, 9.5f, 11f, 13f
		];
	}
}