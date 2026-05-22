using System;
using Game.Resources;
using Godot;

namespace Game.UI;

public partial class LevelSelection : PanelContainer
{

    [Signal]
    public delegate void LevelSelectedEventHandler(int index);


    private Button button;

    private Label resourceCountLabel;

    private Label levelNumberLabel;

    private int levelIndex;

    public override void _Ready()
    {
        button = GetNode<Button>("%Button");
        resourceCountLabel = GetNode<Label>("%ResourceCountLabel");
        levelNumberLabel = GetNode<Label>("%LevelNumberLabel");

        button.Pressed += OnSelectLevelButtonPressed;
    }


    public void SetLevelStartingResourceCount(LevelResource levelResource)
    {
        resourceCountLabel.Text = levelResource.StaringResourcesCount.ToString();
    }


    public void SetLevelNumber(int index)
    {
        levelIndex = index;
        levelNumberLabel.Text = $"Level {index + 1}";
    }

    private void OnSelectLevelButtonPressed()
    {
        EmitSignal(SignalName.LevelSelected, levelIndex);
    }
}
