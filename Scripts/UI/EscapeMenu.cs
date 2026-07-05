using Game.Autoload;
using Game.Utils;
using Godot;


namespace Game.UI;

public partial class EscapeMenu : CanvasLayer
{
    [Export(PropertyHint.File, "*.tscn")]
    private string mainMenuScenePath;

    [Export(PropertyHint.File, "*.tscn")]
    private PackedScene optionsMenuScene;

    private Button quitButton;
    private Button resumeButton;
    private Button optionsButton;
    private MarginContainer rootMarginContainer;

    public override void _Ready()
    {

        quitButton = GetNode<Button>("%QuitButton");
        resumeButton = GetNode<Button>("%ResumeButton");
        optionsButton = GetNode<Button>("%OptionsButton");
        rootMarginContainer = GetNode<MarginContainer>("%RootMarginContainer");
        
        AudioEvents.RegisterButton([
            quitButton,
            resumeButton,
            optionsButton
        ]);

        quitButton.Pressed += OnQuitButtonPressed;
        resumeButton.Pressed += OnResumeButtonPressed;
        optionsButton.Pressed += OnOptionsButtonPressed;
    }

    public override void _UnhandledInput(InputEvent evt)
    {
        if (evt.IsActionPressed(Constants.ESCAPE))
        {
            QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnQuitButtonPressed() => GetTree().ChangeSceneToFile(mainMenuScenePath);
    private void OnResumeButtonPressed() => QueueFree();

    private void OnOptionsButtonPressed()
    {
        rootMarginContainer.Visible = false;
        var optionsMenu = optionsMenuScene.Instantiate<OptionsMenu>();
        AddChild(optionsMenu);

        optionsMenu.DonePressed += () => OnOptionsMenuDonePressed(optionsMenu); ;
    }

    private void OnOptionsMenuDonePressed(OptionsMenu optionsMenu)
    {
        rootMarginContainer.Visible = true;
        optionsMenu.QueueFree();
    }
}
