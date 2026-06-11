using System.Collections.Generic;
using Godot;

namespace Game.Extentions;

public static class Rect2IExtentions
{
    public static List<Vector2I> ToTiles(this Rect2I rect)
    {
        var result = new List<Vector2I>(rect.Size.X * rect.Size.Y);

        for (int x = rect.Position.X; x < rect.End.X; x++)
        {
            for (int y = rect.Position.Y; y < rect.End.Y; y++)
            {
                result.Add(new Vector2I(x, y));
            }
        }

        return result;
    }


    public static Rect2 ToRect2F(this Rect2I rect) => new(rect.Position, rect.Size);
}



