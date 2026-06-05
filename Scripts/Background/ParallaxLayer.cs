using Godot;
using System;

namespace Game.Background;

public partial class ParallaxLayer : Parallax2D
{
    [Export]
    public float Speed { get; set; } = 20f;

    [Export]
    public Sprite2D[] CloudSprites { get; set; }

    [Export]
    public float CloudAlpha { get; set; } = 0.6f;

    private float _screenWidth;
    private Random _random = new Random();

    public override void _Ready()
    {
        //GD.Print("ParallaxLayer _Ready called");
        //GD.Print($"CloudSprites count: {CloudSprites?.Length ?? 0}");

        ProcessMode = ProcessModeEnum.Always;
        _screenWidth = GetViewport().GetVisibleRect().Size.X;

        if (CloudSprites == null || CloudSprites.Length == 0) return;

        foreach (Sprite2D sprite in CloudSprites)
        {
            if (sprite == null) continue;
            RandomizeSprite(sprite, spawnAnywhere: true);

            // Apply static transparency
            sprite.Modulate = new Color(1, 1, 1, CloudAlpha);
        }
    }

    public override void _Process(double delta)
    {
        if (CloudSprites is null) return;

        foreach (Sprite2D sprite in CloudSprites)
        {
            if (sprite is null) continue;

            // Move right
            sprite.Position = new Vector2(
                sprite.Position.X + Speed * (float)delta,
                sprite.Position.Y
            );

            // If fully off the right edge, respawn on the left
            float halfWidth = sprite.Texture is not null ? sprite.Texture.GetWidth() * sprite.Scale.X / 2f : 32f;
            if (sprite.Position.X - halfWidth > _screenWidth)
            {
                RandomizeSprite(sprite, spawnAnywhere: false);

                // Re-apply static transparency after respawn
                sprite.Modulate = new Color(1, 1, 1, CloudAlpha);
            }
        }
    }

    private void RandomizeSprite(Sprite2D sprite, bool spawnAnywhere)
    {
        float halfWidth = sprite.Texture is not null ? sprite.Texture.GetWidth() * sprite.Scale.X / 2f : 32f;

        // Set X position
        float newX = spawnAnywhere
            ? (float)(_random.NextDouble() * _screenWidth)
            : -halfWidth - (float)(_random.NextDouble() * 200f);

        // Keep existing Y position
        sprite.Position = new Vector2(newX, sprite.Position.Y);

        // Random scale only (no random alpha)
        float scale = (float)(_random.NextDouble() * 0.4 + 0.8);
        sprite.Scale = new Vector2(scale, scale);
    }
}