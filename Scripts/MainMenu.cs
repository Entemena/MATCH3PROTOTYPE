using Godot;
using System;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        // Get button nodes
        Button playButton = GetNode<Button>("VBoxContainer/PlayButton");
        Button settingsButton = GetNode<Button>("VBoxContainer/SettingsButton");
        Button quitButton = GetNode<Button>("VBoxContainer/QuitButton");

        // Connect buttons to event handlers
        playButton.Pressed += OnPlayButtonPressed;
        settingsButton.Pressed += OnSettingsButtonPressed;
        quitButton.Pressed += OnQuitButtonPressed;
    }

    private void OnPlayButtonPressed()
    {
        // Change to game scene
        GetTree().ChangeSceneToFile("res://GameScene.tscn");
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("Open Settings Menu (To Be Implemented)");
    }

    private void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }
}
