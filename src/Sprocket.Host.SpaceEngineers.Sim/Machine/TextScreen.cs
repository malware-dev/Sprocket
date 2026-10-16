using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.Game.GUI.TextPanel;
using VRage.ObjectBuilders;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Sim.Machine;

/// <summary>A text panel on the rig, which writes down each image it is told to show.</summary>
public sealed class TextScreen : Fitting
{
    static readonly string[] Apps = { "TSS_ClockAnalog", "TSS_Weather" };

    readonly List<string> _images = new List<string>();

    /// <summary>A text panel on the rig, showing nothing.</summary>
    public TextScreen(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        // Its one screen is the panel itself, as the game hands a panel's screen to a mod: what it writes is the panel's text.
        var surface = Pretend.To<IMyTextSurface>()
            .Gives("DisplayName", () => "")
            .Does("GetFonts", _ => null!)
            .Does("WriteText", arguments =>
            {
                Text = (string)arguments[0];
                return true;
            })
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Gives("Script", () => App)
            .Done();

        Fit(Pretend.To<IMyTextPanel>()
            .Also<IMyTextSurfaceProvider>()
            .Gives("UseGenericLcd", () => false)
            .Gives("SurfaceCount", () => 1)
            .Does("GetSurface", _ => surface)
            .Does("GetText", () => Text)
            .Gives("Script", () => App)
            .Takes("Script", value => App = (string)value)
            .Gives("ContentType", () => Content)
            .Takes("ContentType", value => Content = (ContentType)value)
            .Does("GetScripts", args => ((List<string>)args[0]).AddRange(Apps))
            .Does("ClearImagesFromSelection", _ => _images.Clear())
            .Does("AddImageToSelection", args =>
            {
                _images.Add((string)args[0]);
                Told("image " + args[0]);
            }));
    }

    /// <summary>The text it shows.</summary>
    public string Text { get; set; } = "";

    /// <summary>The app it runs, or empty for none.</summary>
    public string App { get; set; } = "";

    /// <summary>What it shows: nothing, text and images, or an app.</summary>
    public ContentType Content { get; set; }

    /// <summary>The images it shows, in turn.</summary>
    public IList<string> Images => _images;
}
