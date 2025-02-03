    using Godot;
using System;
using System.Collections.Generic;

public partial class MatchBoardManager : Node2D
{
    [Export] public int Rows = 8;
    [Export] public int Cols = 8;
    [Export] public PackedScene MatchTileScene;
    [Export] public Texture2D[] MatchTileTextures;
    [Export] public int MatchTileSize = 128;          // Original size of the tile texture.
    [Export] public float MatchTileScale = 2.0f;        // Scale factor for the tile.
    [Export] public float MatchTileMargin = 10.0f;      // Additional margin between tiles.

    private MatchTile[,] _tiles;
    private float _tileSpacing;      // Effective spacing between tile centers.
    private Vector2 _boardOffset;    // Offset to center the board on screen.
    private MatchTile _currentlySelectedTile;
    private bool _isSwapping;
    private bool _boardLocked;
    private List<Tween> _activeTweens = new List<Tween>();

    public override void _Ready()
    {
        // Connect to viewport resize events.
        GetViewport().Connect("size_changed", new Callable(this, nameof(OnWindowResized)));

        // Register this board with the GameManager (if available).
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentMatchBoard = this;
        }

        InitializeBoard();
        CenterBoard();
    }

    #region Board Initialization & Positioning

    /// <summary>
    /// Calculates the tile spacing so that the board fits within 90% of the viewport.
    /// </summary>
    private void CalculateTileSpacing()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        // Maximum spacing allowed by the viewport.
        float maxSpacingX = (viewportSize.X * 0.9f) / Cols;
        float maxSpacingY = (viewportSize.Y * 0.9f) / Rows;
        float maxSpacing = Mathf.Min(maxSpacingX, maxSpacingY);

        // Default spacing based on the tile’s effective size.
        float defaultSpacing = (MatchTileSize * MatchTileScale) + MatchTileMargin;

        // Use the smaller value to ensure the board fits, but never less than 10.
        _tileSpacing = Mathf.Max(Mathf.Min(defaultSpacing, maxSpacing), 10);
    }

    /// <summary>
    /// Creates the grid of tiles.
    /// </summary>
    private void InitializeBoard()
    {
        CalculateTileSpacing();
        CreateGrid();
        PositionTiles();
    }

    private void CreateGrid()
    {
        if (MatchTileScene == null)
        {
            GD.PrintErr("MatchTileScene is not assigned!");
            return;
        }
        if (MatchTileTextures == null || MatchTileTextures.Length == 0)
        {
            GD.PrintErr("MatchTileTextures array is null or empty!");
            return;
        }

        _tiles = new MatchTile[Rows, Cols];
        Random random = new Random();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                // Instantiate a new tile.
                MatchTile tileInstance = MatchTileScene.Instantiate<MatchTile>();
                AddChild(tileInstance);

                // Set its initial position (will be updated later).
                tileInstance.Position = new Vector2(col * _tileSpacing, row * _tileSpacing);
                tileInstance.TileType = random.Next(MatchTileTextures.Length);
                tileInstance.Board = this;  // Ensure that Tile.Board is of type BoardManager.
                tileInstance.Row = row;
                tileInstance.Col = col;

                // Set up the tile's sprite.
                Sprite2D sprite = tileInstance.GetNode<Sprite2D>("Sprite2D");
                if (sprite != null)
                {
                    sprite.Texture = MatchTileTextures[tileInstance.TileType];
                    sprite.Scale = new Vector2(MatchTileScale, MatchTileScale);
                }

                _tiles[row, col] = tileInstance;
            }
        }
    }

    /// <summary>
    /// Positions each tile based on its row and column.
    /// </summary>
    private void PositionTiles()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] != null && IsInstanceValid(_tiles[row, col]))
                {
                    _tiles[row, col].Position = new Vector2(col * _tileSpacing, row * _tileSpacing);
                }
            }
        }
    }

    /// <summary>
    /// Centers the entire board on the screen.
    /// </summary>
    private void CenterBoard()
    {
        Vector2 totalSize = new Vector2(Cols * _tileSpacing, Rows * _tileSpacing);
        Vector2 viewportSize = GetViewportRect().Size;
        _boardOffset = (viewportSize - totalSize) / 2;
        Position = _boardOffset;
    }

    /// <summary>
    /// Converts a grid coordinate (row, col) to world position.
    /// </summary>
    public Vector2 GridToWorldPosition(int row, int col)
    {
        return new Vector2(col * _tileSpacing, row * _tileSpacing);
    }

    #endregion

    #region Tile Interaction & Swapping

    /// <summary>
    /// Called by a tile when it is clicked.
    /// </summary>
    public void OnTileClicked(MatchTile clickedTile)
    {
        if (_isSwapping || _boardLocked || clickedTile == null || !IsInstanceValid(clickedTile))
            return;

        if (_currentlySelectedTile == null)
        {
            SelectTile(clickedTile);
        }
        else
        {
            HandleTileSelection(clickedTile);
        }
    }

    private void SelectTile(MatchTile tile)
    {
        _currentlySelectedTile = tile;
        _currentlySelectedTile.SetHighlight(true);
    }

    private void HandleTileSelection(MatchTile clickedTile)
    {
        if (_currentlySelectedTile != null)
            _currentlySelectedTile.SetHighlight(false);

        if (AreTilesAdjacent(_currentlySelectedTile, clickedTile))
        {
            SwapTiles(_currentlySelectedTile, clickedTile);
            _currentlySelectedTile = null;
        }
        else
        {
            _currentlySelectedTile = clickedTile;
            _currentlySelectedTile.SetHighlight(true);
        }
    }

    private bool AreTilesAdjacent(MatchTile tileA, MatchTile tileB)
    {
        int rowDiff = Math.Abs(tileA.Row - tileB.Row);
        int colDiff = Math.Abs(tileA.Col - tileB.Col);
        return (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
    }

    /// <summary>
    /// Swaps two adjacent tiles and then processes the board for matches.
    /// </summary>
    private void SwapTiles(MatchTile tileA, MatchTile tileB)
    {
        if (_boardLocked)
            return;

        _isSwapping = true;

        // Update the grid: swap the tiles in the array.
        _tiles[tileA.Row, tileA.Col] = tileB;
        _tiles[tileB.Row, tileB.Col] = tileA;

        // Swap the row and column properties.
        int tempRow = tileA.Row;
        int tempCol = tileA.Col;
        tileA.Row = tileB.Row;
        tileA.Col = tileB.Col;
        tileB.Row = tempRow;
        tileB.Col = tempCol;

        // Animate tileA.
        Tween tweenA = tileA.CreateTween();
        tweenA.TweenProperty(tileA, "position", GridToWorldPosition(tileA.Row, tileA.Col), 0.3f)
              .SetTrans(Tween.TransitionType.Quad)
              .SetEase(Tween.EaseType.Out);
        tweenA.Finished += () =>
        {
            _isSwapping = false;
            ProcessBoard();
        };

        // Animate tileB.
        Tween tweenB = tileB.CreateTween();
        tweenB.TweenProperty(tileB, "position", GridToWorldPosition(tileB.Row, tileB.Col), 0.3f)
              .SetTrans(Tween.TransitionType.Quad)
              .SetEase(Tween.EaseType.Out);
    }

    #endregion

    #region Match, Removal & Refill Logic

    /// <summary>
    /// Checks the board repeatedly for matches and refills it.
    /// </summary>
    private void ProcessBoard()
    {
        int maxIterations = 10;
        int iteration = 0;
        bool foundMatches;
        do
        {
            foundMatches = CheckForMatches();
            if (foundMatches)
            {
                RefillBoard();
            }
            iteration++;
        }
        while (foundMatches && iteration < maxIterations);

        _isSwapping = false;
        _currentlySelectedTile = null;
    }

    /// <summary>
    /// Searches for horizontal and vertical matches.
    /// If any are found, the matching tiles are removed.
    /// </summary>
    private bool CheckForMatches()
    {
        List<MatchTile> matchedTiles = new List<MatchTile>();

        CheckHorizontalMatches(matchedTiles);
        CheckVerticalMatches(matchedTiles);

        if (matchedTiles.Count > 0)
        {
            RemoveMatchedTiles(matchedTiles);
            return true;
        }

        return false;
    }

    private void CheckHorizontalMatches(List<MatchTile> matchedTiles)
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

    private void CheckVerticalMatches(List<MatchTile> matchedTiles)
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

    private void AddMatchToTiles(int startRow, int startCol, int rowStep, int colStep, List<MatchTile> matchedTiles)
    {
        for (int i = 0; i < 3; i++)
        {
            MatchTile tile = _tiles[startRow + i * rowStep, startCol + i * colStep];
            if (!matchedTiles.Contains(tile))
                matchedTiles.Add(tile);
        }
    }

    /// <summary>
    /// Removes the matched tiles from the board.
    /// </summary>
    private void RemoveMatchedTiles(List<MatchTile> matchedTiles = null)
    {
        matchedTiles ??= new List<MatchTile>();
        HashSet<MatchTile> uniqueMatches = new HashSet<MatchTile>(matchedTiles);

        foreach (MatchTile tile in uniqueMatches)
        {
            if (tile != null && IsInstanceValid(tile))
            {
                RemoveChild(tile);
                tile.QueueFree();
                _tiles[tile.Row, tile.Col] = null;
            }
        }
    }

    /// <summary>
    /// Refills the board by moving existing tiles down and creating new tiles.
    /// </summary>
    private async void RefillBoard()
    {
        _boardLocked = true;
        Tween mainTween = CreateTween();
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
                MatchTile tileToMove = _tiles[aboveRow, col];
                _tiles[row, col] = tileToMove;
                _tiles[aboveRow, col] = null;

                mainTween
                    .Parallel()
                    .TweenProperty(tileToMove, "position:y", row * _tileSpacing, 0.3f);
                tileToMove.Row = row;
                break;
            }
        }
    }

    private void CreateNewTiles(Tween mainTween)
    {
        Random random = new Random();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
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
        MatchTile tileInstance = MatchTileScene.Instantiate<MatchTile>();
        AddChild(tileInstance);

        // Position the new tile above the board.
        tileInstance.Position = new Vector2(col * _tileSpacing, -_tileSpacing);
        tileInstance.TileType = random.Next(MatchTileTextures.Length);
        tileInstance.Board = this;  // Ensure that Tile.Board is of type BoardManager.
        tileInstance.Row = row;
        tileInstance.Col = col;

        Sprite2D sprite = tileInstance.GetNode<Sprite2D>("Sprite2D");
        if (sprite != null)
        {
            sprite.Texture = MatchTileTextures[tileInstance.TileType];
            sprite.Scale = new Vector2(MatchTileScale, MatchTileScale);
        }

        mainTween
            .Parallel()
            .TweenProperty(tileInstance, "position:y", row * _tileSpacing, 0.3f);

        _tiles[row, col] = tileInstance;
    }

    #endregion

    #region Dynamic Window Resizing

    /// <summary>
    /// Called when the window or viewport is resized.
    /// Cancels ongoing animations and repositions all tiles.
    /// </summary>
    private void OnWindowResized()
    {
        if (_boardLocked)
            return;

        _boardLocked = true;

        // Cancel any active tweens.
        foreach (Tween tween in _activeTweens)
        {
            tween?.Kill();
        }
        _activeTweens.Clear();

        CalculateTileSpacing();
        PositionTiles();
        CenterBoard();

        _boardLocked = false;
    }

    #endregion

    #region Board Reset

    /// <summary>
    /// Frees all current tiles and reinitializes the board.
    /// </summary>
    public void ResetBoard()
    {
        if (_tiles != null)
        {
            foreach (MatchTile tile in _tiles)
            {
                if (tile != null)
                    tile.QueueFree();
            }
        }
        InitializeBoard();
        CenterBoard();
    }

    #endregion

    #region Debug

    /// <summary>
    /// Prints the positions of all tiles on the board.
    /// </summary>
    public void PrintTilePositions()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] != null)
                {
                    GD.Print($"Tile at [{row}, {col}] position: {_tiles[row, col].Position}");
                }
                else
                {
                    GD.Print($"Tile at [{row}, {col}] is null.");
                }
            }
        }
    }

    /// <summary>
    /// Should be connected to the PrintTilePositionsButton's pressed signal.
    /// </summary>
    public void OnPrintTilePositionsButtonPressed()
    {
        PrintTilePositions();
    }

    #endregion
}
