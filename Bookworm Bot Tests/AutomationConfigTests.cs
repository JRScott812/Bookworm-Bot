using System;
using System.Collections.Generic;
using System.Drawing;

using Bookworm_Bot_Automation;

using Bookworm_Bot_Class;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class AutomationConfigTests
	{
		[TestMethod]
		public void AutomationConfig_builds_sixteen_non_overlapping_cells()
		{
			AutomationConfig config = AutomationConfig.FromBoardBounds(1024, 768, 312, 220, 712, 620);

			Assert.IsTrue(config.IsValid);
			Assert.AreEqual(100, config.CellWidth);
			Assert.AreEqual(100, config.CellHeight);

			HashSet<(int X, int Y)> origins = [];
			int totalWidth = 0;
			int totalHeight = 0;
			foreach (Rectangle rect in config.GetAllCellRects())
			{
				Assert.IsGreaterThan(0, rect.Width);
				Assert.IsGreaterThan(0, rect.Height);
				Assert.IsTrue(origins.Add((rect.X, rect.Y)));
				totalWidth = Math.Max(totalWidth, rect.Right);
				totalHeight = Math.Max(totalHeight, rect.Bottom);
			}

			Assert.HasCount(16, origins);
			Assert.AreEqual(712 - config.CellInset, totalWidth);
			Assert.AreEqual(620 - config.CellInset, totalHeight);
		}

		[TestMethod]
		public void AutomationConfig_distributes_remainder_pixels_across_cells()
		{
			AutomationConfig config = AutomationConfig.FromBoardBounds(800, 600, 305, 308, 496, 508);

			IReadOnlyList<Rectangle> rects = config.GetAllCellRects();
			Assert.HasCount(16, rects);
			Assert.AreEqual(496 - config.CellInset, rects[3].Right);
			Assert.AreEqual(508 - config.CellInset, rects[15].Bottom);
			Assert.AreEqual(48, rects[3].Width + (config.CellInset * 2));
		}

		[TestMethod]
		public void GemColorMatcher_reads_synthetic_amethyst_patch()
		{
			using Bitmap patch = new(32, 32);
			using Graphics graphics = Graphics.FromImage(patch);
			graphics.Clear(Color.FromArgb(168, 85, 247));

			Assert.AreEqual(GemType.Amethyst, GemColorMatcher.Match(patch));
		}
	}
}
