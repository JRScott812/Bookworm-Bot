using System;

namespace Bookworm_Bot_Class
{
	internal enum GemType
	{
		None = 0,
		Amethyst,
		Emerald,
		Sapphire,
		Garnet,
		Ruby,
		Crystal,
		Diamond
	}

	internal readonly record struct Tile(char Letter, GemType Gem = GemType.None)
	{
		public float DamageBonus => GemBonuses.GetDamageBonus(Gem);

		public string Display =>
			Letter == 'q'
				? Gem == GemType.None ? "qu" : $"qu({GemBonuses.ShortName(Gem)})"
				: Gem == GemType.None
					? Letter.ToString()
					: $"{Letter}({GemBonuses.ShortName(Gem)})";
	}

	internal static class GemBonuses
	{
		public static float GetDamageBonus(GemType gem) => gem switch
		{
			GemType.Amethyst => 0.15f,
			GemType.Emerald => 0.20f,
			GemType.Sapphire => 0.25f,
			GemType.Garnet => 0.30f,
			GemType.Ruby => 0.35f,
			GemType.Crystal => 0.50f,
			GemType.Diamond => 1.00f,
			_ => 0f
		};

		public static string ShortName(GemType gem) => gem switch
		{
			GemType.Amethyst => "amethyst",
			GemType.Emerald => "emerald",
			GemType.Sapphire => "sapphire",
			GemType.Garnet => "garnet",
			GemType.Ruby => "ruby",
			GemType.Crystal => "crystal",
			GemType.Diamond => "diamond",
			_ => string.Empty
		};

		public static bool TryParse(ReadOnlySpan<char> name, out GemType gem)
		{
			gem = name.ToString().ToLowerInvariant() switch
			{
				"a" or "amethyst" or "amy" => GemType.Amethyst,
				"em" or "emerald" => GemType.Emerald,
				"s" or "sapphire" or "sap" => GemType.Sapphire,
				"g" or "garnet" or "gar" => GemType.Garnet,
				"r" or "ruby" or "rub" => GemType.Ruby,
				"c" or "crystal" or "cry" => GemType.Crystal,
				"d" or "diamond" or "dia" => GemType.Diamond,
				_ => GemType.None
			};

			return gem != GemType.None;
		}
	}
}
