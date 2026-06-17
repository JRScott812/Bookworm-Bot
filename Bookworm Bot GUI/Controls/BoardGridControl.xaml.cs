using System;
using System.Collections.Generic;

using Bookworm_Bot_Class;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Bookworm_Bot_GUI.Controls
{
	public sealed partial class BoardGridControl : UserControl
	{
		private readonly TileCellControl[] _cells = new TileCellControl[GridBoard.CellCount];
		private GridBoard _board = new();
		private int _highlightMask;
		private int _dropInMask;

		public bool IsDropInMode => _dropInMask != 0;

		public event EventHandler<int>? StatusEditRequested;
		public event EventHandler? BoardChanged;

		public BoardGridControl()
		{
			InitializeComponent();
			BuildGrid();
		}

		public GridBoard Board => _board;

		public int HighlightMask
		{
			get => _highlightMask;
			set
			{
				_highlightMask = value;
				RefreshVisuals();
			}
		}

		public void SetBoard(GridBoard board)
		{
			_board = board;
			RefreshVisuals();
		}

		public void RefreshAppearance() => RefreshVisuals();

		public GridBoard GetBoard() => _board.Clone();

		public int DropInMask => _dropInMask;

		public void ClearBoard()
		{
			_board = new GridBoard();
			_highlightMask = 0;
			_dropInMask = 0;
			RefreshVisuals();
		}

		public async System.Threading.Tasks.Task BeginWordPlayAsync(int usedGridMask)
		{
			GridBoard before = _board.Clone();
			GridBoard after = before.ApplyRemovedWord(usedGridMask);
			Dictionary<int, double> fallOffsets = ComputeFallOffsets(before, usedGridMask);

			_board = after;
			_dropInMask = GridBoard.GetEmptyCellMask(after);
			_highlightMask = _dropInMask;
			RefreshVisuals();

			List<System.Threading.Tasks.Task> animations = [];
			foreach ((int index, double offsetY) in fallOffsets)
			{
				if (offsetY != 0)
				{
					animations.Add(_cells[index].PlayDropAnimationAsync(offsetY));
				}
			}

			if (animations.Count > 0)
			{
				await System.Threading.Tasks.Task.WhenAll(animations);
			}
		}

		public void BeginWordPlay(int usedGridMask)
		{
			// Never block the UI thread waiting on animations.
			_ = BeginWordPlayAsync(usedGridMask);
		}

		public async System.Threading.Tasks.Task CompleteWordPlayAsync()
		{
			List<System.Threading.Tasks.Task> animations = [];
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				if ((_dropInMask & (1 << index)) == 0 || _board.GetCell(index).IsEmpty)
				{
					continue;
				}

				int row = GridBoard.GetRow(index);
				double offsetY = -(row + 1) * CellStep;
				animations.Add(_cells[index].PlayDropAnimationAsync(offsetY));
			}

			_highlightMask = 0;
			_dropInMask = 0;
			RefreshVisuals();

			if (animations.Count > 0)
			{
				await System.Threading.Tasks.Task.WhenAll(animations);
			}
		}

		public void EndWordPlay()
		{
			_highlightMask = 0;
			_dropInMask = 0;
			RefreshVisuals();
		}

		public void SetCell(int index, GridCell cell)
		{
			_board.SetCell(index, cell);
			RefreshCell(index);
		}

		public async System.Threading.Tasks.Task ApplyPlayedWordAsync(
			int usedGridMask,
			IReadOnlyList<GridCell> replacements)
		{
			GridBoard before = _board.Clone();
			GridBoard next = before.ApplyPlayedWord(usedGridMask, replacements);
			Dictionary<int, double> dropOffsets = ComputeFallOffsets(before, usedGridMask);

			_board = next;
			_highlightMask = 0;
			_dropInMask = 0;
			RefreshVisuals();

			List<System.Threading.Tasks.Task> animations = [];
			foreach ((int index, double offsetY) in dropOffsets)
			{
				if (offsetY != 0)
				{
					animations.Add(_cells[index].PlayDropAnimationAsync(offsetY));
				}
			}

			if (animations.Count > 0)
			{
				await System.Threading.Tasks.Task.WhenAll(animations);
			}
		}

		private const double CellStep = 78;

		private static Dictionary<int, double> ComputeFallOffsets(
			GridBoard before,
			int usedGridMask)
		{
			Dictionary<int, double> offsets = new();
			for (int column = 0; column < GridBoard.Size; column++)
			{
				int emptyCount = 0;
				for (int row = 0; row < GridBoard.Size; row++)
				{
					int index = GridBoard.ToIndex(row, column);
					if ((usedGridMask & (1 << index)) != 0)
					{
						emptyCount++;
					}
				}

				int survivorIndex = 0;
				for (int row = 0; row < GridBoard.Size; row++)
				{
					int oldIndex = GridBoard.ToIndex(row, column);
					if ((usedGridMask & (1 << oldIndex)) != 0 || before.GetCell(oldIndex).IsEmpty)
					{
						continue;
					}

					int newRow = emptyCount + survivorIndex;
					survivorIndex++;
					if (newRow == row)
					{
						continue;
					}

					int newIndex = GridBoard.ToIndex(newRow, column);
					offsets[newIndex] = (row - newRow) * CellStep;
				}
			}

			return offsets;
		}

		private void BuildGrid()
		{
			for (int row = 0; row < GridBoard.Size; row++)
			{
				RowDefinition rowDefinition = new() { Height = GridLength.Auto };
				CellGrid.RowDefinitions.Add(rowDefinition);
			}

			for (int column = 0; column < GridBoard.Size; column++)
			{
				ColumnDefinition columnDefinition = new() { Width = GridLength.Auto };
				CellGrid.ColumnDefinitions.Add(columnDefinition);
			}

			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				TileCellControl cell = new() { CellIndex = index };
				cell.LetterCommitted += OnCellLetterCommitted;
				cell.StatusEditRequested += (_, cellIndex) => StatusEditRequested?.Invoke(this, cellIndex);
				Grid.SetRow(cell, index / GridBoard.Size);
				Grid.SetColumn(cell, index % GridBoard.Size);
				CellGrid.Children.Add(cell);
				_cells[index] = cell;
			}

			LegendText.Text =
				"Type a letter in each tile · click tile border for gem/status · " +
				$"{EmojiCatalog.ForModifier(TileModifier.Locked)} locked";
		}

		private void OnCellLetterCommitted(object? sender, TileCellLetterEventArgs e)
		{
			GridCell current = _board.GetCell(e.CellIndex);
			if (!TileCellControl.TryParseLetter(e.Text, out char? letter))
			{
				RefreshCell(e.CellIndex);
				return;
			}

			if (IsDropInMode && (_dropInMask & (1 << e.CellIndex)) == 0)
			{
				RefreshCell(e.CellIndex);
				return;
			}

			if (letter is null)
			{
				if (IsDropInMode && (_dropInMask & (1 << e.CellIndex)) != 0)
				{
					_board.SetCell(e.CellIndex, GridCell.Empty);
					RefreshCell(e.CellIndex);
					BoardChanged?.Invoke(this, EventArgs.Empty);
				}
				else
				{
					RefreshCell(e.CellIndex);
				}

				return;
			}

			if (!IsDropInMode && current.IsEmpty)
			{
				RefreshCell(e.CellIndex);
				return;
			}

			GridCell updated = new(letter.Value, GemType.None, TileModifier.None);

			if (updated == current)
			{
				return;
			}

			_board.SetCell(e.CellIndex, updated);
			RefreshCell(e.CellIndex);
			BoardChanged?.Invoke(this, EventArgs.Empty);
		}

		private void RefreshVisuals()
		{
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				RefreshCell(index);
			}
		}

		private void RefreshCell(int index) =>
			_cells[index].UpdateVisual(_board.GetCell(index), (_highlightMask & (1 << index)) != 0);
	}
}
