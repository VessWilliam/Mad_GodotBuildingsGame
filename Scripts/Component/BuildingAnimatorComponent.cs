using System.Linq;
using Godot;

namespace Game.Component;

public partial class BuildingAnimatorComponent : Node2D
{
    private Tween activeTween;
    private Node2D animationRootNode;

    public override void _Ready()
    {
        SetUpNode();
        PlayInAnimation();
    }

    public void PlayInAnimation()
    {
        if(animationRootNode is null) return;

        if (activeTween is not null && activeTween.IsValid())
            activeTween.Kill();

        activeTween = CreateTween();

        activeTween
            .TweenProperty(animationRootNode, "position", Vector2.Zero, .3)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .From(Vector2.Up * 128);
        
        activeTween.TweenProperty(animationRootNode, "position", Vector2.Up * 16, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        activeTween.TweenProperty(animationRootNode, "position", Vector2.Zero, .1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    private void SetUpNode()
    {
       var spriteNode = FindChildren("*", "Sprite2D", true, false)
            .FirstOrDefault() as Sprite2D;

        GD.Print($"Sprite node: {spriteNode}");

        if (spriteNode is null) return;

        var originalParent = spriteNode.GetParent();    

        originalParent.RemoveChild(spriteNode);
        originalParent.QueueFree();

        Position = new Vector2(Position.X, spriteNode.Position.Y);


        animationRootNode = new();
        AddChild(animationRootNode);
        
        animationRootNode.AddChild(spriteNode);
        spriteNode.Position = new Vector2(spriteNode.Position.X, 0);
    }
}