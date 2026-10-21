using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.Game.GUI.TextPanel;
using VRage.ObjectBuilders;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Sim.Machine;

/// <summary>A text panel on the rig: a block with one screen, as the game hands a panel's screen to a mod.</summary>
public sealed class TextScreen : Fitting
{
    /// <summary>A text panel on the rig, showing nothing.</summary>
    public TextScreen(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyTextPanel>()
            .Also<IMyTextSurfaceProvider>()
            .Gives("UseGenericLcd", () => false)
            .Gives("SurfaceCount", () => 1)
            .Does("GetSurface", _ => Face.Surface)
            .Does("GetText", () => Text)
            .Gives("Script", () => App)
            .Takes("Script", value => App = (string)value)
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Does("GetScripts", arguments => ((List<string>)arguments[0]).AddRange(Face.Apps)));
    }

    /// <summary>Its one screen, which the game names none for.</summary>
    public Face Face { get; } = new Face("");

    /// <summary>The text it shows.</summary>
    public string Text
    {
        get => Face.Text;
        set => Face.Text = value;
    }

    /// <summary>The app it runs, or empty for none.</summary>
    public string App
    {
        get => Face.App;
        set => Face.App = value;
    }

    /// <summary>What it shows: nothing, text and images, or an app.</summary>
    public ContentType Content
    {
        get => Face.Content;
        set => Face.Content = value;
    }

    /// <summary>The images it shows, in turn.</summary>
    public IList<string> Images => Face.Images;
}
