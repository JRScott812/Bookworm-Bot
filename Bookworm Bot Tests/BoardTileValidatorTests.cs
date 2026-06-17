using System.Drawing;
using System.Drawing.Imaging;

using Bookworm_Bot_Automation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class BoardTileValidatorTests
	{
		[TestMethod]
		public void LooksLikeLetterTile_accepts_synthetic_tile()
		{
			using Bitmap tile = CreateSyntheticTile('A');
			Assert.IsTrue(BoardTileValidator.LooksLikeLetterTile(tile));
		}

		[TestMethod]
		public void LooksLikeLetterTile_rejects_plain_background()
		{
			using Bitmap tile = new(64, 64, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(tile);
			graphics.Clear(Color.FromArgb(120, 80, 60));
			Assert.IsFalse(BoardTileValidator.LooksLikeLetterTile(tile));
		}

		private static Bitmap CreateSyntheticTile(char letter)
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
