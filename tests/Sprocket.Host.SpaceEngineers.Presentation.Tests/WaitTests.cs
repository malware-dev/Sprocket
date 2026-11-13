using Lumel.Controls;
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
/// The length changes as the panel closes: Done or Enter keeps what it says, and Escape or a press anywhere else leaves
/// the wait as it was. A wait added a moment ago is added with the length it closes on, as one change, and backing out
/// takes it away.
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

    /// <summary>Escape, or a press anywhere else, on a wait just added takes it away, leaving nothing to undo.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BackingOutOfAWaitJustAddedTakesItAway(bool byEscape)
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = screen.Editor.Program.Systems[0];
        var before = screen.Editor.UndoLabel(system, screen.Editor.Program.Sequences[0]);

        screen.Editor.PressAdd("delay");
        screen.Run(3);
        if (byEscape)
            screen.Editor.Input.Key(MyKeys.Escape, false, false);
        else
        {
            Panel(screen).Box.Insert("4");
            screen.Do("800", "900");
        }

        screen.Run(2);

        Assert.Null(Page(screen).Lengths);
        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
        Assert.Equal(before, screen.Editor.UndoLabel(system, screen.Editor.Program.Sequences[0]));
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

        screen.Editor.Undo(screen.Editor.Program.Systems[0], screen.Editor.Program.Sequences[0]);
        screen.Run(2);
        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
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

        screen.Editor.Undo(screen.Editor.Program.Systems[0], screen.Editor.Program.Sequences[0]);
        Assert.Equal(Duration.OfSeconds(1), wait.Length);
    }

    /// <summary>Escape, or a press anywhere else, on the panel of a wait already there closes it and leaves the wait as it was.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BackingOutOfAWaitAlreadyThereLeavesItAsItWas(bool byEscape)
    {
        var screen = Added();
        if (byEscape)
            screen.Editor.Input.Key(MyKeys.Enter, false, false);
        else
            Panel(screen).Box.Submit!.Execute();
        screen.Run(2);

        PressLength(screen);
        if (byEscape)
        {
            Panel(screen).Box.Insert("7");
            screen.Editor.Input.Key(MyKeys.Escape, false, false);
        }
        else
        {
            Panel(screen).Box.SelectAll();
            Panel(screen).Box.Insert("4");
            screen.Do("800", "900");
        }

        screen.Run(2);

        Assert.Null(Page(screen).Lengths);
        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(Duration.OfSeconds(1), wait.Length);
    }

    /// <summary>Minus and plus move the length half a second, and a length picked is marked as the one given.</summary>
    [Fact]
    public void StepsAndPicksMoveTheLength()
    {
        var screen = Added();
        var panel = Panel(screen);

        PressStep(screen, true);
        Assert.Equal("1.5", panel.Box.Text);

        PressStep(screen, false);
        PressStep(screen, false);
        Assert.Equal("0.5", panel.Box.Text);

        screen.Do("press", "10 s");
        Assert.Equal("10", panel.Box.Text);
        Assert.True(screen.Shows("= 600 ticks"));
    }

    /// <summary>In a panel hanging from a sentence, with all the room it asks for, a wait's lengths to pick stand on one line.</summary>
    [Fact]
    public void TheLengthsToPickStandOnOneLine()
    {
        var panel = Panel(Added());

        var lines = panel.Within().OfType<Button>().Where(button => button.IsVisible && button.Text.Length > 0 && button.Bounds.Width == 64f).Select(button => button.Bounds.Y).Distinct();

        Assert.Single(lines);
    }

    /// <summary>The lengths to pick are the same lengths in either unit.</summary>
    [Fact]
    public void ThePicksMatchInBothUnits()
    {
        var panel = Panel(Added());

        Assert.Equal(new[] { "0.5 s", "1 s", "5 s", "10 s", "30 s", "1 min" }, Picks(panel));

        panel.Give("60", true);

        Assert.Equal(new[] { "30 t", "60 t", "300 t", "600 t", "1800 t", "3600 t" }, Picks(panel));
    }

    /// <summary>What the lengths to pick say in the unit the panel shows.</summary>
    static string[] Picks(DurationPanel panel) =>
        panel.Within().OfType<Button>().Where(button => button.Bounds.Width == 64f).Select(button => button.Text).ToArray();

    /// <summary>A wait added after ticks were chosen starts at a second, in ticks, and its panel opens in ticks.</summary>
    [Fact]
    public void ANewWaitStartsInTheUnitLastChosen()
    {
        var screen = Added();
        Panel(screen).Give("90", true);
        Panel(screen).Box.Submit!.Execute();
        screen.Run(2);

        screen.Editor.PressAdd("delay");
        screen.Run(3);

        var panel = Panel(screen);
        Assert.False(panel.InSeconds);
        Assert.Equal("60", panel.Box.Text);
        var wait = Assert.IsType<Wait>(screen.Editor.Program.Sequences[0].Steps[1]);
        Assert.False(wait.Length.InSeconds);
        Assert.Equal(60, wait.Length.Ticks);
    }

    /// <summary>A wait opens in the unit it was given in, whichever unit was chosen last.</summary>
    [Fact]
    public void AWaitOpensInItsOwnUnit()
    {
        var screen = Added();
        Panel(screen).Box.Submit!.Execute();
        screen.Run(2);
        screen.Editor.PressAdd("delay");
        screen.Run(3);
        Panel(screen).Give("90", true);
        Panel(screen).Box.Submit!.Execute();
        screen.Run(2);

        var first = Page(screen).View!.Within().OfType<StepRow>().First();
        first.Slots.OfType<DurationSlot>().Single().Opens!.Execute();
        screen.Run(2);

        Assert.True(Panel(screen).InSeconds);
        Assert.Equal("1", Panel(screen).Box.Text);
    }

    /// <summary>The unit chosen last is kept with the world, so a wait added after a reload starts in it.</summary>
    [Fact]
    public void TheUnitChosenOutlivesAReload()
    {
        var screen = Added();
        Panel(screen).Give("90", true);
        Panel(screen).Box.Submit!.Execute();
        screen.Run(2);

        screen.Scene.Reload();
        screen.Run(2);
        screen.Editor.PressRow(0);
        screen.Run(2);
        screen.Editor.PressRow(0);
        screen.Run(2);
        screen.Editor.PressAdd("delay");
        screen.Run(3);

        Assert.False(Panel(screen).InSeconds);
        Assert.Equal("60", Panel(screen).Box.Text);
    }

    /// <summary>Done keeps the length and puts the panel away, and it is drawn as the button Enter presses.</summary>
    [Fact]
    public void DoneKeepsTheLength()
    {
        var screen = Added();
        Panel(screen).Box.SelectAll();
        Panel(screen).Box.Insert("4");

        var done = Panel(screen).Parent!.Within().OfType<Button>().Single(button => button.Text == "Done");
        Assert.Same(Styles.Primary, done.Style);
        done.Command!.Execute();
        screen.Run(2);

        Assert.Null(Page(screen).Lengths);
        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(Duration.OfSeconds(4), wait.Length);
    }

    /// <summary>The panel keeps its size as the unit changes, both ways.</summary>
    [Fact]
    public void ThePanelKeepsItsSizeAcrossUnits()
    {
        var screen = Added();
        var panel = Panel(screen);
        var seconds = panel.Bounds;

        panel.Give("90", true);
        screen.Run(1);
        Assert.Equal(seconds.Width, panel.Bounds.Width);
        Assert.Equal(seconds.Height, panel.Bounds.Height);

        panel.Give("2", false);
        screen.Run(1);
        Assert.Equal(seconds.Width, panel.Bounds.Width);
        Assert.Equal(seconds.Height, panel.Bounds.Height);
    }

    static Screen Added()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("delay");
        return screen.Run(3);
    }

    static SequencePage Page(Screen screen) => Assert.IsType<SequencePage>(screen.Editor.Current);

    static DurationPanel Panel(Screen screen) => Page(screen).Lengths ?? throw new Xunit.Sdk.XunitException("no panel is open");

    /// <summary>Presses the plus where <paramref name="plus" /> is true, and the minus otherwise, where the panel draws it.</summary>
    static void PressStep(Screen screen, bool plus)
    {
        var sign = Panel(screen).Within().OfType<StepSign>().Single(s => s.IsPlus == plus);
        var button = sign.Parent!;
        var middle = button.ToScreen(new VRageMath.Vector2(button.Bounds.X + button.Bounds.Width / 2f, button.Bounds.Y + button.Bounds.Height / 2f));
        screen.Scene.Press(middle, 1);
        screen.Run(2);
    }

    /// <summary>Presses the length in the wait's sentence.</summary>
    static void PressLength(Screen screen)
    {
        var row = Page(screen).View!.Within().OfType<StepRow>().Single();
        var length = row.Slots.OfType<DurationSlot>().Single();
        length.Opens!.Execute();
        screen.Run(2);
    }
}
