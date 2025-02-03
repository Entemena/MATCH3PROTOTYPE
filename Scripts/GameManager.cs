using Godot;
using System;

public enum GameState
{
    MainMenu,
    Puzzle,
    Paused,
    GameOver
}

public partial class GameManager : Node
{
    // Singleton instance.
    public static GameManager Instance { get; private set; }

    // Global game state (not including board-specific states).
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // Global game data (score, settings, etc.).
    public int GlobalScore { get; set; } = 0;

    // A reference to the current puzzle board (if any).
    public BoardManager CurrentBoard { get; set; } = null;

    private bool _isPaused = false;
    private Control _pauseMenu;

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
            return;
        }

        GD.Print("GameManager ready.");
        SetState(GameState.MainMenu);
        CallDeferred(nameof(DeferredLoadMainMenu));

        // Load Pause Menu but keep it hidden
        _pauseMenu = (Control)GD.Load<PackedScene>("res://Scenes/PauseMenu.tscn").Instantiate();
        AddChild(_pauseMenu);
        _pauseMenu.Visible = false;
        // Ensure PauseMenu is the last child (rendered on top)
        MoveChild(_pauseMenu, GetChildCount() - 1);
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            TogglePause(!_isPaused);
        }
    }
    public void TogglePause(bool pause)
    {
        _isPaused = pause;
        _pauseMenu.Visible = _isPaused;
        GetTree().Paused = _isPaused;
    }

    /// <summary>
    /// Changes the global game state.
    /// </summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        GD.Print("Game state changed to: " + newState);
    }

    /// <summary>
    /// Starts a new game by switching to the puzzle scene.
    /// </summary>
    public void StartNewGame()
    {
        SetState(GameState.Puzzle);
        GD.Print("Starting new game...");
        // Change the scene to your puzzle scene. Adjust the path as needed.
        GetTree().ChangeSceneToFile("res://Scenes/PuzzleScene.tscn");
    }

    /// <summary>
    /// Returns to the Main Menu.
    /// </summary>
    public void ShowMainMenu()
    {
        SetState(GameState.MainMenu);

        if (_pauseMenu != null)
        {
            _pauseMenu.Visible = false; // Hide pause menu when switching scenes
        }

        GetTree().Paused = false; // Ensure game is not paused

        CallDeferred(nameof(DeferredLoadMainMenu));
    }
    private void DeferredLoadMainMenu()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
    }

    /// <summary>
    /// Continues an existing game (implementation depends on your save system).
    /// </summary>
    public void ContinueGame()
    {
        // Implement your continue logic here.
    }

    /// <summary>
    /// Loads a saved game.
    /// </summary>
    public void LoadGame()
    {
        // Implement your load game logic here.
    }
}
