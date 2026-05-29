using Game.Extentions;
using Godot;

namespace Game.Component;

public partial class BuildingAnimatorComponent : Node2D
{
    [Signal]
    public delegate void DestroyAnimationFinishedEventHandler();

    [Export]
    private PackedScene impactParticlesScene;

    [Export]
    private PackedScene destroyParticlesScene;

    [Export]
    private Texture2D maskTexture;

    private Tween activeTween;

    private Node2D animationRootNode;

    private Sprite2D maskNode;

    public override void _Ready()
    {
        SetUpNode();
        PlayPlaceAnimation();
    }

    public void PlayPlaceAnimation()
    {
        if (!InitTween())
            return;

        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .3)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .From(Vector2.Up * 128);

        InitParticlesTween();

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Up * 16, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Zero, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }


    public void PlayDestroyAnimation()
    {
        if (!InitTween())
            return;

        animationRootNode.Position = Vector2.Zero;

        maskNode.ClipChildren = ClipChildrenMode.Only;
        maskNode.Texture = maskTexture;

        var particles = destroyParticlesScene.Instantiate<Node2D>();
        Owner.GetParent().AddChild(particles);
        particles.GlobalPosition = GlobalPosition;

        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 5, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", -2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 2, .1);
        activeTween.TweenProperty(animationRootNode, "rotation_degrees", 0, .1);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Down * 300, .4)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);

        activeTween.Finished += () => EmitSignal(SignalName.DestroyAnimationFinished);
    }

    private void SetUpNode()
    {
        var spriteNode = this.GetFirstNodeOfType<Sprite2D>();

        GD.Print($"Sprite node: {spriteNode}");

        if (spriteNode is null) return;

        var originalParent = spriteNode.GetParent();

        originalParent.RemoveChild(spriteNode);
        originalParent.QueueFree();

        Position = new Vector2(spriteNode.Position.X, spriteNode.Position.Y);

        maskNode = new()
        {
            Centered = true,
            Offset = new Vector2(0, -130)
        };

        AddChild(maskNode);

        animationRootNode = new();
        maskNode.AddChild(animationRootNode);

        animationRootNode.AddChild(spriteNode);
        spriteNode.Position = new Vector2(0, 0);
    }


    private bool InitTween()
    {
        if (animationRootNode is null) return false;

        if (activeTween is not null && activeTween.IsValid())
            activeTween.Kill();

        activeTween = CreateTween();

        return activeTween is not null;
    }

    private void InitParticlesTween()
    {
        activeTween.TweenCallback(Callable.From(() =>
        {
            var particles = impactParticlesScene.Instantiate<Node2D>();
            Owner.GetParent().AddChild(particles);
            particles.GlobalPosition = GlobalPosition;
        }));
    }
}