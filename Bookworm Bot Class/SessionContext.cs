namespace Bookworm_Bot_Class
{
	public sealed class SessionContext
	{
		public int LexLevel { get; set; } = 1;
		public bool PowerUpActive { get; set; }
		public static float GetLexLevelMultiplier(int lexLevel)
		{
			if (lexLevel <= 2)
			{
				return 1f;
			}

			if (lexLevel >= 42)
			{
				return 4.5f;
			}

			int tier = (lexLevel - 3) / 3;
			return 1.25f + (tier * 0.25f);
		}

		public float GetDamageMultiplier() => GetLexLevelMultiplier(LexLevel) * (PowerUpActive ? 1.25f : 1f);
	}
}