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
            .Gives("DisplayName", () => name)
            .Does("GetFonts", _ => null!)
            .Does("WriteText", arguments =>
            {
                Text = (string)arguments[0];
                return true;
            })
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Gives("FontSize", () => FontSize)
            .Takes("FontSize", value => FontSize = (float)value)
            .Gives("FontColor", () => FontColor)
            .Takes("FontColor", value => FontColor = (Color)value)
            .Gives("Alignment", () => Alignment)
            .Takes("Alignment", value => Alignment = (TextAlignment)value)
            .Done();
    }

    /// <summary>The screen as the game hands it to a mod.</summary>
    public IMyTextSurface Surface { get; }

    /// <summary>The text last written on it.</summary>
    public string Text { get; private set; } = "";

    /// <summary>What it is set to show.</summary>
    public ContentType Content { get; set; }

    /// <summary>Its text's size.</summary>
    public float FontSize { get; set; } = 1f;

    /// <summary>Its text's color.</summary>
    public Color FontColor { get; set; } = Color.White;

    /// <summary>Where its text sits across it.</summary>
    public TextAlignment Alignment { get; set; }
}
