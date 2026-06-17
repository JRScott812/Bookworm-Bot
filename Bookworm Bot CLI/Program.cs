using System;
using System.Collections.Generic;
using System.Linq;

using Bookworm_Bot_Class;

namespace Bookworm_Bot_CLI
{
	internal static class Program
	{
		private static void Main()
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			if (!WordBankPaths.TryGetWordBanksDirectory(out string wordBanksPath))
			{
				Console.WriteLine("Could not find the Word Banks folder.");
				Console.WriteLine($"Expected: {WordBankPaths.GetWordBanksDirectory()}");
				return;
			}

			Console.WriteLine("Loading dictionary...");
			WordDictionary dictionary = WordDictionary.Load(wordBanksPath);
			Console.WriteLine($"Ready — {dictionary.WordCount:N0} words loaded.\n");
			GameSession session = new(dictionary, ConfigureLoadout());
			ApplyConfiguredLoadout(session, session.Profile);
			List<Tile>? pendingBoard = null;
			while (true)
			{
				if (pendingBoard is null && session.Tiles.Count == 0)
				{
					pendingBoard = ReadInitialTiles();
					if (pendingBoard is null)
					{
						break;
					}

					if (pendingBoard.Count == 0)
					{
						continue;
					}

					session.SetBoard(pendingBoard);
					pendingBoard = null;
				}

				if (session.Tiles.Count == 0)
				{
					continue;
				}

				try
				{
					PrintTiles(session.Tiles);
					IReadOnlyList<WordResult> words = session.GetSuggestions();
					PrintResults(words);
					Console.WriteLine("Next: # or word = played, new = fresh board, loadout, enemy, quit");
					Console.Write("> ");
					string? input = Console.ReadLine()?.Trim();
					if (string.IsNullOrWhiteSpace(input))
					{
						continue;
					}

					string command = input.ToLowerInvariant();
					if (command is "quit" or "exit")
					{
						break;
					}

					if (command is "l" or "loadout")
					{
						ApplyConfiguredLoadout(session, ConfigureLoadout());
						continue;
					}

					if (command.StartsWith("enemy ", StringComparison.Ordinal))
					{
						string enemyQuery = input[6..].Trim();
						if (TryApplyEnemy(session, enemyQuery, out string message))
						{
							Console.WriteLine(message);
						}
						else
						{
							Console.WriteLine("Unknown enemy. Try: enemy list, or enemy sphinx\n");
						}

						continue;
					}

					if (command is "new" or "reset")
					{
						session.ClearBoard();
						pendingBoard = ReadInitialTiles();
						if (pendingBoard is null)
						{
							break;
						}

						if (pendingBoard.Count > 0)
						{
							session.SetBoard(pendingBoard);
						}

						pendingBoard = null;
						continue;
					}

					if (!session.TrySelectPlayedWord(input, words, out WordResult playedWord))
					{
						Console.WriteLine("Could not match that word to your tiles.\n");
						continue;
					}

					List<Tile> usedTiles = TileBoard.GetUsedTiles(session.Tiles, playedWord.UsedMask);
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

						session.ApplyPlayedWord(playedWord, replacements);
						continue;
					}

					List<Tile> updatedBoard = LetterInput.Parse(boardInput);
					if (updatedBoard.Count == 0)
					{
						Console.WriteLine("No valid tiles found.\n");
						continue;
					}

					session.SetBoard(updatedBoard);
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
			{
				return [];
			}

			if (input.ToLowerInvariant() is "quit" or "exit")
			{
				return null;
			}

			List<Tile> tiles = LetterInput.Parse(input);
			if (tiles.Count == 0)
			{
				Console.WriteLine("No valid tiles found.\n");
				return [];
			}

			return tiles;
		}

		private static AbilityProfile ConfigureLoadout()
		{
			AbilityProfile profile = new();
			Console.WriteLine();
			Console.WriteLine("=== Configure loadout ===");
			Console.WriteLine("Pick up to 3 treasures (game limit). Upgrades replace their base version.\n");
			profile.Fight = ReadFightContext();
			if (profile.Fight is null)
			{
				profile.EnemyWeakness = ReadEnemyWeakness();
				profile.EnemyWeaknessMultiplier = ReadEnemyMultiplier(profile.EnemyWeakness);
			}

			profile.Session = ReadSessionContext();
			Console.WriteLine();
			profile.Loadout = ReadTreasureLoadout();
			if (!LoadoutValidator.TryValidate(profile.Loadout, out string? loadoutError))
			{
				Console.WriteLine(loadoutError);
				profile.Loadout = new Loadout();
			}

			PrintLoadoutSummary(profile);
			return profile;
		}

		private static void ApplyConfiguredLoadout(GameSession session, AbilityProfile profile)
		{
			session.SetProfile(profile);
			if (profile.Fight is not null)
			{
				session.SetFight(profile.Fight);
			}

			session.SetSession(profile.Session);
		}

		private static FightContext? ReadFightContext()
		{
			Console.WriteLine("Enemy (from catalog, or blank for manual weakness):");
			for (int index = 0; index < EnemyCatalog.All.Count; index++)
			{
				EnemyDefinition enemy = EnemyCatalog.All[index];
				Console.WriteLine($"  {index + 1,2}) {enemy.DisplayName}  [{FormatWeakness(enemy)}]");
			}

			Console.Write("> Enemy # or id (blank = manual): ");
			string? input = Console.ReadLine()?.Trim();
			if (string.IsNullOrWhiteSpace(input))
			{
				return null;
			}

			if (int.TryParse(input, out int number) && EnemyCatalog.TryGetByIndex(number, out EnemyDefinition byIndex))
			{
				return EnemyCatalog.ToFightContext(byIndex);
			}

			if (EnemyCatalog.TryGet(input, out EnemyDefinition byId))
			{
				return EnemyCatalog.ToFightContext(byId);
			}

			Console.WriteLine("  Unrecognized enemy — using manual weakness instead.");
			return null;
		}

		private static SessionContext ReadSessionContext()
		{
			SessionContext session = new();
			Console.Write("Lex level (1-42, default 1): ");
			string? lexInput = Console.ReadLine()?.Trim();
			if (int.TryParse(lexInput, out int lexLevel) && lexLevel is >= 1 and <= 42)
			{
				session.LexLevel = lexLevel;
			}

			Console.Write("Power Up active? (y/N): ");
			string? powerUpInput = Console.ReadLine()?.Trim().ToLowerInvariant();
			session.PowerUpActive = powerUpInput is "y" or "yes" or "true" or "1";
			return session;
		}

		private static bool TryApplyEnemy(GameSession session, string query, out string message)
		{
			if (query.Equals("list", StringComparison.OrdinalIgnoreCase)
				|| query.Equals("?", StringComparison.Ordinal))
			{
				Console.WriteLine();
				for (int index = 0; index < EnemyCatalog.All.Count; index++)
				{
					EnemyDefinition enemy = EnemyCatalog.All[index];
					Console.WriteLine($"  {index + 1,2}) {enemy.Id,-16} {enemy.DisplayName}  [{FormatWeakness(enemy)}]");
				}

				Console.WriteLine();
				message = string.Empty;
				return true;
			}

			EnemyDefinition? enemyMatch = null;
			if (int.TryParse(query, out int number) && EnemyCatalog.TryGetByIndex(number, out EnemyDefinition byIndex))
			{
				enemyMatch = byIndex;
			}
			else if (EnemyCatalog.TryGet(query, out EnemyDefinition byId))
			{
				enemyMatch = byId;
			}
			else
			{
				List<EnemyDefinition> matches = [.. EnemyCatalog.Search(query).Take(2)];
				if (matches.Count == 1)
				{
					enemyMatch = matches[0];
				}
			}

			if (enemyMatch is null)
			{
				message = string.Empty;
				return false;
			}

			FightContext fight = EnemyCatalog.ToFightContext(enemyMatch.Value);
			session.SetFight(fight);
			session.Profile.EnemyWeakness = WordCategory.None;
			message = $"Fight set: {enemyMatch.Value.DisplayName} ({FormatWeakness(enemyMatch.Value)}, min {fight.MinWordLength} letters)";
			return true;
		}

		private static string FormatWeakness(EnemyDefinition enemy)
		{
			if (enemy.WeaknessCategories == WordCategory.None)
			{
				return enemy.MinWordLength > Solver.MinWordLength
					? $"min {enemy.MinWordLength} letters"
					: "no weakness";
			}

			string categories = enemy.WeaknessCategories.ToString().Replace(", ", "/");
			return $"{categories} {enemy.WeaknessMultiplier:0.#}x";
		}

		private static Loadout ReadTreasureLoadout()
		{
			Loadout loadout = new();
			Console.WriteLine("Treasures (enter number or name, blank for none):");
			for (int slot = 1; slot <= 3; slot++)
			{
				Console.WriteLine($"  Slot {slot}:");
				for (int index = 0; index < TreasureCatalog.ScoringTreasures.Count; index++)
				{
					TreasureId treasure = TreasureCatalog.ScoringTreasures[index];
					Console.WriteLine($"    {index + 1,2}) {TreasureCatalog.GetDisplayName(treasure)}");
				}

				Console.Write($"> Slot {slot} treasure: ");
				string? input = Console.ReadLine()?.Trim();
				if (string.IsNullOrWhiteSpace(input))
				{
					continue;
				}

				TreasureId selected = ReadTreasureChoice(input);
				if (selected == TreasureId.None)
				{
					Console.WriteLine("  Unrecognized treasure, slot left empty.");
					continue;
				}

				loadout.SetSlot(slot, selected);
			}

			loadout.Normalize();
			if (!LoadoutValidator.TryValidate(loadout, out string? error))
			{
				Console.WriteLine($"  Loadout warning: {error}");
			}

			return loadout;
		}

		private static TreasureId ReadTreasureChoice(string input)
		{
			return int.TryParse(input, out int number)
				&& number >= 1
				&& number <= TreasureCatalog.ScoringTreasures.Count
				? TreasureCatalog.ScoringTreasures[number - 1]
				: TreasureCatalog.TryParse(input, out TreasureId treasure)
				? treasure
				: TreasureId.None;
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
			{
				return 1f;
			}

			Console.Write("Weakness multiplier (default 3x): ");
			string? input = Console.ReadLine()?.Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(input))
			{
				return 3f;
			}

			input = input.TrimEnd('x');
			return float.TryParse(input, out float multiplier) && multiplier > 0f
				? multiplier
				: 3f;
		}

		private static void PrintLoadoutSummary(AbilityProfile profile)
		{
			Console.WriteLine();
			Console.WriteLine("Active loadout:");
			if (profile.Fight is FightContext fight)
			{
				Console.WriteLine($"  Enemy: {fight.EnemyName ?? fight.EnemyId ?? "unknown"}");
				if (fight.WeaknessCategories != WordCategory.None)
				{
					Console.WriteLine($"  Weakness: {fight.WeaknessCategories} at {fight.WeaknessMultiplier:0.#}x");
				}

				if (fight.MinWordLength > Solver.MinWordLength)
				{
					Console.WriteLine($"  Min word length: {fight.MinWordLength}");
				}
			}
			else if (profile.EnemyWeakness == WordCategory.None)
			{
				Console.WriteLine("  Enemy weakness: none");
			}
			else
			{
				Console.WriteLine($"  Enemy weakness: {profile.EnemyWeakness} at {profile.EnemyWeaknessMultiplier:0.#}x");
			}

			if (profile.Session.LexLevel > 1 || profile.Session.PowerUpActive)
			{
				Console.WriteLine(
					$"  Session: Lex {profile.Session.LexLevel} ({profile.Session.GetDamageMultiplier() * 100:0}% damage)"
					+ (profile.Session.PowerUpActive ? ", Power Up active" : string.Empty));
			}

			List<string> treasures = [];
			foreach (TreasureId treasure in profile.Loadout.Equipped)
			{
				treasures.Add(TreasureCatalog.GetDisplayName(treasure));
			}

			Console.WriteLine(treasures.Count == 0
				? "  Treasures: none"
				: $"  Treasures: {string.Join(", ", treasures)}");
			Console.WriteLine();
		}

		private static string FormatTileList(IReadOnlyList<Tile> tiles) => string.Join("  ", tiles.Select(tile => tile.Display));

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
