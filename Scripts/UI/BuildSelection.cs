using Game.Autoload;
using Game.Resources;
using Godot;

namespace Game.UI;

public partial class BuildSelection : PanelContainer
{
    [Signal]
    public delegate void SelectButtonPressedEventHandler();

    private Label titleLabel;
    private Label descLabel;
    private Label costLabel;
    private Button selectButton;

    public override void _Ready()
    {
        titleLabel = GetNode<Label>("%TitleLabel");
        descLabel = GetNode<Label>("%DescLabel");
        costLabel = GetNode<Label>("%CostLabel");

        selectButton = GetNode<Button>("%SelectBuildingButton");

        AudioEvents.RegisterButton([selectButton]);

        selectButton.Pressed += () => EmitSignal(SignalName.SelectButtonPressed);
    }

    public void SetBuildResource(BuildingResource resource)
    {
        titleLabel.Text = resource.DisplayName;
        costLabel.Text = $"{resource.ResourceCost}";
        descLabel.Text = $"{resource.Description}";

    }
}
