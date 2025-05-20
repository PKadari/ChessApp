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
    private StackLayout moveHistoryPanel;
    private Grid boardGrid;
    private Grid parentGrid;

    public MainPage()
    {
        game.StartNewGame();
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

        // Move history panel
        moveHistoryPanel = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            WidthRequest = 120,
            BackgroundColor = Colors.WhiteSmoke,
            Padding = new Thickness(5),
            Spacing = 2
        };
        UpdateMoveHistoryPanel();

        // Parent grid for labels + board + reset button + move history
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
        // Add column labels (a-h)
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
        // Add row labels (8-1)
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
        // Place the boardGrid at (1,1) and span 8x8
        Grid.SetRow(boardGrid, 1);
        Grid.SetColumn(boardGrid, 1);
        Grid.SetRowSpan(boardGrid, 8);
        Grid.SetColumnSpan(boardGrid, 8);
        parentGrid.Children.Add(boardGrid);
        // Place move history panel at right
        Grid.SetRow(moveHistoryPanel, 1);
        Grid.SetColumn(moveHistoryPanel, 9);
        Grid.SetRowSpan(moveHistoryPanel, 8);
        parentGrid.Children.Add(moveHistoryPanel);

        // Add Reset button below the board
        var resetButton = new Button
        {
            Text = "Reset Game",
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        resetButton.Clicked += (s, e) =>
        {
            game.StartNewGame();
            BuildChessBoard(boardGrid);
            UpdateMoveHistoryPanel();
        };
        Grid.SetRow(resetButton, 9);
        Grid.SetColumn(resetButton, 1);
        Grid.SetColumnSpan(resetButton, 8);
        parentGrid.Children.Add(resetButton);

        Content = parentGrid;
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
                    game.Board.Board[row, col] = promoted;
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
                            await DisplayAlert("Checkmate!", $"{(game.IsWhiteTurn ? "White" : "Black")} is checkmated.", "OK");
                        else
                            await DisplayAlert("Stalemate!", "Draw by stalemate.", "OK");
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

    private void UpdateMoveHistoryPanel()
    {
        moveHistoryPanel.Children.Clear();
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
}
