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
            .Does("GetFonts", arguments => ((List<string>)arguments[0]).AddRange(Fonts))
            .Does("WriteText", arguments =>
            {
                Text = (string)arguments[0];
                return true;
            })
            .Does("GetText", () => Text)
            .Does("ClearImagesFromSelection", _ => Images.Clear())
            .Does("AddImageToSelection", arguments => Images.Add((string)arguments[0]))
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Gives("Script", () => App)
            .Takes("Script", value => App = (string)value)
            .Does("GetScripts", arguments => ((List<string>)arguments[0]).AddRange(Apps))
            .Gives("FontSize", () => FontSize)
            .Takes("FontSize", value => FontSize = (float)value)
            .Gives("FontColor", () => FontColor)
            .Takes("FontColor", value => FontColor = (Color)value)
            .Gives("BackgroundColor", () => BackgroundColor)
            .Takes("BackgroundColor", value => BackgroundColor = (Color)value)
            .Gives("Font", () => Font)
            .Takes("Font", value => Font = (string)value)
            .Gives("Alignment", () => Alignment)
            .Takes("Alignment", value => Alignment = (TextAlignment)value)
            .Gives("TextureSize", () => (Vector2)Shape.TextureSize)
            .Gives("SurfaceSize", () => Shape.SurfaceSize)
            .Gives("TextPadding", () => Padding)
            .Takes("TextPadding", value => Padding = (float)value)
            .Gives("ScriptForegroundColor", () => Foreground)
            .Takes("ScriptForegroundColor", value => Foreground = (Color)value)
            .Gives("ScriptBackgroundColor", () => Background)
            .Takes("ScriptBackgroundColor", value => Background = (Color)value)
            .Gives("ChangeInterval", () => Interval)
            .Takes("ChangeInterval", value => Interval = (float)value)
            .Gives("PreserveAspectRatio", () => KeepsAspectRatio)
            .Takes("PreserveAspectRatio", value => KeepsAspectRatio = (bool)value)
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
    public string Text { get; set; } = "";

    /// <summary>The images it shows, in turn, by the game's ids.</summary>
    public List<string> Images { get; } = [];

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

    /// <summary>The color behind its text.</summary>
    public Color BackgroundColor { get; set; } = Color.Black;

    // The fonts the game defines itself, as Content/Data/Fonts.sbc lists them.
    static readonly string[] Stock =
    [
        "Debug", "Red", "Green", "Blue", "White", "DarkBlue", "UrlNormal", "UrlHighlight", "ErrorMessageBoxCaption",
        "ErrorMessageBoxText", "InfoMessageBoxCaption", "InfoMessageBoxText", "ScreenCaption", "GameCredits", "LoadingScreen",
        "BuildInfo", "BuildInfoHighlight", "Monospace"
    ];

    /// <summary>The fonts it offers, by the game's ids: every font defined, the game's own and then the mods'.</summary>
    /// <remarks>The game answers a screen's fonts with every font definition loaded, so the mods' fonts are among them.</remarks>
    public List<string> Fonts { get; } = Offered();

    /// <summary>The font its text is in, by the game's id.</summary>
    public string Font { get; set; } = "Debug";

    /// <summary>Where its text sits across it.</summary>
    public TextAlignment Alignment { get; set; }

    /// <summary>Its shape, which says how big its texture is and how much of it the face shows; a square LCD panel's unless set.</summary>
    public ScreenShape Shape { get; set; } = new ScreenShape("lcd-panel", 512, 1, 1, 2.5f);

    /// <summary>How far in from its edges text sits, in hundredths of its size, as its text settings say.</summary>
    public float Padding { get; set; } = 2f;

    /// <summary>How long each of its images shows, in seconds.</summary>
    public float Interval { get; set; }

    /// <summary>Whether its images keep their shape.</summary>
    public bool KeepsAspectRatio { get; set; }

    /// <summary>The color an app draws in, as its app settings say.</summary>
    public Color Foreground { get; set; } = MyTextSurfaceScriptBase.DEFAULT_FONT_COLOR;

    /// <summary>The color behind an app, as its app settings say.</summary>
    public Color Background { get; set; } = MyTextSurfaceScriptBase.DEFAULT_BACKGROUND_COLOR;

    /// <summary>The sprites of the last frame an app drew on it, in the order they were added.</summary>
    public List<MySprite> Sprites { get; } = [];

    static List<string> Offered()
    {
        var fonts = new List<string>(Stock);
        foreach (var font in SprocketContent.Instance.Fonts)
            fonts.Add(font.Name);

        return fonts;
    }
}
