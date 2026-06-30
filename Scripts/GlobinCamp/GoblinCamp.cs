using System;
using Game.Component;
using Godot;

public partial class GoblinCamp : Node2D
{

    [Export]
    private BuildingComponent buildingComponent;
    
    [Export]
    private Node2D FireParent;
    
    [Export]
    private AnimatedSprite2D animatedSprite2D;
     
    private AudioStreamPlayer audioStreamPlayer;
    
    public override void _Ready()
    {
        audioStreamPlayer = GetNode<AudioStreamPlayer>("%AudioStreamPlayer");

        FireParent.Visible = false;
        buildingComponent.Disabled += OnDisabled;
        buildingComponent.Enabled += OnEnabled;
    }

    private void OnEnabled()
    {
        animatedSprite2D.Play("default");
        FireParent.Visible = false;
    }


    private void OnDisabled()
    {
        audioStreamPlayer.Play();
        animatedSprite2D.Play("destroyed");
        FireParent.Visible = true;
    }

}
