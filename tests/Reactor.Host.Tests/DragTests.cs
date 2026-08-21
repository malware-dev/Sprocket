using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reactor.Host.Features.Shell;
using Reactor.Host.Machine;
using Reactor.Ui;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// A step can be picked up and dropped where it belongs.
/// </summary>
/// <remarks>
/// Driven through real pointer input rather than by calling the command, because the gesture
/// is the entire feature - core already moved steps to any position long before a person could
/// ask it to.
/// </remarks>
public class DragTests
{
    [AvaloniaFact]
    public void DraggingAStepMovesItWhereItWasDropped()
    {
        var window = Build(out var editor);

        Assert.Equal("close, depressurize, open", Order(editor));

        DragBefore(window, from: 2, row: 0);

        Assert.Equal("open, close, depressurize", Order(editor));
    }

    /// <summary>
    /// One drag is one thing somebody did, so it is one thing to take back.
    /// </summary>
    /// <remarks>
    /// Sending a run of single-step moves would have filled the history with three entries for
    /// one act, and undoing would have crawled the line back a row at a time.
    /// </remarks>
    [AvaloniaFact]
    public void ADragIsASingleUndo()
    {
        var window = Build(out var editor);

        DragAfter(window, from: 0, row: 2);
        Assert.Equal("depressurize, open, close", Order(editor));

        editor.Invoke(editor.Sentence().UndoCommand);
        Run(window);

        Assert.Equal("close, depressurize, open", Order(editor));
    }

    [AvaloniaFact]
    public void DroppingAStepBackWhereItStartedChangesNothing()
    {
        var window = Build(out var editor);

        DropOnItself(window, row: 1);

        Assert.Equal("close, depressurize, open", Order(editor));

        // Three steps were added, so there is history - but the drag added nothing to it.
        Assert.Equal("adding a step", editor.Sentence().UndoText);
    }

    /// <summary>
    /// Where the line lands does not depend on where you took hold of it.
    /// </summary>
    /// <remarks>
    /// Somebody grabs the grip wherever it happens to be under their finger and then aims with
    /// the row, because the row is the thing they can see moving. Judging the drop by the
    /// pointer instead put the space a grab-offset away from the line hovering over it, so it
    /// landed somewhere other than where the gap had been held open. Same drag, three different
    /// grab points, one answer.
    /// </remarks>
    [AvaloniaTheory]
    [InlineData(0.15)]
    [InlineData(0.5)]
    [InlineData(0.85)]
    public void WhereItLandsDoesNotDependOnWhereItWasGrabbed(double grabAt)
    {
        var window = Build(out var editor);

        // Taken hold of near the top of the grip, in the middle, and near the bottom. A person
        // grabs wherever their finger lands.
        var grip = Grips(window)[0];
        var hold = grip.TranslatePoint(new Point(4, grip.Bounds.Height * grabAt), window)!.Value;

        // Carried far enough that the row is unambiguously past the last row's middle, rather
        // than balanced exactly on it - that boundary is exact on purpose and is not what this
        // is about.
        var travel = Edge(window, 2, top: false) - Middle(window, Rows(window)[0]).Y;

        window.MouseDown(hold, MouseButton.Left);
        window.MouseMove(new Point(hold.X, hold.Y + travel));
        window.MouseUp(new Point(hold.X, hold.Y + travel), MouseButton.Left);
        Run(window);

        Assert.Equal("depressurize, open, close", Order(editor));
    }

    /// <summary>
    /// A line goes before a row or after one, so the tests say which.
    /// </summary>
    /// <remarks>
    /// Aiming at the middle of a row would be aiming at the boundary between its two answers,
    /// which is exactly the ambiguity the gap model exists to remove.
    /// </remarks>
    static void DragBefore(MainWindow window, int from, int row) =>
        Drag(window, from, Edge(window, row, top: true));

    static void DragAfter(MainWindow window, int from, int row) =>
        Drag(window, from, Edge(window, row, top: false));

    static void DropOnItself(MainWindow window, int row) =>
        Drag(window, row, Middle(window, Rows(window)[row]).Y);

    static void Drag(MainWindow window, int from, double toY)
    {
        // Only the grip starts a drag, so that is what the pointer takes hold of.
        var grab = Middle(window, Grips(window)[from]);

        window.MouseDown(grab, MouseButton.Left);
        window.MouseMove(new Point(grab.X, toY));
        window.MouseUp(new Point(grab.X, toY), MouseButton.Left);
        Run(window);
    }

    static double Edge(MainWindow window, int row, bool top)
    {
        var frame = Rows(window)[row];
        var at = frame.TranslatePoint(new Point(0, top ? 2 : frame.Bounds.Height - 2), window);
        return at!.Value.Y;
    }

    /// <summary>
    /// The step rows, found through their grips.
    /// </summary>
    /// <remarks>
    /// There is exactly one grip per step and nothing else has one, where "a Border wrapping a
    /// Grid" also describes several pieces of chrome - which had these tests measuring against
    /// the wrong things entirely.
    /// </remarks>
    static List<Border> Rows(MainWindow window) =>
        Grips(window).Select(g => g.FindAncestorOfType<Border>()!).ToList();

    static List<TextBlock> Grips(MainWindow window) =>
        window.GetVisualDescendants().OfType<TextBlock>()
            .Where(t => t.Text == "⋮⋮")
            .ToList();

    static Point Middle(MainWindow window, Visual visual) =>
        visual.TranslatePoint(new Point(8, visual.Bounds.Height / 2), window)!.Value;

    static string Order(Reactor.Editing.ReactorEditor editor) =>
        string.Join(", ", editor.Component<SequenceComponent>().Steps
            .Select(s => s.Slots[0].Text));

    static MainWindow Build(out Reactor.Editing.ReactorEditor editor)
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        Step(editor, "Airlock Inner Door", "close");
        Step(editor, "Airlock Vent", "depressurize");
        Step(editor, "Airlock Outer Door", "open");

        Run(window);
        return window;
    }

    static void Step(Reactor.Editing.ReactorEditor editor, string entity, string action)
    {
        editor.PressAdd("do something");
        editor.PressBlock(entity);
        editor.PressChoice(action);
        editor.PressPrimary();
    }

    static void Run(MainWindow window)
    {
        for (var i = 0; i < 5; i++)
        {
            window.Tick();
            window.Measure(new Size(820, 680));
            window.Arrange(new Rect(0, 0, 820, 680));
            Dispatcher.UIThread.RunJobs();
        }
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
}
