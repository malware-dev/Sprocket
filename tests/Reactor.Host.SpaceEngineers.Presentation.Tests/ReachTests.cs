using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Look;
using Reactor.Host.SpaceEngineers.Picking;
using Reactor.Host.SpaceEngineers.Screen;
using VRageMath;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a press reaches, and what it must not.
/// </summary>
/// <remarks>
/// Each of these was a rule held by hand while presses were gathered into a list beside the layout:
/// a flag saying a rectangle could have been scrolled out of sight, an order that put a switch after
/// the row it sits on, and a guard in the picker against the rows of a list that is no longer on
/// screen. Walking the elements keeps all three by being a tree, so all three are held here.
/// </remarks>
public class ReachTests
{
    /// <summary>From an empty construct to the block picker: name a system, then ask what starts it.</summary>
    const string OpenThePicker = "60 130 type Airlock enter 60 130";

    /// <summary>A program from before the script language, which will not read.</summary>
    const string Older = "reactor 1\nblock|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:21,t:B\n";

    /// <summary>
    /// A row scrolled up out of its column cannot be pressed where it used to be.
    /// </summary>
    /// <remarks>
    /// It keeps the rectangle it was arranged at, which is now behind the column's own heading, and
    /// only the clip knows it is out of sight.
    /// </remarks>
    [Fact]
    public void ARowScrolledOutOfSightCannotBePressed()
    {
        var picker = Screen.Fresh(OpenThePicker.Split(' '));
        picker.Do("scroll", "250", "600", "300");

        var row = picker.Text("Airlock Inner Door");
        Assert.False(row.IsInsideItsClip,
            "the row is still on show, so pressing it proves nothing");

        picker.Do(At(Screen.Ui(row.At.X, row.At.Y)));

        Assert.DoesNotContain(Blocks(picker).Entries, e => e.IsChosen);
    }

    /// <summary>
    /// The switch on a row answers the press, and the row it stands on does not.
    /// </summary>
    /// <remarks>
    /// The two overlap, and the switch is inside the row, so the deeper of the two wins - which is
    /// what stops switching a system off from also walking into it.
    /// </remarks>
    [Fact]
    public void ASwitchIsPressedRatherThanTheRowItStandsOn()
    {
        var root = Screen.Fresh("60 130 type Airlock enter 25 62".Split(' '));

        // A row that is both, which is the only kind where the two can disagree: a system's own row
        // carries no command, so pressing one has never done anything but work its switch.
        var sequence = Switchable(root);
        Assert.NotEqual(0, sequence.Command);
        Assert.True(sequence.SwitchIsOn);

        var was = root.Ended;
        var name = root.Text(sequence.Summary);
        var on = root.Drawings.First(d =>
            d.IsText && d.Text == "ON" && Math.Abs(d.At.Y - name.At.Y) < name.Scale * 30f);

        root.Do(At(Screen.Ui(on.At.X, on.At.Y)));

        // A tick, because what a row says about itself is worked out when the screen is next asked
        // for rather than the moment a switch is thrown.
        root.Run(1);

        Assert.False(Switchable(root).SwitchIsOn);
        Assert.Equal(was, root.Ended);
    }

    /// <summary>
    /// A list that is no longer hanging over the screen cannot be pressed through.
    /// </summary>
    /// <remarks>
    /// Its rows keep the rectangles they had when it was last open, and the picker used to have to
    /// ask the screen whether they were still showing before offering them. A closed list is not in
    /// the layout at all, so nothing in it is reachable and nothing has to ask.
    /// </remarks>
    [Fact]
    public void AClosedListCannotBePressedThrough()
    {
        var picker = Screen.Fresh(OpenThePicker.Split(' '));

        var first = Blocks(picker).Filters[0].Label;
        var other = Blocks(picker).Filters[1].Label;

        // The line the list hangs from. Pressing it opens the list, and pressing it again puts the
        // list away, which is what a thing that opens downward is expected to do.
        var line = picker.Text(first);
        var opener = At(Screen.Ui(line.At.X, line.At.Y));

        picker.Do(opener);

        // Inside the list rather than on the line, which is still showing the same word. Drawn last,
        // because the list hangs over everything the column had already drawn.
        var wanted = Row(picker, other);
        var back = Row(picker, first);

        picker.Do(wanted);
        Assert.Equal(other, Current(picker));

        picker.Do(opener);
        picker.Do(back);

        Assert.Equal(other, Current(picker));
    }

    /// <summary>
    /// Nothing in a banner answers a press while the banner is still arriving.
    /// </summary>
    /// <remarks>
    /// It slides in over a list somebody may already be pressing, so a control that answered on the
    /// way in is one hit by accident. The rule belongs to the thing that moves rather than to the
    /// banner: what the reveal has not finished showing, nobody can press.
    /// </remarks>
    [Fact]
    public void NothingInABannerAnswersWhileItSlidesIn()
    {
        var screen = Screen.Fresh();

        // A program that will not read is what raises a banner with something to open.
        screen.Scene.Load(Older);
        screen.Run(2);

        Assert.False(Notice(screen).Expanded);

        // The part of the banner that is in view, which is what its own clip says it may draw into.
        var wash = screen.Drawings.First(d =>
            !d.IsText && (d.Ink == Palette.ProblemWash || d.Ink == Palette.NoticeWash));

        var strip = wash.Clip;
        var at = At(Screen.Ui(strip.X + strip.Width / 2f, strip.Y + strip.Height / 2f));

        screen.Do(at);
        Assert.False(Notice(screen).Expanded, "the banner answered a press before it had arrived");

        screen.Run(14);
        screen.Do(at);

        Assert.True(Notice(screen).Expanded, "the banner would not open once it had arrived");
    }

    static NoticeComponent Notice(Screen screen) =>
        screen.Editor.Screen.Components.OfType<NoticeComponent>().First();

    /// <summary>Where to press for a row of the open list, which is drawn over everything else.</summary>
    static string[] Row(Screen screen, string text)
    {
        var drawn = screen.Drawings.Last(d => d.IsText && d.Text == text);
        return At(Screen.Ui(drawn.At.X, drawn.At.Y));
    }

    static string[] At(Vector2 point) =>
        new[] { ((int)point.X).ToString(), ((int)point.Y).ToString() };

    static string Current(Screen screen) =>
        Blocks(screen).Filters.First(f => f.IsCurrent).Label;

    static PickStage Blocks(Screen screen) =>
        screen.Editor.Component<PickComponent>().Stages[0];

    /// <summary>The one row on the root that both leads somewhere and carries a switch.</summary>
    static StatusEntry Switchable(Screen screen) =>
        screen.Editor.Component<StatusListComponent>().Entries
            .First(e => e.CanSwitch && e.Command != 0);
}
