using System;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Windows.UI.ViewManagement;

namespace Bookworm_Bot_GUI
{
	internal static class ThemeHelper
	{
		private static AppThemeMode _currentMode = AppThemeMode.Auto;
		private static FrameworkElement? _root;
		private static UISettings? _uiSettings;

		public static event EventHandler? ThemeChanged;

		public static void Apply(AppThemeMode mode, FrameworkElement? root = null)
		{
			_currentMode = mode;
			if (root is not null)
			{
				_root = root;
			}

			if (_root is null)
			{
				return;
			}

			_root.RequestedTheme = mode switch
			{
				AppThemeMode.Light => ElementTheme.Light,
				AppThemeMode.Dark => ElementTheme.Dark,
				_ => ElementTheme.Default
			};

			SubscribeSystemThemeChanges(mode == AppThemeMode.Auto);
			ThemeChanged?.Invoke(null, EventArgs.Empty);
		}

		private static void SubscribeSystemThemeChanges(bool enabled)
		{
			_uiSettings ??= new UISettings();
			_uiSettings.ColorValuesChanged -= OnSystemThemeChanged;

			if (enabled)
			{
				_uiSettings.ColorValuesChanged += OnSystemThemeChanged;
			}
		}

		private static void OnSystemThemeChanged(UISettings sender, object args)
		{
			if (_currentMode != AppThemeMode.Auto || _root is null)
			{
				return;
			}

			DispatcherQueue queue = _root.DispatcherQueue;
			queue.TryEnqueue(ApplyAutoThemeOnUiThread);
		}

		private static void ApplyAutoThemeOnUiThread()
		{
			if (_currentMode != AppThemeMode.Auto || _root is null)
			{
				return;
			}

			_root.RequestedTheme = ElementTheme.Default;
			ThemeChanged?.Invoke(null, EventArgs.Empty);
		}
	}
}
