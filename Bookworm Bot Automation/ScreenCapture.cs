using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

using Bookworm_Bot_Automation.Native;

namespace Bookworm_Bot_Automation
{
	public static class ScreenCapture
	{
		public static bool TryCaptureClientArea(GameWindowInfo window, out Bitmap bitmap)
		{
			bitmap = null!;
			if (!TryGetClientScreenBounds(window.Handle, out Win32.Point origin, out int width, out int height))
			{
				return false;
			}

			IntPtr previousForeground = Win32.GetForegroundWindow();
			for (int attempt = 0; attempt < 3; attempt++)
			{
				if (GameWindowFocus.TryActivate(window.Handle))
				{
					Thread.Sleep(GameWindowFocus.FocusSettleMs);
					break;
				}

				Thread.Sleep(75);
			}

			try
			{
				return TryCaptureClientAreaCore(window.Handle, origin, width, height, out bitmap);
			}
			finally
			{
				if (previousForeground != IntPtr.Zero
					&& previousForeground != window.Handle
					&& GameWindowFocus.IsForeground(window.Handle))
				{
					GameWindowFocus.TryActivate(previousForeground);
				}
			}
		}

		private static bool TryCaptureClientAreaCore(
			IntPtr windowHandle,
			Win32.Point origin,
			int width,
			int height,
			out Bitmap bitmap)
		{
			bitmap = null!;
			if (TryCaptureFromScreen(origin.X, origin.Y, width, height, out Bitmap? screenCapture)
				&& screenCapture is not null
				&& !IsMostlyBlack(screenCapture))
			{
				bitmap = screenCapture;
				return true;
			}

			screenCapture?.Dispose();

			if (TryCaptureWithPrintWindow(windowHandle, width, height, out Bitmap? printCapture)
				&& printCapture is not null
				&& !IsMostlyBlack(printCapture))
			{
				bitmap = printCapture;
				return true;
			}

			printCapture?.Dispose();

			// Return the screen capture even if dark so the user can see something failed.
			if (TryCaptureFromScreen(origin.X, origin.Y, width, height, out screenCapture) && screenCapture is not null)
			{
				bitmap = screenCapture;
				return true;
			}

			return false;
		}

		public static bool TryCaptureClientArea(IntPtr windowHandle, out Bitmap bitmap)
		{
			Win32.Rect clientRect = default;
			if (!Win32.GetClientRect(windowHandle, out clientRect))
			{
				bitmap = null!;
				return false;
			}

			return TryCaptureClientArea(
				new GameWindowInfo(windowHandle, string.Empty, clientRect.Width, clientRect.Height),
				out bitmap);
		}

		public static bool IsMostlyBlack(Bitmap bitmap, int sampleStride = 16, byte brightnessThreshold = 12)
		{
			long totalBrightness = 0;
			long samples = 0;

			for (int y = 0; y < bitmap.Height; y += sampleStride)
			{
				for (int x = 0; x < bitmap.Width; x += sampleStride)
				{
					Color pixel = bitmap.GetPixel(x, y);
					totalBrightness += (pixel.R + pixel.G + pixel.B) / 3;
					samples++;
				}
			}

			if (samples == 0)
			{
				return true;
			}

			return (totalBrightness / samples) < brightnessThreshold;
		}

		private static bool TryGetClientScreenBounds(IntPtr windowHandle, out Win32.Point origin, out int width, out int height)
		{
			origin = default;
			width = 0;
			height = 0;

			Win32.Rect clientRect = default;
			if (!Win32.GetClientRect(windowHandle, out clientRect))
			{
				return false;
			}

			width = clientRect.Width;
			height = clientRect.Height;
			if (width <= 0 || height <= 0)
			{
				return false;
			}

			Win32.Point topLeft = default;
			topLeft.X = 0;
			topLeft.Y = 0;
			if (!Win32.ClientToScreen(windowHandle, ref topLeft))
			{
				return false;
			}

			origin = topLeft;
			return true;
		}

		private static bool TryCaptureFromScreen(int screenX, int screenY, int width, int height, out Bitmap? bitmap)
		{
			bitmap = null;
			if (width <= 0 || height <= 0)
			{
				return false;
			}

			Bitmap capture = new(width, height, PixelFormat.Format32bppArgb);
			using Graphics graphics = Graphics.FromImage(capture);
			IntPtr destHdc = graphics.GetHdc();
			IntPtr screenDc = Win32.GetDC(IntPtr.Zero);
			try
			{
				if (!Win32.BitBlt(destHdc, 0, 0, width, height, screenDc, screenX, screenY, Win32.Srccopy))
				{
					capture.Dispose();
					return false;
				}
			}
			finally
			{
				Win32.ReleaseDC(IntPtr.Zero, screenDc);
				graphics.ReleaseHdc(destHdc);
			}

			bitmap = capture;
			return true;
		}

		private static bool TryCaptureWithPrintWindow(IntPtr windowHandle, int width, int height, out Bitmap? bitmap)
		{
			bitmap = null;
			if (width <= 0 || height <= 0)
			{
				return false;
			}

			Bitmap capture = new(width, height, PixelFormat.Format32bppArgb);
			using Graphics graphics = Graphics.FromImage(capture);
			IntPtr destHdc = graphics.GetHdc();
			try
			{
				if (!Win32.PrintWindow(windowHandle, destHdc, Win32.PwRenderFullContent)
					&& !Win32.PrintWindow(windowHandle, destHdc, Win32.PwClientOnly))
				{
					capture.Dispose();
					return false;
				}
			}
			finally
			{
				graphics.ReleaseHdc(destHdc);
			}

			bitmap = capture;
			return true;
		}
	}
}
