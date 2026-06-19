using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public static class DamageCalculator
	{
		public static float CalculateDamage(
			AbilityProfile profile,
			string word,
			WordCategory categories,
			IReadOnlyList<Tile> usedTilesInOrder)
		{
			if (usedTilesInOrder.Count == 0)
			{
				return 0f;
			}

			int adjustedLength = AbilityProfile.CalculateAdjustedLength(word, usedTilesInOrder, profile.Loadout);
			float damage = AbilityProfile.GetBaseDamageFromLength(adjustedLength);
			damage *= profile.Session.GetDamageMultiplier();
			damage += GetFlatHeartBonus(profile.Loadout);

			bool scimitar = profile.Loadout.Has(TreasureId.ScimitarOfJustice);
			float gemBonus = GemBoostCalculator.SumGemBonus(usedTilesInOrder, scimitar);
			float bonusPercent = gemBonus + GetTreasureBonusPercent(profile.Loadout, categories, word);

			if (bonusPercent > 0f)
			{
				damage *= 1f + bonusPercent;
			}

			if (TryGetWeaknessMultiplier(profile, categories, out float weaknessMultiplier))
			{
				damage *= weaknessMultiplier;
			}

			return damage;
		}

		public static IReadOnlyList<string> DescribeBonuses(
			AbilityProfile profile,
			string word,
			WordCategory categories,
			IReadOnlyList<Tile> usedTilesInOrder)
		{
			List<string> bonuses = [];
			if (categories != WordCategory.None)
			{
				bonuses.Add(FormatCategories(categories));
			}

			float flatBonus = GetFlatHeartBonus(profile.Loadout);
			if (flatBonus > 0f)
			{
				bonuses.Add($"+{flatBonus:0.##} heart");
			}

			float treasureBonus = GetTreasureBonusPercent(profile.Loadout, categories, word);
			if (treasureBonus > 0f)
			{
				bonuses.Add($"+{treasureBonus * 100:0}% treasure");
			}

			bool scimitar = profile.Loadout.Has(TreasureId.ScimitarOfJustice);
			float gemBonus = GemBoostCalculator.SumGemBonus(usedTilesInOrder, scimitar);
			if (gemBonus > 0f)
			{
				string label = scimitar ? "+{0:0}% gems (Scimitar)" : "+{0:0}% gems";
				bonuses.Add(string.Format(label, gemBonus * 100f));
			}

			if (profile.Session.LexLevel > 2 || profile.Session.PowerUpActive)
			{
				bonuses.Add($"Lex {profile.Session.GetDamageMultiplier() * 100:0}%");
			}

			if (TryGetWeaknessMultiplier(profile, categories, out float weaknessMultiplier))
			{
				bonuses.Add($"{weaknessMultiplier:0.#}x enemy weakness");
			}

			if (usedTilesInOrder.Count > 0
				&& AbilityProfile.CalculateAdjustedLength(word, usedTilesInOrder, profile.Loadout)
					!= AbilityProfile.CalculateAdjustedLength(word, profile.Loadout))
			{
				bonuses.Add("letter tile bonus");
			}

			string? gemDrop = GemDropCalculator.DescribeShortWordGemChance(
				profile.Loadout,
				AbilityProfile.CalculateAdjustedLength(word, usedTilesInOrder, profile.Loadout));
			if (gemDrop is not null)
			{
				bonuses.Add(gemDrop);
			}

			return bonuses;
		}

		public static bool MeetsMinWordLength(AbilityProfile profile, string word)
		{
			int minLength = profile.Fight?.MinWordLength ?? Solver.MinWordLength;
			return word.Length >= minLength;
		}

		private static bool TryGetWeaknessMultiplier(
			AbilityProfile profile,
			WordCategory categories,
			out float multiplier)
		{
			multiplier = 1f;
			if (profile.Fight is FightContext fight && fight.MatchesWeakness(categories))
			{
				multiplier = fight.WeaknessMultiplier;
				return true;
			}

			if (profile.EnemyWeakness != WordCategory.None
				&& categories != WordCategory.None
				&& categories.HasFlag(profile.EnemyWeakness))
			{
				multiplier = profile.EnemyWeaknessMultiplier;
				return true;
			}

			return false;
		}

		private static float GetFlatHeartBonus(Loadout loadout) => loadout.Has(TreasureId.HandOfHercules) ? 1f : loadout.Has(TreasureId.HephaestusHammer) ? 0.5f : 0f;

		private static float GetTreasureBonusPercent(Loadout loadout, WordCategory categories, string word)
		{
			float bonus = 0f;
			if (categories.HasFlag(WordCategory.Colors))
			{
				if (loadout.Has(TreasureId.TabletOfTheAges))
				{
					bonus += 1.50f;
				}
				else if (loadout.Has(TreasureId.TomeOfAncients))
				{
					bonus += 1.00f;
				}
			}

			if (categories.HasFlag(WordCategory.Metals) && loadout.Has(TreasureId.HandOfHercules))
			{
				bonus += 0.50f;
			}

			if (categories.HasFlag(WordCategory.Mammals))
			{
				if (loadout.Has(TreasureId.SlayerTalisman))
				{
					bonus += 0.75f;
				}
				else if (loadout.Has(TreasureId.WolfbaneNecklace))
				{
					bonus += 0.50f;
				}
			}

			if (loadout.Has(TreasureId.QuadrumvirSignet) && word.Contains("qua", StringComparison.Ordinal))
			{
				bonus += 0.50f;
			}

			return bonus;
		}

		private static readonly (WordCategory Flag, string Label)[] CategoryLabels =
		[
			(WordCategory.Colors, "color"),
			(WordCategory.Metals, "metal"),
			(WordCategory.Mammals, "mammal"),
			(WordCategory.Felines, "feline"),
			(WordCategory.Bone, "bone"),
			(WordCategory.Fire, "fire"),
			(WordCategory.FruitsAndVegetables, "fruit/veg"),
			(WordCategory.Adjectives, "adjective"),
			(WordCategory.Verbs, "verb"),
			(WordCategory.Words, "word")
		];

		private static string FormatCategories(WordCategory categories) =>
			string.Join('/', CategoryLabels.Where(pair => categories.HasFlag(pair.Flag)).Select(pair => pair.Label));
	}
}