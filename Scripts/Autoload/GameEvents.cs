using Game.Component;
using Godot;

namespace Game.Autoload;

public partial class GameEvents : Node
{
    public static GameEvents Instance { get; private set; }

    [Signal]
    public delegate void BuildingPlacedEventHandler(BulidingComponent buildingComponent);

    [Signal]
    public delegate void BuildingDestroyedEventHandler(BulidingComponent buildingComponent);


    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Instance = this;

    }

    public static void EmitBuildingPlaced(BulidingComponent buildingComponent) =>
        Instance.EmitSignal(SignalName.BuildingPlaced, buildingComponent);

    public static void EmitBuildingDestroyed(BulidingComponent buildingComponent) =>
        Instance.EmitSignal(SignalName.BuildingDestroyed, buildingComponent); 
   
}
