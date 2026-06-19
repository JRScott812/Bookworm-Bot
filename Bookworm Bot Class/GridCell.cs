namespace Bookworm_Bot_Class
{
	public readonly record struct GridCell(char? Letter, GemType Gem = GemType.None, TileModifier Modifier = TileModifier.None)
	{
		public static GridCell Empty => new(null);

		public bool IsEmpty => Letter is null;

		public bool IsPlayable => !IsEmpty && TileModifierRules.IsPlayable(Modifier);

		public Tile ToTile() => new(Letter!.Value, Gem, Modifier);

		public static GridCell FromTile(Tile tile) => new(tile.Letter, tile.Gem, tile.Modifier);
	}
}
