using Godot;

public partial class Tile : Area2D
{
    private bool _isAnimating;
    public bool IsAnimating
    {
        get => _isAnimating;
        set => SetAnimatingState(value);
    }

    public int TileType { get; set; }
    public BoardManager Board { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }

    private AnimatedSprite2D _highlightAnim;

    public override void _Ready()
    {
        InitializeHighlight();
    }

    private void InitializeHighlight()
    {
        _highlightAnim = GetNode<AnimatedSprite2D>("HighlightAnim");
        _highlightAnim.Visible = false;
    }

    private void OnTileInputEvent(Viewport viewport, InputEvent @event, long shapeIdx)
    {
        if (IsAnimating || !Visible) return;
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            Board.OnTileClicked(this);
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        _highlightAnim.Visible = isHighlighted;
        if (isHighlighted)
            _highlightAnim.Play("select");
        else
            _highlightAnim.Stop();
    }
    private void SetAnimatingState(bool value)
    {
        _isAnimating = value;
        Monitorable = !value;  // Disable interactions during animation
        Monitoring = !value;
    }
}