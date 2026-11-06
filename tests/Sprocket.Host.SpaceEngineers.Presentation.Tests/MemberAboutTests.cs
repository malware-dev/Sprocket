using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What the picker says of the block member chosen, at the top of the column about it.</summary>
public class MemberAboutTests
{
    static Screen Stepping()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        return screen;
    }

    static Screen Triggering()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        return screen;
    }

    /// <summary>An action says what it does over its options.</summary>
    [Fact]
    public void AnActionIsDescribedUnderItsName()
    {
        var screen = Stepping();

        screen.Editor.PressChoice("open");

        var column = screen.Editor.Pick().Arguments!;
        Assert.Equal("OPEN", column.Title);
        Assert.True(column.About.IsVisible);
        Assert.StartsWith("Opens the door.", column.About.Markdown);
    }

    /// <summary>A status word and an action of one name each say their own thing.</summary>
    [Fact]
    public void AStatusWordIsDescribedApartFromTheActionOfItsName()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd(SentenceText.IfWord);
        screen.Editor.PressBlock("Airlock Inner Door");

        screen.Editor.PressChoice("open");

        Assert.DoesNotContain("Opens the door", screen.Editor.Pick().Arguments!.About.Markdown);
    }

    /// <summary>A moment takes no options and is described all the same, and the column carries its name.</summary>
    [Fact]
    public void AMomentIsDescribedUnderItsName()
    {
        var screen = Triggering();

        screen.Editor.PressChoice("finishes opening");

        var does = screen.Editor.Pick().Stages[2];
        Assert.Equal("FINISHES OPENING", does.Title);
        Assert.Contains("when the door is fully open", screen.Editor.Pick().Arguments!.About.Markdown);
    }

    /// <summary>A number watched is described over how it compares.</summary>
    [Fact]
    public void ANumberWatchedIsDescribedUnderItsName()
    {
        var screen = Triggering();

        screen.Editor.PressChoice("how far open");

        Assert.Equal("HOW FAR OPEN", screen.Editor.Pick().Stages[2].Title);
        Assert.StartsWith("How much of the way", screen.Editor.Pick().Arguments!.About.Markdown);
    }

    /// <summary>Whether the world has airtightness is about the world, so a vent's list leaves it out.</summary>
    [Fact]
    public void AVentLeavesOutWhatIsAboutTheWorld()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd(SentenceText.IfWord);
        screen.Editor.PressBlock("Airlock Vent");

        var rows = screen.Editor.Pick().Stages[1].Entries().Select(row => row.Text).ToList();

        Assert.Contains("airtight", rows);
        Assert.DoesNotContain("in a world with airtightness", rows);
    }

    /// <summary>The terminal's list is the game's own, and the codex says nothing of it.</summary>
    [Fact]
    public void TheTerminalListIsNotDescribed()
    {
        var screen = Stepping();

        screen.Editor.Pick().Stages[1].PressView("Terminal");
        screen.Editor.PressChoice("Open/Closed");

        Assert.False(screen.Editor.Pick().Arguments!.About.IsVisible);
    }
}
