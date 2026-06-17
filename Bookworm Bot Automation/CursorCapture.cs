using System;
using System.Threading;

using Bookworm_Bot_Automation.Native;

namespace Bookworm_Bot_Automation
{
	internal static class CursorCapture
	{
		public static bool TryWaitForHotkeyCapture(
			GameWindowInfo window,
			string prompt,
			out Point clientPoint)
		{
			clientPoint = default;
			Console.WriteLine();
			Console.WriteLine(prompt);
			Console.WriteLine("  Hover the mouse over the spot in the GAME window, then press F8.");
			Console.WriteLine("  Press Esc to cancel.");
			Console.WriteLine("  You do not need to click — and you do not need to press Enter in this console.");

			while (true)
			{
				if (IsKeyDown(Win32.VkEscape))
				{
					Console.WriteLine("Cancelled.");
					return false;
				}

				if (IsKeyDown(Win32.VkF8))
				{
					WaitForKeyRelease(Win32.VkF8);
					Thread.Sleep(100);

					if (!TryReadCursorInClient(window, out clientPoint, out string? error))
					{
						Console.WriteLine(error);
						Console.WriteLine("Try again — keep the mouse over the game window when you press F8.");
						continue;
					}

					Console.WriteLine($"Captured ({clientPoint.X}, {clientPoint.Y}).");
					return true;
				}

				Thread.Sleep(20);
			}
		}

		private static bool TryReadCursorInClient(GameWindowInfo window, out Point clientPoint, out string? error)
		{
			clientPoint = default;
			error = null;

			if (!Win32.GetCursorPos(out Win32.Point screenPoint))
			{
				error = "Could not read cursor position.";
				return false;
			}

			IntPtr windowAtCursor = Win32.WindowFromPoint(screenPoint);
			if (windowAtCursor != window.Handle && !IsSameWindowOrChild(window.Handle, windowAtCursor))
			{
				error = "Cursor was not over the Bookworm Adventures window. Hover the game, then press F8.";
				return false;
			}

			Win32.Point point = screenPoint;
			if (!Win32.ScreenToClient(window.Handle, ref point))
			{
				error = "Could not convert cursor position to game coordinates.";
				return false;
			}

			if (point.X < 0 || point.Y < 0 || point.X >= window.ClientWidth || point.Y >= window.ClientHeight)
			{
				error = $"Cursor mapped to ({point.X}, {point.Y}), which is outside the {window.ClientWidth}x{window.ClientHeight} game area.";
				return false;
			}

			clientPoint = new Point(point.X, point.Y);
			return true;
		}

		private static bool IsSameWindowOrChild(IntPtr parent, IntPtr candidate)
		{
			// WindowFromPoint may return a child/control handle; walk up is not implemented here.
			// For PopCap games the main HWND is usually hit directly.
			return candidate == parent;
		}

		private static bool IsKeyDown(int virtualKey) => (Win32.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

		private static void WaitForKeyRelease(int virtualKey)
		{
			while (IsKeyDown(virtualKey))
			{
				Thread.Sleep(10);
			}
		}

		internal readonly record struct Point(int X, int Y);
	}
}
