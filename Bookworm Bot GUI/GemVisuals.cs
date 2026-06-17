using Bookworm_Bot_Class;

using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace Bookworm_Bot_GUI
{
	internal static class GemVisuals
	{
		private static readonly SolidColorBrush AmethystBrush = Create(GemColors.GetRgb(GemType.Amethyst));
		private static readonly SolidColorBrush EmeraldBrush = Create(GemColors.GetRgb(GemType.Emerald));
		private static readonly SolidColorBrush SapphireBrush = Create(GemColors.GetRgb(GemType.Sapphire));
		private static readonly SolidColorBrush GarnetBrush = Create(GemColors.GetRgb(GemType.Garnet));
		private static readonly SolidColorBrush RubyBrush = Create(GemColors.GetRgb(GemType.Ruby));
		private static readonly SolidColorBrush CrystalBrush = Create(GemColors.GetRgb(GemType.Crystal));
		private static readonly SolidColorBrush DiamondBrush = Create(GemColors.GetRgb(GemType.Diamond));
		private static readonly SolidColorBrush DarkTextBrush = Create(15, 23, 42);
		private static readonly SolidColorBrush LightTextBrush = Create(255, 255, 255);

		public static string Label(GemType gem) => gem switch
		{
			GemType.Amethyst => "Amethyst",
			GemType.Emerald => "Emerald",
			GemType.Sapphire => "Sapphire",
			GemType.Garnet => "Garnet",
			GemType.Ruby => "Ruby",
			GemType.Crystal => "Crystal",
			GemType.Diamond => "Diamond",
			_ => "None"
		};

		public static SolidColorBrush GetBackgroundBrush(GemType gem) => gem switch
		{
			GemType.Amethyst => AmethystBrush,
			GemType.Emerald => EmeraldBrush,
			GemType.Sapphire => SapphireBrush,
			GemType.Garnet => GarnetBrush,
			GemType.Ruby => RubyBrush,
			GemType.Crystal => CrystalBrush,
			GemType.Diamond => DiamondBrush,
			_ => AmethystBrush
		};

		public static SolidColorBrush GetForegroundBrush(GemType gem)
		{
			Color color = GetBackgroundBrush(gem).Color;
			double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
			return luminance > 0.62 ? DarkTextBrush : LightTextBrush;
		}

		private static SolidColorBrush Create(RgbColor color) => Create(color.R, color.G, color.B);

		private static SolidColorBrush Create(byte r, byte g, byte b) =>
			new(Color.FromArgb(255, r, g, b));
	}
}
