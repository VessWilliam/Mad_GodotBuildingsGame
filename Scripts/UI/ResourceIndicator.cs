using Godot;


namespace Game.UI;

public partial class ResourceIndicator : Node2D
{
    private AnimatedSprite2D animatedSprite2D;

    private Tween activeTween;

    public override void _Ready()
    {
        animatedSprite2D = GetNode<AnimatedSprite2D>("%AnimatedSprite2D");

        var duration = GD.RandRange(0.5f, 0.55f);

        activeTween = CreateTween();
        activeTween.SetLoops();
        activeTween.TweenProperty(animatedSprite2D, "position", Vector2.Up * 5, duration)
                    .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.InOut);
        activeTween.TweenProperty(animatedSprite2D, "position", Vector2.Down * 5, duration)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.InOut);
    }

    public void Destroy()
    {
        if (activeTween is not null && activeTween.IsValid())
            activeTween.Kill();

        activeTween = CreateTween();
        activeTween.SetParallel();
        activeTween.TweenInterval(GD.RandRange(0.1f, 0.3f));
        activeTween.Chain();
        activeTween.TweenProperty(animatedSprite2D, "scale", Vector2.Zero, 0.2)
                   .SetTrans(Tween.TransitionType.Back)
                   .SetEase(Tween.EaseType.In);

        activeTween.TweenProperty(animatedSprite2D, "position", Vector2.Up * 32, 0.3)
                   .SetTrans(Tween.TransitionType.Quad)
                   .SetEase(Tween.EaseType.In);

        activeTween.Chain();
        activeTween.TweenCallback(Callable.From(() => QueueFree()));

    }
}
