using Godot;
using System;
using System.Collections.Generic;

public partial class Board : Node2D
{
    [Export] public int Rows = 8;
    [Export] public int Cols = 8;
    [Export] public int TileSize = 128;
    [Export] public PackedScene TileScene;
    [Export] public Texture2D[] TileTextures;
    private List<Tween> _activeTweens = new List<Tween>();

    private float _gapSize;
    private Tile[,] _tiles;
    private Tile _selectedTile;
    private bool _isSwapping = false;
    private bool _boardLocked = false;

    public override void _Ready()
    {
        InitializeWindow();
        // Wait for the window to initialize (if needed)
        CallDeferred(nameof(InitializeBoard));
        CallDeferred(nameof(CenterBoardOnScreen));
    }

    private void InitializeWindow()
    {
        GetWindow().Mode = Window.ModeEnum.Windowed;
        GetWindow().SetSize(new Vector2I(800, 600));
        GetWindow().MinSize = new Vector2I(400, 300);
        GetWindow().MaxSize = DisplayServer.ScreenGetSize();

        GetWindow().Connect("size_changed", new Callable(this, nameof(OnWindowResized)));
    }

    private async void OnWindowResized()
    {
        if (_boardLocked) return;

        _boardLocked = true;

        // Cancel all ongoing tweens
        foreach (var tween in _activeTweens)
        {
            tween?.Kill();
        }
        _activeTweens.Clear();

        // Immediately update tile positions without animation
        CalculateGapSize();
        UpdateAllTilePositions();
        CenterBoardOnScreen();

        // Small delay to allow UI to settle
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");

        _boardLocked = false;
    }

    private void CalculateGapSize()
    {
        var windowSize = GetWindow().Size;

        // Ensure window size is valid
        if (windowSize.X <= 0 || windowSize.Y <= 0)
            return;

        float maxBoardWidth = windowSize.X * 0.9f;
        float maxBoardHeight = windowSize.Y * 0.9f;

        float maxGapWidth = maxBoardWidth / Cols;
        float maxGapHeight = maxBoardHeight / Rows;

        _gapSize = Mathf.Min(maxGapWidth, maxGapHeight);
        _gapSize = Mathf.Max(_gapSize, 10); // Ensure minimum gap size
        _gapSize = Mathf.Min(_gapSize, TileSize * 0.67f);
    }

    private void InitializeBoard()
    {
        CalculateGapSize(); // Calculate gap size before creating tiles
        _tiles = new Tile[Rows, Cols];
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        var random = new Random();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                CreateTile(row, col, random);
            }
        }
    }

    private void CreateTile(int row, int col, Random random)
    {
        var tileInstance = (Tile)TileScene.Instantiate();
        AddChild(tileInstance);

        tileInstance.Position = CalculateTilePosition(row, col);
        tileInstance.TileType = random.Next(TileTextures.Length);
        tileInstance.Board = this;

        var sprite = tileInstance.GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = TileTextures[tileInstance.TileType];
        sprite.Scale = new Vector2(2, 2);

        _tiles[row, col] = tileInstance;
        tileInstance.Row = row;
        tileInstance.Col = col;
    }

    private Vector2 CalculateTilePosition(int row, int col)
    {
        return new Vector2(col * _gapSize, row * _gapSize);
    }

    private void CenterBoardOnScreen()
    {
        var boardWidth = Cols * _gapSize;
        var boardHeight = Rows * _gapSize;
        var windowSize = GetWindow().Size;

        Position = new Vector2(
            (windowSize.X - boardWidth) / 2f,
            (windowSize.Y - boardHeight) / 2f
        );
    }

    public void OnTileClicked(Tile clickedTile)
    {
        // Check if the clicked tile is valid and in the scene tree
        if (_isSwapping || _boardLocked || clickedTile == null || !IsInstanceValid(clickedTile))
            return;

        if (_selectedTile == null)
        {
            SelectTile(clickedTile);
        }
        else
        {
            HandleTileSelection(clickedTile);
        }
    }

    private void SelectTile(Tile tile)
    {
        _selectedTile = tile;
        _selectedTile.SetHighlight(true);
    }

    private void HandleTileSelection(Tile clickedTile)
    {
        _selectedTile.SetHighlight(false);

        if (AreTilesAdjacent(_selectedTile, clickedTile))
        {
            SwapTiles(_selectedTile, clickedTile);
        }
        else
        {
            SelectTile(clickedTile);
        }
    }

    private bool AreTilesAdjacent(Tile tileA, Tile tileB)
    {
        int rowDiff = Math.Abs(tileA.Row - tileB.Row);
        int colDiff = Math.Abs(tileA.Col - tileB.Col);
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    private void SwapTiles(Tile tileA, Tile tileB)
    {
        if (_boardLocked)
            return;

        _isSwapping = true;

        UpdateTilePositionsInArray(tileA, tileB);
        UpdateTileProperties(tileA, tileB);
        AnimateTileSwap(tileA, tileB);

        ProcessBoard();
    }

    private void UpdateTilePositionsInArray(Tile tileA, Tile tileB)
    {
        _tiles[tileA.Row, tileA.Col] = tileB;
        _tiles[tileB.Row, tileB.Col] = tileA;
    }
    private void UpdateAllTilePositions()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] != null && IsInstanceValid(_tiles[row, col]))
                {
                    _tiles[row, col].Position = CalculateTilePosition(row, col);
                }
            }
        }
    }

    private void UpdateTileProperties(Tile tileA, Tile tileB)
    {
        int tempRow = tileA.Row;
        int tempCol = tileA.Col;

        tileA.Row = tileB.Row;
        tileA.Col = tileB.Col;

        tileB.Row = tempRow;
        tileB.Col = tempCol;
    }

    private void AnimateTileSwap(Tile tileA, Tile tileB)
    {
        var posA = tileA.Position;
        tileA.Position = tileB.Position;
        tileB.Position = posA;
    }

    private void ProcessBoard()
    {
        int maxIterations = 10; // Prevent infinite loops
        int iteration = 0;

        bool foundMatches;
        do
        {
            foundMatches = CheckForMatches();
            if (foundMatches)
            {
                RemoveMatchedTiles();
                RefillBoard();
            }

            iteration++;
        }
        while (foundMatches && iteration < maxIterations);

        _isSwapping = false;
        _selectedTile = null;
    }

    private bool CheckForMatches()
    {
        var matchedTiles = new List<Tile>();

        CheckHorizontalMatches(matchedTiles);
        CheckVerticalMatches(matchedTiles);

        if (matchedTiles.Count > 0)
        {
            RemoveMatchedTiles(matchedTiles);
            return true;
        }

        return false;
    }

    private void CheckHorizontalMatches(List<Tile> matchedTiles)
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols - 2; col++)
            {
                if (IsMatch(row, col, 0, 1))
                {
                    AddMatchToTiles(row, col, 0, 1, matchedTiles);
                }
            }
        }
    }

    private void CheckVerticalMatches(List<Tile> matchedTiles)
    {
        for (int col = 0; col < Cols; col++)
        {
            for (int row = 0; row < Rows - 2; row++)
            {
                if (IsMatch(row, col, 1, 0))
                {
                    AddMatchToTiles(row, col, 1, 0, matchedTiles);
                }
            }
        }
    }

    private bool IsMatch(int startRow, int startCol, int rowStep, int colStep)
    {
        if (startRow < 0 || startRow >= Rows || startCol < 0 || startCol >= Cols)
            return false;

        int type = _tiles[startRow, startCol]?.TileType ?? -1;
        if (type == -1)
            return false;

        for (int i = 1; i <= 2; i++)
        {
            int nextRow = startRow + i * rowStep;
            int nextCol = startCol + i * colStep;

            if (nextRow < 0 || nextRow >= Rows || nextCol < 0 || nextCol >= Cols)
                return false;

            if (_tiles[nextRow, nextCol]?.TileType != type)
                return false;
        }

        return true;
    }

    private void AddMatchToTiles(int startRow, int startCol, int rowStep, int colStep, List<Tile> matchedTiles)
    {
        for (int i = 0; i < 3; i++)
        {
            matchedTiles.Add(_tiles[startRow + i * rowStep, startCol + i * colStep]);
        }
    }

    private void RemoveMatchedTiles(List<Tile> matchedTiles = null)
    {
        matchedTiles ??= new List<Tile>();
        var uniqueMatches = new HashSet<Tile>(matchedTiles);

        foreach (var tile in uniqueMatches)
        {
            if (tile != null && IsInstanceValid(tile))
            {
                RemoveChild(tile);
                tile.QueueFree(); // Properly free the tile
                _tiles[tile.Row, tile.Col] = null;
            }
        }
    }

    private async void RefillBoard()
    {
        _boardLocked = true;
        var mainTween = CreateTween();
        _activeTweens.Add(mainTween);

        MoveTilesDown(mainTween);
        CreateNewTiles(mainTween);

        await ToSignal(mainTween, Tween.SignalName.Finished);
        _activeTweens.Remove(mainTween);
        _boardLocked = false;
        ProcessBoard();
    }

    private void MoveTilesDown(Tween mainTween)
    {
        for (int col = 0; col < Cols; col++)
        {
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (_tiles[row, col] == null)
                {
                    MoveTileAboveDown(row, col, mainTween);
                }
            }
        }
    }

    private void MoveTileAboveDown(int row, int col, Tween mainTween)
    {
        for (int aboveRow = row - 1; aboveRow >= 0; aboveRow--)
        {
            if (_tiles[aboveRow, col] != null)
            {
                var tileToMove = _tiles[aboveRow, col];
                _tiles[row, col] = tileToMove;
                _tiles[aboveRow, col] = null;

                mainTween
                    .Parallel()
                    .TweenProperty(tileToMove, "position:y", row * _gapSize, 0.3f);

                tileToMove.IsAnimating = true;
                mainTween.Finished += () => tileToMove.IsAnimating = false;

                tileToMove.Row = row;
                break;
            }
        }
    }

    private void CreateNewTiles(Tween mainTween)
    {
        var random = new Random();
        for (int col = 0; col < Cols; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (_tiles[row, col] == null)
                {
                    CreateAndAnimateNewTile(row, col, random, mainTween);
                }
            }
        }
    }

    private void CreateAndAnimateNewTile(int row, int col, Random random, Tween mainTween)
    {
        var tileInstance = (Tile)TileScene.Instantiate();
        AddChild(tileInstance);

        tileInstance.Position = new Vector2(col * _gapSize, -_gapSize);
        tileInstance.TileType = random.Next(TileTextures.Length);
        tileInstance.Board = this;

        var sprite = tileInstance.GetNode<Sprite2D>("Sprite2D");
        sprite.Texture = TileTextures[tileInstance.TileType];
        sprite.Scale = new Vector2(2, 2);

        mainTween
            .Parallel()
            .TweenProperty(tileInstance, "position:y", row * _gapSize, 0.3f);

        tileInstance.IsAnimating = true;
        mainTween.Finished += () => tileInstance.IsAnimating = false;

        _tiles[row, col] = tileInstance;
        tileInstance.Row = row;
        tileInstance.Col = col;
    }

    public void OnPrintButtonPressed()  
    {
        if (_boardLocked)
        {
            GD.Print("Board is locked; cannot print tile positions.");
            return;
        }

        GD.Print("Printing tile positions:");
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] != null)
                {
                    GD.Print($"Tile at Row: {row}, Col: {col} has position: {_tiles[row, col].Position}");
                }
                else
                {
                    GD.Print($"Tile at Row: {row}, Col: {col} is null (no tile here).");
                }
            }
        }
    }
}