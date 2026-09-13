using Avalonia.Headless.XUnit;
using Reactor.Editing;
using Reactor.Ui;
using VRageMath;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A step can be picked up and dropped where it belongs.
/// </summary>
/// <remarks>
/// Driven through real pointer input rather than by calling the command, because the gesture is the
/// entire feature - core already moved steps to any position long before a person could ask it to.
/// </remarks>
public class DragTests
{
    /// <summary>How tall the six dots of one grip stand.</summary>
    const float GripHeight = 15f;

    [AvaloniaFact]
    public void DraggingAStepMovesItWhereItWasDropped()
    {
        var screen = Build(out var editor);

        Assert.Equal("close, depressurize, open", Order(editor));

        DragBefore(screen, from: 2, row: 0);

        Assert.Equal("open, close, depressurize", Order(editor));
    }

    /// <summary>
    /// One drag is one thing somebody did, so it is one thing to take back.
    /// </summary>
    /// <remarks>
    /// Sending a run of single-step moves would have filled the history with three entries for one
    /// act, and undoing would have crawled the line back a row at a time.
    /// </remarks>
    [AvaloniaFact]
    public void ADragIsASingleUndo()
    {
        var screen = Build(out var editor);

        DragAfter(screen, from: 0, row: 2);
        Assert.Equal("depressurize, open, close", Order(editor));

        editor.Invoke(editor.Sentence().UndoCommand);
        screen.Run(5);

        Assert.Equal("close, depressurize, open", Order(editor));
    }

    [AvaloniaFact]
    public void DroppingAStepBackWhereItStartedChangesNothing()
    {
        var screen = Build(out var editor);

        Drag(screen, from: 1, toY: Grips(screen)[1].Y);

        Assert.Equal("close, depressurize, open", Order(editor));

        // Three steps were added, so there is history - but the drag added nothing to it.
        Assert.Equal("adding a step", editor.Sentence().UndoText);
    }

    /// <summary>
    /// Where the line lands does not depend on where you took hold of it.
    /// </summary>
    /// <remarks>
    /// Somebody grabs the grip wherever it happens to be under their finger and then aims with the
    /// row, because the row is the thing they can see moving. Judging the drop by the pointer instead
    /// put the space a grab-offset away from the line hovering over it, so it landed somewhere other
    /// than where the gap had been held open. Same drag, three different grab points, one answer.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(-0.35)]
    [InlineData(0.0)]
    [InlineData(0.35)]
    public void WhereItLandsDoesNotDependOnWhereItWasGrabbed(double grabAt)
    {
        var screen = Build(out var editor);
        var grips = Grips(screen);

        // Taken hold of near the top of the grip, in the middle, and near the bottom. A person grabs
        // wherever their finger lands.
        var hold = new Vector2(grips[0].X, grips[0].Y + (float)(GripHeight * grabAt));

        // Carried far enough that the row is unambiguously past the last row's middle, rather than
        // balanced exactly on it - that boundary is exact on purpose and is not what this is about.
        var toY = Edge(grips, 2, top: false);

        screen.Do("drag", Say(hold.X), Say(hold.Y), Say(hold.X), Say(toY));
        screen.Run(5);

        Assert.Equal("depressurize, open, close", Order(editor));
    }

    /// <summary>
    /// A line goes before a row or after one, so the tests say which.
    /// </summary>
    /// <remarks>
    /// Aiming at the middle of a row would be aiming at the boundary between its two answers, which
    /// is exactly the ambiguity the gap model exists to remove.
    /// </remarks>
    static void DragBefore(Screen screen, int from, int row) =>
        Drag(screen, from, Edge(Grips(screen), row, top: true));

    static void DragAfter(Screen screen, int from, int row) =>
        Drag(screen, from, Edge(Grips(screen), row, top: false));

    static void Drag(Screen screen, int from, float toY)
    {
        // Only the grip starts a drag, so that is what the pointer takes hold of.
        var grab = Grips(screen)[from];

        screen.Do("drag", Say(grab.X), Say(grab.Y), Say(grab.X), Say(toY));
        screen.Run(5);
    }

    static float Edge(IReadOnlyList<Vector2> grips, int row, bool top)
    {
        var height = grips[1].Y - grips[0].Y;
        return top ? grips[row].Y - height / 2f + 2f : grips[row].Y + height / 2f - 2f;
    }

    /// <summary>
    /// Where each step's grip is, in the order the steps are in.
    /// </summary>
    /// <remarks>
    /// A grip is six small squares and nothing else on the screen is, which is the reasoning the
    /// Avalonia prototype's version of this used too: find the one thing only a step has, and let the
    /// row be wherever its grip turned out to be.
    /// </remarks>
    static IReadOnlyList<Vector2> Grips(Screen screen)
    {
        var dots = screen.Drawings
            .Where(d => !d.IsText && d.Where.Width == 3 && d.Where.Height == 3)
            .ToList();

        var rows = new List<Vector2>();

        foreach (var band in dots.GroupBy(d => d.Where.Y / 40).OrderBy(g => g.Key))
        {
            var left = band.Min(d => d.Where.X);
            var right = band.Max(d => d.Where.X + d.Where.Width);
            var top = band.Min(d => d.Where.Y);
            var bottom = band.Max(d => d.Where.Y + d.Where.Height);

            rows.Add(Screen.Ui((left + right) / 2f, (top + bottom) / 2f));
        }

        return rows;
    }

    static string Say(float value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    static string Order(ReactorEditor editor) =>
        string.Join(", ", editor.Component<SequenceComponent>().Steps
            .Select(s => ((ActionSlot)s.Slots[0]).Action.Text));

    static Screen Build(out ReactorEditor editor)
    {
        var screen = Screen.Fresh();
        editor = screen.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        Step(editor, "Airlock Inner Door", "close");
        Step(editor, "Airlock Vent", "depressurize");
        Step(editor, "Airlock Outer Door", "open");

        screen.Run(5);
        return screen;
    }

    static void Step(ReactorEditor editor, string entity, string action)
    {
        editor.PressAdd("do something");
        editor.PressBlock(entity);
        editor.PressChoice(action);
        editor.PressPrimary();
    }
}
