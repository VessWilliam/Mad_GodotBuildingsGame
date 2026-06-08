namespace Game.Extentions;

using Godot;


public static class GodotObjectExtensions
{

    public static bool IsValid(this GodotObject obj) => GodotObject.IsInstanceValid(obj);

    public static void SafeQueueFree(this Node node)
    {
        if (node.IsValid()) node.QueueFree();
    }
}