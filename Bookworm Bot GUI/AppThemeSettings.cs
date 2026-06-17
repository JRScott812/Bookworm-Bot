using System;
using System.IO;
using System.Text.Json;

namespace Bookworm_Bot_GUI
{
	internal static class AppThemeSettings
	{
		private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

		private static string SettingsPath =>
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Bookworm Bot",
				"app-settings.json");

		public static AppThemeMode Load()
		{
			try
			{
				if (!File.Exists(SettingsPath))
				{
					return AppThemeMode.Auto;
				}

				AppSettingsData? data = JsonSerializer.Deserialize<AppSettingsData>(File.ReadAllText(SettingsPath));
				return data?.Theme is string theme && Enum.TryParse(theme, out AppThemeMode mode)
					? mode
					: AppThemeMode.Auto;
			}
			catch
			{
				return AppThemeMode.Auto;
			}
		}

		public static void Save(AppThemeMode mode)
		{
			try
			{
				string directory = Path.GetDirectoryName(SettingsPath)!;
				Directory.CreateDirectory(directory);
				File.WriteAllText(
					SettingsPath,
					JsonSerializer.Serialize(new AppSettingsData { Theme = mode.ToString() }, JsonOptions));
			}
			catch
			{
				// Persistence is best-effort for an unpackaged desktop app.
			}
		}

		private sealed class AppSettingsData
		{
			public string Theme { get; set; } = AppThemeMode.Auto.ToString();
		}
	}
}
