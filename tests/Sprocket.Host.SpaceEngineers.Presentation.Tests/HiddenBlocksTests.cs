using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block hidden from toolbars is left out of a picker's list of blocks until the eye beside the list is opened, as the
/// game's toolbar list leaves it out.
/// </summary>
public class HiddenBlocksTests
{
    static Screen WithAHiddenVent(Rig rig)
    {
        rig.Fitted.Single(fitting => fitting.Name == "Airlock Vent").InToolbars = false;
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        return screen.Run(2);
    }

    static StageColumn Blocks(Screen screen) => screen.Editor.Pick().Stages[0];

    static bool Lists(Screen screen, string name) => Blocks(screen).Entries().Any(entry => entry.Text == name);

    /// <summary>The eye starts shut, so a hidden block is not listed, and opening it lists the block; shutting it takes it away again.</summary>
    [Fact]
    public void TheEyeShowsWhatIsHiddenFromToolbars()
    {
        var screen = WithAHiddenVent(Rig.Demo());

        Assert.False(Lists(screen, "Airlock Vent"), "a block hidden from toolbars is listed");
        Assert.True(Lists(screen, "Airlock Inner Door"), "a block shown in toolbars is not listed");
        Assert.False(Blocks(screen).ShowsHidden, "the eye does not start shut");

        Blocks(screen).ShowHidden!.Execute();
        screen.Run(2);
        Assert.True(Lists(screen, "Airlock Vent"), "opening the eye does not list the hidden block");
        Assert.True(Blocks(screen).ShowsHidden);

        Blocks(screen).ShowHidden!.Execute();
        screen.Run(2);
        Assert.False(Lists(screen, "Airlock Vent"), "shutting the eye leaves the hidden block listed");
    }

    /// <summary>A hidden block already picked stays listed with the eye shut, so it can still be unpicked.</summary>
    [Fact]
    public void APickedHiddenBlockStaysListed()
    {
        var screen = WithAHiddenVent(Rig.Demo());

        Blocks(screen).ShowHidden!.Execute();
        screen.Run(2);
        screen.Editor.PressBlock("Airlock Vent");
        screen.Run(2);

        Blocks(screen).ShowHidden!.Execute();
        screen.Run(2);
        Assert.True(Lists(screen, "Airlock Vent"), "a picked block went from the list as the eye shut");
    }

    /// <summary>The eye is drawn shut, and open once pressed.</summary>
    [Fact]
    public void TheEyeIsDrawnShutAndThenOpen()
    {
        var screen = WithAHiddenVent(Rig.Demo());
        Assert.Contains(screen.Drawings, drawn => drawn.IsSymbol && drawn.Text == Symbols.HiddenHidden);

        Blocks(screen).ShowHidden!.Execute();
        screen.Run(2);
        Assert.Contains(screen.Drawings, drawn => drawn.IsSymbol && drawn.Text == Symbols.HiddenShown);
    }
}
