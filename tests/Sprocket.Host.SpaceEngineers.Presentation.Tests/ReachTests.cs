using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Screen;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

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
    const string OpenThePicker = "60 130 type Airlock enter 60 161";

    /// <summary>A program that is not this language and will not read.</summary>
    const string Older = "sprocket 1\nblock|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:21,t:B\n";

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

        // Taking one puts the list away, the way a list somebody has answered should.
        picker.Do(wanted);
        Assert.Equal(other, Current(picker));

        // And now the row that would have said something else is not there to be pressed, though its
        // rectangle is where it was.
        picker.Do(back);

        Assert.Equal(other, Current(picker));
    }

    /// <summary>
    /// The wheel does not reach through what is hanging over the screen.
    /// </summary>
    /// <remarks>
    /// A press, a hover and a grab all ask who owns the screen; turning the wheel used to ask nobody.
    /// With a list hanging open, the rows behind it moved out from under a pointer that was resting on
    /// the list, and closing it left somebody somewhere else entirely.
    /// </remarks>
    [Fact]
    public void TheWheelDoesNotReachThroughAnOpenList()
    {
        var picker = Screen.Fresh(OpenThePicker.Split(' '));

        var first = Blocks(picker).Filters[0].Label;
        var line = picker.Text(first);

        // The list hangs open, and the pointer rests on the line that opened it.
        picker.Do(At(Screen.Ui(line.At.X, line.At.Y)));

        var was = picker.Text("Airlock Button").At.Y;
        picker.Do("scroll", "250", "600", "300");

        Assert.Equal(was, picker.Text("Airlock Button").At.Y);

        // And the wheel works again the moment the list is out of the way.
        picker.Do(At(Screen.Ui(line.At.X, line.At.Y)));
        picker.Do("scroll", "250", "600", "300");

        Assert.NotEqual(was, picker.Text("Airlock Button").At.Y);
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

    /// <summary>
    /// Everything a press can reach lights up when the pointer rests on it.
    /// </summary>
    /// <remarks>
    /// The rule stated on <c>Element.Highlight</c>, read the other way round, and held by sweeping the
    /// whole screen rather than by anybody remembering to look: every point on it, on every screen the
    /// editor has, and whatever answers has to say so under the pointer. A control that cannot be lit is
    /// one a person has no way to find.
    /// </remarks>
    [Fact]
    public void EverythingAPressCanReachLightsUp()
    {
        // Every screen, and every thing that hangs over one. A sweep that only ever walked the
        // ordinary screens said nothing about the popups, and the banner's whole strip answered a
        // press and could not be lit for as long as it existed.
        AssertEverythingLights("the construct", _ => { });

        AssertEverythingLights("the name box open", screen => screen.Do("60", "130"));

        AssertEverythingLights("a system", screen => screen.Editor.NewSystem("Airlock"));

        AssertEverythingLights("the picker", screen =>
        {
            screen.Editor.NewSystem("Airlock");
            screen.Editor.PressStart("When…");
        });

        AssertEverythingLights("the picker with its kinds open", screen =>
        {
            screen.Editor.NewSystem("Airlock");
            screen.Editor.PressStart("When…");
            screen.Run(2);

            var line = screen.Text(Blocks(screen).Filters[0].Label);
            screen.Do(At(Screen.Ui(line.At.X, line.At.Y)));
        });

        AssertEverythingLights("a system with a sequence in it", screen =>
        {
            screen.Editor.NewSystem("Airlock");
            screen.Editor.PressStart("When…");
            screen.Editor.PressBlock("Airlock Button");
            screen.Editor.PressChoice("is pressed");
            screen.Editor.PressPrimary();
            screen.Editor.Back();
        });

        AssertEverythingLights("a sequence", screen =>
        {
            screen.Editor.NewSystem("Airlock");
            screen.Editor.PressStart("When…");
            screen.Editor.PressBlock("Airlock Button");
            screen.Editor.PressChoice("is pressed");
            screen.Editor.PressPrimary();
        });

        AssertEverythingLights("a banner that has arrived", screen =>
        {
            screen.Scene.Load(Older);
            screen.Run(20);
        });

        AssertEverythingLights("a banner opened", screen =>
        {
            screen.Scene.Load(Older);
            screen.Run(20);
            screen.Editor.Invoke(Notice(screen).ExpandCommand);
            screen.Run(20);
        });
    }

    static void AssertEverythingLights(string where, Action<Screen> reach)
    {
        var screen = Screen.Fresh();
        reach(screen);
        screen.Run(2);

        var dark = new List<string>();

        foreach (var found in Everything(screen))
        {
            // A box to type in is the one exception, and deliberately: it says "words go here" with
            // its own well, its edge and a caret, and a field that lit like a button would be
            // promising a press.
            if (found.Press.Means == Means.Nothing || found.Press.Means == Means.Type)
                continue;

            if (Lights(found))
                continue;

            var said = found.GetType().Name + " " + found.Press.Means + " at " + found.Bounds;
            if (!dark.Contains(said))
                dark.Add(said);
        }

        Assert.True(dark.Count == 0,
            $"at {where}, these answer a press and cannot be lit: " + string.Join(", ", dark));
    }

    /// <summary>Whether being under the pointer changes how <paramref name="element" /> is filled or edged.</summary>
    static bool Lights(Element element)
    {
        if (element.Highlight.A > 0 || element.HighlightEdge.A > 0)
            return true;

        var was = element.IsHot;
        var fill = element.Background.PackedValue;
        var edge = element.Edge.PackedValue;

        element.IsHot = !was;
        var changed = element.Background.PackedValue != fill || element.Edge.PackedValue != edge;
        element.IsHot = was;

        return changed;
    }

    /// <summary>Everything the pointer can reach, by asking at every point on the screen.</summary>
    /// <remarks>
    /// A sweep rather than a walk of the tree, because what a person can reach is the question, and the
    /// walk is the thing under test. Twenty units is finer than the smallest control on any screen.
    /// </remarks>
    static IEnumerable<Element> Everything(Screen screen)
    {
        var seen = new List<Element>();

        // The editor's own area, in the units a press arrives in: a screen is the full width of the
        // game's safe rectangle and about nine tenths of its height, which is where it draws itself.
        for (var y = 0f; y < 1080f; y += 20f)
        {
            for (var x = 0f; x < 1440f; x += 20f)
            {
                var found = screen.Scene.Under(new Vector2(x, y));

                if (found != null && !seen.Contains(found))
                {
                    seen.Add(found);
                    yield return found;
                }
            }
        }
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
