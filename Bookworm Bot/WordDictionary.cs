using System;
using System.Collections.Generic;
using System.IO;

namespace Bookworm_Bot
{
	internal sealed class WordDictionary
	{
		private readonly HashSet<string> _words = new(StringComparer.Ordinal);
		private readonly Dictionary<string, WordCategory> _categories = new(StringComparer.Ordinal);

		public int WordCount => _words.Count;

		public static WordDictionary Load(string wordBanksDirectory)
		{
			WordDictionary dictionary = new();

			foreach (string filePath in Directory.EnumerateFiles(wordBanksDirectory, "*.txt"))
			{
				string fileName = Path.GetFileName(filePath);
				WordCategory? category = GetCategoryFromFileName(fileName);

				foreach (string rawLine in File.ReadLines(filePath))
				{
					string word = rawLine.Trim().ToLowerInvariant();
					if (word.Length < Solver.MinWordLength)
						continue;

					if (dictionary._words.Add(word))
						dictionary.Root.Insert(word);

					if (category.HasValue)
						dictionary.AddCategory(word, category.Value);
				}
			}

			return dictionary;
		}

		public bool Contains(string word) => _words.Contains(word);

		public WordCategory GetCategories(string word) =>
			_categories.TryGetValue(word, out WordCategory categories)
				? categories
				: WordCategory.None;

		public TrieNode Root { get; } = new();

		private void AddCategory(string word, WordCategory category)
		{
			if (_categories.TryGetValue(word, out WordCategory existing))
				_categories[word] = existing | category;
			else
				_categories[word] = category;
		}

		private static WordCategory? GetCategoryFromFileName(string fileName) =>
			fileName.ToLowerInvariant() switch
			{
				"colors.txt" => WordCategory.Colors,
				"metals.txt" => WordCategory.Metals,
				"mammals.txt" => WordCategory.Mammals,
				_ => null
			};
	}

	internal sealed class TrieNode
	{
		private readonly Dictionary<char, TrieNode> _children = new();

		public bool IsWord { get; private set; }

		public bool HasChildren => _children.Count > 0;

		public bool TryGetChild(char letter, out TrieNode? child) =>
			_children.TryGetValue(letter, out child);

		public TrieNode GetOrAddChild(char letter)
		{
			if (!_children.TryGetValue(letter, out TrieNode? child))
			{
				child = new TrieNode();
				_children[letter] = child;
			}

			return child;
		}

		public void Insert(string word)
		{
			TrieNode node = this;
			foreach (char letter in word)
				node = node.GetOrAddChild(letter);

			node.IsWord = true;
		}
	}
}
