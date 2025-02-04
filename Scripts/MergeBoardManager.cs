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
        EnsureNoInitialMatches(); // Make sure there are no starting matches
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
                int tileType;
                int attempts = 0;
                const int maxAttempts = 100; // Prevent infinite loops

                do
                {
                    tileType = random.Next(TileTextureResources.Length);
                    attempts++;
                }
                while (attempts < maxAttempts && WouldCauseMatch(row, col, tileType));

                // Debugging Output
                //GD.Print($"Assigned TileType {tileType} at ({row}, {col})");

                SpawnTile(row, col, tileType, 1);
            }
        }
    }

    /// <summary>
    /// Ensures there are no immediate matches when the board is first created.
    /// If a match is found, it replaces the tiles and checks again.
    /// </summary>
    private void EnsureNoInitialMatches()
    {
        Random random = new Random();
        HashSet<Vector2I> matchedPositions = new HashSet<Vector2I>();

        bool hasMatches;
        do
        {
            hasMatches = false;
            matchedPositions.Clear();

            // Step 1: Identify all matches
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    MergeTile tile = _tiles[row, col];
                    if (tile == null) continue;

                    List<MergeTile> matches = FindConnectedMatches(tile);
                    if (matches.Count >= 3)
                    {
                        hasMatches = true;
                        foreach (MergeTile matchedTile in matches)
                            matchedPositions.Add(new Vector2I(matchedTile.Row, matchedTile.Col));
                    }
                }
            }

            // Step 2: Replace all matched tiles
            foreach (Vector2I pos in matchedPositions)
            {
                int newType;
                int attempts = 0;
                do
                {
                    newType = random.Next(TileTextureResources.Length);
                    attempts++;
                } while (attempts < 100 && WouldCauseMatch(pos.X, pos.Y, newType));

                RemoveTile(_tiles[pos.X, pos.Y]);
                SpawnTile(pos.X, pos.Y, newType, 1);
            }
        } while (hasMatches);
    }


    private bool WouldCauseMatch(int row, int col, int tileType)
    {
        int requiredMatches = 2; // Need at least 2 neighbors of the same type to form a match

        // Check horizontal match
        if (col >= requiredMatches &&
            _tiles[row, col - 1] != null &&
            _tiles[row, col - 2] != null &&
            _tiles[row, col - 1].TileType == tileType &&
            _tiles[row, col - 2].TileType == tileType)
        {
            return true;
        }

        // Check vertical match
        if (row >= requiredMatches &&
            _tiles[row - 1, col] != null &&
            _tiles[row - 2, col] != null &&
            _tiles[row - 1, col].TileType == tileType &&
            _tiles[row - 2, col].TileType == tileType)
        {
            return true;
        }

        return false;
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
    #region Match Handling
    /// <summary>
    /// Finds all matching tiles starting from the given (row, col) in a specified direction.
    /// </summary>
    private List<MergeTile> FindMatches(int row, int col, int rowStep, int colStep)
    {
        List<MergeTile> matchingTiles = new List<MergeTile>();

        MergeTile baseTile = _tiles[row, col];
        if (baseTile == null) return matchingTiles;

        int baseTileType = baseTile.TileType;
        int baseTileLevel = baseTile.Level;

        matchingTiles.Add(baseTile);

        // Look ahead in the specified direction for matching tiles
        for (int i = 1; i < 3; i++)  // Looking ahead 2 more tiles
        {
            int nextRow = row + i * rowStep;
            int nextCol = col + i * colStep;

            // Break if out of bounds
            if (nextRow < 0 || nextRow >= Rows || nextCol < 0 || nextCol >= Cols)
                break;

            MergeTile nextTile = _tiles[nextRow, nextCol];
            if (nextTile != null && nextTile.TileType == baseTileType && nextTile.Level == baseTileLevel)
            {
                matchingTiles.Add(nextTile);
            }
            else
            {
                break;  // No match
            }
        }

        return matchingTiles;
    }

    /// <summary>
    /// Handles the replacement of matched tiles, upgrading some and removing others.
    /// </summary>
    private void HandleMatchReplacement(HashSet<MergeTile> matchedTiles)
    {
        int matchedCount = matchedTiles.Count;

        if (matchedCount < 3) return;  // Ignore if not enough tiles to match

        // Calculate how many tiles to level up
        int tilesToUpgrade = (matchedCount / 2) - 1;

        // Manually sorting the tiles by position using a simple sort method (bubble sort, for example)
        List<MergeTile> sortedTiles = SortTilesByPosition(matchedTiles);

        // Upgrade the necessary tiles
        for (int i = 0; i < tilesToUpgrade; i++)
        {
            MergeTile tileToUpgrade = sortedTiles[i];
            int newLevel = tileToUpgrade.Level + 1;
            tileToUpgrade.Level = newLevel;

            // Update the tile appearance
            tileToUpgrade.UpdateAppearance();
        }

        // Remove the rest of the tiles in the match
        for (int i = tilesToUpgrade; i < sortedTiles.Count; i++)
        {
            RemoveTile(sortedTiles[i]);
        }
    }

    /// <summary>
    /// Sorts the matched tiles by position using a simple sorting algorithm.
    /// </summary>
    private List<MergeTile> SortTilesByPosition(HashSet<MergeTile> matchedTiles)
    {
        List<MergeTile> sortedList = new List<MergeTile>(matchedTiles);

        // Simple bubble sort (you can replace this with a more efficient sorting algorithm if needed)
        for (int i = 0; i < sortedList.Count; i++)
        {
            for (int j = 0; j < sortedList.Count - 1; j++)
            {
                if (sortedList[j].Position.Y > sortedList[j + 1].Position.Y)
                {
                    // Swap the elements
                    MergeTile temp = sortedList[j];
                    sortedList[j] = sortedList[j + 1];
                    sortedList[j + 1] = temp;
                }
            }
        }

        return sortedList;
    }

    /// <summary>
    /// Checks for matches at the given (row, col), and replaces the matched tiles if necessary.
    /// </summary>
    private bool CheckForMatchAt(int row, int col)
    {
        // Get horizontal and vertical matches
        var horizontalMatches = FindMatches(row, col, 0, 1);
        var verticalMatches = FindMatches(row, col, 1, 0);

        // Manually merge the two match lists
        List<MergeTile> allMatches = new List<MergeTile>();

        // Add the horizontal matches to the list
        foreach (var tile in horizontalMatches)
        {
            allMatches.Add(tile);
        }

        // Add the vertical matches to the list
        foreach (var tile in verticalMatches)
        {
            allMatches.Add(tile);
        }

        // Remove duplicates manually (use hash set logic)
        HashSet<MergeTile> uniqueMatches = new HashSet<MergeTile>(allMatches);

        if (uniqueMatches.Count >= 3)
        {
            HandleMatchReplacement(uniqueMatches);
            return true;  // Match found
        }

        return false;  // No match found
    }

    #endregion

    #region Tile Management
    /// <summary>
    /// Removes a tile from the grid and frees it.
    /// </summary>
    private void RemoveTile(MergeTile tile)
    {
        _tiles[tile.Row, tile.Col] = null;
        tile.QueueFree();
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
            //GD.PrintErr("ERROR: TileTextureResources is NULL!");
            return null;
        }

        if (tileType >= TileTextureResources.Length)
        {
            //GD.PrintErr($"ERROR: Invalid tileType {tileType}, but we have {TileTextureResources.Length} tile types!");
            return null;
        }

        MergeTileTextureData tileData = TileTextureResources[tileType];

        if (tileData == null || tileData.LevelTextures.Count == 0)
        {
            //GD.PrintErr($"ERROR: Tile texture data is missing or empty for type {tileType}");
            return null;
        }

        int index = Mathf.Clamp(level - 1, 0, tileData.LevelTextures.Count - 1);

        //GD.Print($"Fetching texture for TileType {tileType}, Level {level}, using index {index} -> {tileData.LevelTextures[index].ResourcePath}");

        return tileData.LevelTextures[index];
    }



    /// <summary>
    /// Checks if a tile should merge after a swap.
    /// </summary>
    private void CheckAndMerge(MergeTile tile)
    {
        List<MergeTile> matchingTiles = FindConnectedMatches(tile);

        if (matchingTiles.Count >= 3)
        {
            MergeTiles(matchingTiles, tile);
        }
    }

    /// <summary>
    /// Finds all connected matching tiles using a flood-fill approach.
    /// </summary>
    private List<MergeTile> FindConnectedMatches(MergeTile baseTile)
    {
        List<MergeTile> matchingTiles = new List<MergeTile>();
        if (baseTile == null) return matchingTiles;

        int baseTileType = baseTile.TileType;
        int baseTileLevel = baseTile.Level;

        HashSet<MergeTile> visited = new HashSet<MergeTile>();
        Queue<MergeTile> queue = new Queue<MergeTile>();

        queue.Enqueue(baseTile);
        visited.Add(baseTile);

        while (queue.Count > 0)
        {
            MergeTile currentTile = queue.Dequeue();
            matchingTiles.Add(currentTile);

            foreach (MergeTile neighbor in GetNeighbors(currentTile))
            {
                if (!visited.Contains(neighbor) &&
                    neighbor.TileType == baseTileType &&
                    neighbor.Level == baseTileLevel)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return matchingTiles;
    }
    /// <summary>
    /// Gets the four adjacent tiles of the given tile.
    /// </summary>
    private List<MergeTile> GetNeighbors(MergeTile tile)
    {
        List<MergeTile> neighbors = new List<MergeTile>();

        int[,] directions = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } }; // Right, Left, Down, Up

        for (int i = 0; i < directions.GetLength(0); i++) // Use a standard for-loop
        {
            int newRow = tile.Row + directions[i, 0];
            int newCol = tile.Col + directions[i, 1];

            if (newRow >= 0 && newRow < Rows && newCol >= 0 && newCol < Cols)
            {
                if (_tiles[newRow, newCol] != null)
                {
                    neighbors.Add(_tiles[newRow, newCol]);
                }
            }
        }

        return neighbors;
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
     /// After refilling, it checks for new matches and processes them.
     /// </summary>
    private async Task AnimateTileRefill()
    {
        List<Tween> tweens = new List<Tween>();

        // Step 1: Let existing tiles fall into place
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

        // Step 2: Spawn new tiles at the top
        SpawnNewTiles(tweens);

        // Wait for new tile animations to complete
        if (tweens.Count > 0)
            await ToSignal(tweens[^1], Tween.SignalName.Finished);

        // Step 3: Handle new matches after refilling
        await HandlePostRefillMerges();

        // Reposition all tiles
        PositionTiles();
    }
    /// <summary>
    /// Checks for and processes merges after a board refill.
    /// </summary>
    private async Task HandlePostRefillMerges()
    {
        bool mergeFound;

        do
        {
            mergeFound = false;
            HashSet<MergeTile> allMatchedTiles = new HashSet<MergeTile>();

            // Step 1: Find all new matches
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    MergeTile tile = _tiles[row, col];
                    if (tile != null)
                    {
                        List<MergeTile> matchingTiles = FindConnectedMatches(tile);
                        if (matchingTiles.Count >= 3)
                        {
                            allMatchedTiles.UnionWith(matchingTiles);
                            mergeFound = true;
                        }
                    }
                }
            }

            // Step 2: Process Matches
            if (allMatchedTiles.Count >= 3)
            {
                await MergeAndRemoveTiles(allMatchedTiles);
            }

        } while (mergeFound); // Keep checking for new merges
    }
    /// <summary>
    /// Merges matched tiles by selecting the lowest, leftmost tile, leveling it up, and removing others.
    /// </summary>
    private async Task MergeAndRemoveTiles(HashSet<MergeTile> matchedTiles)
    {
        if (matchedTiles.Count < 3) return;

        // Step 1: Identify the lowest-leftmost tile
        MergeTile mainTile = GetLowestLeftmostTile(matchedTiles);
        if (mainTile == null) return;

        List<Tween> tweens = new List<Tween>();

        // Step 2: Remove other tiles with animation
        foreach (MergeTile tile in matchedTiles)
        {
            if (tile != mainTile)
            {
                _tiles[tile.Row, tile.Col] = null;

                Tween tween = CreateTween();
                tween.TweenProperty(tile, "scale", Vector2.Zero, 0.3f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);
                tween.TweenProperty(tile, "modulate:a", 0, 0.3f);
                tweens.Add(tween);
            }
        }

        // Wait for animations
        if (tweens.Count > 0)
            await ToSignal(tweens[^1], Tween.SignalName.Finished);

        // Remove matched tiles
        foreach (MergeTile tile in matchedTiles)
        {
            if (tile != mainTile)
            {
                tile.QueueFree();
            }
        }

        // Step 3: Level up the selected tile
        mainTile.Level++;
        mainTile.UpdateAppearance();

        // Step 4: Trigger another refill in case gaps appear
        await AnimateTileRefill();
    }

    /// <summary>
    /// Gets the lowest and leftmost tile from a set of matched tiles.
    /// </summary>
    private MergeTile GetLowestLeftmostTile(HashSet<MergeTile> tiles)
    {
        MergeTile selectedTile = null;

        foreach (MergeTile tile in tiles)
        {
            if (selectedTile == null ||
                tile.Row > selectedTile.Row ||  // Prioritize lower row
                (tile.Row == selectedTile.Row && tile.Col < selectedTile.Col)) // If same row, prioritize leftmost column
            {
                selectedTile = tile;
            }
        }

        return selectedTile;
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
