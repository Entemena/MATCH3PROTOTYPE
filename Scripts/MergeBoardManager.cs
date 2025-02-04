using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages the grid and gameplay logic for the Merge-3 puzzle.
/// </summary>
public partial class MergeBoardManager : Node2D
{
    #region Fields & Properties

    [Export] public int Rows = 8;
    [Export] public int Cols = 8;
    [Export] public int MergeTileSize = 128;
    [Export] public float MergeTileScale = 2.0f;
    [Export] public float MergeTileMargin = 10.0f;
    [Export] public PackedScene MergeTileScene;
    [Export] public Texture2D[] MergeTileTextures;
    [Export] public MergeTileTextureData[] TileTextureResources;

    private MergeTile[,] _tiles;
    private float _tileSpacing;
    private Vector2 _boardOffset;
    private bool _boardLocked;
    private List<Tween> _activeTweens = new List<Tween>();
    private MergeTile _selectedTile = null; // Tracks the currently selected tile

    #endregion

    #region Initialization

    /// <summary>
    /// Called when the board is ready. Initializes the board and centers it.
    /// </summary>
    public override void _Ready()
    {
        GD.Print("Loading TileTextureResources...");

        for (int i = 0; i < TileTextureResources.Length; i++)
        {
            GD.Print($"Tile {i}: {TileTextureResources[i]?.LevelTextures.Count} textures loaded");
        }
        GetViewport().Connect("size_changed", new Callable(this, nameof(OnWindowResized)));

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CurrentMergeBoard = this;
        }

        InitializeBoard();
        CenterBoard();
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
    #region Board Setup & Positioning

    /// <summary>
    /// Calculates tile spacing based on the viewport size.
    /// </summary>
    private void CalculateTileSpacing()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float maxSpacingX = (viewportSize.X * 0.9f) / Cols;
        float maxSpacingY = (viewportSize.Y * 0.9f) / Rows;
        float maxSpacing = Mathf.Min(maxSpacingX, maxSpacingY);
        float defaultSpacing = (MergeTileSize * MergeTileScale) + MergeTileMargin;
        _tileSpacing = Mathf.Max(Mathf.Min(defaultSpacing, maxSpacing), 10);
    }

    /// <summary>
    /// Initializes the grid, placing all tiles.
    /// </summary>
    private void InitializeBoard()
    {
        CalculateTileSpacing();
        CreateGrid();
        PositionTiles();
    }

    /// <summary>
    /// Creates the grid and populates it with MergeTiles.
    /// </summary>
    private void CreateGrid()
    {
        if (MergeTileScene == null)
        {
            GD.PrintErr("MergeTileScene is not assigned!");
            return;
        }
        if (MergeTileTextures == null || MergeTileTextures.Length == 0)
        {
            GD.PrintErr("MergeTileTextures array is null or empty!");
            return;
        }

        _tiles = new MergeTile[Rows, Cols];
        Random random = new Random();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                int tileType = random.Next(TileTextureResources.Length);

                // Debug to confirm the tileType is correctly assigned
                GD.Print($"?? Assigned TileType {tileType} at ({row}, {col}) -> Expected Texture: {TileTextureResources[tileType].LevelTextures[0].ResourcePath}");

                SpawnTile(row, col, tileType, 1);
            }
        }


    }

    /// <summary>
    /// Spawns a new tile at the given row and column.
    /// </summary>
    private void SpawnTile(int row, int col, int tileType, int level)
    {
        MergeTile tileInstance = MergeTileScene.Instantiate<MergeTile>();
        AddChild(tileInstance);
        tileInstance.Initialize(tileType, level, this, row, col);
        _tiles[row, col] = tileInstance;
    }

    /// <summary>
    /// Updates the position of all tiles on the board.
    /// </summary>
    private void PositionTiles()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] != null)
                {
                    _tiles[row, col].Position = GridToWorldPosition(row, col);
                }
            }
        }
    }


    /// <summary>
    /// Centers the board in the viewport.
    /// </summary>
    private void CenterBoard()
    {
        Vector2 totalSize = new Vector2(Cols * _tileSpacing, Rows * _tileSpacing);
        Vector2 viewportSize = GetViewportRect().Size;
        _boardOffset = (viewportSize - totalSize) / 2;
        Position = _boardOffset;
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// </summary>
    public Vector2 GridToWorldPosition(int row, int col)
    {
        return new Vector2(col * _tileSpacing, row * _tileSpacing);
    }

    #endregion

    #region Tile Selection & Swapping

    /// <summary>
    /// Handles when a tile is clicked.
    /// </summary>
    public void OnTileClicked(MergeTile clickedTile)
    {
        if (_selectedTile == null)
        {
            _selectedTile = clickedTile;
            _selectedTile.SetHighlight(true);
        }
        else
        {
            SwapTiles(_selectedTile, clickedTile);
            _selectedTile.SetHighlight(false);
            _selectedTile = null;
        }
    }

    /// <summary>
    /// Swaps two tiles and checks for merges.
    /// </summary>
    private void SwapTiles(MergeTile tile1, MergeTile tile2)
    {
        int tempRow = tile1.Row;
        int tempCol = tile1.Col;

        tile1.Row = tile2.Row;
        tile1.Col = tile2.Col;
        tile2.Row = tempRow;
        tile2.Col = tempCol;

        _tiles[tile1.Row, tile1.Col] = tile1;
        _tiles[tile2.Row, tile2.Col] = tile2;

        tile1.Position = GridToWorldPosition(tile1.Row, tile1.Col);
        tile2.Position = GridToWorldPosition(tile2.Row, tile2.Col);

        CheckAndMerge(tile1);
        CheckAndMerge(tile2);
    }

    #endregion

    #region Merging Logic
    /// <summary>
    /// Retrieves the correct texture based on the tile's type and level.
    /// </summary>
    public Texture2D GetTileTexture(int tileType, int level)
    {
        if (TileTextureResources == null)
        {
            GD.PrintErr("ERROR: TileTextureResources is NULL!");
            return null;
        }

        if (tileType >= TileTextureResources.Length)
        {
            GD.PrintErr($"ERROR: Invalid tileType {tileType}, but we have {TileTextureResources.Length} tile types!");
            return null;
        }

        MergeTileTextureData tileData = TileTextureResources[tileType];

        if (tileData == null || tileData.LevelTextures.Count == 0)
        {
            GD.PrintErr($"ERROR: Tile texture data is missing or empty for type {tileType}");
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, tileData.LevelTextures.Count - 1);

        GD.Print($"Fetching texture for TileType {tileType}, Level {level}, using index {index} -> {tileData.LevelTextures[index].ResourcePath}");

        return tileData.LevelTextures[index];
    }



    /// <summary>
    /// Checks if a tile should merge after a swap.
    /// </summary>
    private void CheckAndMerge(MergeTile tile)
    {
        List<MergeTile> matchingTiles = FindMatchingTiles(tile);

        if (matchingTiles.Count >= 3)
        {
            MergeTiles(matchingTiles, tile);
        }
    }

    /// <summary>
    /// Finds all matching tiles with the same type and level.
    /// </summary>
    private List<MergeTile> FindMatchingTiles(MergeTile baseTile)
    {
        List<MergeTile> matchingTiles = new List<MergeTile> { baseTile };

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                MergeTile tile = _tiles[row, col];
                if (tile != null && tile != baseTile && tile.TileType == baseTile.TileType && tile.Level == baseTile.Level)
                {
                    matchingTiles.Add(tile);
                }
            }
        }
        return matchingTiles;
    }

    /// <summary>
    /// Merges tiles by leveling up the target tile and removing others with animation.
    /// </summary>
    private async void MergeTiles(List<MergeTile> tilesToMerge, MergeTile targetTile)
    {
        if (tilesToMerge.Count < 3)
            return;

        List<Tween> tweens = new List<Tween>();

        foreach (var tile in tilesToMerge)
        {
            if (tile != targetTile)
            {
                _tiles[tile.Row, tile.Col] = null;
                Tween tween = CreateTween();
                tween.TweenProperty(tile, "scale", Vector2.Zero, 0.3f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                tween.TweenProperty(tile, "modulate:a", 0, 0.3f);
                tweens.Add(tween);
            }
        }

        // Wait for all animations to finish
        await ToSignal(tweens[^1], Tween.SignalName.Finished);

        // Remove the merged tiles
        foreach (var tile in tilesToMerge)
        {
            if (tile != targetTile)
            {
                tile.QueueFree();
            }
        }

        // Increase the level of the target tile
        targetTile.Level++;
        targetTile.UpdateAppearance();

        // Refill empty spaces
        await AnimateTileRefill();
    }
    /// <summary>
    /// Animates falling tiles and spawns new ones to refill the board.
    /// </summary>
    private async Task AnimateTileRefill()
    {
        List<Tween> tweens = new List<Tween>();

        for (int col = 0; col < Cols; col++)
        {
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (_tiles[row, col] == null)
                {
                    MoveTileDown(row, col, tweens);
                }
            }
        }

        // Wait for all falling animations to complete
        if (tweens.Count > 0)
            await ToSignal(tweens[^1], Tween.SignalName.Finished);

        // Spawn new tiles at the top
        SpawnNewTiles(tweens);

        // Wait for new tile animations to complete
        if (tweens.Count > 0)
            await ToSignal(tweens[^1], Tween.SignalName.Finished);

        // Reposition all tiles
        PositionTiles();
    }
    /// <summary>
    /// Moves tiles down to fill empty spaces.
    /// </summary>
    private void MoveTileDown(int row, int col, List<Tween> tweens)
    {
        for (int aboveRow = row - 1; aboveRow >= 0; aboveRow--)
        {
            if (_tiles[aboveRow, col] != null)
            {
                MergeTile tileToMove = _tiles[aboveRow, col];
                _tiles[row, col] = tileToMove;
                _tiles[aboveRow, col] = null;

                Tween tween = CreateTween();
                tween.TweenProperty(tileToMove, "position:y", GridToWorldPosition(row, col).Y, 0.3f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);
                tweens.Add(tween);

                tileToMove.Row = row;
                break;
            }
        }
    }
    /// <summary>
    /// Spawns new tiles at the top and animates them falling into place.
    /// </summary>
    private void SpawnNewTiles(List<Tween> tweens)
    {
        Random random = new Random();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] == null)
                {
                    // Use TileTextureResources.Length instead of MergeTileTextures.Length
                    int tileType = random.Next(TileTextureResources.Length);
                    MergeTile newTile = MergeTileScene.Instantiate<MergeTile>();
                    AddChild(newTile);
                    newTile.Initialize(tileType, 1, this, row, col);

                    // Start new tile off-screen
                    newTile.Position = new Vector2(GridToWorldPosition(row, col).X, GridToWorldPosition(-1, col).Y);
                    _tiles[row, col] = newTile;

                    // Animate falling
                    Tween tween = CreateTween();
                    tween.TweenProperty(newTile, "position:y", GridToWorldPosition(row, col).Y, 0.3f)
                         .SetTrans(Tween.TransitionType.Quad)
                         .SetEase(Tween.EaseType.Out);
                    tweens.Add(tween);
                }
            }
        }
    }





    /// <summary>
    /// Fills empty spaces with new random level 1 tiles.
    /// </summary>
    private void FillEmptyTiles()
    {
        Random random = new Random();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                if (_tiles[row, col] == null) // If there's an empty space
                {
                    int tileType = random.Next(MergeTileTextures.Length); // Pick a random tile type
                    SpawnTile(row, col, tileType, 1); // Spawn a new level 1 tile
                }
            }
        }

        // Reposition tiles to align properly
        PositionTiles();
    }


    #endregion
}
