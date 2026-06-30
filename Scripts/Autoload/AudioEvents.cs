using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace Game.Autoload;

public partial class AudioEvents : Node
{
    private static AudioEvents instance;

    private AudioStreamPlayer destructAudioStreamPlayer;
    private AudioStreamPlayer clickAudioStreamPlayer;
    private AudioStreamPlayer victoryAudioStreamPlayer;
    private AudioStreamPlayer musicAudioStreamPlayer;

    public override void _Notification(int what)
    {
        if (what == NotificationSceneInstantiated)
            instance = this;
    }


    public override void _Ready()
    {
        destructAudioStreamPlayer = GetNode<AudioStreamPlayer>("%ExplosionAudioStreamPlayer");
        clickAudioStreamPlayer = GetNode<AudioStreamPlayer>("%ClickAudioStreamPlayer");
        victoryAudioStreamPlayer = GetNode<AudioStreamPlayer>("%VictoryAudioStreamPlayer");
        musicAudioStreamPlayer = GetNode<AudioStreamPlayer>("%MusicAudioStreamPlayer");

        musicAudioStreamPlayer.Finished += OnMusicFinished;

    }

  

    public static void PlayVictory() => instance.victoryAudioStreamPlayer.Play();

    public static void PlayBuildingDestruction() => instance.destructAudioStreamPlayer.Play();

    public static void RegisterButton(IEnumerable<Button> buttons)
    {
        foreach (var button in buttons)
            button.Pressed += () => instance.clickAudioStreamPlayer.Play();
    }

    private void OnMusicFinished() =>
      GetTree().CreateTimer(5).Timeout += () => musicAudioStreamPlayer.Play();
    
}
