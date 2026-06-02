using System.Collections.Generic;
using Godot;

namespace Game.Extentions;

public static class Rect2IExtentions
{
    public static IEnumerable<Vector2I> ToTiles(this Rect2I rect)
    {
        for (int x = rect.Position.X; x < rect.End.X; x++)
        {
            for (int y = rect.Position.Y; y < rect.End.Y; y++)
            {
                yield return new(x, y);
            }
        }
    }


    public static Rect2 ToRect2F(this Rect2I rect) => new(rect.Position, rect.Size);
}



