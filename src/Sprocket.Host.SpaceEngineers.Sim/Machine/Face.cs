using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sprocket.Sim.Content;
using Sprocket.Sim.Fakes;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Sprocket.Sim.Machine;

/// <summary>One screen, holding what it is set to.</summary>
public sealed class Face
{
    /// <summary>A blank screen the game calls <paramref name="name" />.</summary>
    public Face(string name)
    {
        Surface = Pretend.To<IMyTextSurface>()
            .Also<Sandbox.ModAPI.IMyTextSurface>()
            .Gives("DisplayName", () => name)
            .Does("GetFonts", _ => null!)
            .Does("WriteText", arguments =>
            {
                Text = (string)arguments[0];
                return true;
            })
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Gives("Script", () => App)
            .Takes("Script", value => App = (string)value)
            .Does("GetScripts", arguments => ((List<string>)arguments[0]).AddRange(Apps))
            .Gives("FontSize", () => FontSize)
            .Takes("FontSize", value => FontSize = (float)value)
            .Gives("FontColor", () => FontColor)
            .Takes("FontColor", value => FontColor = (Color)value)
            .Gives("Alignment", () => Alignment)
            .Takes("Alignment", value => Alignment = (TextAlignment)value)
            .Gives("TextureSize", () => (Vector2)Shape.TextureSize)
            .Gives("SurfaceSize", () => Shape.SurfaceSize)
            .Gives("TextPadding", () => Padding)
            .Gives("ScriptForegroundColor", () => Foreground)
            .Gives("ScriptBackgroundColor", () => Background)
            .Does("DrawFrame", () => new MySpriteDrawFrame(frame =>
            {
                Sprites.Clear();
                frame.AddToList(Sprites);
            }))
            .Does("MeasureStringInPixels", arguments => Measure(arguments[0].ToString(), (string)arguments[1], (float)arguments[2]))
            .Done();
    }

    /// <summary>The screen whose fonts measure text drawn here, which a picture of this one sets; a panel measures at its scale alone.</summary>
    public IScreenSpace? Measuring { get; set; }

    Vector2 Measure(string text, string font, float scale)
    {
        if (Measuring == null)
            throw new System.NotSupportedException("the harness has no fonts to measure " + font + " with on this screen");

        // The game's screen scales text by its height against the HUD's reference height, which a panel's text is not.
        var unscaled = scale * ScreenSpaceGraphics.GameReferenceHeight / Measuring.SafeArea.Height;
        return Measuring.MeasureText(font, new StringBuilder(text), unscaled);
    }

    /// <summary>The screen as the game hands it to a mod.</summary>
    public IMyTextSurface Surface { get; }

    /// <summary>The text last written on it.</summary>
    public string Text { get; private set; } = "";

    /// <summary>What it is set to show.</summary>
    public ContentType Content { get; set; }

    /// <summary>The apps it can run, by the game's ids.</summary>
    public List<string> Apps { get; } = ["TSS_ClockAnalog", "TSS_Weather"];

    /// <summary>The app it runs while it is set to show one, by the game's id for it.</summary>
    public string App { get; set; } = "";

    /// <summary>Its text's size.</summary>
    public float FontSize { get; set; } = 1f;

    /// <summary>Its text's color.</summary>
    public Color FontColor { get; set; } = Color.White;

    /// <summary>Where its text sits across it.</summary>
    public TextAlignment Alignment { get; set; }

    /// <summary>Its shape, which says how big its texture is and how much of it the face shows; a square LCD panel's unless set.</summary>
    public ScreenShape Shape { get; set; } = new ScreenShape("lcd-panel", 512, 1, 1, 2.5f);

    /// <summary>How far in from its edges text sits, in hundredths of its size, as its text settings say.</summary>
    public float Padding { get; set; } = 2f;

    /// <summary>The color an app draws in, as its app settings say.</summary>
    public Color Foreground { get; set; } = MyTextSurfaceScriptBase.DEFAULT_FONT_COLOR;

    /// <summary>The color behind an app, as its app settings say.</summary>
    public Color Background { get; set; } = MyTextSurfaceScriptBase.DEFAULT_BACKGROUND_COLOR;

    /// <summary>The sprites of the last frame an app drew on it, in the order they were added.</summary>
    public List<MySprite> Sprites { get; } = [];
}
