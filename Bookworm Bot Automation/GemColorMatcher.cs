using System;
using System.Drawing;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_Automation
{
	public static class GemColorMatcher
	{
		public static GemType Match(Bitmap tileCrop)
		{
			if (!TryGetAverageColor(tileCrop, out byte r, out byte g, out byte b))
			{
				return GemType.None;
			}

			return GemColors.TryMatch(r, g, b, out GemType gem) ? gem : GemType.None;
		}

		public static bool TryGetAverageColor(Bitmap tileCrop, out byte r, out byte g, out byte b)
		{
			r = g = b = 0;
			if (tileCrop.Width <= 0 || tileCrop.Height <= 0)
			{
				return false;
			}

			int insetX = (int)(tileCrop.Width * 0.2);
			int insetY = (int)(tileCrop.Height * 0.2);
			int sampleWidth = Math.Max(1, tileCrop.Width - (insetX * 2));
			int sampleHeight = Math.Max(1, tileCrop.Height - (insetY * 2));

			long totalR = 0;
			long totalG = 0;
			long totalB = 0;
			long count = 0;

			for (int y = insetY; y < insetY + sampleHeight; y++)
			{
				for (int x = insetX; x < insetX + sampleWidth; x++)
				{
					Color pixel = tileCrop.GetPixel(x, y);
					totalR += pixel.R;
					totalG += pixel.G;
					totalB += pixel.B;
					count++;
				}
			}

			if (count == 0)
			{
				return false;
			}

			r = (byte)(totalR / count);
			g = (byte)(totalG / count);
			b = (byte)(totalB / count);
			return true;
		}
	}
}
