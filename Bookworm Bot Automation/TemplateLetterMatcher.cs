using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Bookworm_Bot_Automation
{
	public readonly record struct TemplateMatchScore(char Letter, double Score);

	public static class TemplateLetterMatcher
	{
		private const int TemplateSize = TileLetterExtractor.NormalizedSize;

		private static readonly IReadOnlyDictionary<char, bool[,]> Templates = BuildTemplates();

		public static bool TryMatch(Bitmap tileCrop, out char letter)
		{
			letter = '?';
			if (tileCrop.Width <= 0 || tileCrop.Height <= 0)
			{
				return false;
			}

			bool[,] sample = TileLetterExtractor.ExtractLetterMask(tileCrop);
			return TryMatch(sample, out letter);
		}

		public static bool TryMatch(bool[,] sample, out char letter)
		{
			letter = '?';
			if (!TryGetBestMatch(sample, out TemplateMatchScore best, out TemplateMatchScore second))
			{
				return false;
			}

			bool confident = best.Score >= 0.12
				&& (second.Letter == '?' || best.Score >= second.Score + 0.05);
			if (!confident)
			{
				return false;
			}

			letter = best.Letter;
			return true;
		}

		public static IReadOnlyList<TemplateMatchScore> RankMatches(bool[,] sample)
		{
			List<TemplateMatchScore> scores = [];
			foreach ((char candidate, bool[,] template) in Templates)
			{
				double score = ScoreMatch(sample, candidate, template);
				if (score > 0)
				{
					scores.Add(new TemplateMatchScore(candidate, score));
				}
			}

			return scores
				.OrderByDescending(static match => match.Score)
				.ThenBy(static match => match.Letter)
				.ToList();
		}

		public static bool TryGetBestMatch(bool[,] sample, out TemplateMatchScore best, out TemplateMatchScore second)
		{
			best = default;
			second = default;
			if (CountFilled(sample) < 4)
			{
				return false;
			}

			IReadOnlyList<TemplateMatchScore> ranked = RankMatches(sample);
			if (ranked.Count == 0)
			{
				return false;
			}

			best = ranked[0];
			second = ranked.Count > 1 ? ranked[1] : new TemplateMatchScore('?', 0);
			return best.Letter != '?';
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

		private static double ScoreMatch(bool[,] sample, char letter, bool[,] template)
		{
			double syntheticScore = ScoreMasks(sample, template);
			if (!GameLetterTemplateStore.TryGetMask(letter, out bool[,]? gameTemplate) || gameTemplate is null)
			{
				return syntheticScore;
			}

			return Math.Max(syntheticScore, ScoreMasks(sample, gameTemplate) * 1.05);
		}

		private static double ScoreMasks(bool[,] sample, bool[,] template)
		{
			int width = Math.Min(sample.GetLength(0), template.GetLength(0));
			int height = Math.Min(sample.GetLength(1), template.GetLength(1));
			int intersection = 0;
			int union = 0;
			int templateFilled = 0;
			int sampleFilled = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					bool sampleOn = sample[x, y];
					bool templateOn = template[x, y];
					if (templateOn)
					{
						templateFilled++;
					}

					if (sampleOn)
					{
						sampleFilled++;
					}

					if (sampleOn && templateOn)
					{
						intersection++;
					}

					if (sampleOn || templateOn)
					{
						union++;
					}
				}
			}

			if (templateFilled == 0 || sampleFilled == 0)
			{
				return 0;
			}

			double iou = union == 0 ? 0 : (double)intersection / union;
			double templateCoverage = (double)intersection / templateFilled;
			double samplePrecision = (double)intersection / sampleFilled;
			return Math.Max(iou, Math.Max(templateCoverage * 0.92, samplePrecision * 0.78));
		}

		private static IReadOnlyDictionary<char, bool[,]> BuildTemplates()
		{
			Dictionary<char, bool[,]> templates = new();
			string letters = "abcdefghijklmnopqrstuvwxyz";

			foreach (string fontName in new[]
			         {
				         "Impact",
				         "Franklin Gothic Heavy",
				         "Arial Black",
				         "Trebuchet MS",
				         "Verdana",
			         })
			{
				try
				{
					using Font font = new(fontName, 21, FontStyle.Bold, GraphicsUnit.Pixel);
					foreach (char letter in letters)
					{
						if (templates.ContainsKey(letter))
						{
							continue;
						}

						templates[letter] = RenderTemplate(
							letter == 'q' ? "Q" : letter.ToString().ToUpperInvariant(),
							font);
					}
				}
				catch
				{
					// Try the next font.
				}
			}

			if (templates.Count == 0)
			{
				using Font font = new(FontFamily.GenericSansSerif, 21, FontStyle.Bold, GraphicsUnit.Pixel);
				foreach (char letter in letters)
				{
					templates[letter] = RenderTemplate(
						letter == 'q' ? "Q" : letter.ToString().ToUpperInvariant(),
						font);
				}
			}

			return templates;
		}

		private static bool[,] RenderTemplate(string text, Font font)
		{
			using Bitmap bitmap = new(TemplateSize, TemplateSize, PixelFormat.Format24bppRgb);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.Black);
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			using StringFormat format = new()
			{
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			graphics.DrawString(text, font, Brushes.White, new RectangleF(0, 0, TemplateSize, TemplateSize), format);

			bool[,] mask = new bool[TemplateSize, TemplateSize];
			for (int y = 0; y < TemplateSize; y++)
			{
				for (int x = 0; x < TemplateSize; x++)
				{
					Color pixel = bitmap.GetPixel(x, y);
					mask[x, y] = pixel.GetBrightness() > 0.45f;
				}
			}

			return mask;
		}
	}
}
