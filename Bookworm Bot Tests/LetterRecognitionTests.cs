using System;
using System.Drawing;
using System.Drawing.Imaging;

using Bookworm_Bot_Automation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class LetterRecognitionTests
	{
		[TestMethod]
		public void TileLetterExtractor_finds_letter_pixels_on_synthetic_tile()
		{
			using Bitmap tile = CreateSyntheticTile(letter: 'Z', background: Color.FromArgb(220, 38, 38));
			bool[,] mask = TileLetterExtractor.ExtractLetterMask(tile);

			int filled = 0;
			for (int y = 0; y < mask.GetLength(1); y++)
			{
				for (int x = 0; x < mask.GetLength(0); x++)
				{
					if (mask[x, y])
					{
						filled++;
					}
				}
			}

			Assert.IsGreaterThanOrEqualTo(20, filled);
		}

		[TestMethod]
		public void TileLetterExtractor_builds_letter_mask_from_synthetic_tile()
		{
			using Bitmap tile = CreateSyntheticTile(letter: 'A', background: Color.FromArgb(34, 197, 94));
			bool[,] mask = TileLetterExtractor.ExtractLetterMask(tile);

			int darkPixels = 0;
			for (int y = 0; y < mask.GetLength(1); y++)
			{
				for (int x = 0; x < mask.GetLength(0); x++)
				{
					if (mask[x, y])
					{
						darkPixels++;
					}
				}
			}

			Assert.IsGreaterThanOrEqualTo(20, darkPixels);
		}

		[TestMethod]
		public void TileLetterExtractor_finds_dark_letter_on_tan_tile()
		{
			using Bitmap tile = CreateTanTile('W');
			bool[,] mask = TileLetterExtractor.ExtractLetterMask(tile);

			int filled = 0;
			for (int y = 0; y < mask.GetLength(1); y++)
			{
				for (int x = 0; x < mask.GetLength(0); x++)
				{
					if (mask[x, y])
					{
						filled++;
					}
				}
			}

			Assert.IsGreaterThanOrEqualTo(20, filled);
		}

		private static Bitmap CreateSyntheticTile(char letter, Color background)
		{
			Bitmap tile = new(45, 46, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(tile);
			graphics.Clear(background);
			using Font font = new("Arial Black", 22, FontStyle.Bold, GraphicsUnit.Pixel);
			using StringFormat format = new()
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			RectangleF bounds = new(0, 0, tile.Width, tile.Height);
			using SolidBrush shadow = new(Color.FromArgb(30, 30, 30));
			graphics.DrawString(letter.ToString(), font, shadow, new RectangleF(1, 1, tile.Width, tile.Height), format);
			graphics.DrawString(letter.ToString(), font, Brushes.White, bounds, format);
			return tile;
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
