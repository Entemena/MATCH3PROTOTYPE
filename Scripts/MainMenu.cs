using Godot;
using System;

public partial class MainMenu : Control
{
    private Button newGameButton;
    private Button settingsButton;
    private Button quitGameButton;

    public override void _Ready()
    {
        CallDeferred(nameof(ApplyAnchorsAndResize));
        GetViewport().Connect("size_changed", new Callable(this, nameof(ApplyAnchorsAndResize)));

    CallDeferred(nameof(DeferredSetup));
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


    private void DeferredSetup()
    {
        newGameButton = GetNodeOrNull<Button>("VBoxContainer/NewGameButton");
        settingsButton = GetNodeOrNull<Button>("VBoxContainer/SettingsButton");
        quitGameButton = GetNodeOrNull<Button>("VBoxContainer/QuitGameButton");

        GD.Print($"Play Button found: {newGameButton != null}");
        GD.Print($"Settings Button found: {settingsButton != null}");
        GD.Print($"Quit Button found: {quitGameButton != null}");

        if (newGameButton != null)
        {
            newGameButton.Pressed += OnPlayButtonPressed;
            GD.Print("Play button connected.");
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


    private void OnPlayButtonPressed()
    {
        GD.Print("Play button pressed!");
        GameManager.Instance?.StartMatchGame();
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
