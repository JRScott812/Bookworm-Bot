namespace Bookworm_Bot_Class
{
	public interface IGameBoardReader
	{
		bool TryReadBoard(out GridBoard board);
	}
}
