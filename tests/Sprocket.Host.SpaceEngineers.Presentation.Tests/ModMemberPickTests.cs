using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sandbox.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Members of two mods that share an id on one block are offered apart, each with its mod's name under it, and picking one
/// makes a step in that mod's namespace.
/// </summary>
public class ModMemberPickTests
{
    const string Hover = "malforge.hover_repulsors";
    const string Elevator = "malforge.elevator";

    /// <summary>A sequence open on a fresh construct whose tank offers lift from two mods, both registered.</summary>
    static Screen TwoLifts()
    {
        Assert.True(HostNamespaces.Register(Hover, "Hover Repulsors", "Hover Repulsors", "Malforge"));
        Assert.True(HostNamespaces.Register(Elevator, "Elevator", "Elevator", "Malforge"));

        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);

        var kind = screen.Editor.World.Entities.First(entity => entity.Name == "Hydrogen Tank").Kind;
        foreach (var space in new[] { Hover, Elevator })
        {
            kind.WithAction("lift", "lift", OwnAction.Of<IMyTerminalBlock>());
            kind.Actions[kind.Actions.Count - 1].InNamespace(space);
        }

        return screen;
    }

    /// <summary>The entries of the what column, the last of the picker's.</summary>
    static List<PickRow> Entries(Screen screen) => screen.Editor.Pick().Stages.Last().Entries();

    /// <summary>Presses the lift entry whose second line names <paramref name="mod" />.</summary>
    static void PressLift(Screen screen, string mod)
    {
        Entries(screen).Single(entry => entry.Text == "lift" && entry.Secondary == mod).Pick(HeldModifier.Alone, 1);
        screen.Run(2);
    }

    /// <summary>
    /// Picks the tank's own Stockpile On in the terminal view, where a mod offers <c>Stockpile_On</c> too where
    /// <paramref name="collides" /> says so, and gives back what the step holds and how the program writes it.
    /// </summary>
    static (MemberKey Key, string Written) PickStockpile(bool collides)
    {
        Assert.True(HostNamespaces.Register(Hover, "Hover Repulsors", "Hover Repulsors", "Malforge"));
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        if (collides)
        {
            var kind = editor.World.Entities.First(entity => entity.Name == "Hydrogen Tank").Kind;
            kind.WithAction("Stockpile_On", "stockpile on", OwnAction.Of<IMyTerminalBlock>());
            kind.Actions[kind.Actions.Count - 1].InNamespace(Hover);
        }

        editor.PressAdd("task");
        editor.PressBlock("Hydrogen Tank");
        editor.Pick().Stages.Single(stage => stage.Views.Count > 0).PressView("Terminal");
        screen.Run(2);
        editor.PressChoice("Stockpile On");
        editor.PressPrimary();
        screen.Run(2);

        var act = Assert.IsType<Act>(editor.Program.Sequences[0].Steps[0]);
        return (act.Key, ProgramWriter.Save(editor.Program, editor.World.Values));
    }

    /// <summary>The game's own member picked where a mod has one of its id is written qualified by <c>game</c>.</summary>
    [Fact]
    public void AGameMemberAModCollidesWithIsPickedQualified()
    {
        try
        {
            var (key, written) = PickStockpile(true);

            Assert.Equal(new MemberKey(BlockKinds.Game, "Stockpile_On"), key);
            Assert.Contains("call game.Stockpile_On on [ ", written);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>The game's own member picked where nothing else has its id is written bare, as it always was.</summary>
    [Fact]
    public void AGameMemberNothingCollidesWithIsPickedBare()
    {
        try
        {
            var (key, written) = PickStockpile(false);

            Assert.Equal(MemberKey.Bare("Stockpile_On"), key);
            Assert.Contains("call Stockpile_On on [ ", written);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>Both lifts are listed, each with its mod's name, and an entry whose id no other shares has none.</summary>
    [Fact]
    public void BothLiftsAreListedWithTheirModsNames()
    {
        try
        {
            var screen = TwoLifts();
            screen.Editor.PressAdd("task");
            screen.Editor.PressBlock("Hydrogen Tank");
            screen.Run(2);

            var entries = Entries(screen);
            Assert.Equal(new[] { "Hover Repulsors", "Elevator" }, entries.Where(entry => entry.Text == "lift").Select(entry => entry.Secondary));
            Assert.All(entries.Where(entry => !entry.IsHeading && entry.Text != "lift"), entry => Assert.Equal("", entry.Secondary));
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>Picking each lift makes a step in its mod's namespace, and the step opens on the one it was made with.</summary>
    [Fact]
    public void EachLiftMakesAStepInItsNamespaceAndReopensOnIt()
    {
        try
        {
            var screen = TwoLifts();
            var editor = screen.Editor;
            editor.PressAdd("task");
            editor.PressBlock("Hydrogen Tank");
            screen.Run(2);
            PressLift(screen, "Elevator");
            editor.PressPrimary();
            screen.Run(2);

            var act = Assert.IsType<Act>(editor.Program.Sequences[0].Steps[0]);
            Assert.Equal(new MemberKey(Elevator, "lift"), act.Key);

            editor.PressStepSlot(0, 0);
            screen.Run(2);
            Assert.Equal("Elevator", Entries(screen).Single(entry => entry.IsChosen && entry.Text == "lift").Secondary);

            PressLift(screen, "Hover Repulsors");
            editor.PressPrimary();
            screen.Run(2);

            act = Assert.IsType<Act>(editor.Program.Sequences[0].Steps[0]);
            Assert.Equal(new MemberKey(Hover, "lift"), act.Key);

            editor.PressStepSlot(0, 0);
            screen.Run(2);
            Assert.Equal("Hover Repulsors", Entries(screen).Single(entry => entry.IsChosen && entry.Text == "lift").Secondary);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }
}
