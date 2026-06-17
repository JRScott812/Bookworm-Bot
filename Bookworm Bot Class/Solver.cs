using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Bookworm_Bot_Class
{
	public readonly record struct WordResult(
		string Word,
		float Damage,
		float BaseDamage,
		int AdjustedLength,
		WordCategory Categories,
		IReadOnlyList<string> Bonuses,
		int UsedMask,
		int UsedGridMask = 0)
	{
		public int TilesUsed => BitOperations.PopCount((uint)(UsedGridMask != 0 ? UsedGridMask : UsedMask));
	}

	public sealed class Solver(WordDictionary dictionary, AbilityProfile profile)
	{
		public const int MinWordLength = 3;
		private readonly WordDictionary _dictionary = dictionary;
		private readonly AbilityProfile _profile = profile;
		public IReadOnlyList<WordResult> FindWords(IReadOnlyList<Tile> tiles, int topCount = 25)
		{
			Tile[] pool = [.. tiles];
			Dictionary<string, WordResult> best = new(StringComparer.Ordinal);
			List<int> usedIndices = [];
			SearchBag(_dictionary.Root, pool, usedMask: 0, gemBonus: 0f, usedIndices, new StringBuilder(16), best);
			return RankResults(best.Values, topCount);
		}

		public bool TryFindBestWord(IReadOnlyList<Tile> tiles, string word, out WordResult result)
		{
			word = word.Trim().ToLowerInvariant();
			int minLength = _profile.Fight?.MinWordLength ?? MinWordLength;
			if (word.Length < minLength || !_dictionary.Contains(word))
			{
				result = default;
				return false;
			}

			Tile[] pool = [.. tiles];
			int bestMask = 0;
			float bestGemBonus = -1f;
			List<int> bestIndices = [];
			List<int> currentIndices = [];
			SearchExactWord(pool, usedMask: 0, gemBonus: 0f, currentIndices, word, wordIndex: 0, ref bestMask, ref bestGemBonus, ref bestIndices);
			if (bestGemBonus < 0f)
			{
				result = default;
				return false;
			}

			List<Tile> usedTiles = GetUsedTilesInOrder(pool, bestIndices);
			WordCategory categories = _dictionary.GetCategories(word);
			if (!DamageCalculator.MeetsMinWordLength(_profile, word))
			{
				result = default;
				return false;
			}

			float baseDamage = AbilityProfile.GetBaseDamageFromLength(
				AbilityProfile.CalculateAdjustedLength(word, usedTiles, _profile.Loadout));
			float damage = DamageCalculator.CalculateDamage(_profile, word, categories, usedTiles);
			result = new WordResult(
				word,
				damage,
				baseDamage,
				AbilityProfile.CalculateAdjustedLength(word, usedTiles, _profile.Loadout),
				categories,
				DamageCalculator.DescribeBonuses(_profile, word, categories, usedTiles),
				bestMask);
			return true;
		}

		private void SearchBag(
			TrieNode node,
			Tile[] pool,
			int usedMask,
			float gemBonus,
			List<int> usedIndices,
			StringBuilder path,
			Dictionary<string, WordResult> best)
		{
			if (node.IsWord)
			{
				TryAddResult(path.ToString(), gemBonus, usedMask, usedIndices, pool, best);
			}

			foreach (KeyValuePair<char, TrieNode> child in GetChildren(node))
			{
				char letter = child.Key;
				if (letter == 'q')
				{
					if (!child.Value.TryGetChild('u', out TrieNode? afterQu) || afterQu is null)
					{
						continue;
					}

					for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
					{
						if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != 'q')
						{
							continue;
						}

						Tile tile = pool[tileIndex];
						_ = path.Append('q').Append('u');
						usedIndices.Add(tileIndex);
						SearchBag(
							afterQu,
							pool,
							SetUsed(usedMask, tileIndex),
							gemBonus + tile.DamageBonus,
							usedIndices,
							path,
							best);
						usedIndices.RemoveAt(usedIndices.Count - 1);
						path.Length -= 2;
					}

					continue;
				}

				for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
				{
					if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != letter)
					{
						continue;
					}

					Tile tile = pool[tileIndex];
					_ = path.Append(letter);
					usedIndices.Add(tileIndex);
					SearchBag(
						child.Value,
						pool,
						SetUsed(usedMask, tileIndex),
						gemBonus + tile.DamageBonus,
						usedIndices,
						path,
						best);
					usedIndices.RemoveAt(usedIndices.Count - 1);
					path.Length -= 1;
				}
			}
		}

		private void SearchExactWord(
			Tile[] pool,
			int usedMask,
			float gemBonus,
			List<int> currentIndices,
			string word,
			int wordIndex,
			ref int bestMask,
			ref float bestGemBonus,
			ref List<int> bestIndices)
		{
			if (wordIndex >= word.Length)
			{
				if (gemBonus > bestGemBonus)
				{
					bestGemBonus = gemBonus;
					bestMask = usedMask;
					bestIndices = [.. currentIndices];
				}

				return;
			}

			if (word[wordIndex] == 'q' && wordIndex + 1 < word.Length && word[wordIndex + 1] == 'u')
			{
				for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
				{
					if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != 'q')
					{
						continue;
					}

					Tile tile = pool[tileIndex];
					currentIndices.Add(tileIndex);
					SearchExactWord(
						pool,
						SetUsed(usedMask, tileIndex),
						gemBonus + tile.DamageBonus,
						currentIndices,
						word,
						wordIndex + 2,
						ref bestMask,
						ref bestGemBonus,
						ref bestIndices);
					currentIndices.RemoveAt(currentIndices.Count - 1);
				}

				return;
			}

			char letter = word[wordIndex];
			for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
			{
				if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != letter)
				{
					continue;
				}

				Tile tile = pool[tileIndex];
				currentIndices.Add(tileIndex);
				SearchExactWord(
					pool,
					SetUsed(usedMask, tileIndex),
					gemBonus + tile.DamageBonus,
					currentIndices,
					word,
					wordIndex + 1,
					ref bestMask,
					ref bestGemBonus,
					ref bestIndices);
				currentIndices.RemoveAt(currentIndices.Count - 1);
			}
		}

		private static IEnumerable<KeyValuePair<char, TrieNode>> GetChildren(TrieNode node)
		{
			foreach (char letter in "abcdefghijklmnopqrstuvwxyz")
			{
				if (node.TryGetChild(letter, out TrieNode? child) && child is not null)
				{
					yield return KeyValuePair.Create(letter, child);
				}
			}
		}

		private void TryAddResult(
			string word,
			float gemBonus,
			int usedMask,
			IReadOnlyList<int> usedIndices,
			Tile[] pool,
			Dictionary<string, WordResult> best)
		{
			if (word.Length < MinWordLength)
			{
				return;
			}

			List<Tile> usedTiles = GetUsedTilesInOrder(pool, usedIndices);
			WordCategory categories = _dictionary.GetCategories(word);
			if (!DamageCalculator.MeetsMinWordLength(_profile, word))
			{
				return;
			}

			int adjustedLength = AbilityProfile.CalculateAdjustedLength(word, usedTiles, _profile.Loadout);
			float baseDamage = AbilityProfile.GetBaseDamageFromLength(adjustedLength);
			float damage = DamageCalculator.CalculateDamage(_profile, word, categories, usedTiles);
			if (best.TryGetValue(word, out WordResult existing) && existing.Damage >= damage)
			{
				return;
			}

			best[word] = new WordResult(
				word,
				damage,
				baseDamage,
				adjustedLength,
				categories,
				DamageCalculator.DescribeBonuses(_profile, word, categories, usedTiles),
				usedMask);
		}

		private static List<Tile> GetUsedTilesInOrder(Tile[] pool, IReadOnlyList<int> usedIndices)
		{
			List<Tile> usedTiles = new(usedIndices.Count);
			for (int index = 0; index < usedIndices.Count; index++)
			{
				usedTiles.Add(pool[usedIndices[index]]);
			}

			return usedTiles;
		}

		private static IReadOnlyList<WordResult> RankResults(IEnumerable<WordResult> results, int topCount) =>
			[.. results
				.OrderByDescending(result => result.Damage)
				.ThenByDescending(result => result.AdjustedLength)
				.ThenBy(result => result.Word, StringComparer.Ordinal)
				.Take(topCount)];

		private static bool IsUsed(int usedMask, int tileIndex) => (usedMask & (1 << tileIndex)) != 0;

		private static int SetUsed(int usedMask, int tileIndex) => usedMask | (1 << tileIndex);
	}
}