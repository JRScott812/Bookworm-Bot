namespace Bookworm_Bot_Class
{
	public sealed class FightContext
	{
		public string? EnemyId { get; set; }
		public string? EnemyName { get; set; }
		public WordCategory WeaknessCategories { get; set; }
		public float WeaknessMultiplier { get; set; } = 3f;
		public int MinWordLength { get; set; } = Solver.MinWordLength;
		public bool MatchesWeakness(WordCategory wordCategories) => WeaknessCategories != WordCategory.None && wordCategories != WordCategory.None && (wordCategories & WeaknessCategories) != WordCategory.None;
	}
}