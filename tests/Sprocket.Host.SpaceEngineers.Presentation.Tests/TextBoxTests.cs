using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A box of editable text: typed into, selected by the pointer as well as the keys, and measured only when its text changes.</summary>
public class TextBoxTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Focus = new Color(69, 128, 157);

    /// <summary>A press inside the box puts the caret at the nearest boundary between characters.</summary>
    [Fact]
    public void APressPutsTheCaretAtTheNearestBoundary()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Air"), 1);
        input.PointerUp(At(box, easel, "Air"));

        Assert.Equal(3, box.Caret);
        Assert.False(box.HasSelection);
    }

    /// <summary>A drag from a press selects the run it crosses.</summary>
    [Fact]
    public void ADragSelectsTheRunItCrosses()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Air"), 1);
        input.PointerMove(At(box, easel, "Airlock"));
        input.PointerUp(At(box, easel, "Airlock"));

        Assert.Equal("lock", box.SelectedText());
    }

    /// <summary>A second press takes the word under it.</summary>
    [Fact]
    public void ASecondPressTakesTheWord()
    {
        var (box, easel, input) = Laid("Airlock door");

        input.PointerDown(At(box, easel, "Airlock do"), 2);
        input.PointerUp(At(box, easel, "Airlock do"));

        Assert.Equal("door", box.SelectedText());
    }

    /// <summary>Shift with the arrows extends a selection, and typing replaces it.</summary>
    [Fact]
    public void TypingReplacesASelectionMadeWithTheKeys()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.End, false, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Key(MyKeys.Left, true, false);
        input.Text('!');

        Assert.Equal("Air!", box.Text);
    }

    /// <summary>What is cut can be pasted back, through the root's clipboard.</summary>
    [Fact]
    public void CutAndPasteGoThroughTheClipboard()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.A, false, true);
        input.Key(MyKeys.X, false, true);
        Assert.Equal("", box.Text);

        input.Key(MyKeys.V, false, true);
        input.Key(MyKeys.V, false, true);
        Assert.Equal("AirlockAirlock", box.Text);
    }

    /// <summary>Enter carries out the box's submit command.</summary>
    [Fact]
    public void EnterSubmits()
    {
        var (box, _, input) = Laid("Airlock");
        var submitted = 0;
        box.Submit = new Relay(() => submitted++);

        input.Focus(box);
        input.Key(MyKeys.Enter, false, false);

        Assert.Equal(1, submitted);
    }

    /// <summary>Escape carries out the box's cancel command, and the box keeps the keyboard.</summary>
    [Fact]
    public void EscapeCancels()
    {
        var (box, _, input) = Laid("Airlock");
        var cancelled = 0;
        box.Cancel = new Relay(() => cancelled++);

        input.Focus(box);
        input.Key(MyKeys.Escape, false, false);

        Assert.Equal(1, cancelled);
        Assert.Same(box, input.Focused);
    }

    /// <summary>Escape lets the keyboard go from a box with no cancel command.</summary>
    [Fact]
    public void EscapeWithNothingToCancelLetsTheKeyboardGo()
    {
        var (box, _, input) = Laid("Airlock");

        input.Focus(box);
        input.Key(MyKeys.Escape, false, false);

        Assert.Null(input.Focused);
    }

    /// <summary>Typing says the text was edited; setting it does not.</summary>
    [Fact]
    public void TypingSaysItEditedAndSettingDoesNot()
    {
        var (box, _, input) = Laid("Airlock");
        var edits = 0;
        box.Edited += () => edits++;

        box.Text = "Hangar";
        Assert.Equal(0, edits);

        input.Focus(box);
        input.Text('s');
        Assert.Equal(1, edits);
    }

    /// <summary>A box being edited measures no text on a frame where nothing about it changed, and moving the caret measures none either.</summary>
    [Fact]
    public void AnEditedBoxMeasuresNothingItHasNotChanged()
    {
        var (box, easel, input) = Laid("Airlock door");

        var from = At(box, easel, "Air");
        var to = At(box, easel, "Airlock");

        input.Focus(box);
        easel.Draw(Wide, High);
        var settled = easel.Measured;

        easel.Draw(Wide, High);
        input.Key(MyKeys.Left, false, false);
        input.Key(MyKeys.Home, true, false);
        input.PointerDown(from, 1);
        input.PointerMove(to);
        input.PointerUp(to);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>An empty box is as wide as it is tall, so there is still somewhere to type.</summary>
    [Fact]
    public void AnEmptyBoxIsAsWideAsItIsTall()
    {
        var box = new TextBox { FontSize = 32f };
        _ = new Easel(new HStack { Children = { box } }, Wide, High);

        Assert.Equal(32f, box.Bounds.Width);
    }

    /// <summary>Holding the keyboard takes the style's focused layer.</summary>
    [Fact]
    public void HoldingTheKeyboardTakesTheFocusedLayer()
    {
        var (box, _, input) = Laid("Airlock");
        box.Style = new TextBoxStyle { Focused = { Edge = Focus } };

        input.Focus(box);
        Assert.Equal(Focus, box.Edge);

        input.Focus(null);
        Assert.Equal(Color.Transparent, box.Edge);
    }

    static (TextBox, Easel, InputRoot) Laid(string text)
    {
        var box = new TextBox { Text = text, Placement = Fixed.Wide(300f) };
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, Wide, High);
        return (box, easel, new InputRoot(root));
    }

    /// <summary>The point in the box just after <paramref name="run" />, where the text starts at the box's start.</summary>
    static Vector2 At(TextBox box, Easel easel, string run)
    {
        var x = box.ContentBounds.X + easel.WidthOf(run) / easel.Pixels;
        return new Vector2(x, box.Bounds.Y + box.Bounds.Height / 2f);
    }
}

