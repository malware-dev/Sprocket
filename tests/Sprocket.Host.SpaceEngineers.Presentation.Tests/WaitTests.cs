using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRage.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// How long a step waits is given in a panel hanging from the length in its sentence.
/// </summary>
/// <remarks>
/// The length changes as the panel closes: a press anywhere else or Enter keeps what it says, and Escape leaves the wait
/// as it was. A wait added a moment ago is added with the length it closes on, as one change, and Escape takes it away.
/// </remarks>
public class WaitTests
{
    /// <summary>Adding a wait adds a second's wait and opens its panel, in seconds.</summary>
    [Fact]
    public void AddingAWaitOpensItsPanel()
    {
        var screen = Added();

        var panel = Panel(screen);
        Assert.True(panel.InSeconds);
        Assert.Equal("1", panel.Box.Text);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(Duration.OfSeconds(1), wait.Length);
        Assert.True(screen.Shows("= 60 ticks"));
    }

    /// <summary>Escape on a wait just added takes it away, leaving nothing to undo.</summary>
    [Fact]
    public void EscapeTakesAWaitJustAddedAway()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = screen.Editor.Program.Systems[0];
        var before = screen.Editor.UndoLabel(system);

        screen.Editor.PressAdd("wait");
        screen.Run(3);
        screen.Editor.Input.Key(MyKeys.Escape, false, false);
        screen.Run(2);

        Assert.Null(Page(screen).Lengths);
        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
        Assert.Equal(before, screen.Editor.UndoLabel(system));
    }

    /// <summary>A length typed into a wait just added is kept on Enter, and adding it and giving it its length are one change.</summary>
    [Fact]
    public void AWaitAddedWithItsLengthIsOneChange()
    {
        var screen = Added();

        Panel(screen).Box.Insert("2.5");
        screen.Editor.Input.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(150, wait.Ticks);
        Assert.True(wait.Length.InSeconds);

        screen.Editor.Undo(screen.Editor.Program.Systems[0]);
        screen.Run(2);
        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
    }

    /// <summary>A press anywhere else closes the panel and keeps what it says.</summary>
    [Fact]
    public void APressElsewhereKeepsTheLength()
    {
        var screen = Added();

        Panel(screen).Box.Insert("4");
        screen.Do("800", "900");
        screen.Run(2);

        Assert.Null(Page(screen).Lengths);
        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(240, wait.Ticks);
    }

    /// <summary>Pressing a wait's length opens its panel on it, and a change in ticks is kept in ticks.</summary>
    [Fact]
    public void AWaitsLengthIsChangedInItsPanel()
    {
        var screen = Added();
        screen.Editor.Input.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        PressLength(screen);
        var panel = Panel(screen);
        Assert.Equal("1", panel.Box.Text);

        screen.Do("press", "ticks");
        Assert.Equal("60", panel.Box.Text);

        panel.Box.SelectAll();
        panel.Box.Insert("90");
        screen.Editor.Input.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(Duration.OfTicks(90), wait.Length);
        Assert.True(screen.Shows("90 ticks"));

        screen.Editor.Undo(screen.Editor.Program.Systems[0]);
        Assert.Equal(Duration.OfSeconds(1), wait.Length);
    }

    /// <summary>Escape on a wait already there leaves it as it was.</summary>
    [Fact]
    public void EscapeLeavesAWaitAsItWas()
    {
        var screen = Added();
        screen.Editor.Input.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        PressLength(screen);
        Panel(screen).Box.Insert("7");
        screen.Editor.Input.Key(MyKeys.Escape, false, false);
        screen.Run(2);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(Duration.OfSeconds(1), wait.Length);
    }

    /// <summary>Minus and plus move the length half a second, and a length picked is marked as the one given.</summary>
    [Fact]
    public void StepsAndPicksMoveTheLength()
    {
        var screen = Added();
        var panel = Panel(screen);

        screen.Do("press", Symbols.Plus);
        Assert.Equal("1.5", panel.Box.Text);

        screen.Do("press", Symbols.Minus);
        screen.Do("press", Symbols.Minus);
        Assert.Equal("0.5", panel.Box.Text);

        screen.Do("press", "10 s");
        Assert.Equal("10", panel.Box.Text);
        Assert.True(screen.Shows("= 600 ticks"));
    }

    static Screen Added()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("wait");
        return screen.Run(3);
    }

    static SequencePage Page(Screen screen) => Assert.IsType<SequencePage>(screen.Editor.Current);

    static DurationPanel Panel(Screen screen) => Page(screen).Lengths ?? throw new Xunit.Sdk.XunitException("no panel is open");

    /// <summary>Presses the length in the wait's sentence.</summary>
    static void PressLength(Screen screen)
    {
        var row = Page(screen).View!.Within().OfType<StepRow>().Single();
        var length = row.Slots.OfType<DurationSlot>().Single();
        length.Opens!.Execute();
        screen.Run(2);
    }
}
