using Game.Resources;
using Godot;


namespace Game.UI;

public partial class GameUI : MarginContainer
{

    [Signal]
    public delegate void BuildingResourceSelectedEventHandler(BuildingResource resource);

    [Export]
    private BuildingResource[] resource;

    private HBoxContainer hBoxContainer;

    public override void _Ready()
    {
        hBoxContainer = GetNode<HBoxContainer>("HBoxContainer");

        CreateBuildingButtons();


    }

    private void CreateBuildingButtons()
    {
        foreach (var item in resource)
        {
            var button = new Button();
            button.Text = $"Place {item.DisplayName}";
            hBoxContainer.AddChild(button);

            button.Pressed += () => EmitSignal(SignalName.BuildingResourceSelected, item);
        }
    }



}
