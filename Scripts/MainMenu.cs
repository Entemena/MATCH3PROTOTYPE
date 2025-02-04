using Godot;
using System;

public partial class MainMenu : Control
{
    private Button matchGameButton;
    private Button mergeGameButton;
    private Button settingsButton;
    private Button quitGameButton;

    public override void _Ready()
    {
        ApplyAnchorsAndResize();
        GetViewport().Connect("size_changed", new Callable(this, nameof(ApplyAnchorsAndResize)));

        matchGameButton = GetNodeOrNull<Button>("VBoxContainer/MatchGameButton");
        mergeGameButton = GetNodeOrNull<Button>("VBoxContainer/MergeGameButton");
        settingsButton = GetNodeOrNull<Button>("VBoxContainer/SettingsButton");
        quitGameButton = GetNodeOrNull<Button>("VBoxContainer/QuitGameButton");

        if (matchGameButton != null) matchGameButton.Pressed += OnMatchGameButtonPressed;
        if (mergeGameButton != null) mergeGameButton.Pressed += OnMergeGameButtonPressed;
        if (settingsButton != null) settingsButton.Pressed += OnSettingsButtonPressed;
        if (quitGameButton != null) quitGameButton.Pressed += OnQuitButtonPressed;
    }

    private void ApplyAnchorsAndResize()
    {
        // Ensure the menu stretches fully and stays centered
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;

        SetSize(GetViewportRect().Size);
    }
    /// <summary>
    /// Potentially not needed, can be used with CallDeferred(nameof(DeferredSetup)) in _Ready()
    /// </summary>
    private void DeferredSetup()
    {
        matchGameButton = GetNodeOrNull<Button>("VBoxContainer/MatchGameButton");
        mergeGameButton = GetNodeOrNull<Button>("VBoxContainer/MergeGameButton");
        settingsButton = GetNodeOrNull<Button>("VBoxContainer/SettingsButton");
        quitGameButton = GetNodeOrNull<Button>("VBoxContainer/QuitGameButton");

        GD.Print($"Match Button found: {matchGameButton != null}");
        GD.Print($"Merge Button found: {mergeGameButton != null}");
        GD.Print($"Settings Button found: {settingsButton != null}");
        GD.Print($"Quit Button found: {quitGameButton != null}");

        if (matchGameButton != null)
        {
            matchGameButton.Pressed += OnMatchGameButtonPressed;
            GD.Print("Match button connected.");
        }
        if (mergeGameButton != null)
        {
            mergeGameButton.Pressed += OnMergeGameButtonPressed;
            GD.Print("Merge button connected.");
        }
        if (settingsButton != null)
        {
            settingsButton.Pressed += OnSettingsButtonPressed;
            GD.Print("Settings button connected.");
        }
        if (quitGameButton != null)
        {
            quitGameButton.Pressed += OnQuitButtonPressed;
            GD.Print("Quit button connected.");
        }
    }


    private void OnMatchGameButtonPressed()
    {
        GD.Print("Match button pressed!");
        GameManager.Instance?.StartMatchGame();
    }
    private void OnMergeGameButtonPressed()
    {
        GD.Print("Merge button pressed!");
        GameManager.Instance?.StartMergeGame();
    }
    private void OnSettingsButtonPressed()
    {
        GD.Print("Settings button pressed! (To Be Implemented)");
    }

    private void OnQuitButtonPressed()
    {
        GD.Print("Quit button pressed!");
        GetTree().Quit();
    }
}
