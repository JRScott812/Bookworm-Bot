using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Bookworm_Bot_Automation
{
	public static class ConfigStore
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true
		};

		public static string SettingsDirectory =>
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Bookworm Bot");

		public static string SettingsPath => Path.Combine(SettingsDirectory, "automation.json");

		public static bool TryLoad(int clientWidth, int clientHeight, out AutomationConfig config, out string source)
		{
			config = null!;
			source = string.Empty;
			string key = FormatKey(clientWidth, clientHeight);

			if (TryLoadAll(out Dictionary<string, AutomationConfig>? all)
				&& all is not null
				&& all.TryGetValue(key, out AutomationConfig? saved)
				&& saved is not null
				&& Normalize(saved, clientWidth, clientHeight, out config)
				&& config.FitsWithinClient())
			{
				source = $"saved calibration ({key})";
				return true;
			}

			if (TryLoadPreset(clientWidth, clientHeight, out AutomationConfig? preset)
				&& preset is not null
				&& Normalize(preset, clientWidth, clientHeight, out config))
			{
				source = $"built-in preset ({key})";
				return true;
			}

			if (all is not null && all.Count > 0)
			{
				AutomationConfig? best = all.Values
					.Where(candidate => candidate.FitsWithinClient())
					.OrderByDescending(candidate => ResolutionSimilarity(candidate, clientWidth, clientHeight))
					.FirstOrDefault();

				if (best is not null)
				{
					double similarity = ResolutionSimilarity(best, clientWidth, clientHeight);
					if (similarity >= 0.75
						&& ScaleConfig(best, clientWidth, clientHeight, out AutomationConfig scaled)
						&& scaled.IsValid)
					{
						config = scaled;
						source = $"scaled from {best.ResolutionKey} -> {key}";
						return true;
					}
				}
			}

			if (TryLoadAnyPreset(out AutomationConfig? anyPreset)
				&& anyPreset is not null
				&& ScaleConfig(anyPreset, clientWidth, clientHeight, out AutomationConfig scaledPreset)
				&& scaledPreset.IsValid)
			{
				config = scaledPreset;
				source = $"scaled preset ({anyPreset.ResolutionKey} -> {key})";
				return true;
			}

			return false;
		}

		public static bool TryLoad(int clientWidth, int clientHeight, out AutomationConfig config)
		{
			return TryLoad(clientWidth, clientHeight, out config, out _);
		}

		public static IReadOnlyList<string> ListSavedResolutionKeys()
		{
			if (!TryLoadAll(out Dictionary<string, AutomationConfig>? all) || all is null)
			{
				return [];
			}

			return all.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public static void Save(AutomationConfig config)
		{
			Dictionary<string, AutomationConfig> all = TryLoadAll(out Dictionary<string, AutomationConfig>? existing)
				? existing ?? new Dictionary<string, AutomationConfig>(StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, AutomationConfig>(StringComparer.OrdinalIgnoreCase);

			all[config.ResolutionKey] = config;
			Directory.CreateDirectory(SettingsDirectory);
			File.WriteAllText(SettingsPath, JsonSerializer.Serialize(all, JsonOptions));
		}

		private static bool TryLoadAll(out Dictionary<string, AutomationConfig>? configs)
		{
			configs = null;
			try
			{
				if (!File.Exists(SettingsPath))
				{
					return false;
				}

				configs = JsonSerializer.Deserialize<Dictionary<string, AutomationConfig>>(File.ReadAllText(SettingsPath), JsonOptions);
				return configs is not null;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryLoadPreset(int clientWidth, int clientHeight, out AutomationConfig? config)
		{
			config = null;
			string presetPath = Path.Combine(AppContext.BaseDirectory, "automation-presets", $"{clientWidth}x{clientHeight}.json");
			if (!File.Exists(presetPath))
			{
				return false;
			}

			try
			{
				config = JsonSerializer.Deserialize<AutomationConfig>(File.ReadAllText(presetPath), JsonOptions);
				return config is not null;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryLoadAnyPreset(out AutomationConfig? config)
		{
			config = null;
			string presetDirectory = Path.Combine(AppContext.BaseDirectory, "automation-presets");
			if (!Directory.Exists(presetDirectory))
			{
				return false;
			}

			string? firstPreset = Directory.EnumerateFiles(presetDirectory, "*.json").FirstOrDefault();
			if (firstPreset is null)
			{
				return false;
			}

			try
			{
				config = JsonSerializer.Deserialize<AutomationConfig>(File.ReadAllText(firstPreset), JsonOptions);
				return config is not null;
			}
			catch
			{
				return false;
			}
		}

		private static bool Normalize(AutomationConfig source, int clientWidth, int clientHeight, out AutomationConfig config)
		{
			if (source.ClientWidth == clientWidth && source.ClientHeight == clientHeight)
			{
				config = source;
				return source.IsValid;
			}

			return ScaleConfig(source, clientWidth, clientHeight, out config) && config.IsValid;
		}

		private static bool ScaleConfig(AutomationConfig source, int clientWidth, int clientHeight, out AutomationConfig config)
		{
			config = null!;
			if (source.ClientWidth <= 0 || source.ClientHeight <= 0)
			{
				return false;
			}

			double scaleX = clientWidth / (double)source.ClientWidth;
			double scaleY = clientHeight / (double)source.ClientHeight;
			config = new AutomationConfig
			{
				ClientWidth = clientWidth,
				ClientHeight = clientHeight,
				BoardLeft = Scale(source.BoardLeft, scaleX),
				BoardTop = Scale(source.BoardTop, scaleY),
				BoardRight = Scale(source.BoardRight, scaleX),
				BoardBottom = Scale(source.BoardBottom, scaleY),
				CellInset = source.CellInset > 0 ? source.CellInset : 4
			};
			return config.IsValid;
		}

		private static int Scale(int value, double scale) =>
			(int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

		private static double ResolutionSimilarity(AutomationConfig config, int clientWidth, int clientHeight)
		{
			if (config.ClientWidth <= 0 || config.ClientHeight <= 0)
			{
				return 0;
			}

			double widthRatio = Math.Min(clientWidth, config.ClientWidth) / (double)Math.Max(clientWidth, config.ClientWidth);
			double heightRatio = Math.Min(clientHeight, config.ClientHeight) / (double)Math.Max(clientHeight, config.ClientHeight);
			return widthRatio * heightRatio;
		}

		private static string FormatKey(int clientWidth, int clientHeight) => $"{clientWidth}x{clientHeight}";
	}
}
