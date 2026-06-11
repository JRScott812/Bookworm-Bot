using System;
using System.Collections.Generic;
using System.IO;

namespace Bookworm_Bot
{
	internal class Program
	{
		static void Main()
		{
			string[] validWords = File.ReadAllLines("F:\\source\\Bookworm Bot\\Bookworm Bot\\Word Banks\\words.txt");
			
			Console.Write("Enter each letter ('q' automatically becomes 'qu') — press Backspace to finish: ");

			List<char> letters = [];
			ConsoleKey input = ConsoleKey.None;

			while (input != ConsoleKey.Backspace)
			{
				ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
				input = keyInfo.Key;

				if (input == ConsoleKey.Backspace)
					break;

				char c = keyInfo.KeyChar;

				// Echo the character
				Console.Write(c);
				letters.Add(c);

				// If user typed 'q', automatically append 'u'
				if (c == 'q')
				{
					letters.Add('u');
					Console.Write('u');
				}
			}

			Console.WriteLine();

			List<string> words = GenerateCombinations(letters, validWords);
			foreach (string word in words)
			{
				Console.WriteLine(word);
			}
		}

		public static List<string> GenerateCombinations(List<char> letters, string[] validWords)
		{
			List<string> result = [];

			int npow = 1 << letters.Count;

			for (int i = 0; i < npow; i++)
			{
				string combination = string.Empty;

				for (int j = 0; j < letters.Count; j++)
				{
					if ((i & (1 << j)) != 0)
					{
						combination += letters[j];
					}
				}

				if (!string.IsNullOrEmpty(combination) && validWords.Contains(combination))
				{
					result.Add(combination);
				}
			}

			return result;
		}
	}
}
