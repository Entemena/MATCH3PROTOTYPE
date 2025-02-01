using Godot;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    [Signal] public delegate void ScoreUpdatedEventHandler(int newScore);

    private int _score;
    private BoardManager _board;

    public override void _EnterTree()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    public void Initialize(BoardManager board)
    {
        _board = board;
        _score = 0;
    }

    public void AddScore(int points)
    {
        _score += points;
        EmitSignal(SignalName.ScoreUpdated, _score);
    }

    public void ResetGame()
    {
        _board.ResetBoard();
        _score = 0;
        EmitSignal(SignalName.ScoreUpdated, _score);
    }
}
