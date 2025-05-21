using Microsoft.Maui.Controls;
using ChessApp.Models;

namespace ChessApp;

public partial class MainPage : ContentPage
{
    private ChessGame game = new ChessGame();
    private (int row, int col)? selectedSquare = null;
    private List<(int row, int col)> highlightedSquares = new();
    private List<(int row, int col)> captureSquares = new();
    private (int row, int col)? kingInCheck = null;
    private List<(int row, int col)> lastMoveSquares = new();
    private StackLayout? moveHistoryPanel;
    private Grid? boardGrid;
    private Grid? parentGrid;
    private bool aiEnabled = true; // Set to true to enable AI opponent
    private bool aiIsWhite = false; // AI plays black by default
    private bool isGameStarted = false;
    private string selectedMode = "";

    public MainPage()
    {
        ShowInitialScreen();
    }

    private void ShowInitialScreen()
    {
        var title = new Label
        {
            Text = "ChessApp",
            FontSize = 36,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 40, 0, 20)
        };
        var pvpButton = new Button
        {
            Text = "Player vs Player",
            FontSize = 22,
            BackgroundColor = Colors.SteelBlue,
            TextColor = Colors.White,
            Margin = new Thickness(0, 10, 0, 10)
        };
        var pvaiButton = new Button
        {
            Text = "Player vs AI",
            FontSize = 22,
            BackgroundColor = Colors.DarkViolet,
            TextColor = Colors.White,
            Margin = new Thickness(0, 10, 0, 10)
        };
        pvpButton.Clicked += (s, e) =>
        {
            aiEnabled = false;
            aiIsWhite = false;
            selectedMode = "PvP";
            StartGameUI();
        };
        pvaiButton.Clicked += (s, e) =>
        {
            aiEnabled = true;
            aiIsWhite = false;
            selectedMode = "PvAI";
            StartGameUI();
        };
        var stack = new StackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 20,
            Children = { title, pvpButton, pvaiButton }
        };
        Content = stack;
    }

    private void StartGameUI()
    {
        isGameStarted = true;
        game = new ChessGame();
        game.StartNewGame();
        selectedSquare = null;
        highlightedSquares.Clear();
        captureSquares.Clear();
        kingInCheck = null;
        lastMoveSquares.Clear();
        boardGrid = new Grid
        {
            RowSpacing = 0,
            ColumnSpacing = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        for (int i = 0; i < 8; i++)
        {
            boardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            boardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        BuildChessBoard(boardGrid);
        moveHistoryPanel = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            WidthRequest = 120,
            BackgroundColor = Colors.WhiteSmoke,
            Padding = new Thickness(5),
            Spacing = 2
        };
        UpdateMoveHistoryPanel();
        parentGrid = new Grid
        {
            RowSpacing = 0,
            ColumnSpacing = 0,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        for (int i = 0; i < 10; i++)
        {
            parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }
        for (int i = 0; i < 10; i++)
        {
            parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        for (int col = 0; col < 8; col++)
        {
            var label = new Label
            {
                Text = ((char)('a' + col)).ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, col + 1);
            parentGrid.Children.Add(label);
        }
        for (int row = 0; row < 8; row++)
        {
            var label = new Label
            {
                Text = (8 - row).ToString(),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };
            Grid.SetRow(label, row + 1);
            Grid.SetColumn(label, 0);
            parentGrid.Children.Add(label);
        }
        Grid.SetRow(boardGrid, 1);
        Grid.SetColumn(boardGrid, 1);
        Grid.SetRowSpan(boardGrid, 8);
        Grid.SetColumnSpan(boardGrid, 8);
        parentGrid.Children.Add(boardGrid);
        Grid.SetRow(moveHistoryPanel, 1);
        Grid.SetColumn(moveHistoryPanel, 9);
        Grid.SetRowSpan(moveHistoryPanel, 8);
        parentGrid.Children.Add(moveHistoryPanel);
        var resetButton = new Button
        {
            Text = "Reset Game",
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        resetButton.Clicked += (s, e) =>
        {
            game.StartNewGame();
            BuildChessBoard(boardGrid!);
            UpdateMoveHistoryPanel();
            if (aiEnabled && aiIsWhite && !game.IsGameOver)
                _ = MakeAIMoveIfNeeded();
        };
        Grid.SetRow(resetButton, 9);
        Grid.SetColumn(resetButton, 1);
        Grid.SetColumnSpan(resetButton, 8);
        parentGrid.Children.Add(resetButton);
        var undoButton = new Button
        {
            Text = "Undo Move",
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.OrangeRed,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        undoButton.Clicked += (s, e) =>
        {
            game.UndoLastMove();
            BuildChessBoard(boardGrid!);
            UpdateMoveHistoryPanel();
        };
        Grid.SetRow(undoButton, 9);
        Grid.SetColumn(undoButton, 8);
        parentGrid.Children.Add(undoButton);
        var homeButton = new Button
        {
            Text = "Home",
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.DimGray,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        homeButton.Clicked += (s, e) =>
        {
            ShowInitialScreen();
        };
        Grid.SetRow(homeButton, 9);
        Grid.SetColumn(homeButton, 2);
        Grid.SetColumnSpan(homeButton, 2);
        parentGrid.Children.Add(homeButton);
        Content = parentGrid;
        if (aiEnabled && aiIsWhite && !game.IsGameOver)
            _ = MakeAIMoveIfNeeded();
    }

    private void BuildChessBoard(Grid boardGrid)
    {
        boardGrid.Children.Clear();
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                var isSelected = selectedSquare != null && selectedSquare.Value.row == row && selectedSquare.Value.col == col;
                var isHighlighted = highlightedSquares.Any(sq => sq.row == row && sq.col == col);
                var isCapture = captureSquares.Any(sq => sq.row == row && sq.col == col);
                var isCheck = kingInCheck != null && kingInCheck.Value.row == row && kingInCheck.Value.col == col;
                var isLastMove = lastMoveSquares.Any(sq => sq.row == row && sq.col == col);
                Color squareColor;
                if (isCheck)
                    squareColor = Colors.Red;
                else if (isLastMove)
                    squareColor = Colors.LightSkyBlue;
                else if (isSelected)
                    squareColor = Colors.Yellow;
                else if (isCapture)
                    squareColor = Colors.Orange;
                else if (isHighlighted)
                    squareColor = Colors.LightGreen;
                else
                    squareColor = (row + col) % 2 == 0 ? Colors.Bisque : Colors.SaddleBrown;
                var square = new Border
                {
                    Background = new SolidColorBrush(squareColor),
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1,
                    Margin = 0,
                    Padding = 0
                };
                Grid.SetRow(square, row);
                Grid.SetColumn(square, col);
                var tapGesture = new TapGestureRecognizer();
                int tappedRow = row, tappedCol = col;
                tapGesture.Tapped += (s, e) => OnSquareTapped(tappedRow, tappedCol, boardGrid);
                square.GestureRecognizers.Add(tapGesture);
                boardGrid.Children.Add(square);

                var piece = game.Board[row, col];
                if (piece != null)
                {
                    var imageName = piece.Name.Replace(".svg", "");
                    var image = new Image
                    {
                        Source = imageName,
                        Aspect = Aspect.AspectFit,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill
                    };
                    image.GestureRecognizers.Add(tapGesture);
                    Grid.SetRow(image, row);
                    Grid.SetColumn(image, col);
                    boardGrid.Children.Add(image);
                }
            }
        }
    }

    private async void OnSquareTapped(int row, int col, Grid boardGrid)
    {
        if (game.IsGameOver) return;
        var piece = game.Board[row, col];
        if (selectedSquare == null)
        {
            if (piece != null && piece.IsWhite == game.IsWhiteTurn)
            {
                selectedSquare = (row, col);
                var moves = piece.GetLegalMoves(game.Board, row, col).ToList();
                highlightedSquares = moves.Where(m => game.Board[m.row, m.col] == null).ToList();
                captureSquares = moves.Where(m => game.Board[m.row, m.col] != null && game.Board[m.row, m.col]!.IsWhite != piece.IsWhite).ToList();
                if (highlightedSquares.Count == 0 && captureSquares.Count == 0)
                    highlightedSquares.Add((row, col));
            }
            else
            {
                selectedSquare = null;
                highlightedSquares.Clear();
                captureSquares.Clear();
            }
        }
        else
        {
            if (highlightedSquares.Any(sq => sq.row == row && sq.col == col) || captureSquares.Any(sq => sq.row == row && sq.col == col))
            {
                var (fromRow, fromCol) = selectedSquare.Value;
                var movingPiece = game.Board[fromRow, fromCol];
                // Pawn promotion UI
                if (movingPiece is Pawn && (row == 0 || row == 7))
                {
                    string[] options = { "Queen", "Rook", "Bishop", "Knight" };
                    string result = await DisplayActionSheet("Promote pawn to:", null, null, options);
                    Piece promoted = result switch
                    {
                        "Rook" => new Rook(movingPiece.IsWhite),
                        "Bishop" => new Bishop(movingPiece.IsWhite),
                        "Knight" => new Knight(movingPiece.IsWhite),
                        _ => new Queen(movingPiece.IsWhite)
                    };
                    if (game.TryMove(fromRow, fromCol, row, col, promoted))
                    {
                        selectedSquare = null;
                        highlightedSquares.Clear();
                        captureSquares.Clear();
                        lastMoveSquares.Clear();
                        lastMoveSquares.Add((fromRow, fromCol));
                        lastMoveSquares.Add((row, col));
                        if (game.IsGameOver)
                        {
                            if (game.IsCheckmate(game.IsWhiteTurn))
                            {
                                bool goHome = await DisplayAlert("Checkmate!", $"{(game.IsWhiteTurn ? "White" : "Black")} is checkmated.", "Home", "OK");
                                if (goHome) { ShowInitialScreen(); return; }
                            }
                            else
                            {
                                bool goHome = await DisplayAlert("Stalemate!", "Draw by stalemate.", "Home", "OK");
                                if (goHome) { ShowInitialScreen(); return; }
                            }
                        }
                        else if (game.IsInCheck(game.IsWhiteTurn))
                        {
                            for (int r = 0; r < 8; r++)
                                for (int c = 0; c < 8; c++)
                                    if (game.Board[r, c] is King k && k.IsWhite == game.IsWhiteTurn)
                                        kingInCheck = (r, c);
                        }
                        else
                        {
                            kingInCheck = null;
                        }
                        UpdateMoveHistoryPanel();
                        BuildChessBoard(boardGrid);
                        // --- AI move after player move ---
                        await MakeAIMoveIfNeeded();
                        return;
                    }
                    BuildChessBoard(boardGrid);
                    return;
                }
                if (game.TryMove(fromRow, fromCol, row, col))
                {
                    selectedSquare = null;
                    highlightedSquares.Clear();
                    captureSquares.Clear();
                    // Last move highlight
                    lastMoveSquares.Clear();
                    lastMoveSquares.Add((fromRow, fromCol));
                    lastMoveSquares.Add((row, col));
                    // Show check/checkmate UI
                    if (game.IsGameOver)
                    {
                        if (game.IsCheckmate(game.IsWhiteTurn))
                        {
                            bool goHome = await DisplayAlert("Checkmate!", $"{(game.IsWhiteTurn ? "White" : "Black")} is checkmated.", "Home", "OK");
                            if (goHome) { ShowInitialScreen(); return; }
                        }
                        else
                        {
                            bool goHome = await DisplayAlert("Stalemate!", "Draw by stalemate.", "Home", "OK");
                            if (goHome) { ShowInitialScreen(); return; }
                        }
                    }
                    else if (game.IsInCheck(game.IsWhiteTurn))
                    {
                        for (int r = 0; r < 8; r++)
                            for (int c = 0; c < 8; c++)
                                if (game.Board[r, c] is King k && k.IsWhite == game.IsWhiteTurn)
                                    kingInCheck = (r, c);
                    }
                    else
                    {
                        kingInCheck = null;
                    }
                    UpdateMoveHistoryPanel();
                    BuildChessBoard(boardGrid);
                    // --- AI move after player move ---
                    await MakeAIMoveIfNeeded();
                    return;
                }
            }
            else if (piece != null && piece.IsWhite == game.IsWhiteTurn)
            {
                selectedSquare = (row, col);
                var moves = piece.GetLegalMoves(game.Board, row, col).ToList();
                highlightedSquares = moves.Where(m => game.Board[m.row, m.col] == null).ToList();
                captureSquares = moves.Where(m => game.Board[m.row, m.col] != null && game.Board[m.row, m.col]!.IsWhite != piece.IsWhite).ToList();
                if (highlightedSquares.Count == 0 && captureSquares.Count == 0)
                    highlightedSquares.Add((row, col));
            }
            else
            {
                selectedSquare = null;
                highlightedSquares.Clear();
                captureSquares.Clear();
            }
        }
        BuildChessBoard(boardGrid);
    }

    private async Task MakeAIMoveIfNeeded()
    {
        if (!game.IsGameOver && !game.IsWhiteTurn && aiEnabled)
        {
            // Find all legal moves for black
            var moves = new List<(int fromRow, int fromCol, int toRow, int toCol, Piece? promotion, int captureValue)>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var piece = game.Board[r, c];
                    if (piece != null && !piece.IsWhite)
                    {
                        var legalMoves = piece.GetLegalMoves(game.Board, r, c);
                        foreach (var (toRow, toCol) in legalMoves)
                        {
                            int captureValue = 0;
                            var target = game.Board[toRow, toCol];
                            if (target != null)
                            {
                                // Assign higher value for more valuable pieces
                                captureValue = target switch
                                {
                                    King => 1000,
                                    Queen => 9,
                                    Rook => 5,
                                    Bishop => 3,
                                    Knight => 3,
                                    Pawn => 1,
                                    _ => 0
                                };
                            }
                            if (piece is Pawn && toRow == 7)
                            {
                                moves.Add((r, c, toRow, toCol, new Queen(false), captureValue));
                            }
                            else
                            {
                                moves.Add((r, c, toRow, toCol, null, captureValue));
                            }
                        }
                    }
                }
            }
            if (moves.Count > 0)
            {
                // Prefer capturing moves
                var bestCapture = moves.Where(m => m.captureValue > 0).OrderByDescending(m => m.captureValue).ToList();
                (int fromRow, int fromCol, int toRow, int toCol, Piece? promotion, int captureValue) move;
                if (bestCapture.Count > 0)
                {
                    move = bestCapture.First();
                }
                else
                {
                    var rand = new Random();
                    move = moves[rand.Next(moves.Count)];
                }
                if (move.promotion != null)
                    game.TryMove(move.fromRow, move.fromCol, move.toRow, move.toCol, move.promotion);
                else
                    game.TryMove(move.fromRow, move.fromCol, move.toRow, move.toCol);
                // Highlight last move
                lastMoveSquares.Clear();
                lastMoveSquares.Add((move.fromRow, move.fromCol));
                lastMoveSquares.Add((move.toRow, move.toCol));
                // Check/checkmate UI
                if (game.IsGameOver)
                {
                    if (game.IsCheckmate(game.IsWhiteTurn))
                    {
                        bool goHome = await DisplayAlert("Checkmate!", $"{(game.IsWhiteTurn ? "White" : "Black")} is checkmated.", "Home", "OK");
                        if (goHome) { ShowInitialScreen(); return; }
                    }
                    else
                    {
                        bool goHome = await DisplayAlert("Stalemate!", "Draw by stalemate.", "Home", "OK");
                        if (goHome) { ShowInitialScreen(); return; }
                    }
                }
                else if (game.IsInCheck(game.IsWhiteTurn))
                {
                    for (int r = 0; r < 8; r++)
                        for (int c = 0; c < 8; c++)
                            if (game.Board[r, c] is King k && k.IsWhite == game.IsWhiteTurn)
                                kingInCheck = (r, c);
                }
                else
                {
                    kingInCheck = null;
                }
                UpdateMoveHistoryPanel();
                BuildChessBoard(boardGrid!);
            }
        }
    }

    private void UpdateMoveHistoryPanel()
    {
        moveHistoryPanel!.Children.Clear();
        int moveNum = 1;
        for (int i = 0; i < game.MoveHistory.Count; i += 2)
        {
            string whiteMove = MoveToAlgebraic(game.MoveHistory[i]);
            string blackMove = (i + 1 < game.MoveHistory.Count) ? MoveToAlgebraic(game.MoveHistory[i + 1]) : "";
            moveHistoryPanel.Children.Add(new Label { Text = $"{moveNum++}. {whiteMove} {blackMove}", FontSize = 13 });
        }
    }

    private string MoveToAlgebraic(Move move)
    {
        char fileFrom = (char)('a' + move.FromCol);
        char fileTo = (char)('a' + move.ToCol);
        int rankFrom = 8 - move.FromRow;
        int rankTo = 8 - move.ToRow;
        string piece = game.Board[move.ToRow, move.ToCol]?.Name switch
        {
            string n when n.Contains("knight") => "N",
            string n when n.Contains("bishop") => "B",
            string n when n.Contains("rook") => "R",
            string n when n.Contains("queen") => "Q",
            string n when n.Contains("king") => "K",
            _ => ""
        };
        string capture = move.CapturedPiece != null ? "x" : "";
        return $"{piece}{fileFrom}{rankFrom}{capture}{fileTo}{rankTo}";
    }

    private void MakeAIMove()
    {
        if (!aiEnabled) return;
        if (game.IsGameOver) return;
        if (game.IsWhiteTurn == aiIsWhite)
        {
            // Find all legal moves for AI
            var moves = new List<(int fromRow, int fromCol, int toRow, int toCol)>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    var piece = game.Board[r, c];
                    if (piece != null && piece.IsWhite == aiIsWhite)
                    {
                        foreach (var (tr, tc) in piece.GetLegalMoves(game.Board, r, c))
                        {
                            // Only add legal moves
                            if (game.TryMove(r, c, tr, tc))
                            {
                                game.UndoLastMove();
                                moves.Add((r, c, tr, tc));
                            }
                        }
                    }
                }
            if (moves.Count > 0)
            {
                // Pick a random move (easy AI)
                var rand = new Random();
                var move = moves[rand.Next(moves.Count)];
                game.TryMove(move.fromRow, move.fromCol, move.toRow, move.toCol);
                BuildChessBoard(boardGrid!);
                UpdateMoveHistoryPanel();
            }
        }
    }
}
