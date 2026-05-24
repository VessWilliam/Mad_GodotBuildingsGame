using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{

    private Button playButton;
    private Control mainMenuConatainer;
    private LevelSelectScreen levelSelectScreen;
    private Button exitButton;

    public override void _Ready()
    {
        playButton = GetNode<Button>("%PlayButton");
        exitButton = GetNode<Button>("%ExitButton");
        mainMenuConatainer = GetNode<Control>("%MainMenuContainer");
        levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

        levelSelectScreen.Visible = false;
        mainMenuConatainer.Visible = true;

        playButton.Pressed += OnPlayButtonPressed;
        exitButton.Pressed += OnExitButtonPressed;
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
}
