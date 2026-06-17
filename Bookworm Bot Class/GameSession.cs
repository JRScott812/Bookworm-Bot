using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public sealed class GameSession(WordDictionary dictionary, AbilityProfile profile)
	{
		private readonly WordDictionary _dictionary = dictionary;
		private Solver _solver = new(dictionary, profile);
		private GridBoard _board = new();

		public GridBoard Board => _board;

		public IReadOnlyList<Tile> Tiles => _board.ToFlatPlayableTiles();

		public AbilityProfile Profile { get; private set; } = profile;

		public void SetProfile(AbilityProfile profile)
		{
			Profile = profile;
			_solver = new Solver(_dictionary, profile);
		}

		public void SetLoadout(Loadout loadout)
		{
			loadout.Normalize();
			if (!LoadoutValidator.TryValidate(loadout, out string? error))
			{
				throw new InvalidOperationException(error);
			}

			Profile.Loadout = loadout;
			_solver = new(_dictionary, Profile);
		}

		public void SetFight(FightContext? fight)
		{
			Profile.Fight = fight;
			_solver = new(_dictionary, Profile);
		}

		public void SetSession(SessionContext session)
		{
			Profile.Session = session;
			_solver = new(_dictionary, Profile);
		}

		public void SetBoard(GridBoard board) => _board = board;

		public void SetBoard(IReadOnlyList<Tile> tiles) => _board = GridBoard.FromFlatTiles(tiles);

		public void ClearBoard() => _board = new GridBoard();

		public IReadOnlyList<WordResult> GetSuggestions(int topCount = 25)
		{
			(List<Tile> pool, int[] gridIndices) = _board.GetPlayableTiles();
			if (pool.Count == 0)
			{
				return [];
			}

			return _solver.FindWords(pool, topCount)
				.Select(result => result with
				{
					UsedGridMask = GridBoard.MapPoolMaskToGridMask(result.UsedMask, gridIndices)
				})
				.ToList();
		}

		public bool TrySelectPlayedWord(
			string input,
			IReadOnlyList<WordResult> suggestions,
			out WordResult word)
		{
			input = input.Trim();
			if (int.TryParse(input, out int selection)
				&& selection >= 1
				&& selection <= suggestions.Count)
			{
				word = suggestions[selection - 1];
				return true;
			}

			string normalized = input.ToLowerInvariant();
			for (int index = 0; index < suggestions.Count; index++)
			{
				if (suggestions[index].Word.Equals(normalized, StringComparison.Ordinal))
				{
					word = suggestions[index];
					return true;
				}
			}

			(List<Tile> pool, int[] gridIndices) = _board.GetPlayableTiles();
			if (!_solver.TryFindBestWord(pool, normalized, out WordResult found))
			{
				word = default;
				return false;
			}

			word = found with
			{
				UsedGridMask = GridBoard.MapPoolMaskToGridMask(found.UsedMask, gridIndices)
			};
			return true;
		}

		public void ApplyPlayedWord(WordResult playedWord, IReadOnlyList<GridCell> replacements)
		{
			int usedGridMask = playedWord.UsedGridMask != 0
				? playedWord.UsedGridMask
				: playedWord.UsedMask;
			_board = _board.ApplyPlayedWord(usedGridMask, replacements);
		}

		public void ApplyPlayedWord(WordResult playedWord, IReadOnlyList<Tile> dropInTiles)
		{
			if (playedWord.UsedGridMask != 0)
			{
				List<GridCell> replacements = dropInTiles.Select(GridCell.FromTile).ToList();
				_board = _board.ApplyPlayedWord(playedWord.UsedGridMask, replacements);
				return;
			}

			List<Tile> pool = _board.ToFlatPlayableTiles();
			_board = GridBoard.FromFlatTiles(TileBoard.ApplyPlayedWord(pool, playedWord.UsedMask, dropInTiles));
		}
	}
}
