using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What the construct's own buttons do, read off the toolbars.
/// </summary>
/// <remarks>
/// A trigger that does one plain thing keeps nothing in the program - the slot is the record - so
/// this reading is the only way such a trigger is ever shown. It is the same reading that shows
/// wiring somebody did by hand before Sprocket was on the ship.
/// </remarks>
public class VanillaTests
{
    /// <summary>A rig with one button wired the way a player wires one, by hand.</summary>
    static Simulation Wired()
    {
        Simulation rig = Simulation.Fresh();
        rig.Tick();

        Panel panel = null;
        foreach (Panel found in rig.Panels())
            if (found.Name == "Airlock Button")
                panel = found;

        Assert.NotNull(panel);

        Toolbar.Changes changes = panel.Toolbar.Changing();
        changes.Set(0, MyToolbarItemInfo.BlockAction(2, "Open_On"), null);
        Assert.True(changes.Apply());

        return rig;
    }

    [Fact]
    public void ASlotSomebodyWiredIsRead()
    {
        Simulation rig = Wired();

        var wiring = new Vanilla();
        wiring.Read(rig.World);

        Assert.Single(wiring.Entries);
        Assert.Equal(21, wiring.Entries[0].PanelId);
        Assert.Equal(0, wiring.Entries[0].Slot);
        Assert.Equal(2, wiring.Entries[0].BlockId);
        Assert.Equal("Open_On", wiring.Entries[0].ActionId);
    }

    /// <summary>
    /// It reads as a sentence, in Sprocket's own words where it knows them.
    /// </summary>
    /// <remarks>
    /// The game names the action <c>Open_On</c>; Sprocket says "open" because that is the word its
    /// own sentences use for the same act. A block or an action it does not know is said in the
    /// game's words rather than guessed at.
    /// </remarks>
    [Fact]
    public void ItReadsAsASentence()
    {
        Simulation rig = Wired();

        var wiring = new Vanilla();
        wiring.Read(rig.World);

        Assert.Equal("When Airlock Button button 1 is pressed",
            VanillaText.Pressed(rig.World, wiring.Entries[0]));

        Assert.Equal("open Airlock Inner Door", VanillaText.Does(rig.World, wiring.Entries[0]));
    }

    /// <summary>
    /// Sprocket's own slots are left out.
    /// </summary>
    /// <remarks>
    /// They are the program, which the screens already have. Listing them here would show every
    /// sequence twice, once as itself and once as the button that starts it.
    /// </remarks>
    [Fact]
    public void SprocketsOwnWiringIsNotListed()
    {
        Simulation rig = Simulation.Fresh();

        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart("When…");
        rig.Editor.PressBlock("Airlock Button");
        rig.Editor.PressChoice("is pressed");
        rig.Editor.PressPrimary();
        rig.Editor.PressChoice("Run a new sequence");

        for (var i = 0; i < 5; i++)
            rig.Tick();

        var wiring = new Vanilla();
        wiring.Read(rig.World);

        Assert.Empty(wiring.Entries);
    }
}
