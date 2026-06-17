using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Bookworm_Bot_Automation
{
	public static class CalibrationWizard
	{
		public static bool TryRunInteractive(GameWindowInfo window, out AutomationConfig config)
		{
			config = null!;
			Console.WriteLine();
			Console.WriteLine("Calibration uses F8 while hovering the game — no Enter key, no Alt+Tab needed.");
			Console.WriteLine("Click the OUTER corners of the whole 4x4 grid (not individual tile centers).");

			if (!ScreenCapture.TryCaptureClientArea(window, out Bitmap? frame))
			{
				Console.WriteLine("Could not capture the game window.");
				return false;
			}

			using (frame)
			{
				string previewPath = Path.Combine(ConfigStore.SettingsDirectory, "calibration-preview.png");
				Directory.CreateDirectory(ConfigStore.SettingsDirectory);
				frame.Save(previewPath, ImageFormat.Png);
				Console.WriteLine($"Saved preview to: {previewPath}");

				if (ScreenCapture.IsMostlyBlack(frame))
				{
					Console.WriteLine();
					Console.WriteLine("Warning: preview looks black.");
					Console.WriteLine("  - Bring Bookworm Adventures to the front and make sure it is not minimized.");
					Console.WriteLine("  - Do not cover the game with other windows when capturing.");
					Console.WriteLine("  - If using fullscreen, switch to windowed mode and try again.");
					Console.WriteLine();
				}
			}

			if (!CursorCapture.TryWaitForHotkeyCapture(window, "Step 1/2: TOP-LEFT corner of the 4x4 board", out CursorCapture.Point topLeft)
				|| !CursorCapture.TryWaitForHotkeyCapture(window, "Step 2/2: BOTTOM-RIGHT corner of the 4x4 board", out CursorCapture.Point bottomRight))
			{
				return false;
			}

			config = AutomationConfig.FromBoardBounds(
				window.ClientWidth,
				window.ClientHeight,
				topLeft.X,
				topLeft.Y,
				bottomRight.X,
				bottomRight.Y);

			if (!config.IsValid)
			{
				Console.WriteLine();
				Console.WriteLine("Calibration rejected:");
				Console.WriteLine($"  {config.DescribeValidationIssue()}");
				return false;
			}

			if (!ScreenCapture.TryCaptureClientArea(window, out Bitmap? verifyFrame))
			{
				Console.WriteLine("Could not capture the game window to verify calibration.");
				return false;
			}

			using (verifyFrame)
			{
				if (!BoardTileValidator.ValidateBoardRegion(verifyFrame, config, out int tileLikeCells))
				{
					Console.WriteLine();
					Console.WriteLine($"Calibration rejected: only {tileLikeCells}/16 crops look like letter tiles.");
					Console.WriteLine("  Click the OUTER corners of the tan 4x4 grid in the battle screen.");
					Console.WriteLine("  Do not calibrate on menus, treasure screens, or map screens.");
					TrySaveDebugCellCrops(verifyFrame, config);
					return false;
				}
			}

			ConfigStore.Save(config);
			Console.WriteLine();
			Console.WriteLine($"Saved calibration for {config.ResolutionKey} to:");
			Console.WriteLine($"  {ConfigStore.SettingsPath}");
			SaveOverlayPreview(window, config);
			TrySaveDebugCellCrops(window, config);
			return true;
		}

		private static void SaveOverlayPreview(GameWindowInfo window, AutomationConfig config)
		{
			if (!ScreenCapture.TryCaptureClientArea(window, out Bitmap? frame))
			{
				return;
			}

			using (frame)
			using (Graphics graphics = Graphics.FromImage(frame))
			using (Pen pen = new(Color.Lime, 2))
			{
				graphics.DrawRectangle(
					pen,
					config.BoardLeft,
					config.BoardTop,
					config.BoardRight - config.BoardLeft,
					config.BoardBottom - config.BoardTop);

				foreach (Rectangle cell in config.GetAllCellRects())
				{
					graphics.DrawRectangle(pen, cell);
				}

				string overlayPath = Path.Combine(ConfigStore.SettingsDirectory, "calibration-overlay.png");
				frame.Save(overlayPath, ImageFormat.Png);
				Console.WriteLine($"Saved overlay to: {overlayPath}");
				Console.WriteLine("Open this file — green boxes must sit on each tile.");
			}
		}

		private static void TrySaveDebugCellCrops(Bitmap frame, AutomationConfig config)
		{
			string folder = Path.Combine(ConfigStore.SettingsDirectory, "calibration-cells");
			Directory.CreateDirectory(folder);
			for (int index = 0; index < AutomationConfig.GridBoardSize * AutomationConfig.GridBoardSize; index++)
			{
				Rectangle rect = config.GetCellRect(index);
				if (rect.Right > frame.Width || rect.Bottom > frame.Height)
				{
					continue;
				}

				using Bitmap cell = frame.Clone(rect, frame.PixelFormat);
				cell.Save(Path.Combine(folder, $"cell-{index:D2}.png"), ImageFormat.Png);
			}

			Console.WriteLine($"Saved tile crops to: {folder}");
		}

		private static void TrySaveDebugCellCrops(GameWindowInfo window, AutomationConfig config)
		{
			if (!ScreenCapture.TryCaptureClientArea(window, out Bitmap? frame))
			{
				return;
			}

			using (frame)
			{
				TrySaveDebugCellCrops(frame, config);
			}
		}
	}
}
