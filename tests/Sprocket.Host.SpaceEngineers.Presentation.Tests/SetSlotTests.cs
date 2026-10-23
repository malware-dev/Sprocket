using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The blocks a step picks either name the slots they fill already, the same set the same slot and a set made exactly of
/// smaller sets their slots, or make one new slot; a pick never changes a slot. Arranged on request, the slots are worked out
/// again from the sets, and a slot another installation still holds blocks in is set aside.
/// </summary>
public class SetSlotTests
{
    const string Inner = "Airlock Inner Door";
    const string Outer = "Airlock Outer Door";
    const string Light = "Airlock Light";
    const string Cockpit = "Cockpit Light";

    /// <summary>Several blocks picked for one step are one slot holding all of them, numbered on from the slots there are.</summary>
    [Fact]
    public void APickOfSeveralBlocksIsOneSlot()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);

        var slot = Assert.Single(Targets(screen, 0));
        Assert.Equal(4, system.First!.FilledWith(slot)!.Count);
        Assert.Equal("slot 2", slot);
    }

    /// <summary>A slot of one block is numbered as any other slot is.</summary>
    [Fact]
    public void ASlotOfOneBlockIsNumberedToo()
    {
        var screen = Built(out _);
        Off(screen, Light);

        Assert.Equal("slot 2", Assert.Single(Targets(screen, 0)));
    }

    /// <summary>The number skips a name taken in any case, by a slot, a state or a type.</summary>
    [Fact]
    public void TheNumberSkipsANameTakenInAnyCase()
    {
        var screen = Built(out var system);
        system.States.Add(new State("Slot 2", "", World.Literal.Of(false)));

        Off(screen, Light);

        Assert.Equal("slot 3", Assert.Single(Targets(screen, 0)));
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

    /// <summary>A set made exactly of smaller sets picked before it names their slots, and makes none of its own.</summary>
    [Fact]
    public void ASetMadeOfSmallerSetsNamesTheirSlots()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        var parts = system.Parts.Count;

        Off(screen, Inner, Outer, Light, Cockpit);

        Assert.Equal(Targets(screen, 0).Concat(Targets(screen, 1)).OrderBy(n => n), Targets(screen, 2).OrderBy(n => n));
        Assert.Equal(parts, system.Parts.Count);
    }

    /// <summary>Smaller sets picked after a larger one make slots of their own, and the larger set's slot and step are left as they were.</summary>
    [Fact]
    public void APickNeverChangesASlot()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));

        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Assert.Equal(whole, Assert.Single(Targets(screen, 0)));
        Assert.Equal(4, system.First!.FilledWith(whole)!.Count);
        Assert.DoesNotContain(whole, Targets(screen, 1));
        Assert.DoesNotContain(whole, Targets(screen, 2));
    }

    /// <summary>Taking a step away leaves the slot it named, holding its blocks.</summary>
    [Fact]
    public void TakingAStepAwayLeavesItsSlot()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer);
        var slot = Assert.Single(Targets(screen, 0));

        Remove(screen, 0);

        Assert.NotNull(system.FindPart(slot));
        Assert.Equal(2, system.First!.FilledWith(slot)!.Count);
    }

    /// <summary>Taking back a pick that made a slot takes the slot away with it.</summary>
    [Fact]
    public void UndoingAPickTakesItsSlotAway()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer);
        var slot = Assert.Single(Targets(screen, 0));
        var parts = system.Parts.Count;

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Null(system.FindPart(slot));
        Assert.Equal(parts - 1, system.Parts.Count);
    }

    /// <summary>Arranged, a set made exactly of smaller sets names their slots, and its own slot goes.</summary>
    [Fact]
    public void ArrangedASetMadeOfSmallerSetsNamesTheirSlots()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);

        Arrange(screen, system);

        Assert.Equal(Targets(screen, 1).Concat(Targets(screen, 2)).OrderBy(n => n), Targets(screen, 0).OrderBy(n => n));
        Assert.Null(system.FindPart(whole));
    }

    /// <summary>A slot arranged away while another installation holds blocks in it is kept, hidden, holding that installation's blocks.</summary>
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
        Arrange(screen, system);

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
        Arrange(screen, system);

        var values = screen.Editor.World.Values;
        var after = new Automation();
        Assert.Equal("", ProgramReader.Load(ProgramWriter.Save(screen.Editor.Program, values), after, values));

        var read = after.FindSystem(system.Identity)!;
        Assert.NotNull(read.FindPart(whole));
        Assert.Equal(new[] { Declared(screen, "Hangar Door") }, read.Instances[1].FilledWith(whole));
        Assert.DoesNotContain("'" + whole + "'", ProgramWriter.Blueprint(system));
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
        editor.BackKeeping();
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

    /// <summary>Taking the smaller sets away and arranging again brings back the slot set aside, its name and the other installation's blocks with it.</summary>
    [Fact]
    public void ArrangingAgainBringsTheSlotSetAsideBack()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        var port = system.AddInstance();
        port.Fill(whole, new[] { Declared(screen, Inner) });
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        Arrange(screen, system);

        Remove(screen, 2);
        Remove(screen, 1);
        Arrange(screen, system);

        Assert.Equal(whole, Assert.Single(Targets(screen, 0)));
        Assert.Equal(new[] { Declared(screen, Inner) }, port.FilledWith(whole));
    }

    /// <summary>The blocks set aside stay as every slot is filled again, and are let go when the slots are next arranged.</summary>
    [Fact]
    public void TheBlocksSetAsideGoWhenArrangedOnceEverySlotIsFilled()
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
        Assert.NotNull(system.FindPart(whole));

        // Nothing names it, and the installation shown is the only one holding blocks in it, so arranging lets it go.
        Arrange(screen, system);
        Assert.Null(system.FindPart(whole));
        Assert.Null(port.FilledWith(whole));
        Assert.DoesNotContain("'" + whole + "'", ProgramWriter.Save(screen.Editor.Program, screen.Editor.World.Values));
    }

    /// <summary>A slot that four blocks filled, arranged in two while a second installation held a door in it; on the logic system's screen.</summary>
    static Screen Split(out AutomationSystem system, out SystemInstance port)
    {
        var screen = Built(out system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(screen, 0));
        port = system.AddInstance();
        port.Fill(whole, new[] { Declared(screen, Inner) });

        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        Arrange(screen, system);
        screen.Editor.BackKeeping();
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

        screen.Editor.PressChoice("power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>Gives <paramref name="system" /> the fewest slots its steps can name, as a person asking for it would.</summary>
    static void Arrange(Screen screen, AutomationSystem system)
    {
        screen.Editor.Picks.Arranged(system)!.Apply();
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
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
