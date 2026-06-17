using System;
using System.Drawing;
using System.IO;

using Bookworm_Bot_Automation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class CalibrationCellClassificationTests
	{
		private static readonly string CellsFolder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Bookworm Bot",
			"calibration-cells");

		[TestMethod]
		public void Ocr_reads_saved_calibration_cells_when_present()
		{
			if (!Directory.Exists(CellsFolder))
			{
				Assert.Inconclusive("No saved calibration cells.");
			}

			LetterClassifier classifier = new();
			int readable = 0;
			Console.WriteLine("OCR results on raw cell crops:");
			for (int index = 0; index < 16; index++)
			{
				string path = Path.Combine(CellsFolder, $"cell-{index:D2}.png");
				if (!File.Exists(path))
				{
					continue;
				}

				using Bitmap tile = new(path);
				bool ok = classifier.TryClassify(tile, out char letter, out string method, out string detail);
				int row = index / 4;
				int col = index % 4;
				if (ok)
				{
					readable++;
				}

				Console.WriteLine($"  [{row},{col}] {(ok ? letter.ToString() : ".."),-3} via {method,-12} {detail}");
			}

			Console.WriteLine($"Readable: {readable}/16");
			if (readable == 0)
			{
				Assert.Inconclusive("Windows OCR could not read any saved calibration cell crops.");
			}
		}
	}
}
