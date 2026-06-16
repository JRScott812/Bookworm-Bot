using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bookworm_Bot
{
	internal static class Program
	{
		private static void Main()
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			string wordBanksPath = Path.Combine(AppContext.BaseDirectory, "Word Banks");
			if (!Directory.Exists(wordBanksPath))
			{
				Console.WriteLine("Could not find the Word Banks folder.");
				Console.WriteLine($"Expected: {wordBanksPath}");
				return;
			}

			Console.WriteLine("Loading dictionary...");
			WordDictionary dictionary = WordDictionary.Load(wordBanksPath);
			Console.WriteLine($"Ready — {dictionary.WordCount:N0} words loaded.\n");

			AbilityProfile profile = ConfigureLoadout();
			Solver solver = new(dictionary, profile);
			List<Tile>? currentTiles = null;

			while (true)
			{
				if (currentTiles is null)
				{
					currentTiles = ReadInitialTiles();
					if (currentTiles is null)
						break;

					continue;
				}

				if (currentTiles.Count == 0)
				{
					currentTiles = null;
					continue;
				}

				try
				{
					PrintTiles(currentTiles);
					IReadOnlyList<WordResult> words = solver.FindWords(currentTiles);
					PrintResults(words);

					Console.WriteLine("Next: # or word = played, new = fresh board, loadout, quit");
					Console.Write("> ");
					string? input = Console.ReadLine()?.Trim();
					if (string.IsNullOrWhiteSpace(input))
						continue;

					string command = input.ToLowerInvariant();
					if (command is "quit" or "exit")
						break;

					if (command is "l" or "loadout")
					{
						profile = ConfigureLoadout();
						solver = new Solver(dictionary, profile);
						continue;
					}

					if (command is "new" or "reset")
					{
						currentTiles = ReadInitialTiles();
						if (currentTiles is null)
							break;

						continue;
					}

					if (!TryResolvePlayedWord(input, words, solver, currentTiles, out WordResult playedWord))
					{
						Console.WriteLine("Could not match that word to your tiles.\n");
						continue;
					}

					List<Tile> usedTiles = TileBoard.GetUsedTiles(currentTiles, playedWord.UsedMask);
					Console.WriteLine(
						$"Played {playedWord.Word.ToUpperInvariant()} ({playedWord.TilesUsed} tiles, {playedWord.Damage:0.00} hearts).");
					Console.WriteLine($"  Used: {FormatTileList(usedTiles)}");
					Console.WriteLine();
					Console.WriteLine("Enter ALL tiles on your board now — order does not matter.");
					Console.WriteLine("(Just read them off the game; no need to track what moved.)");
					Console.WriteLine($"Or prefix with + to enter only the {playedWord.TilesUsed} new drop-in tile(s):");
					Console.Write("> ");

					string? boardInput = Console.ReadLine()?.Trim();
					if (string.IsNullOrWhiteSpace(boardInput))
					{
						Console.WriteLine("No tiles entered.\n");
						continue;
					}

					if (boardInput.StartsWith('+'))
					{
						List<Tile> replacements = LetterInput.Parse(boardInput[1..]);
						if (replacements.Count == 0)
						{
							Console.WriteLine("No valid tiles found.\n");
							continue;
						}

						currentTiles = TileBoard.ApplyPlayedWord(currentTiles, playedWord.UsedMask, replacements);
						continue;
					}

					List<Tile> updatedBoard = LetterInput.Parse(boardInput);
					if (updatedBoard.Count == 0)
					{
						Console.WriteLine("No valid tiles found.\n");
						continue;
					}

					currentTiles = updatedBoard;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}\n");
				}
			}
		}

		private static List<Tile>? ReadInitialTiles()
		{
			Console.WriteLine("Enter tiles. qu = Q tile; mark gems with $type:");
			Console.WriteLine("  e$ruby  a$emerald  qu$sapphire   types: amethyst emerald sapphire garnet ruby crystal diamond");
			Console.Write("> ");

			string? input = Console.ReadLine()?.Trim();
			if (string.IsNullOrWhiteSpace(input))
				return [];

			if (input.ToLowerInvariant() is "quit" or "exit")
				return null;

			List<Tile> tiles = LetterInput.Parse(input);
			if (tiles.Count == 0)
			{
				Console.WriteLine("No valid tiles found.\n");
				return [];
			}

			return tiles;
		}

		private static bool TryResolvePlayedWord(
			string input,
			IReadOnlyList<WordResult> words,
			Solver solver,
			IReadOnlyList<Tile> tiles,
			out WordResult playedWord)
		{
			if (int.TryParse(input, out int selection)
				&& selection >= 1
				&& selection <= words.Count)
			{
				playedWord = words[selection - 1];
				return true;
			}

			string word = input.ToLowerInvariant();
			for (int index = 0; index < words.Count; index++)
			{
				if (words[index].Word.Equals(word, StringComparison.Ordinal))
				{
					playedWord = words[index];
					return true;
				}
			}

			return solver.TryFindBestWord(tiles, word, out playedWord);
		}

		private static AbilityProfile ConfigureLoadout()
		{
			AbilityProfile profile = new();

			Console.WriteLine();
			Console.WriteLine("=== Configure loadout ===");
			Console.WriteLine("Set what is active this fight so themed words rank correctly.\n");

			profile.EnemyWeakness = ReadEnemyWeakness();
			profile.EnemyWeaknessMultiplier = ReadEnemyMultiplier(profile.EnemyWeakness);

			Console.WriteLine();
			Console.WriteLine("Equipped treasures (y/n):");
			profile.TomeOfAncients = ReadYesNo("  Tome of Ancients (+100% color words)");
			profile.TabletOfTheAges = ReadYesNo("  Tablet of the Ages (+150% color words)");
			profile.HandOfHercules = ReadYesNo("  Hand of Hercules (+50% metal words)");
			profile.WolfbaneNecklace = ReadYesNo("  Wolfbane Necklace (+50% mammal words)");
			profile.SlayerTalisman = ReadYesNo("  Slayer Talisman (+75% mammal words)");

			if (profile.TabletOfTheAges)
				profile.TomeOfAncients = false;

			if (profile.SlayerTalisman)
				profile.WolfbaneNecklace = false;

			PrintLoadoutSummary(profile);
			return profile;
		}

		private static WordCategory ReadEnemyWeakness()
		{
			Console.WriteLine("Enemy weakness from lore (extra damage for that category):");
			Console.WriteLine("  0) None");
			Console.WriteLine("  1) Colors");
			Console.WriteLine("  2) Metals");
			Console.WriteLine("  3) Mammals");
			Console.Write("> ");

			return Console.ReadLine()?.Trim() switch
			{
				"1" or "colors" or "color" => WordCategory.Colors,
				"2" or "metals" or "metal" => WordCategory.Metals,
				"3" or "mammals" or "mammal" => WordCategory.Mammals,
				_ => WordCategory.None
			};
		}

		private static float ReadEnemyMultiplier(WordCategory weakness)
		{
			if (weakness == WordCategory.None)
				return 1f;

			Console.Write("Weakness multiplier (default 3x): ");
			string? input = Console.ReadLine()?.Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(input))
				return 3f;

			input = input.TrimEnd('x');
			return float.TryParse(input, out float multiplier) && multiplier > 0f
				? multiplier
				: 3f;
		}

		private static bool ReadYesNo(string prompt)
		{
			Console.Write($"{prompt}? [y/N] ");
			string? input = Console.ReadLine()?.Trim().ToLowerInvariant();
			return input is "y" or "yes";
		}

		private static void PrintLoadoutSummary(AbilityProfile profile)
		{
			Console.WriteLine();
			Console.WriteLine("Active loadout:");

			if (profile.EnemyWeakness == WordCategory.None)
				Console.WriteLine("  Enemy weakness: none");
			else
				Console.WriteLine($"  Enemy weakness: {profile.EnemyWeakness} at {profile.EnemyWeaknessMultiplier:0.#}x");

			List<string> treasures = [];
			if (profile.TabletOfTheAges)
				treasures.Add("Tablet of the Ages");
			else if (profile.TomeOfAncients)
				treasures.Add("Tome of Ancients");
			if (profile.HandOfHercules)
				treasures.Add("Hand of Hercules");
			if (profile.SlayerTalisman)
				treasures.Add("Slayer Talisman");
			else if (profile.WolfbaneNecklace)
				treasures.Add("Wolfbane Necklace");

			Console.WriteLine(treasures.Count == 0
				? "  Treasures: none"
				: $"  Treasures: {string.Join(", ", treasures)}");

			Console.WriteLine();
		}

		private static string FormatTileList(IReadOnlyList<Tile> tiles) =>
			string.Join("  ", tiles.Select(tile => tile.Display));

		private static void PrintTiles(IReadOnlyList<Tile> tiles)
		{
			List<Tile> display = tiles.ToList();
			Shuffle(display);

			Console.WriteLine();
			Console.WriteLine($"Tiles ({tiles.Count}) — any order:");
			Console.WriteLine("  " + FormatTileList(display));
			Console.WriteLine();
		}

		private static void Shuffle<T>(IList<T> items)
		{
			for (int index = items.Count - 1; index > 0; index--)
			{
				int swapIndex = Random.Shared.Next(index + 1);
				(items[index], items[swapIndex]) = (items[swapIndex], items[index]);
			}
		}

		private static void PrintResults(IReadOnlyList<WordResult> words)
		{
			if (words.Count == 0)
			{
				Console.WriteLine("No valid words found.\n");
				return;
			}

			Console.WriteLine($"Top {words.Count} words:");
			for (int index = 0; index < words.Count; index++)
			{
				WordResult word = words[index];
				string bonusText = word.Bonuses.Count == 0
					? string.Empty
					: $"  [{string.Join(", ", word.Bonuses)}]";

				Console.WriteLine(
					$"  {index + 1,2}. {word.Word.ToUpperInvariant(),-16} {word.Damage,5:0.00} hearts  ({word.TilesUsed} tiles, base {word.BaseDamage:0.00}){bonusText}");
			}

			Console.WriteLine();
		}
	}
}
