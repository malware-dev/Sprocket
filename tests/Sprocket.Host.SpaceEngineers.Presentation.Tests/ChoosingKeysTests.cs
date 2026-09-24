using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Screen;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A value answered by choosing is chosen from the keyboard as well as with the pointer.
/// </summary>
/// <remarks>
/// A field of words has a box only to hold the word, so keys that would type into it choose instead:
/// the arrows step through the words and a letter goes to the next word starting with it. A dial is
/// typed into, and turned a notch at a time by up and down.
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

    /// <summary>A column holding the fields, with the keyboard in the first.</summary>
    static Ask Form(params Field[] fields)
    {
        var form = Ask.Column(400f);
        form.Values("", 1, fields, new[] { "", "", "" });
        form.Take();
        return form;
    }

    [Fact]
    public void ArrowsStepThroughTheWords()
    {
        var form = Form(Words("Mode", "Standby", "Ready", "Fire"));

        Assert.True(form.Chooses, "a field of words did not choose");

        Assert.True(form.Step(1));
        Assert.Equal("Standby", form.Answer);

        Assert.True(form.Step(1));
        Assert.Equal("Ready", form.Answer);

        form.Step(1);
        Assert.False(form.Step(1), "the words went past the last one");
        Assert.Equal("Fire", form.Answer);

        form.Step(-1);
        Assert.Equal("Ready", form.Answer);
    }

    [Fact]
    public void ALetterGoesToTheNextWordStartingWithIt()
    {
        var form = Form(Words("Mode", "Standby", "Ready", "Stop"));

        Assert.True(form.Letter('s'));
        Assert.Equal("Standby", form.Answer);

        Assert.True(form.Letter('S'));
        Assert.Equal("Stop", form.Answer);

        Assert.True(form.Letter('s'));
        Assert.Equal("Standby", form.Answer);

        Assert.False(form.Letter('x'));
        Assert.Equal("Standby", form.Answer);
    }

    [Fact]
    public void ASwitchIsSteppedLikeAnyTwoWords()
    {
        var form = Form(Words("Power", "On", "Off"));

        form.Step(1);
        Assert.Equal("On", form.Answer);

        form.Step(1);
        Assert.Equal("Off", form.Answer);
    }

    [Fact]
    public void TabComesToAFieldOfWordsAndChoosingKeepsTheOthers()
    {
        var form = Form(Angle(), Words("Mode", "Standby", "Ready"));

        Assert.False(form.Chooses, "the dial was taken for words");
        Assert.True(form.Turns, "the dial did not turn");

        form.Next();
        Assert.True(form.Chooses, "Tab did not come to the words");

        form.Letter('r');
        Assert.Equal("Ready", form.Answer);
    }

    [Fact]
    public void UpAndDownTurnTheDialANotchWithinItsBounds()
    {
        var form = Form(Angle());

        form.Step(1);
        form.Step(1);
        Assert.Equal("10.0", form.Answer);

        for (var i = 0; i < 40; i++)
            form.Step(-1);

        Assert.Equal("-90.0", form.Answer);
    }
}
