using System.Collections.Generic;

namespace Bookworm_Bot_Class
{
	public readonly record struct GemDropChance(GemType Gem, float Probability);

	public static class GemDropCalculator
	{
		public const int MinCreationAdjustedLength = 6;

		public static IReadOnlyList<GemDropChance> GetShortWordChances(Loadout loadout, int adjustedLength)
		{
			if (adjustedLength is < 3 or > 5)
			{
				return [];
			}

			if (loadout.Has(TreasureId.EndlessGemPouch))
			{
				return adjustedLength switch
				{
					3 => [new(GemType.Amethyst, 0.10f)],
					4 =>
					[
						new(GemType.Amethyst, 0.25f),
						new(GemType.Emerald, 0.10f)
					],
					5 =>
					[
						new(GemType.Amethyst, 0.65f),
						new(GemType.Emerald, 0.35f)
					],
					_ => []
				};
			}

			if (loadout.Has(TreasureId.JeweledKey))
			{
				return adjustedLength switch
				{
					3 => [new(GemType.Amethyst, 0.05f)],
					4 => [new(GemType.Amethyst, 0.25f)],
					5 =>
					[
						new(GemType.Amethyst, 0.75f),
						new(GemType.Emerald, 0.25f)
					],
					_ => []
				};
			}

			return [];
		}

		public static string? DescribeShortWordGemChance(Loadout loadout, int adjustedLength)
		{
			IReadOnlyList<GemDropChance> chances = GetShortWordChances(loadout, adjustedLength);
			if (chances.Count == 0)
			{
				return null;
			}

			List<string> parts = [];
			foreach (GemDropChance chance in chances)
			{
				parts.Add($"{chance.Probability * 100:0.#}% {GemBonuses.ShortName(chance.Gem)}");
			}

			string treasure = loadout.Has(TreasureId.EndlessGemPouch) ? "Endless Gem Pouch" : "Jeweled Key";
			return $"{treasure}: {string.Join(", ", parts)}";
		}
	}
}
