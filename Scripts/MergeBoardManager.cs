using Godot;
using System;
using System.Collections.Generic;

public partial class MergeBoardManager : Node2D
{
    [Export] public int Rows = 5;
    [Export] public int Cols = 5;
    [Export] public PackedScene MergeTileScene;
    [Export] public Texture2D[] MergeTileTextures;

    private MergeTile[,] _tiles;

    public override void _Ready()
    {
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        _tiles = new MergeTile[Rows, Cols];
        Random random = new Random();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                // Spawn a new tile with a random level
                int level = random.Next(1, 4); // Levels 1-3
                SpawnTile(row, col, level);
            }
        }
    }

    private void SpawnTile(int row, int col, int level)
    {
        MergeTile tileInstance = MergeTileScene.Instantiate<MergeTile>();
        AddChild(tileInstance);
        tileInstance.Initialize(level, this, row, col);
        _tiles[row, col] = tileInstance;
    }

    public Texture2D GetTileTexture(int level)
    {
        return MergeTileTextures[Mathf.Clamp(level - 1, 0, MergeTileTextures.Length - 1)];
    }

    public void TryMerge(int row, int col)
    {
        MergeTile tile = _tiles[row, col];
        if (tile == null) return;

        List<MergeTile> matchingTiles = FindMatchingTiles(tile);

        if (matchingTiles.Count >= 3)
        {
            MergeTiles(matchingTiles);
        }
    }

    private List<MergeTile> FindMatchingTiles(MergeTile baseTile)
    {
        List<MergeTile> matchingTiles = new List<MergeTile> { baseTile };

        foreach ((int r, int c) in GetAdjacentTiles(baseTile.Row, baseTile.Col))
        {
            MergeTile neighbor = _tiles[r, c];
            if (neighbor != null && neighbor.Level == baseTile.Level)
            {
                matchingTiles.Add(neighbor);
            }
        }

        return matchingTiles;
    }

    private IEnumerable<(int, int)> GetAdjacentTiles(int row, int col)
    {
        return new List<(int, int)>
        {
            (row - 1, col), (row + 1, col),
            (row, col - 1), (row, col + 1)
        };
    }

    private void MergeTiles(List<MergeTile> tilesToMerge)
    {
        MergeTile baseTile = tilesToMerge[0];

        foreach (var tile in tilesToMerge)
        {
            if (tile != baseTile)
            {
                tile.QueueFree();
                _tiles[tile.Row, tile.Col] = null;
            }
        }

        baseTile.Level++;
        baseTile.UpdateAppearance();
    }
}
