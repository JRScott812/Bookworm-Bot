using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Bookworm_Bot_Class
{
	internal readonly record struct WordResult(
		string Word,
		float Damage,
		float BaseDamage,
		int AdjustedLength,
		WordCategory Categories,
		IReadOnlyList<string> Bonuses,
		int UsedMask)
	{
		public int TilesUsed => BitOperations.PopCount((uint)UsedMask);
	}

	internal sealed class Solver(WordDictionary dictionary, AbilityProfile profile)
	{
		public const int MinWordLength = 3;

		private readonly WordDictionary _dictionary = dictionary;
		private readonly AbilityProfile _profile = profile;

		public IReadOnlyList<WordResult> FindWords(IReadOnlyList<Tile> tiles, int topCount = 25)
		{
			Tile[] pool = [.. tiles];
			Dictionary<string, WordResult> best = new(StringComparer.Ordinal);

			SearchBag(_dictionary.Root, pool, usedMask: 0, gemBonus: 0f, new StringBuilder(16), best);

			return RankResults(best.Values, topCount);
		}

		public bool TryFindBestWord(IReadOnlyList<Tile> tiles, string word, out WordResult result)
		{
			word = word.Trim().ToLowerInvariant();
			if (word.Length < MinWordLength || !_dictionary.Contains(word))
			{
				result = default;
				return false;
			}

			Tile[] pool = [.. tiles];
			int bestMask = 0;
			float bestGemBonus = -1f;
			SearchExactWord(pool, usedMask: 0, gemBonus: 0f, word, wordIndex: 0, ref bestMask, ref bestGemBonus);

			if (bestGemBonus < 0f)
			{
				result = default;
				return false;
			}

			WordCategory categories = _dictionary.GetCategories(word);
			float baseDamage = AbilityProfile.GetBaseDamage(word);
			float damage = _profile.CalculateDamage(word, categories, bestGemBonus);

			result = new WordResult(
				word,
				damage,
				baseDamage,
				AbilityProfile.CalculateAdjustedLength(word),
				categories,
				_profile.DescribeBonuses(word, categories, bestGemBonus),
				bestMask);

			return true;
		}

		private void SearchBag(
			TrieNode node,
			Tile[] pool,
			int usedMask,
			float gemBonus,
			StringBuilder path,
			Dictionary<string, WordResult> best)
		{
			if (node.IsWord)
				TryAddResult(path.ToString(), gemBonus, usedMask, best);

			foreach (KeyValuePair<char, TrieNode> child in GetChildren(node))
			{
				char letter = child.Key;

				if (letter == 'q')
				{
					if (!child.Value.TryGetChild('u', out TrieNode? afterQu) || afterQu is null)
						continue;

					for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
					{
						if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != 'q')
							continue;

						Tile tile = pool[tileIndex];
						path.Append('q').Append('u');
						SearchBag(
							afterQu,
							pool,
							SetUsed(usedMask, tileIndex),
							gemBonus + tile.DamageBonus,
							path,
							best);
						path.Length -= 2;
					}

					continue;
				}

				for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
				{
					if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != letter)
						continue;

					Tile tile = pool[tileIndex];
					path.Append(letter);
					SearchBag(
						child.Value,
						pool,
						SetUsed(usedMask, tileIndex),
						gemBonus + tile.DamageBonus,
						path,
						best);
					path.Length -= 1;
				}
			}
		}

		private void SearchExactWord(
			Tile[] pool,
			int usedMask,
			float gemBonus,
			string word,
			int wordIndex,
			ref int bestMask,
			ref float bestGemBonus)
		{
			if (wordIndex >= word.Length)
			{
				if (gemBonus > bestGemBonus)
				{
					bestGemBonus = gemBonus;
					bestMask = usedMask;
				}

				return;
			}

			if (word[wordIndex] == 'q' && wordIndex + 1 < word.Length && word[wordIndex + 1] == 'u')
			{
				for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
				{
					if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != 'q')
						continue;

					Tile tile = pool[tileIndex];
					SearchExactWord(
						pool,
						SetUsed(usedMask, tileIndex),
						gemBonus + tile.DamageBonus,
						word,
						wordIndex + 2,
						ref bestMask,
						ref bestGemBonus);
				}

				return;
			}

			char letter = word[wordIndex];
			for (int tileIndex = 0; tileIndex < pool.Length; tileIndex++)
			{
				if (IsUsed(usedMask, tileIndex) || pool[tileIndex].Letter != letter)
					continue;

				Tile tile = pool[tileIndex];
				SearchExactWord(
					pool,
					SetUsed(usedMask, tileIndex),
					gemBonus + tile.DamageBonus,
					word,
					wordIndex + 1,
					ref bestMask,
					ref bestGemBonus);
			}
		}

		private static IEnumerable<KeyValuePair<char, TrieNode>> GetChildren(TrieNode node)
		{
			foreach (char letter in "abcdefghijklmnopqrstuvwxyz")
			{
				if (node.TryGetChild(letter, out TrieNode? child) && child is not null)
					yield return KeyValuePair.Create(letter, child);
			}
		}

		private void TryAddResult(
			string word,
			float gemBonus,
			int usedMask,
			Dictionary<string, WordResult> best)
		{
			if (word.Length < MinWordLength)
				return;

			WordCategory categories = _dictionary.GetCategories(word);
			float baseDamage = AbilityProfile.GetBaseDamage(word);
			float damage = _profile.CalculateDamage(word, categories, gemBonus);

			if (best.TryGetValue(word, out WordResult existing) && existing.Damage >= damage)
				return;

			best[word] = new WordResult(
				word,
				damage,
				baseDamage,
				AbilityProfile.CalculateAdjustedLength(word),
				categories,
				_profile.DescribeBonuses(word, categories, gemBonus),
				usedMask);
		}

		private static IReadOnlyList<WordResult> RankResults(IEnumerable<WordResult> results, int topCount) =>
			[.. results
				.OrderByDescending(result => result.Damage)
				.ThenByDescending(result => result.AdjustedLength)
				.ThenBy(result => result.Word, StringComparer.Ordinal)
				.Take(topCount)];

		private static bool IsUsed(int usedMask, int tileIndex) =>
			(usedMask & (1 << tileIndex)) != 0;

		private static int SetUsed(int usedMask, int tileIndex) =>
			usedMask | (1 << tileIndex);
	}
}
