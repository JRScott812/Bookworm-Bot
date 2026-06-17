using System;
using System.IO;

namespace Bookworm_Bot_Class
{
	public static class WordBankPaths
	{
		public static string GetWordBanksDirectory() => Path.Combine(AppContext.BaseDirectory, "Word Banks");

		public static bool TryGetWordBanksDirectory(out string path)
		{
			path = GetWordBanksDirectory();
			return Directory.Exists(path);
		}
	}
}