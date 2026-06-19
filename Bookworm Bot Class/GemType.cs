using System;
using System.Collections.Generic;

namespace Bookworm_Bot_Class
{
	public enum GemType
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

	public readonly record struct Tile(char Letter, GemType Gem = GemType.None, TileModifier Modifier = TileModifier.None)
	{
		public float DamageBonus => GemBonuses.GetDamageBonus(Gem);

		public bool ContributesToDamage => TileModifierRules.ContributesToDamage(Modifier);
		public string Display =>
			Letter == 'q'
				? Gem == GemType.None ? "qu" : $"qu({GemBonuses.ShortName(Gem)})"
				: Gem == GemType.None
					? Letter.ToString()
					: $"{Letter}({GemBonuses.ShortName(Gem)})";
	}

	public static class GemBonuses
	{
		public static float GetDamageBonus(GemType gem) => gem switch
		{
			GemType.Amethyst	=> 0.15f,
			GemType.Emerald		=> 0.20f,
			GemType.Sapphire	=> 0.25f,
			GemType.Garnet		=> 0.30f,
			GemType.Ruby		=> 0.35f,
			GemType.Crystal		=> 0.50f,
			GemType.Diamond		=> 1.00f,
			_					=> 0f
		};

		public static string ShortName(GemType gem) => gem switch
		{
			GemType.Amethyst	=> "amethyst",
			GemType.Emerald		=> "emerald",
			GemType.Sapphire	=> "sapphire",
			GemType.Garnet		=> "garnet",
			GemType.Ruby		=> "ruby",
			GemType.Crystal		=> "crystal",
			GemType.Diamond		=> "diamond",
			_					=> string.Empty
		};

		private static readonly Dictionary<string, GemType> Aliases = new(StringComparer.OrdinalIgnoreCase)
		{
			["a"] = GemType.Amethyst,
			["amethyst"] = GemType.Amethyst,
			["amy"] = GemType.Amethyst,
			["em"] = GemType.Emerald,
			["emerald"] = GemType.Emerald,
			["s"] = GemType.Sapphire,
			["sapphire"] = GemType.Sapphire,
			["sap"] = GemType.Sapphire,
			["g"] = GemType.Garnet,
			["garnet"] = GemType.Garnet,
			["gar"] = GemType.Garnet,
			["r"] = GemType.Ruby,
			["ruby"] = GemType.Ruby,
			["rub"] = GemType.Ruby,
			["c"] = GemType.Crystal,
			["crystal"] = GemType.Crystal,
			["cry"] = GemType.Crystal,
			["d"] = GemType.Diamond,
			["diamond"] = GemType.Diamond,
			["dia"] = GemType.Diamond,
		};

		public static bool TryParse(ReadOnlySpan<char> name, out GemType gem) =>
			Aliases.TryGetValue(name.ToString(), out gem);
	}
}
