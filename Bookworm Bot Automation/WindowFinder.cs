using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;

using Bookworm_Bot_Automation.Native;

namespace Bookworm_Bot_Automation
{
	public sealed record GameWindowInfo(IntPtr Handle, string Title, int ClientWidth, int ClientHeight);

	public static class WindowFinder
	{
		private static readonly string[] TitleHints =
		[
			"Bookworm Adventures",
			"Bookworm"
		];

		public static bool TryFind(out GameWindowInfo window)
		{
			List<GameWindowInfo> matches = [];

			Win32.EnumWindows((hWnd, _) =>
			{
				if (!Win32.IsWindowVisible(hWnd))
				{
					return true;
				}

				StringBuilder title = new(256);
				Win32.GetWindowText(hWnd, title, title.Capacity);
				string text = title.ToString();
				if (string.IsNullOrWhiteSpace(text) || !MatchesTitle(text))
				{
					return true;
				}

				Win32.Rect rect = default;
				if (!Win32.GetClientRect(hWnd, out rect) || rect.Width <= 0 || rect.Height <= 0)
				{
					return true;
				}

				matches.Add(new GameWindowInfo(hWnd, text, rect.Width, rect.Height));
				return true;
			}, IntPtr.Zero);

			if (matches.Count == 0)
			{
				window = default!;
				return false;
			}

			window = matches
				.OrderByDescending(static candidate => TitlePriority(candidate.Title))
				.ThenByDescending(static candidate => candidate.ClientWidth * candidate.ClientHeight)
				.First();
			return true;
		}

		private static int TitlePriority(string title)
		{
			if (title.Contains("Bookworm Adventures", StringComparison.OrdinalIgnoreCase))
			{
				return 2;
			}

			if (title.Contains("Bookworm", StringComparison.OrdinalIgnoreCase))
			{
				return 1;
			}

			return 0;
		}

		private static bool MatchesTitle(string title)
		{
			foreach (string hint in TitleHints)
			{
				if (title.Contains(hint, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
