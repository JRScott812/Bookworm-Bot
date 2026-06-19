using Bookworm_Bot_Class;

namespace Bookworm_Bot_GUI
{
	internal static class EmojiCatalog
	{
		public const string Heart = "❤️";
		public const string Tile = "🟨";
		public const string Worm = "🐛";
		public const string Book = "📖";

		public static string ForModifier(TileModifier modifier) => modifier switch
		{
			TileModifier.Locked => "🔒",
			TileModifier.Smashed => "💥",
			TileModifier.Plagued => "🦠",
			_ => string.Empty
		};

		public static string ForEmpty() => "⬜";

		public static string ForTreasure(TreasureId treasure) => treasure switch
		{
			TreasureId.HephaestusHammer => "🔨",
			TreasureId.HandOfHercules => "✊",
			TreasureId.BowOfZyx => "🏹",
			TreasureId.ArchOfXyzzy => "🎯",
			TreasureId.TomeOfAncients => "📜",
			TreasureId.TabletOfTheAges => "🪨",
			TreasureId.WoodenParrot => "🦜",
			TreasureId.ScimitarOfJustice => "⚔️",
			TreasureId.WolfbaneNecklace => "📿",
			TreasureId.SlayerTalisman => "🛡️",
			TreasureId.QuadrumvirSignet => "💍",
			_ => string.Empty
		};

		public static string TreasureDisplayName(TreasureId treasure)
		{
			if (treasure == TreasureId.None)
			{
				return "None";
			}

			string emoji = ForTreasure(treasure);
			return string.IsNullOrEmpty(emoji)
				? TreasureCatalog.GetDisplayName(treasure)
				: $"{emoji} {TreasureCatalog.GetDisplayName(treasure)}";
		}
	}
}
