using System.Drawing;

namespace Bookworm_Bot_Automation
{
	public sealed class LetterClassifier
	{
		private readonly LetterRecognizer _recognizer = new();

		public bool TryClassify(Bitmap tileCrop, out char letter, out string method) =>
			TryClassify(tileCrop, out letter, out method, out _);

		public bool TryClassify(Bitmap tileCrop, out char letter, out string method, out string detail)
		{
			letter = '?';
			method = "unreadable";
			detail = string.Empty;

			if (tileCrop.Width <= 0 || tileCrop.Height <= 0)
			{
				method = "empty";
				return false;
			}

			if (_recognizer.TryRecognizeLetter(tileCrop, out letter, out string? rawText))
			{
				method = "ocr";
				return true;
			}

			detail = $"ocr='{rawText ?? "(null)"}'";
			return false;
		}
	}
}
