using Game.Camera;
using Game.Extentions;
using Godot;

namespace Game.Component;

public partial class BuildingAnimatorComponent : Node2D
{
    [Signal]
    public delegate void DestroyAnimationFinishedEventHandler();

    [Export]
    private PackedScene placeParticlesScene;

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
        //PlayPlaceAnimation();
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

        InitParticlesTween(placeParticlesScene);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Up * 16, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Zero, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }


    public void PlayDestroyAnimation()
    {
        GD.Print($"In tree: {IsInsideTree()}");
        GD.Print($"Process: {ProcessMode}");

        if (!InitTween())
            return;

        animationRootNode.Position = Vector2.Zero;

        maskNode.ClipChildren = ClipChildrenMode.Only;
        maskNode.Texture = maskTexture;

        InitParticlesTween(destroyParticlesScene);

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
        Node2D spriteNode = null;

        spriteNode ??= this.GetFirstNodeOfType<Sprite2D>();
        spriteNode ??= this.GetFirstNodeOfType<AnimatedSprite2D>();

        if (spriteNode is null)
            return;

        var originalParent = spriteNode.GetParent();

        if (originalParent != this)
        {
            originalParent.RemoveChild(spriteNode);
            originalParent.CallDeferred("queue_free");
        }
        else
        {
            RemoveChild(spriteNode);
        }

        Position = spriteNode.Position;

        maskNode = new Sprite2D
        {
            Centered = true,
            Offset = new Vector2(0, -130)
        };

        AddChild(maskNode);

        animationRootNode = new Node2D();
        maskNode.AddChild(animationRootNode);

        animationRootNode.AddChild(spriteNode);
        spriteNode.Position = Vector2.Zero;
    }

    private bool InitTween()
    {
        GD.Print($"animationRootNode: {animationRootNode}");

        if (animationRootNode is null)
        {
            GD.Print("InitTween failed - animationRootNode is null");
            return false;
        }

        if (activeTween is not null && activeTween.IsValid())
            activeTween.Kill();

        activeTween = CreateTween();

        return activeTween is not null;
    }

    private void InitParticlesTween(PackedScene selectedscene) =>
     activeTween.TweenCallback(Callable.From(() => SpawnParticles(selectedscene)));

    private void SpawnParticles(PackedScene particlesScene)
    {
        var particleParent = Owner?.GetParent();

        if (particleParent is null) return;

        var particles = particlesScene.Instantiate<Node2D>();
        particleParent.AddChild(particles);
        particles.GlobalPosition = GlobalPosition;
        GameCamera.Shake();
    }
}