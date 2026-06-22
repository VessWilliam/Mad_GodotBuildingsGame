using Godot;

namespace Game.Camera;

public partial class GameCamera : Camera2D
{
    private const int TileSize = 64;
    private const float PanSpeed = 500f;
    private const float NoiseSampleGrowth = 0.1f;
    private const float MaxCameraOffset = 30f;
    private const float NoiseFrequencyMultiplier = 110f;
    private const float ShakeDecay = 3f;

    [Export] private FastNoiseLite shakeNoise;

    private static GameCamera instance;

    private Vector2 noiseSample;
    private float currentShakePercentage;

    private readonly StringName actionPanLeft = "pan_left";
    private readonly StringName actionPanRight = "pan_right";
    private readonly StringName actionPanUp = "pan_up";
    private readonly StringName actionPanDown = "pan_down";

    public static void Shake() => instance.currentShakePercentage = 1f;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            instance = this;
    }

    public override void _Process(double delta)
    {
        HandlePanning((float)delta);
        ClampPosition();
        ApplyCameraShake((float)delta);
    }

    public void SetBoundingRect(Rect2I rect)
    {
        LimitLeft   = rect.Position.X * TileSize;
        LimitRight  = rect.End.X      * TileSize;
        LimitTop    = rect.Position.Y * TileSize;
        LimitBottom = rect.End.Y      * TileSize;
    }

    public void SetCenter(Vector2 position) => GlobalPosition = position;

    // --- Private ---

    private void HandlePanning(float delta)
    {
        var movementVector = Input.GetVector(actionPanLeft, actionPanRight, actionPanUp, actionPanDown);
        GlobalPosition += movementVector * PanSpeed * delta;
    }

    private void ClampPosition()
    {
        var half = GetViewportRect().Size / 2f;

        var xMin = LimitLeft  + half.X;
        var xMax = LimitRight - half.X;
        var yMin = LimitTop   + half.Y;
        var yMax = LimitBottom - half.Y;

        var x = xMin < xMax ? Mathf.Clamp(GlobalPosition.X, xMin, xMax) : (LimitLeft  + LimitRight)  / 2f;
        var y = yMin < yMax ? Mathf.Clamp(GlobalPosition.Y, yMin, yMax) : (LimitTop   + LimitBottom) / 2f;

        GlobalPosition = new Vector2(x, y);
    }

    private void ApplyCameraShake(float delta)
    {
        if (currentShakePercentage > 0f)
        {
            var step = NoiseSampleGrowth * NoiseFrequencyMultiplier * delta;
            noiseSample += new Vector2(step, step);
            currentShakePercentage = Mathf.Clamp(currentShakePercentage - ShakeDecay * delta, 0f, 1f);
        }

        var xSample = shakeNoise.GetNoise2D(noiseSample.X, 0f);
        var ySample = shakeNoise.GetNoise2D(0f, noiseSample.Y);

        Offset = new Vector2(xSample, ySample) * MaxCameraOffset * currentShakePercentage;
    }
}