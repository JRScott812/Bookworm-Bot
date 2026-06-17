using System;
using System.Threading;

using Bookworm_Bot_Automation.Native;

namespace Bookworm_Bot_Automation
{
	internal static class GameWindowFocus
	{
		public const int FocusSettleMs = 450;

		public static bool TryActivate(IntPtr hWnd)
		{
			if (hWnd == IntPtr.Zero)
			{
				return false;
			}

			if (IsForeground(hWnd))
			{
				return true;
			}

			if (Win32.IsIconic(hWnd))
			{
				Win32.ShowWindow(hWnd, Win32.SwRestore);
			}

			IntPtr foreground = Win32.GetForegroundWindow();
			uint foregroundThread = Win32.GetWindowThreadProcessId(foreground, out _);
			uint targetThread = Win32.GetWindowThreadProcessId(hWnd, out _);
			uint currentThread = Win32.GetCurrentThreadId();

			bool attachedForeground = foregroundThread != 0
				&& foregroundThread != currentThread
				&& Win32.AttachThreadInput(currentThread, foregroundThread, attach: true);
			bool attachedTarget = targetThread != 0
				&& targetThread != currentThread
				&& Win32.AttachThreadInput(currentThread, targetThread, attach: true);

			try
			{
				Win32.SetForegroundWindow(hWnd);
				Win32.BringWindowToTop(hWnd);
			}
			finally
			{
				if (attachedTarget)
				{
					Win32.AttachThreadInput(currentThread, targetThread, attach: false);
				}

				if (attachedForeground)
				{
					Win32.AttachThreadInput(currentThread, foregroundThread, attach: false);
				}
			}

			return IsForeground(hWnd);
		}

		public static bool IsForeground(IntPtr hWnd) =>
			hWnd != IntPtr.Zero && Win32.GetForegroundWindow() == hWnd;
	}
}
