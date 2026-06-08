using Game.Buildings;
using Game.Resources;
using Godot;
namespace Game.UI;

public partial class GameUI : CanvasLayer
{
    [Signal]
    public delegate void BuildingResourceSelectedEventHandler(BuildingResource resource);

    [Export]
    private BuildingManager buildingManager;

    [Export]
    private BuildingResource[] resource;

    [Export]
    private PackedScene buildSelectionScene;

    private VBoxContainer buildSelectionContainer;
    private Label resourceLabel;
    
    public override void _Ready()
    {
        buildSelectionContainer = GetNode<VBoxContainer>("%BuildSelectionContainer");

        resourceLabel = GetNode<Label>("%ResourceLabel");

        buildingManager.AvailableResourceCountChanged += (availableResourceCount) => resourceLabel.Text = availableResourceCount.ToString();
        CreateBuildingSelection();
    }

    public void HideUI() => Visible = false;
    
    private void CreateBuildingSelection()
    {
        foreach (var item in resource)
        {
            var buildselection = buildSelectionScene.Instantiate<BuildSelection>();
            buildSelectionContainer.AddChild(buildselection);
            buildselection.SetBuildResource(item);

            buildselection.SelectButtonPressed += () => EmitSignal(SignalName.BuildingResourceSelected, item);
        }
    }
}
