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

    // Castling rights
    private bool whiteKingMoved = false;
    private bool blackKingMoved = false;
    private bool whiteKingsideRookMoved = false;
    private bool whiteQueensideRookMoved = false;
    private bool blackKingsideRookMoved = false;
    private bool blackQueensideRookMoved = false;

    public void StartNewGame()
    {
        Board.Clear();
        Board.SetupInitialPosition();
        IsWhiteTurn = true;
        MoveHistory.Clear();
        IsGameOver = false;
        EnPassantTarget = null;
        // Reset castling rights
        whiteKingMoved = blackKingMoved = false;
        whiteKingsideRookMoved = whiteQueensideRookMoved = false;
        blackKingsideRookMoved = blackQueensideRookMoved = false;
        // Set King.ChessGameRef for castling
        King.ChessGameRef = this;
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
        var target = Board[toRow, toCol];
        if (target is King) return false;
        // --- Castling logic ---
        if (piece is King && Math.Abs(toCol - fromCol) == 2 && fromRow == toRow)
        {
            // Kingside
            if (toCol == 6 && CanCastleKingside(piece.IsWhite))
            {
                if (!IsInCheck(piece.IsWhite) &&
                    !IsSquareAttacked(fromRow, 5, !piece.IsWhite) &&
                    !IsSquareAttacked(fromRow, 6, !piece.IsWhite))
                {
                    // Move king
                    Board.MovePiece(fromRow, fromCol, toRow, 6);
                    // Move rook
                    Board.MovePiece(fromRow, 7, toRow, 5);
                    // Update castling rights
                    var move = new Move(fromRow, fromCol, toRow, 6, null,
                        whiteKingMoved, blackKingMoved, whiteKingsideRookMoved, whiteQueensideRookMoved, blackKingsideRookMoved, blackQueensideRookMoved);
                    if (piece.IsWhite) { whiteKingMoved = true; whiteKingsideRookMoved = true; }
                    else { blackKingMoved = true; blackKingsideRookMoved = true; }
                    MoveHistory.Add(move);
                    IsWhiteTurn = !IsWhiteTurn;
                    if (IsCheckmate(!IsWhiteTurn)) IsGameOver = true;
                    else if (IsStalemate(!IsWhiteTurn)) IsGameOver = true;
                    return true;
                }
                return false;
            }
            // Queenside
            if (toCol == 2 && CanCastleQueenside(piece.IsWhite))
            {
                if (!IsInCheck(piece.IsWhite) &&
                    !IsSquareAttacked(fromRow, 3, !piece.IsWhite) &&
                    !IsSquareAttacked(fromRow, 2, !piece.IsWhite))
                {
                    // Move king
                    Board.MovePiece(fromRow, fromCol, toRow, 2);
                    // Move rook
                    Board.MovePiece(fromRow, 0, toRow, 3);
                    // Update castling rights
                    var move = new Move(fromRow, fromCol, toRow, 2, null,
                        whiteKingMoved, blackKingMoved, whiteKingsideRookMoved, whiteQueensideRookMoved, blackKingsideRookMoved, blackQueensideRookMoved);
                    if (piece.IsWhite) { whiteKingMoved = true; whiteQueensideRookMoved = true; }
                    else { blackKingMoved = true; blackQueensideRookMoved = true; }
                    MoveHistory.Add(move);
                    IsWhiteTurn = !IsWhiteTurn;
                    if (IsCheckmate(!IsWhiteTurn)) IsGameOver = true;
                    else if (IsStalemate(!IsWhiteTurn)) IsGameOver = true;
                    return true;
                }
                return false;
            }
        }
        // En passant capture
        Piece? captured = Board[toRow, toCol];
        if (piece is Pawn && EnPassantTarget.HasValue && toRow == EnPassantTarget.Value.row && toCol == EnPassantTarget.Value.col && Board[toRow, toCol] == null)
        {
            int capturedRow = piece.IsWhite ? toRow + 1 : toRow - 1;
            captured = Board[capturedRow, toCol];
            Board.Board[capturedRow, toCol] = null;
        }
        Board.MovePiece(fromRow, fromCol, toRow, toCol);
        // Update castling rights after any king or rook move
        var moveNormal = new Move(fromRow, fromCol, toRow, toCol, captured,
            whiteKingMoved, blackKingMoved, whiteKingsideRookMoved, whiteQueensideRookMoved, blackKingsideRookMoved, blackQueensideRookMoved);
        if (piece is King)
        {
            if (piece.IsWhite) whiteKingMoved = true;
            else blackKingMoved = true;
        }
        if (piece is Rook)
        {
            if (piece.IsWhite)
            {
                if (fromRow == 7 && fromCol == 0) whiteQueensideRookMoved = true;
                if (fromRow == 7 && fromCol == 7) whiteKingsideRookMoved = true;
            }
            else
            {
                if (fromRow == 0 && fromCol == 0) blackQueensideRookMoved = true;
                if (fromRow == 0 && fromCol == 7) blackKingsideRookMoved = true;
            }
        }
        bool leavesOwnKingInCheck = IsInCheck(piece.IsWhite);
        if (leavesOwnKingInCheck)
        {
            Board.UndoMove(moveNormal);
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
            MoveHistory.Add(moveNormal);
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
        MoveHistory.Add(moveNormal);
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
        // (No change needed, as it calls the above)
        return TryMove(fromRow, fromCol, toRow, toCol);
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
        // Restore castling rights
        whiteKingMoved = last.WhiteKingMoved;
        blackKingMoved = last.BlackKingMoved;
        whiteKingsideRookMoved = last.WhiteKingsideRookMoved;
        whiteQueensideRookMoved = last.WhiteQueensideRookMoved;
        blackKingsideRookMoved = last.BlackKingsideRookMoved;
        blackQueensideRookMoved = last.BlackQueensideRookMoved;
        MoveHistory.RemoveAt(MoveHistory.Count - 1);
        IsWhiteTurn = !IsWhiteTurn;
        EnPassantTarget = null; // Reset en passant target
    }

    // Instance methods for castling validation
    public bool CanCastleKingside(bool isWhite)
    {
        int row = isWhite ? 7 : 0;
        if (isWhite)
        {
            if (whiteKingMoved || whiteKingsideRookMoved) return false;
            if (Board.Board[row, 4] is not King k || !k.IsWhite) return false;
            if (Board.Board[row, 7] is not Rook r || !r.IsWhite) return false;
        }
        else
        {
            if (blackKingMoved || blackKingsideRookMoved) return false;
            if (Board.Board[row, 4] is not King k || k.IsWhite) return false;
            if (Board.Board[row, 7] is not Rook r || r.IsWhite) return false;
        }
        if (Board.Board[row, 5] != null || Board.Board[row, 6] != null) return false;
        return true;
    }
    public bool CanCastleQueenside(bool isWhite)
    {
        int row = isWhite ? 7 : 0;
        if (isWhite)
        {
            if (whiteKingMoved || whiteQueensideRookMoved) return false;
            if (Board.Board[row, 4] is not King k || !k.IsWhite) return false;
            if (Board.Board[row, 0] is not Rook r || !r.IsWhite) return false;
        }
        else
        {
            if (blackKingMoved || blackQueensideRookMoved) return false;
            if (Board.Board[row, 4] is not King k || k.IsWhite) return false;
            if (Board.Board[row, 0] is not Rook r || r.IsWhite) return false;
        }
        if (Board.Board[row, 1] != null || Board.Board[row, 2] != null || Board.Board[row, 3] != null) return false;
        return true;
    }
}
