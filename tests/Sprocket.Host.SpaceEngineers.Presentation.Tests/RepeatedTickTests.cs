using Sprocket.Host.SpaceEngineers.Screen;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a screen does when nothing is happening to it, sixty times a second.
/// </summary>
/// <remarks>
/// Every one of these faults is invisible to a test that draws once and fatal in the game, where a
/// throwing tick stops the whole thing running and a rebuilding screen flickers under the pointer.
/// </remarks>
public class RepeatedTickTests
{
    [Fact]
    public void TickingRepeatedlyOnEveryScreenDoesNotThrow()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        screen.Run(60);

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        screen.Run(60);

        editor.PressBlock("Airlock Button");
        screen.Run(60);

        editor.PressChoice("is pressed");
        screen.Run(60);

        editor.PressPrimary();
        screen.Run(60);

        editor.PressAdd("do something");
        screen.Run(60);

        editor.Back();
        screen.Run(60);

        editor.GoToRoot();
        screen.Run(60);

        editor.GoToRoot();
        screen.Run(60);

        Assert.NotEqual("", screen.Ended);
    }

    /// <summary>
    /// A screen that nothing is happening on must keep the very components it had.
    /// </summary>
    /// <remarks>
    /// Replacing them makes a target throw away everything it built and build it again, which loses
    /// whatever the pointer was over and whatever was being typed into. Sixty times a second that
    /// reads as the whole screen flickering under the mouse.
    /// </remarks>
    [Fact]
    public void ScreensDoNotRebuildThemselvesWhileNothingHappens()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        AssertStill(screen, "the sequence list");

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        AssertStill(screen, "picking what happens");

        editor.PressBlock("Airlock Button");
        AssertStill(screen, "picking what happens, with a block picked");

        editor.PressChoice("is pressed");
        editor.PressPrimary();
        AssertStill(screen, "the sequence");

        editor.PressAdd("do something");
        AssertStill(screen, "picking what to do");

        editor.Back();
        editor.GoToRoot();
        AssertStill(screen, "the construct");
    }

    /// <summary>
    /// A screen nobody is touching writes nothing onto its controls.
    /// </summary>
    /// <remarks>
    /// The whole of what "the values are pushed rather than polled" means, as a number. Core announces
    /// a value when it actually changes - <c>Observable.Set</c> is silent when it does not - so a frame
    /// where nothing happened has nothing to tell anybody, and a view that reads anyway is a view that
    /// would have to be read every frame for the life of the session.
    /// <para>
    /// Every screen the editor has, because one view left reading is one screen that pays for it.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnIdleScreenWritesNothingOntoItsControls()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        AssertIdle(screen, "the construct");

        editor.NewSystem("Airlock");
        AssertIdle(screen, "a system");

        editor.PressStart("When…");
        AssertIdle(screen, "the picker");

        editor.PressBlock("Airlock Button");
        AssertIdle(screen, "the picker, with a block chosen");

        editor.PressChoice("is pressed");
        AssertIdle(screen, "the picker, with an event chosen");

        editor.PressPrimary();
        AssertIdle(screen, "a sequence");

        editor.PressAdd("do something");
        AssertIdle(screen, "picking what happens");

        editor.Back();
        editor.Back();
        AssertIdle(screen, "a system with a sequence in it");

        editor.Back();
        AssertIdle(screen, "the construct with a system on it");
    }

    /// <summary>
    /// A screen nobody is touching barely asks the game anything.
    /// </summary>
    /// <remarks>
    /// The other half of the same rule, one layer down. Core cannot be told when a door opens or a
    /// block is renamed - nothing in the game's own interface announces either - so those it has to
    /// ask for, and the only honest way to do that is on a cadence rather than on every tick. The
    /// picker is the screen that asks at all, because it is the one showing what forty blocks are
    /// doing; it used to ask about every one of them sixty times a second.
    /// <para>
    /// Counted in the harness, which answers every one of these calls and so is the one place that can
    /// count them. What a call costs there is the proxy's and says nothing about the game; how many
    /// are made is exactly what the game would see.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnIdleScreenBarelyAsksTheGameAnything()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        AssertQuiet(screen, "the construct");

        editor.NewSystem("Airlock");
        AssertQuiet(screen, "a system");

        editor.PressStart("When…");
        AssertQuiet(screen, "the picker, showing what every block is doing");

        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        AssertQuiet(screen, "a sequence");
    }

    /// <summary>How many questions a screen may ask the game on an idle tick.</summary>
    /// <remarks>
    /// The picker, the only screen that asks at all, averages three - twelve blocks every fifteen
    /// ticks. Five leaves room for a block or two more on the rig without leaving room for the fault:
    /// asking on every tick again is forty-six.
    /// </remarks>
    const int Asks = 5;

    static void AssertQuiet(Screen screen, string what)
    {
        screen.Run(5);

        var before = screen.Asked;
        screen.Run(Frames);

        var each = (screen.Asked - before) / (float)Frames;

        Assert.True(each <= Asks,
            $"{what} asked the game {each:0.#} questions on an average tick where nothing happened, "
            + $"which is more than {Asks}. Something is being asked for that could be asked on a "
            + "cadence, or told.");
    }

    static void AssertIdle(Screen screen, string what)
    {
        // Settled first: arriving at a screen is a change, and writing the controls then is the point.
        screen.Run(5);

        var before = screen.Wires;
        screen.Run(Frames);

        Assert.True(screen.Wires == before,
            $"{what} wrote its controls {screen.Wires - before} times over {Frames} frames where "
            + "nothing happened. Something it shows is being read rather than announced.");
    }

    /// <summary>
    /// What a screen costs while nobody is touching it does not grow with what is on it.
    /// </summary>
    /// <remarks>
    /// Measured rather than believed, and measured as growth rather than as a total: a constant cost
    /// is the frame drawing what is on screen, where a cost per row is something being worked out
    /// again for every row on every frame. That is the fault this catches, and it is one nothing else
    /// here can see - the picture is right either way.
    /// <para>
    /// Every hover note used to be built on every frame, of which at most one is ever read. That was
    /// 364 bytes per system per frame; the notes are now built when their words change, and it is 127.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnIdleScreenCostsNoMoreForBeingFullerOfThings()
    {
        var one = Idle(1);
        var four = Idle(4);

        var each = (four - one) / 3 / Frames;

        Assert.True(each < 250,
            $"an idle frame costs {each} bytes for every system on the construct, which is the shape "
            + "of something being worked out per row per frame rather than when it changed. "
            + $"({one} bytes over {Frames} frames at one system, {four} at four.)");
    }

    const int Frames = 60;

    /// <summary>What a construct with this many systems allocates over a second of nothing happening.</summary>
    static long Idle(int systems)
    {
        var screen = Screen.Fresh();

        for (var i = 0; i < systems; i++)
        {
            screen.Editor.NewSystem("System " + i);
            screen.Editor.Back();
        }

        // Settled first. The frames that arrive at a screen for the first time measure its text and
        // build its rows, which is work that happens once and is not what this is about.
        screen.Run(20);

        var before = GC.GetAllocatedBytesForCurrentThread();
        screen.Run(Frames);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    static void AssertStill(Screen screen, string what)
    {
        screen.Run(5);

        var before = new List<EditorComponent>(screen.Editor.Screen.Components);
        screen.Run(60);
        var after = screen.Editor.Screen.Components;

        var same = before.Count == after.Count;
        for (var i = 0; same && i < before.Count; i++)
            same = ReferenceEquals(before[i], after[i]);

        Assert.True(same,
            $"{what} replaced its components while nothing was happening, so a target rebuilds "
            + "everything sixty times a second and the pointer never keeps hold of anything.");
    }
}
