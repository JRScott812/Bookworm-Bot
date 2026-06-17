using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_Automation
{
	public readonly record struct CellReadDiagnostic(
		int Index,
		char? Letter,
		GemType Gem,
		string Method,
		bool Playable,
		string Detail = "");

	public sealed class BwaGameBoardReader : IGameBoardReader
	{
		public const int MinimumPlayableCells = 6;

		private readonly LetterClassifier _letterClassifier = new();
		private IReadOnlyList<CellReadDiagnostic>? _lastDiagnostics;
		private string? _lastError;
		private string? _calibrationSource;

		public string? LastError => _lastError;
		public string? CalibrationSource => _calibrationSource;
		public IReadOnlyList<CellReadDiagnostic>? LastDiagnostics => _lastDiagnostics;

		public bool TryReadBoard(out GridBoard board) => TryReadBoard(verbose: false, out board);

		public bool TryReadBoard(bool verbose, out GridBoard board)
		{
			board = new GridBoard();
			_lastError = null;
			_lastDiagnostics = null;

			if (!WindowFinder.TryFind(out GameWindowInfo window))
			{
				_lastError = "Bookworm Adventures window not found. Start the game and try again.";
				return false;
			}

			if (!ConfigStore.TryLoad(window.ClientWidth, window.ClientHeight, out AutomationConfig? config, out string source)
				|| config is null
				|| !config.IsValid)
			{
				IReadOnlyList<string> saved = ConfigStore.ListSavedResolutionKeys();
				_lastError = saved.Count > 0
					? $"No valid calibration for {window.ClientWidth}x{window.ClientHeight}. Saved keys: {string.Join(", ", saved)}. Run --reset-calibration then --calibrate."
					: $"No calibration for {window.ClientWidth}x{window.ClientHeight}. Press C to calibrate.";
				_calibrationSource = null;
				return false;
			}

			_calibrationSource = source;

			const int maxCaptureAttempts = 4;
			for (int attempt = 0; attempt < maxCaptureAttempts; attempt++)
			{
				if (!ScreenCapture.TryCaptureClientArea(window, out Bitmap? frame))
				{
					_lastError = "Could not capture the game window.";
					return false;
				}

				using (frame)
				{
					bool lettersVisible = FrameHasVisibleLetters(frame, config);
					bool lastAttempt = attempt == maxCaptureAttempts - 1;
					if (lettersVisible || lastAttempt)
					{
						bool ok = TryReadBoard(frame, config, verbose, out board);
						if (ok || board.PlayableCount > 0 || lastAttempt)
						{
							return ok;
						}
					}

					Thread.Sleep(120);
				}
			}

			_lastError = "Captured the game window, but tile letters are not visible. "
				+ "Keep Bookworm on the battle screen and press R or D again.";
			return false;
		}

		public bool TryReadBoard(Bitmap frame, AutomationConfig config, out GridBoard board) =>
			TryReadBoard(frame, config, verbose: false, out board);

		public bool TryReadBoard(Bitmap frame, AutomationConfig config, bool verbose, out GridBoard board)
		{
			board = new GridBoard();
			List<CellReadDiagnostic> diagnostics = [];
			int tileLikeCells = BoardTileValidator.CountTileLikeCells(frame, config);
			if (tileLikeCells < BoardTileValidator.MinimumTileLikeCells)
			{
				_lastError = $"Calibration misses the letter grid ({tileLikeCells}/16 cells look like tiles). "
					+ "Press C on the battle screen and click the outer corners of the tan 4x4 grid. "
					+ "Check calibration-cells\\cell-00.png — it should show one centered letter, not scenery.";
				if (verbose)
				{
					TrySaveDebugCellCrops(frame, config);
				}

				return false;
			}

			int playableCount = 0;

			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				Rectangle cellRect = config.GetCellRect(index);
				if (cellRect.Right > frame.Width || cellRect.Bottom > frame.Height)
				{
					_lastError = "Calibration extends outside the captured frame. Press C to calibrate again.";
					_lastDiagnostics = diagnostics;
					return false;
				}

				using Bitmap cellCrop = frame.Clone(cellRect, frame.PixelFormat);
				GemType gem = GemColorMatcher.Match(cellCrop);
				GridCell cell = ClassifyCell(cellCrop, gem, out string method, out string detail);
				board.SetCell(index, cell);
				diagnostics.Add(new CellReadDiagnostic(index, cell.Letter, cell.Gem, method, cell.IsPlayable, detail));

				if (cell.IsPlayable)
				{
					playableCount++;
				}
			}

			_lastDiagnostics = diagnostics;

			if (verbose)
			{
				PrintDiagnostics(diagnostics);
				TrySaveDebugCellCrops(frame, config);
			}

			if (playableCount == 0)
			{
				_lastError = "Grid calibration looks correct, but OCR read no letters. "
					+ "Bookworm hides letters when unfocused — press R or D from this console to retry. "
					+ "Press D to see ocr='...' for each tile.";

				return false;
			}

			if (playableCount < MinimumPlayableCells)
			{
				_lastError = $"Partial read: {playableCount}/16 letters (target {MinimumPlayableCells}). "
					+ "Board updated with what was read — press D for per-tile details.";
			}

			return true;
		}

		public void PrintDiagnostics(IReadOnlyList<CellReadDiagnostic>? diagnostics = null)
		{
			diagnostics ??= _lastDiagnostics;
			if (diagnostics is null)
			{
				return;
			}

			Console.WriteLine("Tile read diagnostics:");
			foreach (CellReadDiagnostic cell in diagnostics)
			{
				int row = GridBoard.GetRow(cell.Index);
				int column = GridBoard.GetColumn(cell.Index);
				string letter = cell.Letter?.ToString() ?? "..";
				Console.WriteLine($"  [{row},{column}] {letter,-3} gem={cell.Gem,-9} via {cell.Method,-14} {cell.Detail}");
			}
		}

		private GridCell ClassifyCell(Bitmap cellCrop, GemType gem, out string method, out string detail)
		{
			method = "empty";
			detail = string.Empty;
			if (_letterClassifier.TryClassify(cellCrop, out char letter, out method, out detail))
			{
				return new GridCell(letter, gem);
			}

			if (LooksEmpty(cellCrop))
			{
				method = "empty";
				return GridCell.Empty;
			}

			method = "unreadable";
			return GridCell.Empty;
		}

		private static bool LooksEmpty(Bitmap cellCrop)
		{
			if (!GemColorMatcher.TryGetAverageColor(cellCrop, out byte r, out byte g, out byte b))
			{
				return true;
			}

			int spread = Math.Max(Math.Abs(r - g), Math.Max(Math.Abs(g - b), Math.Abs(r - b)));
			return spread < 8;
		}

		private static bool FrameHasVisibleLetters(Bitmap frame, AutomationConfig config)
		{
			int cellsWithLetters = 0;
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				Rectangle cellRect = config.GetCellRect(index);
				if (cellRect.Right > frame.Width || cellRect.Bottom > frame.Height)
				{
					continue;
				}

				using Bitmap cellCrop = frame.Clone(cellRect, frame.PixelFormat);
				bool[,] mask = TileLetterExtractor.ExtractLetterMask(cellCrop);
				if (CountMaskPixels(mask) >= 8)
				{
					cellsWithLetters++;
				}
			}

			return cellsWithLetters >= 4;
		}

		private static int CountMaskPixels(bool[,] mask)
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

		private static void TrySaveDebugCellCrops(Bitmap frame, AutomationConfig config)
		{
			string folder = Path.Combine(ConfigStore.SettingsDirectory, "calibration-cells");
			Directory.CreateDirectory(folder);
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				Rectangle rect = config.GetCellRect(index);
				if (rect.Right > frame.Width || rect.Bottom > frame.Height)
				{
					continue;
				}

				using Bitmap cell = frame.Clone(rect, frame.PixelFormat);
				cell.Save(Path.Combine(folder, $"cell-{index:D2}.png"), ImageFormat.Png);
			}

			Console.WriteLine($"Saved raw tile crops to: {folder}");
		}
	}
}
