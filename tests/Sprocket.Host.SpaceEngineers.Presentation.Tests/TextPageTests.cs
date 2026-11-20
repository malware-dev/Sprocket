using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Text written over several lines is shown in a form by where it starts, and written whole on a page of its own.</summary>
public class TextPageTests
{
    const string CustomData = "60 196 type Airlock enter press Sequences press \"+ sequence\" press \"+ step\" press task press \"Airlock Light\" press \"custom data\"";

    /// <summary>Typing and pasting stop at the most a box may hold, and taking an edit back counts its characters back.</summary>
    [Fact]
    public void ABoxHoldsToTheMostItMay()
    {
        var box = new TextArea { MaxLength = 5, Text = "abc" };

        box.Insert("defgh");
        Assert.Equal("deabc", box.Text);
        Assert.Equal(5, box.Length);

        box.Undo();
        Assert.Equal("abc", box.Text);
        Assert.Equal(3, box.Length);
    }

    /// <summary>A field shows the first line that has anything on it, without the space around it.</summary>
    [Fact]
    public void AFieldShowsItsFirstLineWithAnythingOnIt()
    {
        Assert.Equal("[Sprocket]", LinesField.Start("\n   \n  [Sprocket]  \ncycle=12"));
        Assert.Equal("", LinesField.Start(" \n "));
        Assert.Equal("", LinesField.Start(""));
    }

    /// <summary>Custom Data is written on a page of its own, held to what a block holds, and comes back to the step on Done.</summary>
    [Fact]
    public void CustomDataIsWrittenOnAPageOfItsOwn()
    {
        var screen = Screen.Fresh(CustomData.Split(' '));
        var lines = Assert.IsType<LinesField>(Assert.Single(screen.Editor.Pick().Arguments!.Form.Fields));
        Assert.Equal(64000, lines.Longest);

        screen.Do("press", "Edit", "tick", "2");
        var page = Assert.IsType<TextPage>(screen.Editor.Current);
        Assert.Equal(64000, page.Box!.MaxLength);
        page.Box.Insert("\n[Sprocket]\ncycle=12");
        screen.Do("tick", "2");
        Assert.Equal("20 of 64000 characters", page.Counted);

        screen.Do("press", "Done", "tick", "2");
        Assert.IsType<PickPage>(screen.Editor.Current);
        Assert.Equal("\n[Sprocket]\ncycle=12", lines.Text);
        Assert.True(screen.Shows("[Sprocket]"), "the field does not show where the text starts");
        Assert.Contains("[Sprocket]&br;cycle=12", screen.Editor.Pick().Arguments!.Form.Answer);
    }

    /// <summary>Text written after Done refused it empty clears the refusal as it comes back, and Done then finishes with it.</summary>
    [Fact]
    public void TextWrittenAfterARefusalIsTaken()
    {
        var screen = Screen.Fresh(CustomData.Split(' '));
        screen.Editor.PressPrimary();
        screen.Run(2);
        Assert.True(screen.Shows("Custom data is missing."), "an empty value was not refused");

        screen.Do("press", "Edit", "tick", "2");
        ((TextPage)screen.Editor.Current!).Box!.Insert("cycle=12");
        screen.Do("press", "Done", "tick", "2");

        Assert.IsType<PickPage>(screen.Editor.Current);
        Assert.False(screen.Shows("Custom data is missing."), "the refusal stayed after the text came back");

        screen.Editor.PressPrimary();
        screen.Run(2);
        Assert.IsNotType<PickPage>(screen.Editor.Current);
    }

    /// <summary>Going back from the page after changing the text asks whether to save it, and discarding leaves the step as it was.</summary>
    [Fact]
    public void GoingBackAfterAChangeAsksWhetherToSaveIt()
    {
        var screen = Screen.Fresh(CustomData.Split(' '));
        var lines = (LinesField)screen.Editor.Pick().Arguments!.Form.Fields[0];

        screen.Do("press", "Edit", "tick", "2");
        var page = (TextPage)screen.Editor.Current;
        page.Box!.Insert("changed");

        Assert.False(page.Leaving());
        screen.Do("tick", "2", "press", "\"Discard", "them\"", "tick", "2");

        Assert.IsType<PickPage>(screen.Editor.Current);
        Assert.Equal("", lines.Text);
    }
}
