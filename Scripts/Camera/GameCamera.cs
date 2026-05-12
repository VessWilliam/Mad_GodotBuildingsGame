using Godot;
using System;


namespace Game.Camera;

public partial class GameCamera : Camera2D
{
    private const int TILE_SIZE = 64;   
    private const float PAN_SPEED = 500;

    private readonly StringName ACTION_PAN_LEFT = "pan_left";
    private readonly StringName ACTION_PAN_RIGHT = "pan_right";
    private readonly StringName ACTION_PAN_UP = "pan_up";
    private readonly StringName ACTION_PAN_DOWN = "pan_down";

    public override void _Process(double delta)
    {
        GlobalPosition = GetScreenCenterPosition();

        var movementVector = Input
        .GetVector(ACTION_PAN_LEFT,
        ACTION_PAN_RIGHT,
        ACTION_PAN_UP,
        ACTION_PAN_DOWN);
    
        GlobalPosition += movementVector * PAN_SPEED * (float)delta;

    }

    public void SetBoundingRect(Rect2I rect)
    {
        LimitLeft = rect.Position.X * TILE_SIZE;

        LimitRight = rect.End.X * TILE_SIZE;

        LimitTop = rect.Position.Y * TILE_SIZE;

        LimitBottom = rect.End.Y * TILE_SIZE;
    }
}
