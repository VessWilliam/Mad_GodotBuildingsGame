using Game.Autoload;
using Godot;

namespace Game.UI;


public partial class LevelSelectScreen : MarginContainer
{

    [Signal]
    public delegate void HomePressedEventHandler();

    [Export]
    private PackedScene levelSelectionScene;

    private GridContainer gridContainer;
    private Button homeButton;

    public override void _Ready()
    {
        gridContainer = GetNode<GridContainer>("%GridContainer");        
        homeButton = GetNode<Button>("%HomeButton");

        var levels = LevelEvents.GetLevelResources();

        for (var i = 0; i < levels.Length; i++)
        {
            var levelScene = levels[i];

            var levelSelectScene = levelSelectionScene.Instantiate<LevelSelection>();

            gridContainer.AddChild(levelSelectScene);

            levelSelectScene.SetLevelStartingResourceCount(levelScene);
            levelSelectScene.SetLevelNumber(i);
            levelSelectScene.LevelSelected += OnLevelSelected;
        }


        homeButton.Pressed += OnHomeButtonPressed;
    }

    private void OnHomeButtonPressed() => EmitSignal(SignalName.HomePressed);
    
    private void OnLevelSelected(int index) => LevelEvents.Instance.ChangeLevel(index);
}
