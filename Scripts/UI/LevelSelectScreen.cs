using System;
using Game.Autoload;
using Godot;

namespace Game.UI;


public partial class LevelSelectScreen : MarginContainer
{
    [Export]
    private PackedScene levelSelectionScene;

    private GridContainer gridContainer;

    public override void _Ready()
    {
        gridContainer = GetNode<GridContainer>("%GridContainer");

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
    }

    private void OnLevelSelected(int index) => LevelEvents.Instance.ChangeLevel(index);
}
