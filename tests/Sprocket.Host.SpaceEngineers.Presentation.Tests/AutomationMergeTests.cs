using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using Xunit;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Two automations meeting on one construct become one, and a construct parting leaves each half a copy of its own.</summary>
/// <remarks>
/// The harness runs one automation, so the second is made by hand on another programmable block of the same rig, as a merge
/// or a paste brings one. What the session does with the result - changing the kept one in place, emptying the other's
/// block - is the game's to show.
/// </remarks>
public class AutomationMergeTests
{
    /// <summary>
    /// What a joining Sprocket remembers of an installation it brings goes with it, under the logic system and number the
    /// installation has after the join, and what the one that stays remembers of an installation is kept.
    /// </summary>
    [Fact]
    public void WhatAnInstallationRemembersGoesWithItIntoAJoin()
    {
        var theirs = Guid.NewGuid();
        var ours = Guid.NewGuid();
        var from = new ConstructMemory();
        from.Runs.Add(new RunMemory { System = theirs, Instance = 1, Sequence = 1, Running = true, Step = 2 });
        from.Runs.Add(new RunMemory { System = theirs, Instance = 2, Sequence = 1, Running = true, Step = 4 });
        from.Watches.Add(new WatchMemory { System = theirs, Instance = 1, Trigger = 1, Fired = 3 });
        from.States.Add(new StateMemory { System = theirs, Instance = 1, State = "where", Choice = "outside" });
        var into = new ConstructMemory();
        into.Runs.Add(new RunMemory { System = ours, Instance = 2, Sequence = 1, Step = 0 });
        var moves = new List<InstanceMove>
        {
            new InstanceMove { FromSystem = theirs, FromNumber = 1, System = ours, Number = 3 },
            new InstanceMove { FromSystem = theirs, FromNumber = 2, System = ours, Number = 2 }
        };

        AutomationMerge.Carry(from, into, moves);

        var run = Assert.Single(into.Runs, each => each.Instance == 3);
        Assert.Equal((ours, 1, true, 2), (run.System, run.Sequence, run.Running, run.Step));
        Assert.Equal(0, Assert.Single(into.Runs, each => each.Instance == 2).Step);
        var watch = Assert.Single(into.Watches);
        Assert.Equal((ours, 3, 3), (watch.System, watch.Instance, watch.Fired));
        var state = Assert.Single(into.States);
        Assert.Equal((ours, 3, "outside"), (state.System, state.Instance, state.Choice));
        Assert.Empty(from.Runs);
    }

    /// <summary>Two copies of a logic system that read the same become one, with the installations of both.</summary>
    [Fact]
    public void IdenticalCopiesBecomeOne()
    {
        var sim = Airlock(Rig.Demo());
        var other = Another(sim);
        Elsewhere(other);

        var conflicts = new List<ConflictMemory>();
        var joined = AutomationMerge.Join(sim.Unit, other, conflicts, new List<InstanceMove>());

        var system = Assert.Single(joined.Systems);
        Assert.Equal(2, system.Instances.Count);
        Assert.Empty(conflicts);
    }

    /// <summary>An installation filled exactly as one already there is the same installation, and is not doubled.</summary>
    [Fact]
    public void AnInstallationAlreadyHereIsNotDoubled()
    {
        var sim = Airlock(Rig.Demo());
        var other = Another(sim);

        var joined = AutomationMerge.Join(sim.Unit, other, new List<ConflictMemory>(), new List<InstanceMove>());

        Assert.Single(Assert.Single(joined.Systems).Instances);
    }

    /// <summary>Two copies that differ both stay, switched off, the one that arrived under an identity of its own, and wait for a choice.</summary>
    [Fact]
    public void DifferentCopiesBothStaySwitchedOff()
    {
        var sim = Airlock(Rig.Demo());
        sim.Editor.SwitchOn();
        sim.Tick();
        var other = Another(sim);
        Elsewhere(other);
        other.Program.Systems[0].Sequences[0].Name = "Cycle";

        var conflicts = new List<ConflictMemory>();
        var joined = AutomationMerge.Join(sim.Unit, other, conflicts, new List<InstanceMove>());

        Assert.Equal(2, joined.Systems.Count);
        var conflict = Assert.Single(conflicts);
        var here = joined.FindSystem(conflict.System);
        var waiting = joined.FindSystem(conflict.Waiting);
        Assert.NotNull(here);
        Assert.NotNull(waiting);
        Assert.Equal(sim.Editor.Program.Systems[0].Identity, conflict.System);
        Assert.False(here.Enabled, "the version on the construct runs before anybody chose");
        Assert.False(waiting.Enabled, "the version that arrived runs before anybody chose");
        Assert.Equal("Cycle", waiting.Sequences[0].Name);
        Assert.Single(waiting.Instances);
    }

    /// <summary>A logic system only the arriving automation has joins as it is.</summary>
    [Fact]
    public void ASystemOnlyOneHasJoinsAsItIs()
    {
        var sim = Airlock(Rig.Demo());
        var other = Another(sim);
        Elsewhere(other);
        var stranger = other.Program.Systems[0];
        stranger.Identity = Guid.NewGuid();

        var conflicts = new List<ConflictMemory>();
        var joined = AutomationMerge.Join(sim.Unit, other, conflicts, new List<InstanceMove>());

        Assert.Equal(2, joined.Systems.Count);
        Assert.NotNull(joined.FindSystem(stranger.Identity));
        Assert.Empty(conflicts);
    }

    /// <summary>An installation that joins keeps its own blocks, named in the program it joins.</summary>
    [Fact]
    public void AnInstallationJoinsWithItsOwnBlocks()
    {
        var sim = Airlock(Rig.Demo());
        var other = Another(sim);
        var slot = Elsewhere(other);

        var joined = AutomationMerge.Join(sim.Unit, other, new List<ConflictMemory>(), new List<InstanceMove>());

        var arrived = joined.Systems[0].Instances[1];
        Assert.Equal(new[] { "Hangar Door" }, Blocks(arrived.FilledWith(slot)!, joined, sim.World));
    }

    /// <summary>Two ships each with an automation, merged, have one automation, and the button goes on working.</summary>
    [Fact]
    public void TwoShipsMergedHaveOneAutomation()
    {
        var rig = Rig.Demo();
        var other = rig.Apart("Other Ship");
        var theirs = rig.Programmable(300, "Other Automation");
        rig.OnBase();
        var sim = Airlock(rig);
        sim.Editor.SwitchOn();
        sim.Tick();

        string written;
        sim.Unit.Written(out written, out _);
        BlockStorage.Store(theirs.Block, written, "");
        sim.Roster.Adopt(theirs.Block);
        Assert.Equal(2, sim.Roster.Count);

        rig.Merge(other);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Single(sim.Roster.All);
        Assert.False(BlockStorage.HasProgram(theirs.Block), "the joined automation's copy stayed in its block");
        sim.Press("Airlock Button");
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Contains("Airlock Inner Door:open", sim.Machine.Log);
    }

    /// <summary>A button names itself, so a half that parted answers its buttons as they are, with nothing rewritten.</summary>
    [Fact]
    public void APartedHalfAnswersItsButtonsAsTheyAre()
    {
        var (rig, sim, rotor, spare, button) = Arm();
        Assert.True(button.Toolbar.TryGetItem(0, out var item));
        Assert.Equal(button.Block.EntityId, item.BlockEntityId);

        rig.Detach(rotor);
        sim.Tick();
        string written;
        sim.Unit.Written(out written, out _);
        BlockStorage.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Kinds, sim.Texts, new ProtoBinary(), new ConstructMemory());
        half.TakeOver(sim.Unit.Block.EntityId);
        half.Begin(sim.Now);
        half.Tick(sim.Now + 1);

        Assert.True(button.Toolbar.TryGetItem(0, out item));
        Assert.Equal(button.Block.EntityId, item.BlockEntityId);
    }

    /// <summary>
    /// A slot naming the block the automation is kept in, as an older build wrote it, still runs,
    /// and is Sprocket's own as it is, so the pass that mends leaves it.
    /// </summary>
    [Fact]
    public void ASlotNamingTheAutomationsBlockRunsAndIsLeftAsItIs()
    {
        var (_, sim, _, _, button) = Arm();
        Assert.True(button.Toolbar.TryGetItem(0, out var item));
        var key = new List<TerminalActionParameter>();
        button.Toolbar.Parameters(0, key);
        Assert.True(Toolbars.Write(button.Toolbar, 0, SignalAction.Slot(sim.Unit.Block.EntityId, null), key));

        sim.Machine.Forget();
        sim.Press("Arm Button");
        for (var i = 0; i < 120; i++)
            sim.Tick();
        Assert.Contains("Arm Door:open", sim.Machine.Log);

        sim.Unit.TakeOver(0);
        sim.Tick();
        Assert.True(button.Toolbar.TryGetItem(0, out item));
        Assert.Equal(sim.Unit.Block.EntityId, item.BlockEntityId);
    }

    /// <summary>An arm on a rotor with a button that opens its door, its spare programmable block on the rotor's head.</summary>
    static (Rig rig, Simulation sim, Rotor rotor, Programmable spare, Panel button) Arm()
    {
        var rig = Rig.Demo();
        var rotor = rig.Rotor(50, "Arm Rotor");
        rig.OnHeadOf(rotor);
        var spare = rig.Programmable(200, "Arm Automation");
        var button = rig.Panel(61, "Arm Button");
        rig.Door(60, "Arm Door");
        rig.OnBase();
        var sim = Simulation.On(rig);
        sim.Editor.NewSystem("Arm");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Arm Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Arm Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        sim.Tick();
        return (rig, sim, rotor, spare, button);
    }

    /// <summary>A construct parting leaves each half a copy holding the installations on its own half, and one split across both on both.</summary>
    [Fact]
    public void EachHalfKeepsTheInstallationsOnIt()
    {
        var rig = Rig.Demo();
        var rotor = rig.Rotor(50, "Arm Rotor");
        rig.OnHeadOf(rotor);
        var spare = rig.Programmable(200, "Arm Automation");
        rig.Door(60, "Arm Door");
        rig.OnBase();
        var sim = Airlock(rig);

        var program = sim.Editor.Program;
        var system = program.Systems[0];
        var slot = DoorSlot(system.First!, program, sim.World);
        var arm = program.Declare(sim.World, sim.World.Entities.First(entity => entity.Name == "Arm Door"));
        var inner = system.First!.FilledWith(slot)![0];
        system.AddInstance().Fill(slot, new[] { arm.Name });
        system.AddInstance().Fill(slot, new[] { inner, arm.Name });
        program.Edited();
        sim.Tick();

        var parted = new List<IMyTerminalBlock>();
        sim.Unit.Gathered += unit => parted.AddRange(unit.Parted);
        rig.Detach(rotor);
        sim.Tick();

        Assert.Contains(spare.Block, parted);

        string written;
        sim.Unit.Written(out written, out _);
        BlockStorage.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Kinds, sim.Texts, new ProtoBinary(), new ConstructMemory());

        var onHead = Parted(half, sim.World);
        var onBase = Parted(sim.Unit, half.World);

        Assert.Equal(new[] { 2, 3 }, onHead.Systems[0].Instances.Select(instance => instance.Number));
        Assert.Equal(new[] { 1, 3 }, onBase.Systems[0].Instances.Select(instance => instance.Number));

        // On the head, the inner door is lost rather than taken for the arm door, and the arm door is found from the head's own grid.
        var split = onHead.Systems[0].FindInstance(3)!.FilledWith(slot)!;
        Assert.Equal(new[] { "Arm Door" }, Blocks(split, onHead, half.World));
    }

    /// <summary>A half with none of a logic system's blocks keeps nothing of it.</summary>
    [Fact]
    public void AHalfWithNoneOfASystemsBlocksDropsIt()
    {
        var rig = Rig.Demo();
        var rotor = rig.Rotor(50, "Arm Rotor");
        rig.OnHeadOf(rotor);
        var spare = rig.Programmable(200, "Arm Automation");
        rig.OnBase();
        var sim = Airlock(rig);

        rig.Detach(rotor);
        sim.Tick();
        string written;
        sim.Unit.Written(out written, out _);
        BlockStorage.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Kinds, sim.Texts, new ProtoBinary(), new ConstructMemory());

        Assert.Empty(Parted(half, sim.World).Systems);
        Assert.Single(Parted(sim.Unit, half.World).Systems);
    }

    /// <summary>A part that takes a programmable block and none of the automation's blocks is given no copy of it.</summary>
    [Fact]
    public void APartWithNoneOfTheAutomationsBlocksGetsNoCopy()
    {
        var rig = Rig.Demo();
        var rotor = rig.Rotor(50, "Arm Rotor");
        rig.OnHeadOf(rotor);
        var spare = rig.Programmable(200, "Arm Automation");
        rig.OnBase();
        var sim = Airlock(rig);
        sim.Editor.SwitchOn();
        sim.Tick();

        rig.Detach(rotor);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Single(sim.Roster.All);
        Assert.False(BlockStorage.HasProgram(spare.Block), "the part was written a copy");
    }

    /// <summary>The airlock logic system on <paramref name="rig" />: its button opens the inner door.</summary>
    static Simulation Airlock(Rig rig)
    {
        var sim = Simulation.On(rig);
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Tick();
        return sim;
    }

    /// <summary>A second automation on another programmable block of the same construct, holding a copy of the first's program.</summary>
    static SprocketUnit Another(Simulation sim)
    {
        var block = sim.Machine.Programmable(200, "Other Automation");
        sim.Tick();

        string program;
        sim.Unit.Written(out program, out _);
        BlockStorage.Store(block.Block, program, BlockSwitches.Save(sim.Unit.Program, sim.Unit.Binary));
        return new SprocketUnit(block.Block, new Settings(developerMode: false), new Rota(), sim.Kinds, sim.Texts, new ProtoBinary(), new ConstructMemory());
    }

    /// <summary>
    /// Points the first installation of <paramref name="other" /> at the hangar door in place of the inner door, and empties
    /// its button, so it is an installation of its own rather than one already on the construct; answers the door's slot.
    /// </summary>
    static string Elsewhere(SprocketUnit other)
    {
        var instance = other.Program.Systems[0].First!;
        var slot = DoorSlot(instance, other.Program, other.World);
        var hangar = other.Program.Declare(other.World, other.World.Entities.First(entity => entity.Name == "Hangar Door"));
        instance.Fill(slot, new[] { hangar.Name });
        foreach (var filled in instance.Filled.ToList())
        {
            if (filled != slot)
                instance.Clear(filled);
        }

        return slot;
    }

    /// <summary>The program <paramref name="unit" /> keeps once its construct has parted from <paramref name="away" />.</summary>
    static Automation Parted(SprocketUnit unit, GridWorld away)
    {
        AutomationMerge.Part(unit.Program, unit.World, away);
        return unit.Program;
    }

    /// <summary>The slot of <paramref name="instance" /> the inner door fills.</summary>
    static string DoorSlot(SystemInstance instance, Automation program, GridWorld world)
    {
        return instance.Filled.First(slot => Blocks(instance.FilledWith(slot)!, program, world).Contains("Airlock Inner Door"));
    }

    /// <summary>What the blocks <paramref name="names" /> are called on <paramref name="world" />, as <paramref name="program" /> declares them.</summary>
    static List<string> Blocks(IList<string> names, Automation program, GridWorld world)
    {
        var called = new List<string>();
        foreach (var name in names)
        {
            var declared = program.FindRef(name);
            if (declared == null)
                continue;

            var found = world.Keys.Match(declared.Keys, out _);
            if (found != null)
                called.Add(found.Name);
        }

        return called;
    }
}
