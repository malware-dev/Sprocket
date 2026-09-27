using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Picking;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Walks the screens through every component, with nobody looking.
/// </summary>
/// <remarks>
/// The editor's own behaviour is covered in Sprocket.Core.Tests. What is left to check here is this
/// target's half of the bargain: that every component actually gets drawn by something, and that
/// nothing throws while it is measured, arranged and drawn.
/// </remarks>
public class ComponentSmokeTests
{
    [Fact]
    public void EveryComponentDrawsAndLaysOut()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        var seen = new HashSet<string>();

        Record(seen, screen);

        // The empty root offers the starters straight away, and answering one lands directly on the
        // path: blocks, then what happens to them.
        editor.NewSystem("Airlock");
        Record(seen, screen);

        editor.PressStart("When…");
        Record(seen, screen);

        editor.PressBlock("Approach Sensor");
        Record(seen, screen);

        editor.PressChoice("is switched on");
        editor.PressPrimary();
        Record(seen, screen);

        editor.PressAdd("do something");
        editor.PressBlock("Landing Lights");
        editor.PressChoice("Switch on");
        editor.PressPrimary();

        // The front door: the systems on this construct.
        editor.GoToRoot();
        Record(seen, screen);

        // A question with a fixed set of answers. The starters used to be one of these and are not
        // any more, so the walk reaches for the loader instead.
        editor.PressStart("Load blueprint");
        Record(seen, screen);

        Assert.Contains(nameof(PickComponent), seen);
        Assert.Contains(nameof(SequencePage), seen);

        // Heading and Notice are not asserted: nothing produces one at the moment.
    }

    /// <summary>Draws the screen as it stands, and notes what was on it.</summary>
    static void Record(HashSet<string> seen, Screen screen)
    {
        screen.Run(1);

        seen.Add(screen.Editor.Current.GetType().Name);

        foreach (var component in screen.Editor.Screen.Components)
            seen.Add(component.GetType().Name);
    }
}
