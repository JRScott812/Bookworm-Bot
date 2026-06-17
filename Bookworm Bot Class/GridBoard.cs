using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bookworm_Bot_Class
{
	public sealed class GridBoard
	{
		public const int Size = 4;
		public const int CellCount = Size * Size;

		private readonly GridCell[] _cells = new GridCell[CellCount];

		public GridCell GetCell(int index) => _cells[index];

		public void SetCell(int index, GridCell cell) => _cells[index] = cell;

		public int PlayableCount => _cells.Count(cell => cell.IsPlayable);

		public int FilledCount => _cells.Count(cell => !cell.IsEmpty);

		public bool IsFull => FilledCount == CellCount;

		public (List<Tile> Tiles, int[] GridIndices) GetPlayableTiles()
		{
			List<Tile> tiles = [];
			List<int> gridIndices = [];
			for (int index = 0; index < CellCount; index++)
			{
				if (!_cells[index].IsPlayable)
				{
					continue;
				}

				tiles.Add(_cells[index].ToTile());
				gridIndices.Add(index);
			}

			return (tiles, [.. gridIndices]);
		}

		public static int MapPoolMaskToGridMask(int poolUsedMask, int[] gridIndices)
		{
			int gridMask = 0;
			for (int poolIndex = 0; poolIndex < gridIndices.Length; poolIndex++)
			{
				if ((poolUsedMask & (1 << poolIndex)) != 0)
				{
					gridMask |= 1 << gridIndices[poolIndex];
				}
			}

			return gridMask;
		}

		public static int CountUsedCells(int usedGridMask) => BitOperations.PopCount((uint)usedGridMask);

		public GridBoard ApplyRemovedWord(int usedGridMask)
		{
			if (usedGridMask == 0)
			{
				return Clone();
			}

			GridBoard next = Clone();
			for (int column = 0; column < Size; column++)
			{
				List<GridCell> remaining = [];
				for (int row = 0; row < Size; row++)
				{
					int index = ToIndex(row, column);
					if ((usedGridMask & (1 << index)) == 0 && !next._cells[index].IsEmpty)
					{
						remaining.Add(next._cells[index]);
					}
				}

				int emptyCount = Size - remaining.Count;
				for (int row = 0; row < Size; row++)
				{
					int index = ToIndex(row, column);
					next._cells[index] = row < emptyCount
						? GridCell.Empty
						: remaining[row - emptyCount];
				}
			}

			return next;
		}

		public static int GetEmptyCellMask(GridBoard board)
		{
			int mask = 0;
			for (int index = 0; index < CellCount; index++)
			{
				if (board.GetCell(index).IsEmpty)
				{
					mask |= 1 << index;
				}
			}

			return mask;
		}

		public GridBoard ApplyPlayedWord(int usedGridMask, IReadOnlyList<GridCell> replacements)
		{
			GridBoard next = Clone();
			List<int> usedIndices = [];
			for (int index = 0; index < CellCount; index++)
			{
				if ((usedGridMask & (1 << index)) != 0)
				{
					usedIndices.Add(index);
				}
			}

			Dictionary<int, GridCell> replacementByIndex = new();
			for (int index = 0; index < usedIndices.Count; index++)
			{
				if (index >= replacements.Count)
				{
					throw new InvalidOperationException("Missing drop-in tile for a used cell.");
				}

				GridCell replacement = replacements[index];
				if (replacement.IsEmpty)
				{
					throw new InvalidOperationException("Drop-in tiles must have letters.");
				}

				replacementByIndex[usedIndices[index]] = replacement;
			}

			for (int column = 0; column < Size; column++)
			{
				List<GridCell> dropIns = [];
				List<GridCell> remaining = [];
				for (int row = 0; row < Size; row++)
				{
					int index = ToIndex(row, column);
					if ((usedGridMask & (1 << index)) != 0)
					{
						dropIns.Add(replacementByIndex[index]);
					}
					else
					{
						remaining.Add(next._cells[index]);
					}
				}

				List<GridCell> columnCells = [.. dropIns, .. remaining];
				if (columnCells.Count != Size)
				{
					throw new InvalidOperationException("Column tile count mismatch after applying a word.");
				}

				for (int row = 0; row < Size; row++)
				{
					next._cells[ToIndex(row, column)] = columnCells[row];
				}
			}

			return next;
		}

		public static int ToIndex(int row, int column) => row * Size + column;

		public static int GetRow(int index) => index / Size;

		public static int GetColumn(int index) => index % Size;

		public static GridBoard FromFlatTiles(IReadOnlyList<Tile> tiles)
		{
			GridBoard board = new();
			for (int index = 0; index < Math.Min(tiles.Count, CellCount); index++)
			{
				board._cells[index] = GridCell.FromTile(tiles[index]);
			}

			return board;
		}

		public List<Tile> ToFlatPlayableTiles() => GetPlayableTiles().Tiles;

		public GridBoard Clone()
		{
			GridBoard copy = new();
			Array.Copy(_cells, copy._cells, CellCount);
			return copy;
		}

		public void Clear() => Array.Fill(_cells, GridCell.Empty);
	}
}
