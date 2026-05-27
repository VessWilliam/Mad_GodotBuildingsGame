using System.Linq;
using Godot;

namespace Game.Extentions;

public static class NodeExtensions
{
    public static T GetFirstNodeOfType<T>(this Node node) where T : Node
    {
        foreach (var item in node.GetChildren())
        {
            if (item is T nodeOfType)
                return nodeOfType;

            var result = item.GetFirstNodeOfType<T>();

            if (result is not null)
                return result;
        }

        return null;
    }
}