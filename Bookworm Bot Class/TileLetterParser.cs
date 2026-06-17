using System;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public static class TileLetterParser
	{
		public static bool TryParse(string? text, out char letter) =>
			TryParse(text, requireSingleLetter: false, out letter);

		public static bool TryParseSingleLetter(string? text, out char letter) =>
			TryParse(text, requireSingleLetter: true, out letter);

		private static bool TryParse(string? text, bool requireSingleLetter, out char letter)
		{
			letter = '?';
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}

			string normalized = text.Trim().ToUpperInvariant();
			if (normalized is "QU" or "Q")
			{
				letter = 'q';
				return !requireSingleLetter || normalized is "Q" or "QU";
			}

			int letterCount = normalized.Count(static c => c is >= 'A' and <= 'Z');
			if (requireSingleLetter && letterCount != 1)
			{
				return false;
			}

			foreach (char candidate in normalized)
			{
				if (candidate is >= 'A' and <= 'Z')
				{
					letter = char.ToLowerInvariant(candidate);
					return true;
				}
			}

			return false;
		}
	}
}
