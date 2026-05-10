using Godot;

namespace Game.Buildings;

public partial class BuildingGhost : Node2D
{

    public void SetInvalid() => Modulate = Colors.Red;     
     

    public void SetValid() => Modulate = Colors.White;
    
}
