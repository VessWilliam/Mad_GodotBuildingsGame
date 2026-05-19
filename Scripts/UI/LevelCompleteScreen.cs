using System;
using Game.Autoload;
using Godot;

namespace Game.UI;

public partial class LevelCompleteScreen : CanvasLayer
{
    private Button nextLevelButton;

    public override void _Ready()
    {
        nextLevelButton = GetNode<Button>("%NextLevelBtn");
        nextLevelButton.Pressed += () => LevelEvents.Instance.NextLevel();
    }
}
