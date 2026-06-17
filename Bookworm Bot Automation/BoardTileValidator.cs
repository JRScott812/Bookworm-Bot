using System;
using System.Drawing;

namespace Bookworm_Bot_Automation
{
	public static class BoardTileValidator
	{
		public const int MinimumTileLikeCells = 8;

		public static bool LooksLikeLetterTile(Bitmap cellCrop)
		{
			if (cellCrop.Width < 12 || cellCrop.Height < 12)
			{
				return false;
			}

			bool[,] mask = TileLetterExtractor.ExtractLetterMask(cellCrop);
			int filled = CountFilled(mask);
			if (filled < 8)
			{
				return false;
			}

			if (!TileLetterExtractor.TryGetLetterBounds(cellCrop, out Rectangle bounds))
			{
				return false;
			}

			int cropArea = cellCrop.Width * cellCrop.Height;
			int boundsArea = bounds.Width * bounds.Height;
			if (boundsArea < cropArea / 25 || boundsArea > cropArea * 3 / 4)
			{
				return false;
			}

			int centerX = cellCrop.Width / 2;
			int centerY = cellCrop.Height / 2;
			int boundsCenterX = bounds.X + (bounds.Width / 2);
			int boundsCenterY = bounds.Y + (bounds.Height / 2);
			int maxOffsetX = Math.Max(8, cellCrop.Width / 3);
			int maxOffsetY = Math.Max(8, cellCrop.Height / 3);
			return Math.Abs(boundsCenterX - centerX) <= maxOffsetX
				&& Math.Abs(boundsCenterY - centerY) <= maxOffsetY;
		}

		public static int CountTileLikeCells(Bitmap frame, AutomationConfig config)
		{
			int count = 0;
			for (int index = 0; index < AutomationConfig.GridBoardSize * AutomationConfig.GridBoardSize; index++)
			{
				Rectangle rect = config.GetCellRect(index);
				if (rect.Right > frame.Width || rect.Bottom > frame.Height || rect.Width <= 0 || rect.Height <= 0)
				{
					continue;
				}

				using Bitmap cell = frame.Clone(rect, frame.PixelFormat);
				if (LooksLikeLetterTile(cell))
				{
					count++;
				}
			}

			return count;
		}

		public static bool ValidateBoardRegion(Bitmap frame, AutomationConfig config, out int tileLikeCells)
		{
			tileLikeCells = CountTileLikeCells(frame, config);
			return tileLikeCells >= MinimumTileLikeCells;
		}

		private static int CountFilled(bool[,] mask)
		{
			int width = mask.GetLength(0);
			int height = mask.GetLength(1);
			int filled = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (mask[x, y])
					{
						filled++;
					}
				}
			}

			return filled;
		}
	}
}
