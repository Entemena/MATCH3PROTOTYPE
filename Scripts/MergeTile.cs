using Godot;
using System;

public partial class MergeTile : Area2D
{
    public int Level { get; set; } = 1; // Starts at level 1
    public MergeBoardManager Board { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }

    public void Initialize(int level, MergeBoardManager board, int row, int col)
    {
        Level = level;
        Board = board;
        Row = row;
        Col = col;
        UpdateAppearance();
    }

    public void UpdateAppearance()
    {
        // Change sprite/visuals based on Level
        Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
        if (sprite != null)
        {
            sprite.Texture = Board.GetTileTexture(Level);
        }
    }

    public void MergeInto(MergeTile otherTile)
    {
        if (otherTile == null || otherTile.Level != Level)
            return;

        // Merge tiles, increase level
        Level++;
        otherTile.QueueFree(); // Remove the merged tile
        UpdateAppearance();
    }
}
