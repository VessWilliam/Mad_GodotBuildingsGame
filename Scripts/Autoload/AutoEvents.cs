using Godot;
using System;

namespace Game.Autoload;

public partial class AutoEvents : Node
{
    private static AutoEvents instance;

    private AudioStreamPlayer audioStreamPlayer;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            instance = this;
    }


    public override void _Ready() =>
        audioStreamPlayer = GetNode<AudioStreamPlayer>("%ExplosionAudioStreamPlayer");



    public static void PlayBuildingDestruction() =>
                  instance.audioStreamPlayer.Play();


}
