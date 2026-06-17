using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_Automation
{
	public sealed class LetterRecognizer
	{
		private const int OcrMinLongestSide = 120;

		private readonly OcrEngine? _engine = CreateEngine();

		public bool IsAvailable => _engine is not null;

		public bool TryRecognizeLetter(Bitmap tileCrop, out char letter) =>
			TryRecognizeLetter(tileCrop, out letter, out _);

		public bool TryRecognizeLetter(Bitmap tileCrop, out char letter, out string? rawText)
		{
			letter = '?';
			rawText = null;
			if (_engine is null || tileCrop.Width <= 0 || tileCrop.Height <= 0)
			{
				return false;
			}

			if (TryRecognizeCrop(_engine, tileCrop, out letter, out rawText))
			{
				return true;
			}

			int scale = GetOcrScaleFactor(tileCrop);
			if (scale <= 1)
			{
				return false;
			}

			using Bitmap enlarged = ScaleCrop(tileCrop, scale);
			return TryRecognizeCrop(_engine, enlarged, out letter, out rawText);
		}

		public string? RecognizeRawText(Bitmap bitmap)
		{
			if (_engine is null)
			{
				return null;
			}

			TryRecognizeCrop(_engine, bitmap, out _, out string? rawText);
			return rawText;
		}

		private static bool TryRecognizeCrop(OcrEngine engine, Bitmap bitmap, out char letter, out string? rawText)
		{
			letter = '?';
			OcrResult? result = Recognize(engine, bitmap);
			rawText = FormatOcrText(result);
			return TryParseOcrResult(result, out letter);
		}

		private static int GetOcrScaleFactor(Bitmap tileCrop)
		{
			int longest = Math.Max(tileCrop.Width, tileCrop.Height);
			if (longest >= OcrMinLongestSide)
			{
				return 1;
			}

			return Math.Max(2, (int)Math.Ceiling(OcrMinLongestSide / (double)longest));
		}

		private static Bitmap ScaleCrop(Bitmap tileCrop, int scaleFactor)
		{
			int width = tileCrop.Width * scaleFactor;
			int height = tileCrop.Height * scaleFactor;
			Bitmap enlarged = new(width, height, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(enlarged);
			graphics.Clear(Color.White);
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			graphics.DrawImage(tileCrop, new Rectangle(0, 0, width, height));
			return enlarged;
		}

		private static OcrResult? Recognize(OcrEngine engine, Bitmap bitmap)
		{
			SoftwareBitmap? softwareBitmap = ConvertToSoftwareBitmap(bitmap);
			if (softwareBitmap is null)
			{
				return null;
			}

			using (softwareBitmap)
			{
				return engine.RecognizeAsync(softwareBitmap).AsTask().GetAwaiter().GetResult();
			}
		}

		private static string? FormatOcrText(OcrResult? result)
		{
			if (result is null)
			{
				return null;
			}

			string text = string.Concat(result.Lines
				.SelectMany(line => line.Words)
				.Select(word => word.Text));
			return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
		}

		private static bool TryParseOcrResult(OcrResult? result, out char letter)
		{
			letter = '?';
			if (result is null)
			{
				return false;
			}

			foreach (OcrWord word in result.Lines.SelectMany(line => line.Words))
			{
				if (TileLetterParser.TryParseSingleLetter(word.Text, out letter))
				{
					return true;
				}
			}

			string fullText = string.Concat(result.Lines
				.SelectMany(line => line.Words)
				.Select(word => word.Text));
			if (TileLetterParser.TryParseSingleLetter(fullText, out letter))
			{
				return true;
			}

			char? onlyLetter = null;
			foreach (char candidate in fullText.ToUpperInvariant())
			{
				if (candidate is < 'A' or > 'Z')
				{
					continue;
				}

				if (onlyLetter is not null)
				{
					return false;
				}

				onlyLetter = char.ToLowerInvariant(candidate);
			}

			if (onlyLetter is char resolved)
			{
				letter = resolved;
				return true;
			}

			return false;
		}

		private static OcrEngine? CreateEngine()
		{
			try
			{
				return OcrEngine.TryCreateFromLanguage(new Language("en-US"))
					?? OcrEngine.TryCreateFromUserProfileLanguages();
			}
			catch
			{
				return OcrEngine.TryCreateFromUserProfileLanguages();
			}
		}

		private static SoftwareBitmap? ConvertToSoftwareBitmap(Bitmap bitmap)
		{
			using MemoryStream stream = new();
			bitmap.Save(stream, ImageFormat.Bmp);
			stream.Position = 0;
			IRandomAccessStream randomAccessStream = stream.AsRandomAccessStream();
			BitmapDecoder decoder = BitmapDecoder.CreateAsync(randomAccessStream).AsTask().GetAwaiter().GetResult();
			return decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied)
				.AsTask()
				.GetAwaiter()
				.GetResult();
		}
	}
}
