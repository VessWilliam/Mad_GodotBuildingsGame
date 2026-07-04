using System;
using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{
    [Export]
    private PackedScene optionsMenuScene;


    private Button playButton;
    private Control mainMenuConatainer;
    private LevelSelectScreen levelSelectScreen;
    private Button exitButton;
    private Button optionsButton;

    public override void _Ready()
    {
        playButton = GetNode<Button>("%PlayButton");
        exitButton = GetNode<Button>("%ExitButton");
        optionsButton = GetNode<Button>("%OptionsButton");
        mainMenuConatainer = GetNode<Control>("%MainMenuContainer");
        levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

        AudioEvents.RegisterButton([playButton, exitButton, optionsButton]);

        levelSelectScreen.Visible = false;
        mainMenuConatainer.Visible = true;

        playButton.Pressed += OnPlayButtonPressed;
        exitButton.Pressed += OnExitButtonPressed;
        optionsButton.Pressed += OnOptionsButtonPressed;
        levelSelectScreen.HomePressed += OnLevelSelectHomePressed;
    }

    private void OnExitButtonPressed() => GetTree().Quit();

    private void OnLevelSelectHomePressed()
    {
        mainMenuConatainer.Visible = true;
        levelSelectScreen.Visible = false;
    }

    private void OnPlayButtonPressed()
    {
        mainMenuConatainer.Visible = false;
        levelSelectScreen.Visible = true;
    }

    private void OnOptionsButtonPressed()
    {
        mainMenuConatainer.Visible = false;
        var optionsMenu = optionsMenuScene.Instantiate<OptionsMenu>();
        AddChild(optionsMenu);
        optionsMenu.DonePressed += () => OnOptionsMenuDonePressed(optionsMenu);
    }

    private void OnOptionsMenuDonePressed(OptionsMenu optionsMenu)
    {
        mainMenuConatainer.Visible = true;
        optionsMenu.QueueFree();
    }
}
