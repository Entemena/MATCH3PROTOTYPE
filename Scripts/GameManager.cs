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

        // Optionally, load the main menu scene here:
        // GetTree().ChangeScene("res://MainMenu.tscn");
    }

    /// <summary>
    /// Changes the global game state.
    /// </summary>
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        GD.Print("Game state changed to: " + newState.ToString());
    }

    /// <summary>
    /// Starts a new game by switching to the puzzle scene.
    /// </summary>
    public void StartNewGame()
    {
        SetState(GameState.Puzzle);
        GD.Print("Starting new game...");
        // Change the scene to your puzzle scene. Adjust the path as needed.
        GetTree().ChangeSceneToFile("res://PuzzleScene.tscn");
    }

    /// <summary>
    /// Returns to the Main Menu.
    /// </summary>
    public void ShowMainMenu()
    {
        SetState(GameState.MainMenu);
        // Change the scene to your main menu scene. Adjust the path as needed.
        GetTree().ChangeSceneToFile("res://MainMenu.tscn");
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
