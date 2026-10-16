using System.Globalization;
using VRageMath;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A step can be picked up and dropped where it belongs.
/// </summary>
/// <remarks>
/// Driven through real pointer input rather than by calling the command, because the gesture is the
/// entire feature - core already moved steps to any position long before a person could ask it to.
/// </remarks>
public class DragTests
{
    const float GripHeight = 15f;

    [Fact]
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
    [Fact]
    public void ADragIsASingleUndo()
    {
        var screen = Build(out var editor);

        DragAfter(screen, from: 0, row: 2);
        Assert.Equal("depressurize, open, close", Order(editor));

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        Assert.Equal("close, depressurize, open", Order(editor));
    }

    /// <summary>The step being carried is highlighted while it is held, and the highlight goes as it is dropped.</summary>
    [Fact]
    public void TheStepBeingCarriedIsHighlighted()
    {
        var screen = Build(out var editor);
        var grab = Grips(screen)[1];

        screen.Scene.MoveTo(grab);
        screen.Scene.Down(grab);
        screen.Scene.Move(new Vector2(grab.X, grab.Y + 10f));
        screen.Run(1);
        Assert.Equal(Palette.SlotFill, editor.StepRows()[1].Background);

        screen.Scene.Up();
        screen.Run(1);
        Assert.NotEqual(Palette.SlotFill, editor.StepRows()[1].Background);
    }

    /// <summary>A step carried from inside an if leaves the if's spine whole and where it was, rather than taking its stretch of it along.</summary>
    [Fact]
    public void ACarriedStepLeavesTheIfsSpineWhole()
    {
        var screen = Screen.Holding(WithAnIf);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new SequencePage(editor, system.Sequences[0]));
        screen.Run(5);

        var resting = Spine(screen, editor.StepRows());
        Assert.Single(resting);

        // The third grip is the second step inside the if; carried a little way, short of passing the step below it.
        var grab = Grips(screen)[3];
        screen.Scene.MoveTo(grab);
        screen.Scene.Down(grab);
        screen.Scene.Move(new Vector2(grab.X, grab.Y + 20f));
        screen.Run(5);

        Assert.True(editor.StepRows()[3].IsCarried);
        Assert.Equal(resting, Spine(screen, editor.StepRows()));
    }

    /// <summary>A step's grip brightens while the pointer rests on it, which is what says it can be taken hold of.</summary>
    [Fact]
    public void AGripBrightensUnderThePointer()
    {
        var screen = Build(out _);
        var grips = Grips(screen);

        screen.Scene.MoveTo(grips[1]);
        screen.Run(1);
        Assert.All(DotsAt(screen, grips[1]), dot => Assert.Equal(Palette.Ink, dot.Ink));
        Assert.All(DotsAt(screen, grips[0]), dot => Assert.Equal(Palette.Faint, dot.Ink));

        screen.Scene.MoveTo(Screen.Ui(10f, 10f));
        screen.Run(1);
        Assert.All(DotsAt(screen, grips[1]), dot => Assert.Equal(Palette.Faint, dot.Ink));
    }

    /// <summary>The squares of the grip at <paramref name="grip" />, as drawn.</summary>
    static List<Drawn> DotsAt(Screen screen, Vector2 grip)
    {
        var dots = screen.Drawings
            .Where(d => !d.IsText && d.Where.Width == 3 && d.Where.Height == 3)
            .Where(d => Math.Abs(Screen.Ui(d.Where.X, d.Where.Y).Y - grip.Y) < GripHeight)
            .ToList();
        Assert.NotEmpty(dots);
        return dots;
    }

    [Fact]
    public void DroppingAStepBackWhereItStartedChangesNothing()
    {
        var screen = Build(out var editor);

        Drag(screen, from: 1, toY: Grips(screen)[1].Y);

        Assert.Equal("close, depressurize, open", Order(editor));

        // Three steps were added, so there is history - but the drag added nothing to it.
        Assert.Equal("adding a step", editor.Screen.UndoText);
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
    [Theory]
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

        // Clustered by how close together the dots are rather than by fixed bands, because a grip
        // that happened to straddle a band boundary was counted as two rows.
        var rows = new List<Vector2>();
        var band = new List<Drawn>();

        foreach (var dot in dots.OrderBy(d => d.Where.Y))
        {
            if (band.Count > 0 && dot.Where.Y - band[0].Where.Y > GripHeight)
            {
                rows.Add(Middle(band));
                band.Clear();
            }

            band.Add(dot);
        }

        if (band.Count > 0)
            rows.Add(Middle(band));

        return rows;
    }

    static Vector2 Middle(List<Drawn> band)
    {
        var left = band.Min(d => d.Where.X);
        var right = band.Max(d => d.Where.X + d.Where.Width);
        var top = band.Min(d => d.Where.Y);
        var bottom = band.Max(d => d.Where.Y + d.Where.Height);

        return Screen.Ui((left + right) / 2f, (top + bottom) / 2f);
    }

    static string Say(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    static string Order(SprocketEditor editor) =>
        string.Join(", ", editor.StepRows()
            .Select(s => ((ActionSlot)s.Slots[0]).Action.Text));

    const string WithAnIf = "sprocket 1 alpha 16\n\n"
        + "let door1 = block 'Door/LargeBlockDoor' [ 2 ]\n"
        + "let door2 = block 'Door/LargeBlockDoor' [ 3 ]\n"
        + "\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock {\n"
        + "    sequence 1 {\n"
        + "        call ext.close on [ door1 ]\n"
        + "        if all ext.status on [ door1 ] == closed then {\n"
        + "            call ext.open on [ door2 ]\n"
        + "            call ext.close on [ door1 ]\n"
        + "            call ext.open on [ door1 ]\n"
        + "        }\n"
        + "        else {\n"
        + "            call ext.close on [ door2 ]\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    /// <summary>
    /// The stretches the spine of the first branch of the first if is drawn in, top and bottom in pixels, run together where
    /// they touch: one stretch for a whole spine.
    /// </summary>
    static List<(int Top, int Bottom)> Spine(Screen screen, List<StepRow> rows)
    {
        var opens = rows.FindIndex(row => row.Kind == StepKind.If);
        var closes = rows.FindIndex(opens + 1, row => row.Depth == 0);
        var x = rows[opens + 1].SpineX(0) - Screen.Ui(0f, 0f).X;
        var top = rows[opens].Bounds.Y;
        var bottom = rows[closes].Bounds.Y;

        var stretches = new List<(int Top, int Bottom)>();
        foreach (var drawn in screen.Drawings
                     .Where(d => !d.IsText && d.Ink == Palette.Line && d.Where.Height > d.Where.Width && Math.Abs(d.Where.X - x) < 1.5f
                                 && d.Where.Y >= top && d.Where.Y < bottom)
                     .OrderBy(d => d.Where.Y))
        {
            var last = stretches.Count - 1;
            if (last >= 0 && drawn.Where.Y <= stretches[last].Bottom + 1)
                stretches[last] = (stretches[last].Top, Math.Max(stretches[last].Bottom, drawn.Where.Y + drawn.Where.Height));
            else
                stretches.Add((drawn.Where.Y, drawn.Where.Y + drawn.Where.Height));
        }

        return stretches;
    }

    static Screen Build(out SprocketEditor editor)
    {
        var screen = Screen.Fresh();
        editor = screen.Editor;

        editor.NewButtonSystem("Airlock");

        Step(editor, "Airlock Inner Door", "close");
        Step(editor, "Airlock Vent", "depressurize");
        Step(editor, "Airlock Outer Door", "open");

        // Each of the three steps took time and was given a wait row of its own; the drags are of the steps alone.
        var steps = editor.Program.Sequences[0].Steps;
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (steps[i] is Await)
                steps.RemoveAt(i);
        }

        editor.Program.Edited();
        editor.Invalidate();

        screen.Run(5);
        return screen;
    }

    static void Step(SprocketEditor editor, string entity, string action)
    {
        editor.PressAdd("task");
        editor.PressBlock(entity);
        editor.PressChoice(action);
        editor.PressPrimary();
    }
}
