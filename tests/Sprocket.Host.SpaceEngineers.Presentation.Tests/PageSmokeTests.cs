using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Walks the screens through every kind of page, with nobody looking.
/// </summary>
/// <remarks>
/// The editor's own behaviour is covered in Sprocket.Core.Tests. What is left to check here is that
/// nothing throws while each page is measured, arranged and drawn.
/// </remarks>
public class PageSmokeTests
{
    [Fact]
    public void EveryPageDrawsAndLaysOut()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        var seen = new HashSet<string>();

        Record(seen, screen);

        // The empty root offers the starters straight away, and answering one lands directly on the
        // path: blocks, then what happens to them.
        editor.NewSystem("Airlock");
        Record(seen, screen);

        editor.PressStart("when");
        Record(seen, screen);

        editor.PressBlock("Approach Sensor");
        Record(seen, screen);

        editor.PressChoice("turns on");
        editor.PressPrimary();
        Record(seen, screen);

        editor.PressAdd("task");
        editor.PressBlock("Landing Lights");
        editor.PressChoice("Power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();

        // The front door: the systems on this construct.
        editor.GoToRoot();
        Record(seen, screen);

        // A question with a fixed set of answers. The starters used to be one of these and are not
        // any more, so the walk reaches for the loader instead.
        editor.OpenLibrary();
        Record(seen, screen);

        Assert.Contains(nameof(PickPage), seen);
        Assert.Contains(nameof(SequencePage), seen);

        // Heading and Notice are not asserted: nothing produces one at the moment.
    }

    /// <summary>Draws the screen as it stands, and notes what was on it.</summary>
    static void Record(HashSet<string> seen, Screen screen)
    {
        screen.Run(1);

        seen.Add(screen.Editor.Current.GetType().Name);
    }
}
