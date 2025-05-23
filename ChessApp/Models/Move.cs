namespace ChessApp.Models;

public class Move
{
    public int FromRow { get; }
    public int FromCol { get; }
    public int ToRow { get; }
    public int ToCol { get; }
    public Piece? CapturedPiece { get; }
    public bool WhiteKingMoved { get; }
    public bool BlackKingMoved { get; }
    public bool WhiteKingsideRookMoved { get; }
    public bool WhiteQueensideRookMoved { get; }
    public bool BlackKingsideRookMoved { get; }
    public bool BlackQueensideRookMoved { get; }

    public Move(int fromRow, int fromCol, int toRow, int toCol, Piece? capturedPiece,
        bool whiteKingMoved = false, bool blackKingMoved = false,
        bool whiteKingsideRookMoved = false, bool whiteQueensideRookMoved = false,
        bool blackKingsideRookMoved = false, bool blackQueensideRookMoved = false)
    {
        FromRow = fromRow; FromCol = fromCol; ToRow = toRow; ToCol = toCol; CapturedPiece = capturedPiece;
        WhiteKingMoved = whiteKingMoved;
        BlackKingMoved = blackKingMoved;
        WhiteKingsideRookMoved = whiteKingsideRookMoved;
        WhiteQueensideRookMoved = whiteQueensideRookMoved;
        BlackKingsideRookMoved = blackKingsideRookMoved;
        BlackQueensideRookMoved = blackQueensideRookMoved;
    }
}
