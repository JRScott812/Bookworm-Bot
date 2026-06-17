using System;
using System.Collections.Generic;
using System.Drawing;

namespace Bookworm_Bot_Automation
{
	public sealed class AutomationConfig
	{
		public int ClientWidth { get; set; }
		public int ClientHeight { get; set; }
		public int BoardLeft { get; set; }
		public int BoardTop { get; set; }
		public int BoardRight { get; set; }
		public int BoardBottom { get; set; }
		public int CellInset { get; set; } = 2;

		public string ResolutionKey => $"{ClientWidth}x{ClientHeight}";

		public int CellWidth
		{
			get
			{
				int width = BoardRight - BoardLeft;
				return width > 0 ? width / GridBoardSize : 0;
			}
		}

		public int CellHeight
		{
			get
			{
				int height = BoardBottom - BoardTop;
				return height > 0 ? height / GridBoardSize : 0;
			}
		}

		public const int GridBoardSize = 4;
		public const int MinimumCellSize = 24;

		public bool IsValid =>
			ClientWidth > 0
			&& ClientHeight > 0
			&& BoardRight > BoardLeft
			&& BoardBottom > BoardTop
			&& CellWidth >= MinimumCellSize
			&& CellHeight >= MinimumCellSize
			&& FitsWithinClient();

		public bool FitsWithinClient() =>
			BoardLeft >= 0
			&& BoardTop >= 0
			&& BoardRight <= ClientWidth
			&& BoardBottom <= ClientHeight;

		public string DescribeValidationIssue()
		{
			if (ClientWidth <= 0 || ClientHeight <= 0)
			{
				return "Client size is invalid.";
			}

			if (BoardRight <= BoardLeft || BoardBottom <= BoardTop)
			{
				return "Board corners are in the wrong order (right/bottom must be below left/top).";
			}

			if (!FitsWithinClient())
			{
				return $"Board bounds ({BoardLeft},{BoardTop})-({BoardRight},{BoardBottom}) are outside the {ClientWidth}x{ClientHeight} game window.";
			}

			if (CellWidth < MinimumCellSize || CellHeight < MinimumCellSize)
			{
				return $"Board area is too small ({CellWidth}x{CellHeight} per cell). Click the outer corners of the full 4x4 grid, not a single tile.";
			}

			return "Unknown validation issue.";
		}

		public Rectangle GetCellRect(int index)
		{
			if (index is < 0 or >= GridBoardSize * GridBoardSize)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			int row = index / GridBoardSize;
			int column = index % GridBoardSize;
			int boardWidth = BoardRight - BoardLeft;
			int boardHeight = BoardBottom - BoardTop;
			int cellLeft = BoardLeft + ((column * boardWidth) / GridBoardSize);
			int cellRight = BoardLeft + (((column + 1) * boardWidth) / GridBoardSize);
			int cellTop = BoardTop + ((row * boardHeight) / GridBoardSize);
			int cellBottom = BoardTop + (((row + 1) * boardHeight) / GridBoardSize);
			int left = cellLeft + CellInset;
			int top = cellTop + CellInset;
			int width = Math.Max(1, (cellRight - cellLeft) - (CellInset * 2));
			int height = Math.Max(1, (cellBottom - cellTop) - (CellInset * 2));
			return new Rectangle(left, top, width, height);
		}

		public IReadOnlyList<Rectangle> GetAllCellRects()
		{
			Rectangle[] rects = new Rectangle[GridBoardSize * GridBoardSize];
			for (int index = 0; index < rects.Length; index++)
			{
				rects[index] = GetCellRect(index);
			}

			return rects;
		}

		public static AutomationConfig FromBoardBounds(int clientWidth, int clientHeight, int left, int top, int right, int bottom, int cellInset = 4) =>
			new()
			{
				ClientWidth = clientWidth,
				ClientHeight = clientHeight,
				BoardLeft = left,
				BoardTop = top,
				BoardRight = right,
				BoardBottom = bottom,
				CellInset = cellInset
			};
	}
}
