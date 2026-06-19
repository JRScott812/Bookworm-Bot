namespace Bookworm_Bot_Class
{
	public enum WordPowerRating
	{
		TooShort = 0,
		Good,
		Wow,
		Nice,
		VeryGood,
		Awesome,
		Excellent,
		Fantastic,
		Astonishing
	}

	public static class WordPowerRatings
	{
		public static WordPowerRating FromAdjustedLength(int adjustedLength) => adjustedLength switch
		{
			< Solver.MinWordLength => WordPowerRating.TooShort,
			3 => WordPowerRating.Good,
			4 => WordPowerRating.Wow,
			5 => WordPowerRating.Nice,
			6 => WordPowerRating.VeryGood,
			7 => WordPowerRating.Awesome,
			8 => WordPowerRating.Excellent,
			9 => WordPowerRating.Fantastic,
			_ => WordPowerRating.Astonishing
		};

		public static string GetLabel(WordPowerRating rating) => rating switch
		{
			WordPowerRating.Good => "Good",
			WordPowerRating.Wow => "Wow",
			WordPowerRating.Nice => "Nice",
			WordPowerRating.VeryGood => "Very Good",
			WordPowerRating.Awesome => "Awesome",
			WordPowerRating.Excellent => "Excellent",
			WordPowerRating.Fantastic => "Fantastic",
			WordPowerRating.Astonishing => "Astonishing",
			_ => string.Empty
		};
	}
}
