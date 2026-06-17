using System;
using System.Collections.Generic;
using System.IO;

namespace Bookworm_Bot_Class
{
	public sealed class WordDictionary
	{
		private readonly HashSet<string> _words = new(StringComparer.Ordinal);
		private readonly Dictionary<string, WordCategory> _categories = new(StringComparer.Ordinal);
		public int WordCount => _words.Count;
		public static WordDictionary Load(string wordBanksDirectory)
		{
			WordDictionary dictionary = new();
			string wordsFile = Path.Combine(wordBanksDirectory, "words.txt");
			if (File.Exists(wordsFile))
			{
				LoadLines(dictionary, wordsFile, addToTrie: true, category: null);
			}

			foreach (string filePath in Directory.EnumerateFiles(wordBanksDirectory, "*.txt"))
			{
				string fileName = Path.GetFileName(filePath);
				if (fileName.Equals("words.txt", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				WordCategory? category = GetCategoryFromFileName(fileName);
				if (category.HasValue)
				{
					LoadLines(dictionary, filePath, addToTrie: false, category: category.Value);
				}
			}

			return dictionary;
		}

		public bool Contains(string word) => _words.Contains(word);

		public WordCategory GetCategories(string word)
		{
			return _categories.TryGetValue(word, out WordCategory categories)
						? categories
						: WordCategory.None;
		}

		public TrieNode Root { get; } = new();
		private static void LoadLines(
			WordDictionary dictionary,
			string filePath,
			bool addToTrie,
			WordCategory? category)
		{
			foreach (string rawLine in File.ReadLines(filePath))
			{
				string word = rawLine.Trim().ToLowerInvariant();
				if (word.Length < Solver.MinWordLength)
				{
					continue;
				}

				if (addToTrie && dictionary._words.Add(word))
				{
					dictionary.Root.Insert(word);
				}

				if (category.HasValue)
				{
					dictionary.AddCategory(word, category.Value);
				}
			}
		}

		private void AddCategory(string word, WordCategory category) => _categories[word] = _categories.TryGetValue(word, out WordCategory existing) ? existing | category : category;

		private static readonly Dictionary<string, WordCategory> CategoryFiles = new(StringComparer.OrdinalIgnoreCase)
		{
			["colors.txt"] = WordCategory.Colors,
			["metals.txt"] = WordCategory.Metals,
			["mammals.txt"] = WordCategory.Mammals,
			["felines.txt"] = WordCategory.Felines,
			["bone.txt"] = WordCategory.Bone,
			["fire.txt"] = WordCategory.Fire,
			["fruitsandvegs.txt"] = WordCategory.FruitsAndVegetables,
			["adjectives.txt"] = WordCategory.Adjectives,
			["verbs.txt"] = WordCategory.Verbs,
			["spellwords.txt"] = WordCategory.Words
		};

		private static WordCategory? GetCategoryFromFileName(string fileName) =>
			CategoryFiles.TryGetValue(fileName, out WordCategory category) ? category : null;
	}

	public sealed class TrieNode
	{
		private readonly Dictionary<char, TrieNode> _children = [];
		public bool IsWord { get; private set; }
		public bool HasChildren => _children.Count > 0;
		public bool TryGetChild(char letter, out TrieNode? child) => _children.TryGetValue(letter, out child);

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
			{
				node = node.GetOrAddChild(letter);
			}

			node.IsWord = true;
		}
	}
}