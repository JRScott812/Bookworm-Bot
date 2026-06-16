using System;
using System.Collections.Generic;

namespace Bookworm_Bot
{
	internal sealed class AbilityProfile
	{
		public WordCategory EnemyWeakness { get; set; }
		public float EnemyWeaknessMultiplier { get; set; } = 3f;

		public bool TomeOfAncients { get; set; }
		public bool TabletOfTheAges { get; set; }
		public bool HandOfHercules { get; set; }
		public bool WolfbaneNecklace { get; set; }
		public bool SlayerTalisman { get; set; }

		public float CalculateDamage(string word, WordCategory categories, float gemBonus = 0f)
		{
			float damage = GetBaseDamage(word);
			float bonusPercent = gemBonus + GetTreasureBonusPercent(categories);

			if (bonusPercent > 0f)
				damage *= 1f + bonusPercent;

			if (EnemyWeakness != WordCategory.None
				&& categories != WordCategory.None
				&& categories.HasFlag(EnemyWeakness))
			{
				damage *= EnemyWeaknessMultiplier;
			}

			return damage;
		}

		public IReadOnlyList<string> DescribeBonuses(string word, WordCategory categories, float gemBonus = 0f)
		{
			List<string> bonuses = [];

			if (categories != WordCategory.None)
				bonuses.Add(FormatCategories(categories));

			float treasureBonus = GetTreasureBonusPercent(categories);
			if (treasureBonus > 0f)
				bonuses.Add($"+{treasureBonus * 100:0}% treasure");

			if (gemBonus > 0f)
				bonuses.Add($"+{gemBonus * 100:0}% gems");

			if (EnemyWeakness != WordCategory.None
				&& categories.HasFlag(EnemyWeakness))
			{
				bonuses.Add($"{EnemyWeaknessMultiplier:0.#}x enemy weakness");
			}

			return bonuses;
		}

		private float GetTreasureBonusPercent(WordCategory categories)
		{
			float bonus = 0f;

			if (categories.HasFlag(WordCategory.Colors))
			{
				if (TabletOfTheAges)
					bonus += 1.50f;
				else if (TomeOfAncients)
					bonus += 1.00f;
			}

			if (categories.HasFlag(WordCategory.Metals) && HandOfHercules)
				bonus += 0.50f;

			if (categories.HasFlag(WordCategory.Mammals))
			{
				if (SlayerTalisman)
					bonus += 0.75f;
				else if (WolfbaneNecklace)
					bonus += 0.50f;
			}

			return bonus;
		}

		internal static float GetBaseDamage(string word)
		{
			int adjustedLength = CalculateAdjustedLength(word);
			if (adjustedLength < Solver.MinWordLength)
				return 0f;

			if (adjustedLength < BaseDamageByAdjustedLength.Length)
				return BaseDamageByAdjustedLength[adjustedLength];

			return BaseDamageByAdjustedLength[^1];
		}

		internal static int CalculateAdjustedLength(string word)
		{
			float total = 0f;

			for (int index = 0; index < word.Length; index++)
			{
				if (word[index] == 'q' && index + 1 < word.Length && word[index + 1] == 'u')
				{
					total += 2.75f;
					index++;
					continue;
				}

				total += GetLetterWeight(word[index]);
			}

			return (int)Math.Ceiling(total);
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

		private static string FormatCategories(WordCategory categories)
		{
			List<string> names = [];
			if (categories.HasFlag(WordCategory.Colors))
				names.Add("color");
			if (categories.HasFlag(WordCategory.Metals))
				names.Add("metal");
			if (categories.HasFlag(WordCategory.Mammals))
				names.Add("mammal");

			return string.Join("/", names);
		}

		private static readonly float[] BaseDamageByAdjustedLength =
		[
			0f, 0f, 0f,
			0.5f, 0.75f, 1f, 1.5f, 2f, 2.75f, 3.5f, 4.5f, 5.5f, 6.75f, 8f, 9.5f, 11f, 13f
		];
	}
}
