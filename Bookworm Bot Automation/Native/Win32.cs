using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Bookworm_Bot_Automation.Native
{
	internal static class Win32
	{
		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		public static extern bool GetCursorPos(out Point lpPoint);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(
			IntPtr hdcDest,
			int nXDest,
			int nYDest,
			int nWidth,
			int nHeight,
			IntPtr hdcSrc,
			int nXSrc,
			int nYSrc,
			int dwRop);

		[DllImport("user32.dll")]
		public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

		[DllImport("user32.dll")]
		public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int virtualKey);

		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(Point point);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool BringWindowToTop(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

		public const int SwRestore = 9;

		public const int VkF8 = 0x77;
		public const int VkEscape = 0x1B;

		public static readonly IntPtr PerMonitorAwareV2 = new(-4);

		public const int Srccopy = 0x00CC0020;
		public const uint PwClientOnly = 0x00000001;
		public const uint PwRenderFullContent = 0x00000002;

		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public int Width => Right - Left;
			public int Height => Bottom - Top;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Point
		{
			public int X;
			public int Y;
		}
	}
}
