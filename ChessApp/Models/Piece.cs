namespace ChessApp.Models;

public abstract class Piece
{
    public bool IsWhite { get; }
    public abstract string Name { get; }
    public Piece(bool isWhite) => IsWhite = isWhite;
    public abstract IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col);

    protected static IEnumerable<(int, int)> GetLinearMoves(ChessBoard board, int row, int col, (int dr, int dc)[] directions, bool isWhite)
    {
        var moves = new List<(int, int)>();
        foreach (var (dr, dc) in directions)
        {
            int nr = row + dr, nc = col + dc;
            while (board.IsInBounds(nr, nc))
            {
                var target = board[nr, nc];
                if (target == null)
                    moves.Add((nr, nc));
                else
                {
                    if (target.IsWhite != isWhite)
                        moves.Add((nr, nc));
                    break;
                }
                nr += dr;
                nc += dc;
            }
        }
        return moves;
    }
}

public class King : Piece
{
    public override string Name => IsWhite ? "king_white.svg" : "king_black.svg";
    public King(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        var moves = new List<(int, int)>();
        foreach (var (dr, dc) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) })
        {
            int nr = row + dr, nc = col + dc;
            if (board.IsInBounds(nr, nc))
            {
                var target = board[nr, nc];
                if (target == null || target.IsWhite != IsWhite)
                    moves.Add((nr, nc));
            }
        }
        // Castling handled in ChessBoard
        return moves;
    }
}

public class Queen : Piece
{
    public override string Name => IsWhite ? "queen_white.svg" : "queen_black.svg";
    public Queen(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        return GetLinearMoves(board, row, col, new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) }, IsWhite);
    }
}

public class Rook : Piece
{
    public override string Name => IsWhite ? "rook_white.svg" : "rook_black.svg";
    public Rook(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        return GetLinearMoves(board, row, col, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }, IsWhite);
    }
}

public class Bishop : Piece
{
    public override string Name => IsWhite ? "bishop_white.svg" : "bishop_black.svg";
    public Bishop(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        return GetLinearMoves(board, row, col, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }, IsWhite);
    }
}

public class Knight : Piece
{
    public override string Name => IsWhite ? "knight_white.svg" : "knight_black.svg";
    public Knight(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        var moves = new List<(int, int)>();
        foreach (var (dr, dc) in new[] { (2, 1), (1, 2), (-1, 2), (-2, 1), (-2, -1), (-1, -2), (1, -2), (2, -1) })
        {
            int nr = row + dr, nc = col + dc;
            if (board.IsInBounds(nr, nc))
            {
                var target = board[nr, nc];
                if (target == null || target.IsWhite != IsWhite)
                    moves.Add((nr, nc));
            }
        }
        return moves;
    }
}

public class Pawn : Piece
{
    public override string Name => IsWhite ? "pawn_white.svg" : "pawn_black.svg";
    public Pawn(bool isWhite) : base(isWhite) { }
    public override IEnumerable<(int row, int col)> GetLegalMoves(ChessBoard board, int row, int col)
    {
        var moves = new List<(int, int)>();
        int dir = IsWhite ? -1 : 1;
        int startRow = IsWhite ? 6 : 1;
        // Forward
        if (board.IsInBounds(row + dir, col) && board[row + dir, col] == null)
            moves.Add((row + dir, col));
        // Double move
        if (row == startRow && board[row + dir, col] == null && board[row + 2 * dir, col] == null)
            moves.Add((row + 2 * dir, col));
        // Captures
        foreach (int dc in new[] { -1, 1 })
        {
            int nr = row + dir, nc = col + dc;
            if (board.IsInBounds(nr, nc))
            {
                var target = board[nr, nc];
                if (target != null && target.IsWhite != IsWhite)
                    moves.Add((nr, nc));
            }
        }
        // En passant: handled in ChessGame, but expose a static property for the current target
        if (ChessGame.CurrentEnPassantTarget.HasValue)
        {
            var ep = ChessGame.CurrentEnPassantTarget.Value;
            int enPassantRow = IsWhite ? 4 : 3; // 5th rank for white (row 4), 4th for black (row 3)
            if (row == enPassantRow && ep.row == row + dir && Math.Abs(ep.col - col) == 1)
            {
                // The captured pawn must be a pawn of the opposite color and must have just moved two squares to land next to this pawn
                if (board[row, ep.col] is Pawn capturedPawn && capturedPawn.IsWhite != IsWhite)
                {
                    moves.Add((ep.row, ep.col));
                }
            }
        }
        return moves;
    }
}
