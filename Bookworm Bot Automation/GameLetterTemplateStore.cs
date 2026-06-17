using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Bookworm_Bot_Automation
{
	public static class GameLetterTemplateStore
	{
		private static readonly Lazy<IReadOnlyDictionary<char, bool[,]>> Templates = new(LoadTemplates);

		public static bool TryGetMask(char letter, out bool[,]? mask)
		{
			if (Templates.Value.TryGetValue(letter, out bool[,]? loaded))
			{
				mask = loaded;
				return true;
			}

			mask = null;
			return false;
		}

		public static string TemplatesDirectory =>
			Path.Combine(ConfigStore.SettingsDirectory, "letter-templates");

		private static IReadOnlyDictionary<char, bool[,]> LoadTemplates()
		{
			Dictionary<char, bool[,]> templates = new();
			if (!Directory.Exists(TemplatesDirectory))
			{
				return templates;
			}

			foreach (string filePath in Directory.EnumerateFiles(TemplatesDirectory, "*.png"))
			{
				string name = Path.GetFileNameWithoutExtension(filePath);
				if (name.Length != 1)
				{
					continue;
				}

				char letter = char.ToLowerInvariant(name[0]);
				if (letter is < 'a' or > 'z')
				{
					continue;
				}

				using Bitmap bitmap = new(filePath);
				templates[letter] = TileLetterExtractor.ExtractLetterMask(bitmap);
			}

			return templates;
		}
	}
}
