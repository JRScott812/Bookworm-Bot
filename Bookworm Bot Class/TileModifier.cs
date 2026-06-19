namespace Bookworm_Bot_Class
{
	public enum TileModifier
	{
		None = 0,
		Locked,
		Smashed,
		Plagued
	}

	public static class TileModifierRules
	{
		public static bool IsPlayable(TileModifier modifier) => modifier != TileModifier.Locked;

		public static bool ContributesToDamage(TileModifier modifier) =>
			modifier is not (TileModifier.Smashed or TileModifier.Plagued);
	}
}
