using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

using Bookworm_Bot_Automation.Native;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_Automation
{
	internal static class Program
	{
		private const int TopSuggestionCount = 15;

		private static void Main(string[] args)
		{
			Win32.SetProcessDpiAwarenessContext(Win32.PerMonitorAwareV2);

			bool saveFrame = args.Contains("--save-frame", StringComparer.OrdinalIgnoreCase);
			bool dumpBoard = args.Contains("--dump-board", StringComparer.OrdinalIgnoreCase);
			bool calibrateFirst = args.Contains("--calibrate", StringComparer.OrdinalIgnoreCase)
				|| args.Contains("calibrate", StringComparer.OrdinalIgnoreCase);

			Console.WriteLine("Bookworm Bot Automation");
			Console.WriteLine();

			if (args.Contains("--reset-calibration", StringComparer.OrdinalIgnoreCase))
			{
				ResetCalibration();
				return;
			}

			WordDictionary dictionary = WordDictionary.Load(WordBankPaths.GetWordBanksDirectory());
			AbilityProfile profile = LoadoutSettingsStore.BuildProfile();
			GameSession session = new(dictionary, profile);
			BwaGameBoardReader reader = new();
			Bitmap? lastFrame = null;

			if (calibrateFirst)
			{
				RunCalibration(reader);
			}

			Console.WriteLine("Commands: [R] Refresh   [C] Calibrate   [D] Diagnose   [Q] Quit");
			Console.WriteLine("The game hides tile letters when unfocused; reads briefly focus Bookworm, then return here.");
			Console.WriteLine("Configure loadout in the GUI app; settings are shared via loadout.json.");
			Console.WriteLine();

			bool running = true;
			while (running)
			{
				if (!TryRefresh(session, reader, saveFrame, dumpBoard, ref lastFrame))
				{
					PrintHelpfulFailure(reader);
				}

				Console.WriteLine();
				Console.Write("> ");
				string? input = Console.ReadLine()?.Trim();
				if (string.IsNullOrEmpty(input))
				{
					continue;
				}

				switch (input.ToUpperInvariant())
				{
					case "R":
					case "REFRESH":
						continue;
					case "C":
					case "CALIBRATE":
						RunCalibration(reader);
						break;
					case "D":
					case "DIAGNOSE":
					case "DUMP":
						RunDiagnosticRead(reader);
						break;
					case "Q":
					case "QUIT":
					case "EXIT":
						running = false;
						break;
					default:
						Console.WriteLine("Unknown command. Use R, C, D, or Q.");
						break;
				}
			}

			lastFrame?.Dispose();
		}

		private static bool TryRefresh(
			GameSession session,
			BwaGameBoardReader reader,
			bool saveFrame,
			bool dumpBoard,
			ref Bitmap? lastFrame)
		{
			if (!reader.TryReadBoard(out GridBoard board))
			{
				PrintHelpfulFailure(reader);
				Console.WriteLine();
				Console.WriteLine("Partial board (best effort):");
				PrintBoard(board, dumpBoard: true);
				if (reader.LastDiagnostics is not null)
				{
					reader.PrintDiagnostics();
				}

				return false;
			}

			if (!string.IsNullOrWhiteSpace(reader.LastError))
			{
				Console.WriteLine(reader.LastError);
			}

			session.SetBoard(board);
			IReadOnlyList<WordResult> suggestions = session.GetSuggestions(TopSuggestionCount);

			if (WindowFinder.TryFind(out GameWindowInfo? window))
			{
				Console.WriteLine($"Game window: {window.ClientWidth}x{window.ClientHeight}");
			}

			if (!string.IsNullOrWhiteSpace(reader.CalibrationSource))
			{
				Console.WriteLine($"Using {reader.CalibrationSource}.");
			}

			Console.WriteLine("Board (read):");
			PrintBoard(board, dumpBoard);
			Console.WriteLine();
			PrintSuggestions(suggestions);

			if (saveFrame && WindowFinder.TryFind(out window)
				&& ScreenCapture.TryCaptureClientArea(window, out Bitmap? frame))
			{
				lastFrame?.Dispose();
				lastFrame = frame;
				string path = Path.Combine(ConfigStore.SettingsDirectory, "last-frame.png");
				Directory.CreateDirectory(ConfigStore.SettingsDirectory);
				frame.Save(path, ImageFormat.Png);
				Console.WriteLine();
				Console.WriteLine($"Saved frame to: {path}");
			}

			return true;
		}

		private static void RunCalibration(BwaGameBoardReader reader)
		{
			if (!WindowFinder.TryFind(out GameWindowInfo window))
			{
				Console.WriteLine("Bookworm Adventures window not found.");
				return;
			}

			Console.WriteLine($"Found window: {window.Title} ({window.ClientWidth}x{window.ClientHeight})");
			if (!CalibrationWizard.TryRunInteractive(window, out AutomationConfig config))
			{
				Console.WriteLine("Calibration failed.");
				return;
			}

			Console.WriteLine($"Calibration stored at: {ConfigStore.SettingsPath}");
			Console.WriteLine($"Resolution key: {config.ResolutionKey}");
			Console.WriteLine();
			Console.WriteLine("Testing letter read now (calibration is done — this checks tile recognition)...");
			RunDiagnosticRead(reader);
		}

		private static void RunDiagnosticRead(BwaGameBoardReader reader)
		{
			if (reader.TryReadBoard(verbose: true, out GridBoard board))
			{
				Console.WriteLine("Read succeeded.");
				PrintBoard(board, dumpBoard: true);
				return;
			}

			Console.WriteLine(reader.LastError ?? "Read failed.");
			reader.PrintDiagnostics();
			Console.WriteLine();
			Console.WriteLine($"Board ({board.PlayableCount}/16 tiles read):");
			PrintBoard(board, dumpBoard: true);
		}

		private static void ResetCalibration()
		{
			if (File.Exists(ConfigStore.SettingsPath))
			{
				File.Delete(ConfigStore.SettingsPath);
				Console.WriteLine($"Deleted {ConfigStore.SettingsPath}");
			}
			else
			{
				Console.WriteLine("No saved calibration found.");
			}
		}

		private static void PrintBoard(GridBoard board, bool dumpBoard)
		{
			for (int row = 0; row < GridBoard.Size; row++)
			{
				string[] cells = new string[GridBoard.Size];
				for (int column = 0; column < GridBoard.Size; column++)
				{
					int index = GridBoard.ToIndex(row, column);
					GridCell cell = board.GetCell(index);
					if (dumpBoard)
					{
						cells[column] = cell.IsEmpty
							? ".."
							: $"{cell.Letter}{(cell.Gem == GemType.None ? string.Empty : $"({GemBonuses.ShortName(cell.Gem)[..3]})")}";
					}
					else
					{
						cells[column] = cell.IsEmpty ? ".." : cell.ToTile().Display.PadRight(8);
					}
				}

				Console.WriteLine("  " + string.Join("  ", cells));
			}
		}

		private static void PrintSuggestions(IReadOnlyList<WordResult> suggestions)
		{
			if (suggestions.Count == 0)
			{
				Console.WriteLine("No suggestions (board may be incomplete or empty).");
				return;
			}

			Console.WriteLine("Top suggestions:");
			for (int index = 0; index < suggestions.Count; index++)
			{
				WordResult result = suggestions[index];
				string bonuses = result.Bonuses.Count > 0 ? $" [{string.Join(", ", result.Bonuses)}]" : string.Empty;
				Console.WriteLine(
					$"  {index + 1,2}. {result.Damage,5:0.0}  {result.Word,-16} ({result.TilesUsed} tiles){bonuses}");
			}
		}

		private static void PrintHelpfulFailure(BwaGameBoardReader reader)
		{
			string? error = reader.LastError;
			Console.WriteLine(error ?? "Could not read the board.");

			if (error is not null && error.Contains("No calibration", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Tips:");
				Console.WriteLine("  - Press C to calibrate with the game visible at the size you normally play.");
				Console.WriteLine($"  - Settings file: {ConfigStore.SettingsPath}");
				return;
			}

			if (error is not null && error.Contains("letters are not visible", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Tips:");
				Console.WriteLine("  - Bookworm hides letters when its window is not focused.");
				Console.WriteLine("  - Press R or D from this console — the bot briefly focuses the game, captures, then returns here.");
				Console.WriteLine("  - If focus keeps failing, click the game once manually, then press R immediately.");
				return;
			}

			if (error is not null && error.Contains("letters were read", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Tips:");
				Console.WriteLine("  - Bookworm hides letters when its window is not focused. Press D or R from this console; the bot focuses the game briefly to read.");
				Console.WriteLine("  - Press D to see each tile's OCR result (ocr='...' in diagnostics). Recognition uses raw cell crops only.");
				Console.WriteLine("  - Open calibration-overlay.png — if green boxes line up, do NOT recalibrate again.");
				return;
			}

			if (error is not null && error.Contains("misses the letter grid", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Tips:");
				Console.WriteLine("  - Press C on the BATTLE screen (tan 4x4 tile grid visible).");
				Console.WriteLine("  - F8 on the top-left corner of the whole grid, then F8 on the bottom-right corner.");
				Console.WriteLine("  - Open calibration-cells\\cell-00.png — it must show one letter tile, not background art.");
				Console.WriteLine($"  - Settings file: {ConfigStore.SettingsPath}");
				return;
			}

			if (error is not null && error.Contains("window not found", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine("Tips:");
				Console.WriteLine("  - Start Bookworm Adventures Deluxe before refreshing.");
				return;
			}

			Console.WriteLine("Tips:");
			Console.WriteLine("  - Start Bookworm Adventures Deluxe and open a battle with the tile grid visible.");
			Console.WriteLine("  - Press D for a per-tile diagnostic read.");
		}
	}
}
