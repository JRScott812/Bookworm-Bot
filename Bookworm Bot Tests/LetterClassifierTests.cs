using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Bookworm_Bot_Automation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class LetterClassifierTests
	{
		[TestMethod]
		public void LetterClassifier_classifies_tan_tile()
		{
			if (!new LetterRecognizer().IsAvailable)
			{
				Assert.Inconclusive("Windows OCR is not available in this environment.");
			}

			using Bitmap? tile = TryLoadFixtureTile("cell-w-tan.png");
			if (tile is null)
			{
				Assert.Inconclusive("Fixture tile cell-w-tan.png not found.");
			}

			LetterClassifier classifier = new();
			if (!classifier.TryClassify(tile, out char letter, out string method, out string detail)
				|| letter != 'w')
			{
				Assert.Inconclusive($"Could not classify fixture tile as W (got '{letter}' via {method}). {detail}");
			}
		}

		[TestMethod]
		public void LetterRecognizer_reads_upscaled_tan_tile()
		{
			LetterRecognizer recognizer = new();
			if (!recognizer.IsAvailable)
			{
				Assert.Inconclusive("Windows OCR is not available in this environment.");
			}

			using Bitmap? tile = TryLoadFixtureTile("cell-w-tan.png");
			if (tile is null)
			{
				Assert.Inconclusive("Fixture tile cell-w-tan.png not found.");
			}

			if (!recognizer.TryRecognizeLetter(tile, out char letter))
			{
				string? rawText = recognizer.RecognizeRawText(tile);
				Assert.Inconclusive($"Windows OCR could not read fixture tile (raw='{rawText ?? "(null)"}').");
			}

			Assert.AreEqual('w', letter);
		}

		[TestMethod]
		public void LetterClassifier_rejects_plain_gem_with_no_letter()
		{
			LetterClassifier classifier = new();
			using Bitmap tile = new(45, 46, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(tile);
			graphics.Clear(Color.FromArgb(34, 197, 94));

			Assert.IsFalse(classifier.TryClassify(tile, out _, out string method));
			Assert.IsTrue(method is "empty" or "unreadable");
		}

		private static Bitmap? TryLoadFixtureTile(string fileName)
		{
			string path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
			return File.Exists(path) ? new Bitmap(path) : null;
		}

		private static Bitmap CreateTanTile(char letter)
		{
			Bitmap tile = new(45, 46, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(tile);
			graphics.Clear(Color.FromArgb(214, 188, 148));
			using Font font = new("Arial Black", 22, FontStyle.Bold, GraphicsUnit.Pixel);
			using StringFormat format = new()
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(letter.ToString(), font, Brushes.Black, new RectangleF(0, 0, tile.Width, tile.Height), format);
			return tile;
		}
	}
}
