using Game.Autoload;
using Game.Resources;
using Godot;

namespace Game.UI;

public partial class LevelSelectScreen : MarginContainer
{
    private const int PAGE_SIZE = 6;

    [Signal]
    public delegate void HomePressedEventHandler();

    [Export]
    private PackedScene levelSelectionScene;

    private GridContainer gridContainer;
    private Button homeButton;
    private Button previosButton;
    private Button nextButton;

    private int pageIndex;
    private int maxPageIndex;
    private LevelResource[] levelResources;

    public override void _Ready()
    {
        gridContainer = GetNode<GridContainer>("%GridContainer");
        homeButton = GetNode<Button>("%HomeButton");

        previosButton = GetNode<Button>("%PrevButton");
        nextButton = GetNode<Button>("%NxtButton");
         
        AudioEvents.RegisterButton([homeButton, previosButton, nextButton]);


        levelResources = LevelEvents.GetLevelResources();
        maxPageIndex = levelResources.Length / PAGE_SIZE;

        homeButton.Pressed += OnHomeButtonPressed;
        previosButton.Pressed += () => OnPageChanged(-1);
        nextButton.Pressed += () => OnPageChanged(1);

        ShowPage();
    }

    private void ShowPage()
    {
        UpdateButtonVisibility();

        foreach (var item in gridContainer.GetChildren())
        {
            item.QueueFree();
        }

        var startIndex = pageIndex * PAGE_SIZE;
        var endIndex = Mathf.Min(startIndex + PAGE_SIZE, levelResources.Length);

        for (var i = startIndex; i < endIndex; i++)
        {
            var levelScene = levelResources[i];

            var levelSelectScene = levelSelectionScene.Instantiate<LevelSelection>();

            gridContainer.AddChild(levelSelectScene);

            levelSelectScene.SetLevelStartingResourceCount(levelScene);
            levelSelectScene.SetLevelNumber(i);
            levelSelectScene.LevelSelected += OnLevelSelected;
        }
    }

    private void UpdateButtonVisibility()
    {
        previosButton.Disabled = pageIndex == 0;
        previosButton.Modulate = pageIndex == 0 ? Colors.Transparent : Colors.White;
        nextButton.Disabled = pageIndex == maxPageIndex;
        nextButton.Modulate = pageIndex == maxPageIndex ? Colors.Transparent : Colors.White;
    }

    private void OnHomeButtonPressed() => EmitSignal(SignalName.HomePressed);

    private void OnLevelSelected(int index) => LevelEvents.Instance.ChangeLevel(index);

    private void OnPageChanged(int change)
    {
        pageIndex += change;
        ShowPage();
    }
}
