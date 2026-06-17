using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace Bookworm_Bot_Automation
{
	public static class TileLetterExtractor
	{
		public const int NormalizedSize = 32;

		public static bool[,] ExtractLetterMask(Bitmap tileCrop)
		{
			int width = tileCrop.Width;
			int height = tileCrop.Height;
			if (width <= 0 || height <= 0)
			{
				return new bool[NormalizedSize, NormalizedSize];
			}

			if (!TryBuildLetterCandidates(tileCrop, out bool[,]? candidates, out int innerLeft, out int innerTop, out int innerRight, out int innerBottom)
				|| candidates is null)
			{
				return new bool[NormalizedSize, NormalizedSize];
			}

			int filled = CountTrue(candidates, width, height);
			int minimumPixels = Math.Max(6, (width * height) / 80);

			if (filled < minimumPixels)
			{
				return new bool[NormalizedSize, NormalizedSize];
			}

			if (!TryGetBoundingBox(candidates, width, height, out int minX, out int minY, out int maxX, out int maxY))
			{
				return new bool[NormalizedSize, NormalizedSize];
			}

			int innerWidth = Math.Max(1, innerRight - innerLeft);
			int innerHeight = Math.Max(1, innerBottom - innerTop);
			int boundingArea = Math.Max(1, maxX - minX + 1) * Math.Max(1, maxY - minY + 1);
			int innerArea = innerWidth * innerHeight;
			int averageSaturation = GetAverageSaturation(tileCrop, innerLeft, innerTop, innerRight, innerBottom);
			bool lightBackground = GetBackgroundLuminance(tileCrop, innerLeft, innerTop, innerRight, innerBottom) >= 130;
			if (!lightBackground && averageSaturation > 55 && boundingArea > innerArea * 7 / 10)
			{
				return new bool[NormalizedSize, NormalizedSize];
			}

			int cropWidth = Math.Max(1, maxX - minX + 1);
			int cropHeight = Math.Max(1, maxY - minY + 1);
			bool[,] cropped = new bool[cropWidth, cropHeight];
			for (int y = 0; y < cropHeight; y++)
			{
				for (int x = 0; x < cropWidth; x++)
				{
					cropped[x, y] = candidates[minX + x, minY + y];
				}
			}

			cropped = KeepLargestComponent(cropped, cropWidth, cropHeight);
			PruneSpurs(cropped, cropWidth, cropHeight);
			return ScaleMask(cropped, cropWidth, cropHeight, NormalizedSize, NormalizedSize);
		}

		public static bool TryGetLetterBounds(Bitmap tileCrop, out Rectangle bounds)
		{
			bounds = Rectangle.Empty;
			int width = tileCrop.Width;
			int height = tileCrop.Height;
			if (width <= 0 || height <= 0)
			{
				return false;
			}

			if (!TryBuildLetterCandidates(tileCrop, out bool[,]? candidates, out _, out _, out _, out _)
				|| candidates is null)
			{
				return false;
			}

			int filled = CountTrue(candidates, width, height);
			if (filled < Math.Max(6, (width * height) / 100))
			{
				return false;
			}

			if (!TryGetBoundingBox(candidates, width, height, out int minX, out int minY, out int maxX, out int maxY))
			{
				return false;
			}

			int padX = Math.Max(1, width / 16);
			int padY = Math.Max(1, height / 16);
			int left = Math.Max(0, minX - padX);
			int top = Math.Max(0, minY - padY);
			int right = Math.Min(width, maxX + padX + 1);
			int bottom = Math.Min(height, maxY + padY + 1);
			if (right <= left || bottom <= top)
			{
				return false;
			}

			bounds = new Rectangle(left, top, right - left, bottom - top);
			return true;
		}

		public static Bitmap CropToLetter(Bitmap tileCrop)
		{
			if (!TryGetLetterBounds(tileCrop, out Rectangle bounds))
			{
				return (Bitmap)tileCrop.Clone();
			}

			return tileCrop.Clone(bounds, tileCrop.PixelFormat);
		}

		private static bool TryBuildLetterCandidates(
			Bitmap tileCrop,
			out bool[,]? candidates,
			out int innerLeft,
			out int innerTop,
			out int innerRight,
			out int innerBottom)
		{
			candidates = null;
			innerLeft = innerTop = innerRight = innerBottom = 0;
			int width = tileCrop.Width;
			int height = tileCrop.Height;
			if (width <= 0 || height <= 0)
			{
				return false;
			}

			int marginX = Math.Max(1, width / 8);
			int marginY = Math.Max(1, height / 8);
			innerLeft = marginX;
			innerTop = marginY;
			innerRight = width - marginX;
			innerBottom = height - marginY;

			List<int> luminances = [];
			List<int> saturations = [];
			for (int y = innerTop; y < innerBottom; y++)
			{
				for (int x = innerLeft; x < innerRight; x++)
				{
					Color pixel = tileCrop.GetPixel(x, y);
					luminances.Add(GetLuminance(pixel));
					saturations.Add(GetSaturation(pixel));
				}
			}

			if (luminances.Count == 0)
			{
				return false;
			}

			int averageLuminance = luminances.Sum() / luminances.Count;
			int backgroundLuminance = GetPercentile(luminances, 60);
			bool lightBackground = backgroundLuminance >= 130;
			int darkLetterThreshold = Math.Max(95, backgroundLuminance - 42);

			int brightThreshold = GetPercentile(luminances, 82);
			brightThreshold = Math.Max(brightThreshold, averageLuminance + 35);
			brightThreshold = Math.Clamp(brightThreshold, 145, 235);

			candidates = new bool[width, height];
			for (int y = innerTop; y < innerBottom; y++)
			{
				for (int x = innerLeft; x < innerRight; x++)
				{
					if (IsGemCorner(x, y, innerLeft, innerTop, innerRight, innerBottom))
					{
						continue;
					}

					Color pixel = tileCrop.GetPixel(x, y);
					int luminance = GetLuminance(pixel);
					int saturation = GetSaturation(pixel);
					bool darkOnLight = lightBackground && luminance <= darkLetterThreshold && saturation <= 80;
					bool brightLetter = luminance >= brightThreshold && saturation <= 95;
					bool darkStroke = luminance <= 88 && saturation <= 60;
					bool lightGemLetter = backgroundLuminance >= 165 && luminance <= backgroundLuminance - 18;
					candidates[x, y] = darkOnLight || brightLetter || darkStroke || lightGemLetter;
				}
			}

			int filled = CountTrue(candidates, width, height);
			int minimumPixels = Math.Max(6, (width * height) / 80);
			int averageSaturation = saturations.Sum() / saturations.Count;

			if (filled < minimumPixels && averageSaturation <= 55)
			{
				brightThreshold = Math.Max(GetPercentile(luminances, 70), 130);
				for (int y = innerTop; y < innerBottom; y++)
				{
					for (int x = innerLeft; x < innerRight; x++)
					{
						Color pixel = tileCrop.GetPixel(x, y);
						int luminance = GetLuminance(pixel);
						candidates[x, y] = luminance >= brightThreshold;
					}
				}
			}

			return true;
		}

		private static int GetAverageSaturation(Bitmap tileCrop, int innerLeft, int innerTop, int innerRight, int innerBottom)
		{
			long total = 0;
			long count = 0;
			for (int y = innerTop; y < innerBottom; y++)
			{
				for (int x = innerLeft; x < innerRight; x++)
				{
					total += GetSaturation(tileCrop.GetPixel(x, y));
					count++;
				}
			}

			return count == 0 ? 0 : (int)(total / count);
		}

		private static int GetBackgroundLuminance(Bitmap tileCrop, int innerLeft, int innerTop, int innerRight, int innerBottom)
		{
			List<int> luminances = [];
			for (int y = innerTop; y < innerBottom; y++)
			{
				for (int x = innerLeft; x < innerRight; x++)
				{
					luminances.Add(GetLuminance(tileCrop.GetPixel(x, y)));
				}
			}

			return luminances.Count == 0 ? 0 : GetPercentile(luminances, 60);
		}

		private static void PruneSpurs(bool[,] mask, int width, int height)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (mask[x, y] && CountNeighbors(mask, x, y, width, height) == 0)
					{
						mask[x, y] = false;
					}
				}
			}
		}

		private static int CountNeighbors(bool[,] mask, int x, int y, int width, int height)
		{
			int count = 0;
			for (int offsetY = -1; offsetY <= 1; offsetY++)
			{
				for (int offsetX = -1; offsetX <= 1; offsetX++)
				{
					if (offsetX == 0 && offsetY == 0)
					{
						continue;
					}

					int neighborX = x + offsetX;
					int neighborY = y + offsetY;
					if (neighborX < 0 || neighborY < 0 || neighborX >= width || neighborY >= height)
					{
						continue;
					}

					if (mask[neighborX, neighborY])
					{
						count++;
					}
				}
			}

			return count;
		}

		private static Bitmap RenderMaskBitmap(bool[,] mask, int size)
		{
			Bitmap bitmap = new(size, size, PixelFormat.Format24bppRgb);
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					bitmap.SetPixel(x, y, mask[x, y] ? Color.Black : Color.White);
				}
			}

			return bitmap;
		}

		private static bool[,] NormalizeGlyph(bool[,] mask, int canvasSize)
		{
			int width = mask.GetLength(0);
			int height = mask.GetLength(1);
			if (!TryGetBoundingBox(mask, width, height, out int minX, out int minY, out int maxX, out int maxY))
			{
				return mask;
			}

			int cropWidth = Math.Max(1, maxX - minX + 1);
			int cropHeight = Math.Max(1, maxY - minY + 1);
			int targetSize = (int)(canvasSize * 0.78);
			bool[,] result = new bool[canvasSize, canvasSize];
			int offsetX = (canvasSize - targetSize) / 2;
			int offsetY = (canvasSize - targetSize) / 2;

			for (int y = 0; y < targetSize; y++)
			{
				for (int x = 0; x < targetSize; x++)
				{
					int sourceX = minX + (x * cropWidth / targetSize);
					int sourceY = minY + (y * cropHeight / targetSize);
					if (mask[sourceX, sourceY])
					{
						result[offsetX + x, offsetY + y] = true;
					}
				}
			}

			return result;
		}

		private static bool IsGemCorner(int x, int y, int innerLeft, int innerTop, int innerRight, int innerBottom)
		{
			int innerWidth = innerRight - innerLeft;
			int innerHeight = innerBottom - innerTop;
			return x >= innerRight - Math.Max(4, innerWidth / 3)
				&& y >= innerBottom - Math.Max(4, innerHeight / 3);
		}

		private static int GetPercentile(List<int> values, int percentile)
		{
			int[] sorted = values.ToArray();
			Array.Sort(sorted);
			int index = (sorted.Length * percentile) / 100;
			index = Math.Clamp(index, 0, sorted.Length - 1);
			return sorted[index];
		}

		private static int GetSaturation(Color pixel)
		{
			int maxChannel = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
			int minChannel = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
			return maxChannel - minChannel;
		}

		private static int GetLuminance(Color pixel) =>
			(pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;

		private static int CountTrue(bool[,] mask, int width, int height)
		{
			int count = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (mask[x, y])
					{
						count++;
					}
				}
			}

			return count;
		}

		private static bool TryGetBoundingBox(bool[,] mask, int width, int height, out int minX, out int minY, out int maxX, out int maxY)
		{
			minX = width;
			minY = height;
			maxX = 0;
			maxY = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (!mask[x, y])
					{
						continue;
					}

					minX = Math.Min(minX, x);
					minY = Math.Min(minY, y);
					maxX = Math.Max(maxX, x);
					maxY = Math.Max(maxY, y);
				}
			}

			return maxX >= minX && maxY >= minY;
		}

		private static bool[,] KeepLargestComponent(bool[,] mask, int width, int height)
		{
			bool[,] visited = new bool[width, height];
			bool[,] best = new bool[width, height];
			int bestSize = 0;

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (!mask[x, y] || visited[x, y])
					{
						continue;
					}

					bool[,] component = new bool[width, height];
					int size = FloodFill(mask, visited, component, width, height, x, y);
					if (size > bestSize)
					{
						bestSize = size;
						best = component;
					}
				}
			}

			return bestSize > 0 ? best : mask;
		}

		private static int FloodFill(bool[,] mask, bool[,] visited, bool[,] component, int width, int height, int startX, int startY)
		{
			Stack<(int X, int Y)> stack = new();
			stack.Push((startX, startY));
			int size = 0;

			while (stack.Count > 0)
			{
				(int x, int y) = stack.Pop();
				if (x < 0 || y < 0 || x >= width || y >= height || visited[x, y] || !mask[x, y])
				{
					continue;
				}

				visited[x, y] = true;
				component[x, y] = true;
				size++;
				stack.Push((x + 1, y));
				stack.Push((x - 1, y));
				stack.Push((x, y + 1));
				stack.Push((x, y - 1));
			}

			return size;
		}

		private static bool[,] ScaleMask(bool[,] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
		{
			bool[,] target = new bool[targetWidth, targetHeight];
			for (int y = 0; y < targetHeight; y++)
			{
				for (int x = 0; x < targetWidth; x++)
				{
					int sourceX = x * sourceWidth / targetWidth;
					int sourceY = y * sourceHeight / targetHeight;
					target[x, y] = source[sourceX, sourceY];
				}
			}

			return target;
		}
	}
}
