using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class LevelCompleteScreen : CanvasLayer
{
    [Export(PropertyHint.File, "*.tscn")]
    private string mainMenuScenePath;

    private Button nextLevelButton;

    public override void _Ready()
    {
        nextLevelButton = GetNode<Button>("%NextLevelBtn");

        AudioEvents.PlayVictory();

        if (LevelEvents.IsLastLevel())
        {
            nextLevelButton.Text = "Retuern to Menu";
        }

        nextLevelButton.Pressed += OnNextLevelButtonPressed;
    }


    private void OnNextLevelButtonPressed()
    {

        if (!LevelEvents.IsLastLevel())
        {
            LevelEvents.NextLevel();
            return;
        }

        GetTree().ChangeSceneToFile(mainMenuScenePath);
    }
}
