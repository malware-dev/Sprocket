using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
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
/// So what is running is written beside what is switched off, in the same document, and the runner
/// is asked to take it back up. What is restored is the step and the clock; whether the doors have
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
        /// A fresh program and a fresh runner read from the two documents, which is exactly what a
        /// reload does - the rig stands in for the grid, which the game brings back itself.
        /// </remarks>
        public string Reload(string program, string state)
        {
            Program = new Automation();
            Trouble = ProgramReader.Load(program, Program, World.Values);
            Runner = new AutomationRunner(Program, World);
            return StateText.Load(state, Program, Runner);
        }
    }

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

        var written = StateText.Save(a.Program, a.Runner);
        Assert.Contains("running", written);
        Assert.Contains("step", written);
    }

    [Fact]
    public void NothingRunningWritesNoRuns()
    {
        var a = Build();
        a.Tick(3);

        Assert.DoesNotContain("running", StateText.Save(a.Program, a.Runner));
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
        var state = StateText.Save(a.Program, a.Runner);

        Assert.Equal("", a.Reload(program, state));
        Assert.Equal("", a.Trouble);
        a.Log.Clear();


        a.Tick(300);

        Assert.Contains("Outer Door:open", a.Log);
        Assert.False(a.State.IsRunning);
    }

    /// <summary>A run started by hand on a system that is switched off carries on after a reload, as it was let run before.</summary>
    [Fact]
    public void ARunStartedByHandCarriesOnAfterAReload()
    {
        var a = Build();
        a.Program.Systems[0].Enabled = false;
        a.Tick(3);
        a.Runner.Call(a.Program.Sequences[0], a.Program.Systems[0].Uses[0], true);
        a.Tick(20);

        Assert.True(a.State.IsRunning);
        Assert.True(a.State.ByHand);

        var state = StateText.Save(a.Program, a.Runner);
        Assert.Contains("\"by hand\"", state);
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

        var state = StateText.Save(a.Program, a.Runner);
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
        var opened = SignalTrigger.Written("ext", "finishes-opening");
        opened.Sources.Add(a.Name(a.Outer));
        var close = new Act("close");
        close.Targets.Add(a.Name(a.Inner));
        opened.Does = close;
        a.Program.Systems[0].AddTrigger(opened);
        a.Program.Edited();
        a.Tick(30);

        var state = StateText.Save(a.Program, a.Runner);
        Assert.Contains("when", state);

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
        var opened = SignalTrigger.Written("ext", "finishes-opening");
        opened.Sources.Add(a.Name(a.Outer));
        opened.Sources.Add(a.Name(a.Inner));
        var close = new Act("close");
        close.Targets.Add(a.Name(a.Inner));
        opened.Does = close;
        a.Program.Systems[0].AddTrigger(opened);
        a.Program.Edited();
        a.Outer.ForceOpen();
        a.Tick(30);

        var state = StateText.Save(a.Program, a.Runner);
        Assert.Contains("'Outer Door' on 'Inner Door' off", state);
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
        every.Does = close;
        a.Program.Systems[0].AddTrigger(every);
        a.Program.Edited();
        a.Tick(400);

        var state = StateText.Save(a.Program, a.Runner);
        Assert.Contains("every", state);

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

        var state = StateText.Save(a.Program, a.Runner);
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

        var state = StateText.Save(a.Program, a.Runner);

        // The same program with its steps taken out, which is what a save from before an edit meets.
        var trimmed = a.Program.Sequences[0];
        trimmed.Steps.Clear();

        Assert.Equal("", a.Reload(ProgramWriter.Save(a.Program, a.World.Values), state));
        a.Tick(20);

        Assert.False(a.State.IsRunning);
    }
}
