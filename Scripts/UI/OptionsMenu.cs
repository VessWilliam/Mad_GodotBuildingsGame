using Game.Autoload;
using Godot;
using System;


namespace Game.UI;


public partial class OptionsMenu : CanvasLayer
{
    private const string SFX_BUS_NAME = "SFX";
    private const string MUSIC_BUS_NAME = "Music";
    private const string WINDOW_BUTTON_TEXT_FULLSCREEN = "Fullscreen";
    private const string WINDOW_BUTTON_TEXT_WINDOWED = "Windowed";

    [Signal]
    public delegate void DonePressedEventHandler();


    private Button sfxUpButton;

    private Button sfxDownButton;

    private Button musicUpButton;

    private Button musicDownButton;

    private Button windowButton;

    private Button doneButton;

    private Label sfxLabel;
    private Label musicLabel;


    public override void _Ready()
    {

        sfxUpButton = GetNode<Button>("%SFXUpButton");
        sfxDownButton = GetNode<Button>("%SFXDownButton");
        sfxLabel = GetNode<Label>("%SFXLabel");


        musicUpButton = GetNode<Button>("%MusicUpButton");
        musicDownButton = GetNode<Button>("%MusicDownButton");
        musicLabel = GetNode<Label>("%MusicLabel");


        windowButton = GetNode<Button>("%WindowButton");
        doneButton = GetNode<Button>("%DoneButton");
        
        AudioEvents.RegisterButton(
        [
            sfxUpButton,
            sfxDownButton,
            musicUpButton,
            musicDownButton,
            windowButton,
            doneButton
        ]);

        UpdateDisplay();

        sfxUpButton.Pressed += () => ChnageBusVolume(SFX_BUS_NAME, -0.1f);
        sfxDownButton.Pressed += () => ChnageBusVolume(SFX_BUS_NAME, 0.1f);
        musicDownButton.Pressed += () => ChnageBusVolume(MUSIC_BUS_NAME, 0.1f);
        musicUpButton.Pressed += () => ChnageBusVolume(MUSIC_BUS_NAME, -0.1f);
        windowButton.Pressed += OnWindowButtonPressed;

        doneButton.Pressed += OnDoneButtonPressed;
    }


    private void ChnageBusVolume(string busName, float change)
     { 
         var busVolumePercent = OptionEvents.GetBusVolumePercent(busName);
         busVolumePercent = Mathf.Clamp(busVolumePercent + change, 0, 1);
         OptionEvents.SetBusVolumePercent(busName, busVolumePercent); 
         UpdateDisplay();
     }

    private void UpdateDisplay()
    {
        sfxLabel.Text = Mathf.Round(OptionEvents.GetBusVolumePercent(SFX_BUS_NAME) * 10).ToString();  
        musicLabel.Text = Mathf.Round(OptionEvents.GetBusVolumePercent(MUSIC_BUS_NAME) * 10).ToString(); 

        windowButton.Text = OptionEvents.IsFullScreen() ?  WINDOW_BUTTON_TEXT_WINDOWED : WINDOW_BUTTON_TEXT_FULLSCREEN;
    }

    private void OnWindowButtonPressed()
    {
        OptionEvents.ToggleWindowMode();
        UpdateDisplay();
    }

    private void OnDoneButtonPressed() => EmitSignal(SignalName.DonePressed);
}
