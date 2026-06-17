using System;
using System.Collections.Generic;

namespace Bookworm_Bot_Class
{
	public readonly record struct RgbColor(byte R, byte G, byte B)
	{
		public double DistanceTo(RgbColor other)
		{
			int dr = R - other.R;
			int dg = G - other.G;
			int db = B - other.B;
			return Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
		}
	}

	public static class GemColors
	{
		public const double DefaultMatchThreshold = 85d;

		private static readonly IReadOnlyList<(GemType Gem, RgbColor Color)> Palette =
		[
			(GemType.Amethyst, new RgbColor(168, 85, 247)),
			(GemType.Emerald, new RgbColor(34, 197, 94)),
			(GemType.Sapphire, new RgbColor(37, 99, 235)),
			(GemType.Garnet, new RgbColor(249, 115, 22)),
			(GemType.Ruby, new RgbColor(220, 38, 38)),
			(GemType.Crystal, new RgbColor(236, 72, 153)),
			(GemType.Diamond, new RgbColor(255, 255, 255))
		];

		public static RgbColor GetRgb(GemType gem)
		{
			foreach ((GemType candidate, RgbColor color) in Palette)
			{
				if (candidate == gem)
				{
					return color;
				}
			}

			return default;
		}

		public static bool TryMatch(byte r, byte g, byte b, out GemType gem, double maxDistance = DefaultMatchThreshold)
		{
			gem = GemType.None;
			RgbColor sample = new(r, g, b);
			double bestDistance = double.MaxValue;
			GemType bestGem = GemType.None;

			foreach ((GemType candidate, RgbColor color) in Palette)
			{
				double distance = sample.DistanceTo(color);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestGem = candidate;
				}
			}

			if (bestGem == GemType.None || bestDistance > maxDistance)
			{
				return false;
			}

			gem = bestGem;
			return true;
		}
	}
}
