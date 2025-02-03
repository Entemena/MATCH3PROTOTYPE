using Godot;
using System;

public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        CallDeferred(nameof(ApplyAnchorsAndResize));
        GetViewport().Connect("size_changed", new Callable(this, nameof(ApplyAnchorsAndResize)));

        // Connect button signals
        Button resumeGameButton = GetNode<Button>("VBoxContainer/ResumeGameButton");
        Button settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");
        Button quitGameButton = GetNode<Button>("VBoxContainer/QuitGameButton");

        resumeGameButton.Pressed += OnResumePressed;
        settingsButton.Pressed += OnSettingsButtonPressed;
        quitGameButton.Pressed += OnQuitPressed;
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

    public override void _EnterTree()
    {
        ProcessMode = ProcessModeEnum.Always; // Allow UI interactions even when paused
    }

    private void OnResumePressed()
    {
        GameManager.Instance?.TogglePause(false);
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("Settings button pressed! (To Be Implemented)");
    }

    private void OnQuitPressed()
    {
        GameManager.Instance?.ShowMainMenu();
    }
}
