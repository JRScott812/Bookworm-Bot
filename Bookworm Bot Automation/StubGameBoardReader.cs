using Bookworm_Bot_Class;

namespace Bookworm_Bot_Automation
{
	/// <summary>
	/// Placeholder until screen capture and tile recognition are implemented.
	/// </summary>
	public sealed class StubGameBoardReader : IGameBoardReader
	{
		public bool TryReadBoard(out GridBoard board)
		{
			board = new GridBoard();
			return false;
		}
	}
}
