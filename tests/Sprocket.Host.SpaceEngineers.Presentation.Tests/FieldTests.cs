using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using VRageMath;
using Lumel.Input;
using Lumel;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A field says what it will take, and then refuses what it will not.
/// </summary>
/// <remarks>
/// The bounds come from whoever described the action, because nothing in the game carries them: a
/// light's color setter declares three values and never says that each is a byte. Without this a
/// 300 is typed, accepted, written into the program, and then thrown away by the block with nobody
/// told - which is the failure this exists to stop happening in the one place somebody is looking.
/// </remarks>
public class FieldTests
{
    /// <summary>A field says what it takes beside its name, and on its own where nobody named the value.</summary>
    [Fact]
    public void WhatItTakesIsSaidBesideItsName()
    {
        Assert.Equal("Red (0 - 255)", Field.Between("Red", "0", "255").Label(new Texts()));
        Assert.Equal("Angle (0 or more)", Field.Between("Angle", "0", "").Label(new Texts()));
        Assert.Equal("Angle (90 or less)", Field.Between("Angle", "", "90").Label(new Texts()));
        Assert.Equal("Angle", Field.Called("Angle").Label(new Texts()));

        Assert.Equal("(0 - 255)", Field.Between("", "0", "255").Label(new Texts()));
        Assert.Equal("", Field.Unnamed().Label(new Texts()));
    }

    /// <summary>A value within the bounds is taken, and one outside them or not a number is refused in words saying what the field takes.</summary>
    [Fact]
    public void AValueWithinThemIsTaken()
    {
        Field red = Field.Between("Red", "0", "255");

        Assert.Equal("", red.Refuses(new Texts(), "Red", "0"));
        Assert.Equal("", red.Refuses(new Texts(), "Red", "136"));
        Assert.Equal("", red.Refuses(new Texts(), "Red", "255"));

        Assert.Equal("Red must be between 0 and 255.", red.Refuses(new Texts(), "Red", "256"));
        Assert.Equal("Red must be between 0 and 255.", red.Refuses(new Texts(), "Red", "-1"));
        Assert.Equal("Angle must be 0 or more.", Field.Between("Angle", "0", "").Refuses(new Texts(), "Angle", "-4"));
        Assert.Equal("Angle must be 90 or less.", Field.Between("Angle", "", "90").Refuses(new Texts(), "Angle", "91"));

        Assert.Equal("Red must be a number.", Field.Between("Red", "0", "255").Refuses(new Texts(), "Red", "crimson"));
    }

    /// <summary>
    /// An empty field is a value not given, which is a different thing from a value of nothing.
    /// </summary>
    /// <remarks>
    /// The action then does what it declared it does without one. Refusing an empty field would mean
    /// a form of three could not be answered with one.
    /// </remarks>
    [Fact]
    public void AnEmptyFieldIsNotRefused()
    {
        Assert.Equal("", Field.Between("Red", "0", "255").Refuses(new Texts(), "Red", ""));
    }

    /// <summary>
    /// A field told nothing takes whatever is put in it, which is nearly every field there is.
    /// </summary>
    /// <remarks>
    /// An action declares how many values it wants and not one word about any of them, so a value
    /// nobody has described has nothing it can be held to.
    /// </remarks>
    [Fact]
    public void AFieldToldNothingTakesAnything()
    {
        Assert.Equal("", Field.Called("Radius").Refuses(new Texts(), "Radius", "anything at all"));
    }

    /// <summary>
    /// A bound nobody can read as a number is said beside the box and enforced nowhere.
    /// </summary>
    /// <remarks>
    /// The file is somebody else's, so a bound can be anything they wrote. Treating an unreadable
    /// one as zero would refuse values that are perfectly good.
    /// </remarks>
    [Fact]
    public void ABoundThatIsNotANumberHoldsNobodyToAnything()
    {
        Field odd = Field.Between("Mood", "sullen", "cheerful");

        Assert.Equal("Mood (sullen - cheerful)", odd.Label(new Texts()));
        Assert.Equal("", odd.Refuses(new Texts(), "Mood", "7"));
        Assert.Equal("", odd.Refuses(new Texts(), "Mood", "livid"));
    }

    /// <summary>The form is refused in the words of the field that is wrong.</summary>
    [Fact]
    public void TheFormSaysWhichFieldIsWrong()
    {
        var form = Asking();

        form.Fields[0].Text = "255";
        form.Judge();
        Assert.Equal("", form.Wrong);

        form.Fields[1].Text = "300";
        Assert.Equal("Green must be between 0 and 255.", form.Wrong);
    }

    /// <summary>The first one, because one reason is what there is room to say.</summary>
    [Fact]
    public void AndSaysOneReasonAtATime()
    {
        var form = Asking();

        form.Fields[0].Text = "300";
        form.Fields[1].Text = "400";
        form.Judge();

        Assert.Equal("Red must be between 0 and 255.", form.Wrong);
    }

    /// <summary>A value nobody named is refused by its place in the order, which is what is beside it.</summary>
    [Fact]
    public void AValueWithNoNameIsRefusedByItsNumber()
    {
        var form = new FieldSet();
        form.Show(new[] { Field.Unnamed(), Field.Between("", "0", "255") }, new string[0], new ColorOffer(new Hsv[0], null, null), new Texts());

        form.Fields[1].Text = "900";
        form.Judge();

        Assert.Equal("Option 2 must be between 0 and 255.", form.Wrong);
    }

    /// <summary>What a field is about is told where it is filled in, on its name and on what is seen of the field; a field about nothing more tells nothing.</summary>
    [Fact]
    public void AFieldSaysWhatItIsAboutWhereItIsFilledIn()
    {
        var about = Field.Among("when stuck", new[] { "move on", "hold" }, new[] { "1", "0" });
        about.About = "**Hold** waits.";
        var form = new FieldSet();
        form.Show(new[] { about, Field.Called("speed") }, new string[0], new ColorOffer(new Hsv[0], null, null), new Texts());

        Assert.NotNull(((Element)form.Fields[0]).GetVisualChild(0).Tip);
        Assert.Null(((Element)form.Fields[1]).Tip);
        Assert.NotNull(Assert.Single(form.Within().OfType<Label>(), l => l.Text == "when stuck").Tip);
    }

    /// <summary>On or off is a switch; any other two words are a line of them, which has room for long ones.</summary>
    [Fact]
    public void OnlyOnOrOffIsASwitch()
    {
        var flag = Field.OneOf("keep aspect ratio", Literal.Of(true).Said, Literal.Of(false).Said);
        flag.Edits = FieldEditor.Chosen;
        var named = Field.Among("mode", new[] { "Keep the current direction", "Turn round at the end" }, new[] { "0", "1" });
        var form = new FieldSet();
        form.Show(new[] { flag, named }, new string[0], new ColorOffer(new Hsv[0], null, null), new Texts());

        Assert.IsType<SwitchField>(form.Fields[0]);
        Assert.IsType<WordsField>(form.Fields[1]);
    }

    /// <summary>A double press on a switch finishes the form with the word its first press gave, rather than flipping back.</summary>
    [Fact]
    public void ADoublePressOnASwitchFinishes()
    {
        var flag = Field.OneOf("keep aspect ratio", Literal.Of(true).Said, Literal.Of(false).Said);
        flag.Edits = FieldEditor.Chosen;
        var form = new FieldSet();
        form.Show(new[] { flag }, new string[0], new ColorOffer(new Hsv[0], null, null), new Texts());
        var finished = 0;
        form.Submitted += () => finished++;
        var flip = form.Within().OfType<Lumel.Controls.Switch>().Single();

        ((IInputTarget)flip).PointerDown(Vector2.Zero, PointerButton.Left, 1, false, false);
        flip.Command!.Execute();
        var given = form.Answer;
        ((IInputTarget)flip).PointerDown(Vector2.Zero, PointerButton.Left, 2, false, false);
        flip.Command!.Execute();

        Assert.Equal(1, finished);
        Assert.Equal(given, form.Answer);
    }

    /// <summary>Nothing is held against what is typed until finishing is tried.</summary>
    [Fact]
    public void NothingIsRefusedBeforeFinishingIsTried()
    {
        var form = Asking();

        form.Fields[0].Text = "300";

        Assert.Equal("", form.Wrong);
    }

    static FieldSet Asking()
    {
        var form = new FieldSet();
        form.Show(new[]
        {
            Field.Between("Red", "0", "255"),
            Field.Between("Green", "0", "255"),
            Field.Between("Blue", "0", "255")
        }, new string[0], new ColorOffer(new Hsv[0], null, null), new Texts());

        return form;
    }
}
