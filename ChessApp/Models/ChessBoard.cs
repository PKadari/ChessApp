namespace ChessApp.Models;

public class ChessBoard
{
    public Piece?[,] Board { get; } = new Piece[8, 8];
    public Piece? this[int row, int col] => Board[row, col];
    public bool IsInBounds(int row, int col) => row >= 0 && row < 8 && col >= 0 && col < 8;

    public void SetupInitialPosition()
    {
        // Place all pieces in starting positions
        for (int i = 0; i < 8; i++)
        {
            Board[1, i] = new Pawn(false);
            Board[6, i] = new Pawn(true);
        }
        Board[0, 0] = new Rook(false); Board[0, 7] = new Rook(false);
        Board[0, 1] = new Knight(false); Board[0, 6] = new Knight(false);
        Board[0, 2] = new Bishop(false); Board[0, 5] = new Bishop(false);
        Board[0, 3] = new Queen(false); Board[0, 4] = new King(false);
        Board[7, 0] = new Rook(true); Board[7, 7] = new Rook(true);
        Board[7, 1] = new Knight(true); Board[7, 6] = new Knight(true);
        Board[7, 2] = new Bishop(true); Board[7, 5] = new Bishop(true);
        Board[7, 3] = new Queen(true); Board[7, 4] = new King(true);
        // Empty squares
        for (int r = 2; r <= 5; r++)
            for (int c = 0; c < 8; c++)
                Board[r, c] = null;
    }

    public void Clear()
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                Board[r, c] = null;
    }

    public void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
    {
        Board[toRow, toCol] = Board[fromRow, fromCol];
        Board[fromRow, fromCol] = null;
    }

    public void UndoMove(Move move)
    {
        Board[move.FromRow, move.FromCol] = Board[move.ToRow, move.ToCol];
        Board[move.ToRow, move.ToCol] = move.CapturedPiece;
    }
}
