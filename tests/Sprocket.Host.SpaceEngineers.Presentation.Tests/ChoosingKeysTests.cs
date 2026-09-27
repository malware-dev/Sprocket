using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using VRage.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A value answered by choosing is chosen from the keyboard as well as with the pointer.
/// </summary>
/// <remarks>
/// A field of words has no box to type into, so keys that would type choose instead: the arrows step through the words and
/// a letter goes to the next word starting with it. A dial is typed into, and turned a notch at a time by up and down.
/// </remarks>
public class ChoosingKeysTests
{
    static Field Words(string name, params string[] words)
    {
        return new Field { Name = name, Edits = Editor.Chosen, Words = new List<string>(words) };
    }

    static Field Angle()
    {
        return new Field { Name = "Angle", Edits = Editor.Angle, Least = "-90", Most = "90", Entry = Entry.Decimal };
    }

    /// <summary>A form holding the fields, with the keyboard in the first.</summary>
    static FieldSet Form(out InputRoot input, params Field[] fields)
    {
        var form = new FieldSet();
        form.Show(fields, new[] { "", "", "" }, new Hsv[0]);

        input = new InputRoot(form);
        form.Take(input);
        return form;
    }

    static void Press(InputRoot input, MyKeys key) => input.Key(key, false, false);

    [Fact]
    public void ArrowsStepThroughTheWords()
    {
        var form = Form(out var input, Words("Mode", "Standby", "Ready", "Fire"));

        Assert.Same(form.Fields[0].Keyboard, input.Focused);

        Press(input, MyKeys.Right);
        Assert.Equal("Standby", form.Answer);

        Press(input, MyKeys.Right);
        Assert.Equal("Ready", form.Answer);

        Press(input, MyKeys.Right);
        Press(input, MyKeys.Right);
        Assert.Equal("Fire", form.Answer);

        Press(input, MyKeys.Left);
        Assert.Equal("Ready", form.Answer);
    }

    [Fact]
    public void ALetterGoesToTheNextWordStartingWithIt()
    {
        var form = Form(out var input, Words("Mode", "Standby", "Ready", "Stop"));

        input.Text('s');
        Assert.Equal("Standby", form.Answer);

        input.Text('S');
        Assert.Equal("Stop", form.Answer);

        input.Text('s');
        Assert.Equal("Standby", form.Answer);

        input.Text('x');
        Assert.Equal("Standby", form.Answer);
    }

    [Fact]
    public void ASwitchIsSteppedLikeAnyTwoWords()
    {
        var form = Form(out var input, Words("Power", "On", "Off"));

        Press(input, MyKeys.Right);
        Assert.Equal("On", form.Answer);

        Press(input, MyKeys.Right);
        Assert.Equal("Off", form.Answer);
    }

    [Fact]
    public void TabComesToAFieldOfWordsAndChoosingKeepsTheOthers()
    {
        var form = Form(out var input, Angle(), Words("Mode", "Standby", "Ready"));

        Assert.Same(form.Fields[0].Keyboard, input.Focused);

        Press(input, MyKeys.Tab);
        Assert.Same(form.Fields[1].Keyboard, input.Focused);

        input.Text('r');
        Assert.Equal("Ready", form.Fields[1].Text);
    }

    [Fact]
    public void UpAndDownTurnTheDialANotchWithinItsBounds()
    {
        var form = Form(out var input, Angle());

        Press(input, MyKeys.Up);
        Press(input, MyKeys.Up);
        Assert.Equal("10.0", form.Answer);

        for (var i = 0; i < 40; i++)
            Press(input, MyKeys.Down);

        Assert.Equal("-90.0", form.Answer);
    }
}
