namespace ChessApp.Models;

public class ChessGame
{
    public ChessBoard Board { get; } = new ChessBoard();
    public bool IsWhiteTurn { get; private set; } = true;
    public List<Move> MoveHistory { get; } = new();
    public bool IsGameOver { get; private set; } = false;

    // Track en passant target square (row, col)
    public (int row, int col)? EnPassantTarget { get; private set; } = null;

    public static (int row, int col)? CurrentEnPassantTarget { get; set; } = null;

    public void StartNewGame()
    {
        Board.Clear();
        Board.SetupInitialPosition();
        IsWhiteTurn = true;
        MoveHistory.Clear();
        IsGameOver = false;
        EnPassantTarget = null;
    }

    public bool IsInCheck(bool white)
    {
        // Find king
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = Board[r, c];
                if (piece is King king && king.IsWhite == white)
                {
                    return IsSquareAttacked(r, c, !white);
                }
            }
        return false;
    }

    public bool IsCheckmate(bool white)
    {
        if (!IsInCheck(white)) return false;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = Board[r, c];
                if (piece != null && piece.IsWhite == white)
                {
                    var moves = piece.GetLegalMoves(Board, r, c);
                    foreach (var (tr, tc) in moves)
                    {
                        var captured = Board[tr, tc];
                        Board.MovePiece(r, c, tr, tc);
                        bool stillInCheck = IsInCheck(white);
                        Board.UndoMove(new Move(r, c, tr, tc, captured));
                        if (!stillInCheck) return false;
                    }
                }
            }
        return true;
    }

    public bool IsStalemate(bool white)
    {
        if (IsInCheck(white)) return false;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = Board[r, c];
                if (piece != null && piece.IsWhite == white)
                {
                    var moves = piece.GetLegalMoves(Board, r, c);
                    foreach (var (tr, tc) in moves)
                    {
                        var captured = Board[tr, tc];
                        Board.MovePiece(r, c, tr, tc);
                        bool inCheck = IsInCheck(white);
                        Board.UndoMove(new Move(r, c, tr, tc, captured));
                        if (!inCheck) return false;
                    }
                }
            }
        return true;
    }

    public bool TryMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        CurrentEnPassantTarget = EnPassantTarget;

        if (IsGameOver) return false;
        var piece = Board[fromRow, fromCol];
        if (piece == null || piece.IsWhite != IsWhiteTurn) return false;
        var legalMoves = piece.GetLegalMoves(Board, fromRow, fromCol);
        // Prevent capturing the enemy king
        var target = Board[toRow, toCol];
        if (target is King) return false;
        if (!legalMoves.Contains((toRow, toCol))) return false;
        // En passant capture
        Piece? captured = Board[toRow, toCol];
        if (piece is Pawn && EnPassantTarget.HasValue && toRow == EnPassantTarget.Value.row && toCol == EnPassantTarget.Value.col && Board[toRow, toCol] == null)
        {
            int capturedRow = piece.IsWhite ? toRow + 1 : toRow - 1;
            captured = Board[capturedRow, toCol];
            Board.Board[capturedRow, toCol] = null;
        }
        Board.MovePiece(fromRow, fromCol, toRow, toCol);
        bool leavesOwnKingInCheck = IsInCheck(piece.IsWhite);
        if (leavesOwnKingInCheck)
        {
            Board.UndoMove(new Move(fromRow, fromCol, toRow, toCol, captured));
            // Restore en passant state
            return false;
        }
        // Pawn promotion (auto-queen for now, UI can override)
        if (piece is Pawn && (toRow == 0 || toRow == 7))
        {
            Board.Board[toRow, toCol] = new Queen(piece.IsWhite);
            // After promotion, do NOT clear the original square again (already moved)
            // Always switch turn after promotion
            IsWhiteTurn = !IsWhiteTurn;
            MoveHistory.Add(new Move(fromRow, fromCol, toRow, toCol, captured));
            if (IsCheckmate(!IsWhiteTurn))
            {
                IsGameOver = true;
            }
            else if (IsStalemate(!IsWhiteTurn))
            {
                IsGameOver = true;
            }
            return true;
        }
        // Set en passant target
        if (piece is Pawn && Math.Abs(toRow - fromRow) == 2)
        {
            // The en passant target should be the square the pawn passed over, not the destination
            int epRow = (fromRow + toRow) / 2;
            EnPassantTarget = (epRow, toCol);
        }
        else
        {
            EnPassantTarget = null;
        }
        MoveHistory.Add(new Move(fromRow, fromCol, toRow, toCol, captured));
        // Check/checkmate/stalemate
        if (IsCheckmate(!IsWhiteTurn))
        {
            IsGameOver = true;
        }
        else if (IsStalemate(!IsWhiteTurn))
        {
            IsGameOver = true;
        }
        // Always switch turn after a valid move
        IsWhiteTurn = !IsWhiteTurn;
        return true;
    }

    // Overload for pawn promotion
    public bool TryMove(int fromRow, int fromCol, int toRow, int toCol, Piece? promotion = null)
    {
        CurrentEnPassantTarget = EnPassantTarget;

        if (IsGameOver) return false;
        var piece = Board[fromRow, fromCol];
        if (piece == null || piece.IsWhite != IsWhiteTurn) return false;
        var legalMoves = piece.GetLegalMoves(Board, fromRow, fromCol);
        // Prevent capturing the enemy king
        var target = Board[toRow, toCol];
        if (target is King) return false;
        if (!legalMoves.Contains((toRow, toCol))) return false;
        // En passant capture
        Piece? captured = Board[toRow, toCol];
        if (piece is Pawn && EnPassantTarget.HasValue && toRow == EnPassantTarget.Value.row && toCol == EnPassantTarget.Value.col && Board[toRow, toCol] == null)
        {
            int capturedRow = piece.IsWhite ? toRow + 1 : toRow - 1;
            captured = Board[capturedRow, toCol];
            Board.Board[capturedRow, toCol] = null;
        }
        Board.MovePiece(fromRow, fromCol, toRow, toCol);
        bool leavesOwnKingInCheck = IsInCheck(piece.IsWhite);
        if (leavesOwnKingInCheck)
        {
            Board.UndoMove(new Move(fromRow, fromCol, toRow, toCol, captured));
            return false;
        }
        // Pawn promotion (UI-driven or auto-queen)
        if (piece is Pawn && (toRow == 0 || toRow == 7))
        {
            Board.Board[toRow, toCol] = promotion ?? new Queen(piece.IsWhite);
            IsWhiteTurn = !IsWhiteTurn;
            MoveHistory.Add(new Move(fromRow, fromCol, toRow, toCol, captured));
            if (IsCheckmate(!IsWhiteTurn))
            {
                IsGameOver = true;
            }
            else if (IsStalemate(!IsWhiteTurn))
            {
                IsGameOver = true;
            }
            return true;
        }
        // Set en passant target
        if (piece is Pawn && Math.Abs(toRow - fromRow) == 2)
        {
            // The en passant target should be the square the pawn passed over, not the destination
            int epRow = (fromRow + toRow) / 2;
            EnPassantTarget = (epRow, toCol);
        }
        else
        {
            EnPassantTarget = null;
        }
        MoveHistory.Add(new Move(fromRow, fromCol, toRow, toCol, captured));
        if (IsCheckmate(!IsWhiteTurn))
        {
            IsGameOver = true;
        }
        else if (IsStalemate(!IsWhiteTurn))
        {
            IsGameOver = true;
        }
        IsWhiteTurn = !IsWhiteTurn;
        return true;
    }

    private bool IsSquareAttacked(int row, int col, bool byWhite)
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = Board[r, c];
                if (piece != null && piece.IsWhite == byWhite)
                {
                    var moves = piece.GetLegalMoves(Board, r, c);
                    if (moves.Contains((row, col)))
                        return true;
                }
            }
        return false;
    }

    public void UndoLastMove()
    {
        if (MoveHistory.Count == 0) return;
        var last = MoveHistory[^1];
        Board.UndoMove(last);        
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        IsWhiteTurn = !IsWhiteTurn;
        EnPassantTarget = null; // Reset en passant target
    }
}
