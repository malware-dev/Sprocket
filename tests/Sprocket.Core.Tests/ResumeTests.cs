using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A cycle under way when the world is saved carries on when it is loaded.
/// </summary>
/// <remarks>
/// The world is written out mid-airlock: the inner door shut, the room emptying, the outer door
/// still closed. Reloading used to forget the run entirely, which leaves somebody sealed in a room
/// with both doors shut and nothing left to open either of them - the machine having done the
/// dangerous half of its job and dropped the rest.
///
/// So what is running is kept in the construct's memory, which the world saves, and a runner made over
/// that memory carries on from it. What is kept is the step and the clock; whether the doors have
/// arrived is asked of the doors.
/// </remarks>
public class ResumeTests
{
    sealed class Airlock
    {
        public readonly Rigged Rig = new();

        public Door Inner = null!;
        public Door Outer = null!;
        public Vent Vent = null!;
        public Panel Button = null!;

        public Automation Program = new();
        public AutomationRunner Runner = null!;

        /// <summary>What the program itself would not read, which must be nothing.</summary>
        public string Trouble = "";

        int _frame;

        public GridWorld World => Rig.World;

        public List<string> Log => Rig.Rig.Log;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        public string Name(Fitting fitting) =>
            Program.Declare(World, Rig.Entity(fitting.Id)).Name;

        public SequenceRunner State =>
            Runner.Get(Program.Sequences[0], Program.Systems[0].Uses[0]);

        /// <summary>The button being pressed, which is the construct calling the sequence.</summary>
        public void Press() =>
            Runner.Call(Program.Sequences[0], Program.Systems[0].Uses[0]);

        /// <summary>
        /// The world loading again: the blocks are where they were, and nothing else is.
        /// </summary>
        /// <remarks>
        /// A fresh program and a fresh runner read from the block's switches and the world's memory, which is what a
        /// reload does - the rig stands in for the grid, which the game brings back itself.
        /// </remarks>
        public string Reload(string program, Saved state)
        {
            Program = new Automation { Memory = Binary.Unpack<ConstructMemory>(state.Memory) };
            Trouble = ProgramReader.Load(program, Program, World.Values);
            Runner = new AutomationRunner(Program, World);
            return BlockSwitches.Load(state.Switches, Program, Binary);
        }

        /// <summary>What a save writes: the switches the block keeps, and the construct's memory the world keeps.</summary>
        public Saved Save() => new(BlockSwitches.Save(Program, Binary), Binary.Pack(Program.Memory));
    }

    /// <summary>The switches a block keeps and the memory a world keeps, packed.</summary>
    sealed class Saved
    {
        /// <summary>The switches the block keeps, packed.</summary>
        public readonly string Switches;

        /// <summary>The construct's memory, packed.</summary>
        public readonly string Memory;

        /// <summary>A save of <paramref name="switches" /> and <paramref name="memory" />.</summary>
        public Saved(string switches, string memory)
        {
            Switches = switches;
            Memory = memory;
        }
    }

    static readonly ProtoBinary Binary = new();

    static Airlock Build()
    {
        var a = new Airlock();
        a.Inner = a.Rig.Door(1, "Inner Door");
        a.Outer = a.Rig.Door(2, "Outer Door");
        a.Vent = a.Rig.Vent(3, "Airlock Vent");
        a.Button = a.Rig.Panel(4, "Inside Button");

        var button = a.Program.DeclareSignal(a.Name(a.Button), new ToolbarSlot(1, 1, 1), "button");
        var trigger = SignalTrigger.Named(button.Name);

        var cycle = a.Program.AddSequence(trigger);
        cycle.Name = "Let me out";

        // Named, because an unnamed system does not survive being written down - the format has no
        // spelling for one. Nothing to do with what is being tested here.
        cycle.System.Name = "Airlock";
        Then(cycle, "close", a.Name(a.Inner));
        Then(cycle, "depressurize", a.Name(a.Vent));
        Then(cycle, "open", a.Name(a.Outer));

        a.Runner = new AutomationRunner(a.Program, a.World);
        return a;
    }

    static void Then(Sequence sequence, string actionId, string target)
    {
        var act = new Act(actionId);
        act.Targets.Add(target);
        sequence.Steps.Add(Given.Defaults(act));
        sequence.Steps.Add(new Await());
    }

    [Fact]
    public void ARunUnderWayIsWrittenDown()
    {
        var a = Build();
        a.Tick(3);
        a.Press();
        a.Tick(20);

        Assert.True(a.State.IsRunning);

        var run = Assert.Single(a.Program.Memory.Runs, kept => kept.Running);
        Assert.True(run.Step >= 0);
    }

    [Fact]
    public void NothingRunningWritesNoRuns()
    {
        var a = Build();
        a.Tick(3);

        Assert.DoesNotContain(a.Program.Memory.Runs, kept => kept.Running);
    }

    /// <summary>
    /// The cycle finishes after a reload, without anybody pressing the button again.
    /// </summary>
    /// <remarks>
    /// The outer door opening is the whole of the proof: nothing else in the program can open it,
    /// and the trigger is never fired in the second half of this test.
    /// </remarks>
    [Fact]
    public void ACycleFinishesAfterAReload()
    {
        var a = Build();
        a.Tick(3);
        a.Press();
        a.Tick(20);

        // Mid-cycle: the inner door has been told to shut and the outer one has not been told
        // anything at all.
        Assert.Contains("Inner Door:close", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);

        var program = ProgramWriter.Save(a.Program, a.World.Values);
        var state = a.Save();

        Assert.Equal("", a.Reload(program, state));
        Assert.Equal("", a.Trouble);
        a.Log.Clear();


        a.Tick(300);

        Assert.Contains("Outer Door:open", a.Log);
        Assert.False(a.State.IsRunning);
    }

    /// <summary>A run started by hand carries on after a reload, and is still known to have been started by hand.</summary>
    [Fact]
    public void ARunStartedByHandCarriesOnAfterAReload()
    {
        var a = Build();
        a.Tick(3);
        a.Runner.Call(a.Program.Sequences[0], a.Program.Systems[0].Uses[0], true);
        a.Tick(20);

        Assert.True(a.State.IsRunning);
        Assert.True(a.State.ByHand);

        var state = a.Save();
        Assert.Contains(a.Program.Memory.Runs, kept => kept.Running && kept.ByHand);
        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Log.Clear();

        a.Tick(300);

        Assert.Contains("Outer Door:open", a.Log);
    }

    /// <summary>A run waiting for a sequence it started waits for it again after a reload, and goes on when it finishes.</summary>
    [Fact]
    public void ARunWaitingOnAnotherSequenceCarriesOnAfterAReload()
    {
        var a = Build();
        var cycle = a.Program.Sequences[0];
        var system = cycle.System;

        // The cycle hands the emptying to a sequence of its own and waits for it before opening the outer door.
        var empty = a.Program.AddSequence(system);
        empty.Name = "Empty";
        Then(empty, "depressurize", a.Name(a.Vent));
        cycle.Steps.RemoveAt(3);
        cycle.Steps.RemoveAt(2);
        cycle.Steps.Insert(2, new Run(empty.Number));
        cycle.Steps.Insert(3, new Await());
        a.Program.Edited();

        a.Tick(3);
        a.Press();
        a.Tick(20);

        Assert.Contains("Airlock Vent:depressurize", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);

        var state = a.Save();
        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Log.Clear();
        a.Tick(600);

        Assert.Contains("Outer Door:open", a.Log);
    }

    /// <summary>A door that opens while the world is being written still sets off its <c>when</c> once the world loads.</summary>
    /// <remarks>Without what the trigger read before the save, the first look after loading would read the door as open all along.</remarks>
    [Fact]
    public void AWhenRemembersWhatItReadAcrossAReload()
    {
        var a = Build();
        var opened = SignalTrigger.Written("sprocket", "finishes_opening");
        opened.Sources.Add(a.Name(a.Outer));
        var close = new Act("close");
        close.Targets.Add(a.Name(a.Inner));
        a.Program.AddSequence(a.Program.Systems[0], opened).Steps.Add(close);
        a.Program.Edited();
        a.Tick(30);

        var state = a.Save();
        Assert.NotEmpty(a.Program.Memory.Watches);

        a.Outer.ForceOpen();
        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Log.Clear();
        a.Tick(30);

        Assert.Contains("Inner Door:close", a.Log);
    }

    /// <summary>What a <c>when any</c> read of each block is written as the block's name and on or off, and read back.</summary>
    [Fact]
    public void WhatAWhenReadOfEachBlockIsReadBack()
    {
        var a = Build();
        var opened = SignalTrigger.Written("sprocket", "finishes_opening");
        opened.Sources.Add(a.Name(a.Outer));
        opened.Sources.Add(a.Name(a.Inner));
        var close = new Act("close");
        close.Targets.Add(a.Name(a.Inner));
        a.Program.AddSequence(a.Program.Systems[0], opened).Steps.Add(close);
        a.Program.Edited();
        a.Outer.ForceOpen();
        a.Tick(30);

        var state = a.Save();
        Assert.Contains(a.Program.Memory.Watches, kept => kept.Readings.Count == 2);
        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
    }

    /// <summary>An <c>every</c> keeps its beat across a reload rather than waiting a whole interval again.</summary>
    [Fact]
    public void AnEveryKeepsItsBeatAcrossAReload()
    {
        var a = Build();
        var every = new EveryTrigger(600);
        var close = new Act("close");
        close.Targets.Add(a.Name(a.Inner));
        a.Program.AddSequence(a.Program.Systems[0], every).Steps.Add(close);
        a.Program.Edited();
        a.Tick(400);

        var state = a.Save();
        Assert.Contains(a.Program.Memory.Watches, kept => kept.Due >= 0);

        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Log.Clear();
        a.Tick(300);

        Assert.Contains("Inner Door:close", a.Log);
    }

    /// <summary>
    /// A wait picks up where it left off rather than starting again.
    /// </summary>
    /// <remarks>
    /// Kept as what is left rather than as when it started: a tick count means nothing across a
    /// reload, because the world's own clock begins again.
    /// </remarks>
    [Fact]
    public void AWaitKeepsWhatIsLeftOfIt()
    {
        var a = Build();
        a.Program.Sequences[0].Steps.Insert(0, new Wait(600));
        a.Tick(3);
        a.Press();
        a.Tick(100);

        Assert.True(a.State.IsWaiting);
        var left = a.State.WaitTicks;
        Assert.InRange(left, 450, 599);

        var state = a.Save();
        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Tick();

        Assert.Equal(left, a.State.WaitTicks + 1);
    }

    /// <summary>
    /// A run of a step that no longer exists is dropped, not resumed.
    /// </summary>
    /// <remarks>
    /// Somebody editing a program between saving and loading is the ordinary way this happens, and a
    /// run resumed into a sequence that has changed underneath it is a machine doing something
    /// nobody asked for.
    /// </remarks>
    [Fact]
    public void ARunIntoAStepThatIsGoneIsDropped()
    {
        var a = Build();
        a.Tick(3);
        a.Press();
        a.Tick(20);

        var state = a.Save();

        // The same program with its steps taken out, which is what a save from before an edit meets.
        var trimmed = a.Program.Sequences[0];
        trimmed.Steps.Clear();

        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Tick(20);

        Assert.False(a.State.IsRunning);
    }
}
