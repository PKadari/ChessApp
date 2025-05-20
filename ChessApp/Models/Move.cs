namespace ChessApp.Models;

public class Move
{
    public int FromRow { get; }
    public int FromCol { get; }
    public int ToRow { get; }
    public int ToCol { get; }
    public Piece? CapturedPiece { get; }
    public Move(int fromRow, int fromCol, int toRow, int toCol, Piece? capturedPiece)
    {
        FromRow = fromRow; FromCol = fromCol; ToRow = toRow; ToCol = toCol; CapturedPiece = capturedPiece;
    }
}
