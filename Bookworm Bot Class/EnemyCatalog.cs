using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public readonly record struct EnemyDefinition(
		string Id,
		string Name,
		int Book,
		int Chapter,
		WordCategory WeaknessCategories,
		float WeaknessMultiplier = 3f,
		int MinWordLength = 3,
		string? Notes = null)
	{
		public string DisplayName => $"{Book}-{Chapter} {Name}";
	}

	public static class EnemyCatalog
	{
		private static readonly EnemyDefinition[] AllEnemies =
		[
			new("sphinx", "The Sphinx", 1, 4, WordCategory.Colors, Notes: "Color weakness"),
			new("medusa", "Medusa", 1, 10, WordCategory.None, MinWordLength: 3, Notes: "Petrify boss"),
			new("nemean-lion", "Nemean Lion", 1, 8, WordCategory.Metals | WordCategory.Felines, 3f, Notes: "Metals and felines"),
			new("minotaur", "Minotaur", 2, 1, WordCategory.Mammals, 3f),
			new("roc", "Roc", 2, 3, WordCategory.Mammals | WordCategory.Felines, 3f),
			new("tomb-ancients", "Tomb Guardian", 2, 3, WordCategory.Colors, 3f),
			new("mirage", "Mirage", 2, 6, WordCategory.FruitsAndVegetables, 3f),
			new("roc-boss", "Roc Boss", 2, 9, WordCategory.Mammals, 3f),
			new("lycanthropy", "Lycanthropy", 3, 2, WordCategory.Metals, 3f, Notes: "Metals, not mammals"),
			new("warped-words", "Warped Words", 3, 4, WordCategory.None, 3f, MinWordLength: 4, Notes: "No 3-letter words"),
			new("quadrumvir", "Quadrumvir", 3, 6, WordCategory.Metals | WordCategory.Bone, 3f),
			new("mummy-king", "Mummy King", 3, 7, WordCategory.Bone, 3f),
			new("frankenstein", "Frankenstein", 3, 8, WordCategory.Metals | WordCategory.Bone, 3f),
			new("final-boss", "Final Boss", 3, 10, WordCategory.Colors | WordCategory.Metals, 4f, MinWordLength: 4)
		];

		public static IReadOnlyList<EnemyDefinition> All => AllEnemies;
		public static bool TryGet(string id, out EnemyDefinition enemy)
		{
			foreach (EnemyDefinition candidate in AllEnemies)
			{
				if (candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase)
					|| candidate.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
				{
					enemy = candidate;
					return true;
				}
			}

			enemy = default;
			return false;
		}

		public static bool TryGetByIndex(int index, out EnemyDefinition enemy)
		{
			if (index >= 1 && index <= AllEnemies.Length)
			{
				enemy = AllEnemies[index - 1];
				return true;
			}

			enemy = default;
			return false;
		}

		public static FightContext ToFightContext(EnemyDefinition enemy)
		{
			return new()
			{
				EnemyId = enemy.Id,
				EnemyName = enemy.Name,
				WeaknessCategories = enemy.WeaknessCategories,
				WeaknessMultiplier = enemy.WeaknessMultiplier,
				MinWordLength = enemy.MinWordLength
			};
		}

		public static IEnumerable<EnemyDefinition> Search(string? query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return AllEnemies;
			}

			query = query.Trim();
			return AllEnemies.Where(enemy =>
				enemy.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| enemy.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| enemy.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase));
		}
	}
}