using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Two automations meeting on one construct become one, and a construct parting leaves each half a copy of its own.</summary>
/// <remarks>
/// The harness runs one automation, so the second is made by hand on another programmable block of the same rig, as a merge
/// or a paste brings one. What the session does with the result - building the kept one again, emptying the other's block -
/// is the game's to show.
/// </remarks>
public class MeetingTests
{
    /// <summary>Two copies of a logic system that read the same become one, with the installations of both.</summary>
    [Fact]
    public void IdenticalCopiesBecomeOne()
    {
        var sim = Airlock(Rig.Demo());
        var other = Another(sim);
        Elsewhere(other);

        var conflicts = new List<ConflictMemory>();
        var joined = Read(sim, Meeting.Join(sim.Unit, other, conflicts, out _));

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

        var joined = Read(sim, Meeting.Join(sim.Unit, other, new List<ConflictMemory>(), out _));

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
        string switches;
        var joined = Read(sim, Meeting.Join(sim.Unit, other, conflicts, out switches), switches);

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
        var joined = Read(sim, Meeting.Join(sim.Unit, other, conflicts, out _));

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

        var joined = Read(sim, Meeting.Join(sim.Unit, other, new List<ConflictMemory>(), out _));

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
        SprocketUnit.Store(theirs.Block, written, "");
        sim.Roster.Adopt(theirs.Block);
        Assert.Equal(2, sim.Roster.Count);

        rig.Merge(other);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Single(sim.Roster.All);
        Assert.False(SprocketUnit.HasProgram(theirs.Block), "the joined automation's copy stayed in its block");
        sim.Press("Airlock Button");
        for (var i = 0; i < 300; i++)
            sim.Tick();
        Assert.Contains("Airlock Inner Door:open", sim.Machine.Log);
    }

    /// <summary>A half that parted takes over its buttons, which still name the block its automation came from, though that block is live on the other half.</summary>
    [Fact]
    public void APartedHalfTakesOverItsButtons()
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
        Assert.True(button.Toolbar.TryGetItem(0, out var item));
        Assert.Equal(sim.Unit.Block.EntityId, item.BlockEntityId);

        rig.Detach(rotor);
        sim.Tick();
        string written;
        sim.Unit.Written(out written, out _);
        SprocketUnit.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Texts, new ConstructMemory());
        half.TakeOver(sim.Unit.Block.EntityId);
        half.Begin(sim.Now);
        half.Tick(sim.Now + 1);

        Assert.True(button.Toolbar.TryGetItem(0, out item));
        Assert.Equal(spare.Block.EntityId, item.BlockEntityId);
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
        SprocketUnit.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Texts, new ConstructMemory());

        var onHead = Read(sim, Meeting.Part(half, sim.World, out _));
        var onBase = Read(sim, Meeting.Part(sim.Unit, half.World, out _));

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
        SprocketUnit.Store(spare.Block, written, "");
        var half = new SprocketUnit(spare.Block, new Settings(developerMode: false), new Rota(), sim.Texts, new ConstructMemory());

        Assert.Empty(Read(sim, Meeting.Part(half, sim.World, out _)).Systems);
        Assert.Single(Read(sim, Meeting.Part(sim.Unit, half.World, out _)).Systems);
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
        SprocketUnit.Store(block.Block, program, StateText.Save(sim.Unit.Program, null, false));
        return new SprocketUnit(block.Block, new Settings(developerMode: false), new Rota(), sim.Texts, new ConstructMemory());
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

    static Automation Read(Simulation sim, string program, string switches = "")
    {
        var read = new Automation();
        Assert.Equal("", ProgramText.Load(program, read, sim.World.Values));
        StateText.Load(switches, read, null);
        return read;
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

            var found = world.Match(declared.Keys, out _);
            if (found != null)
                called.Add(found.Name);
        }

        return called;
    }
}
