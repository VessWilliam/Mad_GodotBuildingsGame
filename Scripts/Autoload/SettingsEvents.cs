using Godot;

namespace Game.Autoload;

public partial class SettingsEvents : Node
{
    public override void _Ready()
    {
       RenderingServer.SetDefaultClearColor(new Color("367978"));
       GetViewport().GetWindow().MinSize = new Vector2I(1280, 720);
    }

}
