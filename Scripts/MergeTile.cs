using Godot;
using System;

/// <summary>
/// Represents a single tile in the Merge-3 puzzle.
/// </summary>
public partial class MergeTile : Area2D
{
    public int TileType { get;  set; }  // Store tile type
    public int Level { get;  set; } = 1;
    public MergeBoardManager Board { get; private set; }
    public int Row { get; set; }
    public int Col { get; set; }

    private AnimatedSprite2D _highlightAnim;

    public override void _Ready()
    {
        InitializeHighlight();
    }

    private void InitializeHighlight()
    {
        _highlightAnim = GetNodeOrNull<AnimatedSprite2D>("HighlightAnim");
        if (_highlightAnim == null)
        {
            GD.PrintErr("ERROR: HighlightAnim not found in MergeTile!");
        }
        else
        {
            _highlightAnim.Visible = false;
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (_highlightAnim == null) return;

        _highlightAnim.Visible = isHighlighted;
        if (isHighlighted)
            _highlightAnim.Play("select");
        else
            _highlightAnim.Stop();
    }

    public void Initialize(int tileType, int level, MergeBoardManager board, int row, int col)
    {
        TileType = tileType;
        Level = level;
        Board = board;
        Row = row;
        Col = col;
        UpdateAppearance();
    }

    public void UpdateAppearance()
    {
        Sprite2D sprite = GetNode<Sprite2D>("Sprite2D");
        if (sprite != null)
        {
            Texture2D newTexture = Board.GetTileTexture(TileType, Level);

            if (newTexture != null)
            {
                sprite.Texture = newTexture;
                GD.Print($"Updated tile appearance: TileType {TileType}, Level {Level} -> {newTexture.ResourcePath}");
            }
            else
            {
                GD.PrintErr($"ERROR: Failed to get texture for TileType {TileType}, Level {Level}");
            }
        }
    }



    private void OnTileInputEvent(Viewport viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            Board.OnTileClicked(this);
        }
    }
}
