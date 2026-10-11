using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The blocks a step picks are a set, and a set is a slot: the same set is the same slot, a set made exactly of smaller sets
/// names their slots, and the slots follow the sets as the logic system is built and taken apart again.
/// </summary>
public class SetSlotTests
{
    const string Inner = "Airlock Inner Door";
    const string Outer = "Airlock Outer Door";
    const string Light = "Airlock Light";
    const string Cockpit = "Cockpit Light";

    /// <summary>Several blocks picked for one step are one slot holding all of them, numbered as a slot of several blocks is.</summary>
    [Fact]
    public void APickOfSeveralBlocksIsOneSlot()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);

        var slot = Assert.Single(Targets(screen, 0));
        Assert.Equal(4, system.First!.FilledWith(slot)!.Count);
        Assert.Equal("Slot 1", slot);
    }

    /// <summary>A slot of one block takes the block's name.</summary>
    [Fact]
    public void ASlotOfOneBlockTakesItsName()
    {
        var screen = Built(out _);
        Off(screen, Light);

        Assert.Equal(Light, Assert.Single(Targets(screen, 0)));
    }

    /// <summary>The same set picked again, in any order, is the same slot.</summary>
    [Fact]
    public void TheSameSetAgainIsTheSameSlot()
    {
        var screen = Built(out _);
        Off(screen, Inner, Outer, Light);
        Off(screen, Light, Inner, Outer);

        Assert.Equal(Targets(screen, 0), Targets(screen, 1));
    }

    /// <summary>A block of a set picked on its own is a slot of its own too, and fills both.</summary>
    [Fact]
    public void ABlockOfASetPickedAloneIsASlotOfItsOwn()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        Off(screen, Light);

        var all = Assert.Single(Targets(screen, 0));
        var one = Assert.Single(Targets(screen, 1));
        Assert.NotEqual(all, one);
        Assert.Contains(Declared(screen, Light), system.First!.FilledWith(all)!);
        Assert.Equal(new[] { Declared(screen, Light) }, system.First!.FilledWith(one));
    }

    /// <summary>A set made exactly of smaller sets has no slot of its own: its step names theirs, and its slot goes.</summary>
    [Fact]
    public void ASetMadeOfSmallerSetsNamesTheirSlots()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));

        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Assert.Equal(Targets(screen, 1).Concat(Targets(screen, 2)).OrderBy(n => n), Targets(screen, 0).OrderBy(n => n));
        Assert.Null(system.FindPart(whole));
    }

    /// <summary>Taking the smaller sets away again makes the whole set a slot of its own once more.</summary>
    [Fact]
    public void TakingTheSmallerSetsAwayMakesTheWholeASlotAgain()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Remove(screen, 2);
        Remove(screen, 1);

        var slot = Assert.Single(Targets(screen, 0));
        Assert.Equal(4, system.First!.FilledWith(slot)!.Count);
    }

    /// <summary>Taking back the pick that split a slot puts back the slot, its name and description, and every step's names.</summary>
    [Fact]
    public void UndoingASplitPutsTheSlotBack()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        Off(screen, Inner, Outer);
        var whole = Assert.Single(Targets(screen, 0));
        system.FindPart(whole)!.Annotation = "Everything switched off while cycling.";
        var parts = system.Parts.Count;

        Off(screen, Light, Cockpit);
        Assert.Null(system.FindPart(whole));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Equal(whole, Assert.Single(Targets(screen, 0)));
        Assert.Equal("Everything switched off while cycling.", system.FindPart(whole)!.Annotation);
        Assert.Equal(parts, system.Parts.Count);
    }

    /// <summary>A slot split while another installation holds blocks in it is kept, hidden, holding that installation's blocks.</summary>
    [Fact]
    public void AnotherInstallationsBlocksAreSetAside()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        var port = system.AddInstance();
        port.Fill(whole, new[] { Declared(screen, "Hangar Door") });

        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Assert.DoesNotContain(whole, Targets(screen, 0));
        Assert.NotNull(system.FindPart(whole));
        Assert.Equal(new[] { Declared(screen, "Hangar Door") }, port.FilledWith(whole));
    }

    /// <summary>A slot set aside is saved with the construct, holding its installation's blocks, and left out of the blueprint.</summary>
    [Fact]
    public void ASetAsideSlotIsSavedButNotShared()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        system.AddInstance().Fill(whole, new[] { Declared(screen, "Hangar Door") });
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        var values = screen.Editor.World.Values;
        var after = new Automation();
        Assert.Equal("", ProgramText.Load(ProgramText.Save(screen.Editor.Program, values), after, values));

        var read = after.FindSystem(system.Identity)!;
        Assert.NotNull(read.FindPart(whole));
        Assert.Equal(new[] { Declared(screen, "Hangar Door") }, read.Instances[1].FilledWith(whole));
        Assert.DoesNotContain("'" + whole + "'", ProgramText.Blueprint(system));
    }

    /// <summary>Another installation is marked under each slot it has to fill again, on the logic system's screen.</summary>
    [Fact]
    public void AnotherInstallationIsMarkedWhereItFillsAgain()
    {
        var screen = Split(out _, out _);

        var rows = screen.Editor.SystemSlots();
        var blocks = rows.Where(row => row.Mark == Symbols.Block).ToList();
        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, row => Assert.Equal("Installation 2: changed, fill it again", row.Marks));
        Assert.Equal("Installation 2: new, fill it", rows.Single(row => row.Mark == Symbols.Signal).Marks);
    }

    /// <summary>The installation's panel says which slots to fill again, and the blocks it had, which the picker lists first.</summary>
    [Fact]
    public void ThePanelOffersTheBlocksSetAsideFirst()
    {
        var screen = Split(out var system, out var port);
        var editor = screen.Editor;
        editor.Choose(system, port);
        editor.Push(new BlueprintPage(editor, system));
        screen.Run(2);

        var row = editor.Parts().First(r => r.Name == Assert.Single(Targets(screen, 1)));
        Assert.Equal("Set aside, offered first: " + Inner, row.SetAside);
        Assert.Contains(editor.Blueprint().View.Within().OfType<Lumel.Text.Label>(), label => label.Text == "2 slots to fill again");

        row.Fills!.Execute();
        screen.Run(2);
        var first = editor.Offered()[0];
        Assert.Equal(Inner, first.Text);
        Assert.Equal("Set aside", first.Secondary);
    }

    /// <summary>While its logic system is switched on, a slot is neither opened nor refilled from its screen, and its name says why.</summary>
    [Fact]
    public void ASlotWaitsForTheSwitchLikeEverythingElse()
    {
        var screen = Built(out var system);
        Off(screen, Light, Cockpit);
        var editor = screen.Editor;
        editor.Back();
        screen.Run(2);
        system.Annotation = "Cycles the airlock.";
        editor.SwitchSystem(system, true);
        screen.Run(2);

        var row = Assert.Single(editor.SystemSlots(), r => r.Mark == Symbols.Block);
        Assert.Null(row.Opens);
        Assert.Null(row.Fills);
        Assert.Equal(editor.Texts.SystemTurnOffToEdit, row.OpensAbout);

        var page = (Features.LogicSystems.LogicSystemPage)editor.Current!;
        Assert.False(page.About.IsVisible, "the description box was offered while the logic system may run");
        Assert.False(Headed(page, editor), "the description read as it is carried a heading");

        editor.SwitchSystem(system, false);
        editor.PressAnswer("Switch them off");
        screen.Run(2);
        row = Assert.Single(editor.SystemSlots(), r => r.Mark == Symbols.Block);
        Assert.NotNull(row.Opens);
        Assert.NotNull(row.Fills);
        Assert.True(page.About.IsVisible);
        Assert.True(Headed(page, editor));
    }

    /// <summary>Whether the logic system's screen shows the heading over its description.</summary>
    static bool Headed(Features.LogicSystems.LogicSystemPage page, Features.Shell.SprocketEditor editor) =>
        page.View.Within().OfType<Lumel.Text.Label>().Any(label => label.IsVisible && label.Text == editor.Texts.BlueprintDescription.ToUpperInvariant());

    /// <summary>Home says, on the other installation's row, how many of its slots it has to fill again.</summary>
    [Fact]
    public void HomeCountsTheSlotsToFillAgain()
    {
        var screen = Split(out _, out _);
        screen.Editor.GoToRoot();
        screen.Run(70);

        Assert.Equal("2 slots to fill again", screen.Editor.RootRows()[1].Problem);
    }

    /// <summary>Taking the smaller sets away brings back the slot set aside, its name and the other installation's blocks with it.</summary>
    [Fact]
    public void UndoingASplitByHandBringsTheSlotSetAsideBack()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        var port = system.AddInstance();
        port.Fill(whole, new[] { Declared(screen, Inner) });
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Remove(screen, 2);
        Remove(screen, 1);

        Assert.Equal(whole, Assert.Single(Targets(screen, 0)));
        Assert.Equal(new[] { Declared(screen, Inner) }, port.FilledWith(whole));
    }

    /// <summary>The blocks set aside are let go once every slot is filled again.</summary>
    [Fact]
    public void TheBlocksSetAsideGoOnceEverySlotIsFilled()
    {
        var screen = Split(out var system, out var port);
        var whole = Hidden(system);

        port.Fill(Assert.Single(Targets(screen, 1)), new[] { Declared(screen, Inner) });
        screen.Editor.Choose(system, port);
        screen.Editor.Push(new BlueprintPage(screen.Editor, system));
        screen.Run(2);

        var row = screen.Editor.Parts().First(r => r.Name == Assert.Single(Targets(screen, 2)));
        row.Fills!.Execute();
        screen.Run(2);
        screen.Editor.PressBlock(Light);
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Empty(port.FilledWith(whole)!);
        Assert.Empty(system.First!.FilledWith(whole)!);
        Assert.DoesNotContain("'" + whole + "'", ProgramText.Save(screen.Editor.Program, screen.Editor.World.Values));
    }

    /// <summary>A slot that four blocks filled, split in two while a second installation held a door in it; on the logic system's screen.</summary>
    static Screen Split(out AutomationSystem system, out SystemInstance port)
    {
        var screen = Built(out system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        port = system.AddInstance();
        port.Fill(whole, new[] { Declared(screen, Inner) });

        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        screen.Editor.Back();
        screen.Run(2);
        return screen;
    }

    /// <summary>The one slot of <paramref name="system" /> nothing names.</summary>
    static string Hidden(AutomationSystem system)
    {
        var used = new List<string>();
        system.Acted(used);
        return system.Parts.Single(part => !part.IsSignal && !used.Contains(part.Id)).Id;
    }

    /// <summary>A logic system with a sequence, for steps to be added to.</summary>
    static Screen Built(out AutomationSystem system)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        system = screen.Editor.Program.Systems[0];
        return screen;
    }

    /// <summary>Adds a step switching <paramref name="blocks" /> off, picked together.</summary>
    static void Off(Screen screen, params string[] blocks)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(blocks[0]);
        for (var i = 1; i < blocks.Length; i++)
            screen.Editor.TickBlock(blocks[i]);

        screen.Editor.PressChoice("Power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>Takes out the step at <paramref name="row" />.</summary>
    static void Remove(Screen screen, int row)
    {
        screen.Editor.StepRows()[row].Deletes!.Execute();
        screen.Run(2);
    }

    /// <summary>What the act at <paramref name="index" />, counting acts only, names.</summary>
    static List<string> Targets(Screen screen, int index) =>
        screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ElementAt(index).Targets.ToList();

    /// <summary>The variable the block called <paramref name="name" /> is declared as.</summary>
    static string Declared(Screen screen, string name) =>
        screen.Editor.Program.Declare(screen.Editor.World, screen.Editor.World.Entities.First(e => e.Name == name)).Name;
}
