using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A block renamed in the game's terminal is named by its new name wherever a screen names it: a line's tooltip, which lists what fills its slot.</summary>
public class RenamedBlockTests
{
    /// <summary>A step's sentence on the sequence's screen lists the block by its new name.</summary>
    [Fact]
    public void AStepNamesTheBlockByItsNewName()
    {
        var screen = Built();
        Assert.Contains("Airlock Inner Door", Said(screen));

        Rename(screen, "Airlock Inner Door", "Lobby Door");
        screen.Run(2);

        Assert.Contains("Lobby Door", Said(screen));
        Assert.DoesNotContain("Airlock Inner Door", Said(screen));
    }

    /// <summary>The same on the logic system's screen, where its trigger's button is named.</summary>
    [Fact]
    public void ATriggerNamesItsBlockByItsNewName()
    {
        var screen = Built();
        screen.Editor.BackKeeping();
        screen.Run(2);
        Assert.Contains("Airlock Button", Said(screen));

        Rename(screen, "Airlock Button", "Exit Button");
        screen.Run(2);

        Assert.Contains("Exit Button", Said(screen));
    }

    /// <summary>A block renamed while nobody is looking is named by its new name as the screen next opens.</summary>
    [Fact]
    public void ABlockRenamedWhileClosedShowsItsNewNameOnOpening()
    {
        var screen = Built();
        var unit = screen.Scene.Ship.Unit;
        unit.Showing = false;

        Rename(screen, "Airlock Inner Door", "Lobby Door");
        screen.Run(5);

        unit.Showing = true;
        screen.Editor.Opened();
        screen.Run(2);

        Assert.Contains("Lobby Door", Said(screen));
    }

    static Screen Built()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.PressAdd("task");
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("close");
        editor.PressPrimary();
        screen.Run(2);
        return screen;
    }

    static void Rename(Screen screen, string from, string to) =>
        screen.Scene.Ship.Machine.Fitted.First(fitting => fitting.Name == from).Name = to;

    /// <summary>What the parts of the sentences on the screen say, and their tooltips, which list the blocks filling their slots.</summary>
    static string Said(Screen screen) =>
        string.Join(" ", screen.Legible) + " "
        + string.Join(" ", screen.Editor.Current.View!.Within().OfType<SlotChip>().Select(chip => chip.Tip?.Text ?? ""));
}
