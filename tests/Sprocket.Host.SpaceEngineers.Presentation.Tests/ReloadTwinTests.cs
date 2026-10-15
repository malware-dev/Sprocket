using System.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A save and load cannot be noticed from inside the world.
/// </summary>
/// <remarks>
/// Each scenario runs twice, the same way, side by side. One of the two is saved and loaded again part way, at many
/// points; both carry on, and every block action, every run's state and every line on the home screen must match, tick
/// for tick. Whatever a reload forgets makes the twins drift, and the test names the tick and the difference.
/// </remarks>
public class ReloadTwinTests
{
    /// <summary>How long the twins carry on after the reload.</summary>
    const int After = 400;

    /// <summary>An airlock cycle: the inner door closes, the room waits, the outer door opens, waits again and closes.</summary>
    [Theory]
    [MemberData(nameof(Every), 5, 300, 7)]
    public void AnAirlockCycleCarriesOn(int reloadAt)
    {
        Twins(Cycle, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
        });
    }

    /// <summary>A timer flashing a light: on every two seconds, off half a second later.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 360, 9)]
    public void ATimerKeepsItsBeat(int reloadAt)
    {
        Twins(Flasher, reloadAt, delegate { });
    }

    /// <summary>A watcher on the inner door: when it finishes closing, the light goes off; the cycle closes it.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 120, 5)]
    public void AWatcherRemembersWhatItSaw(int reloadAt)
    {
        Twins(Watched, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
        });
    }

    /// <summary>A cycle stopped from outside by a second button says so, and stays stopped.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 250, 9)]
    public void AStopFromOutsideIsRemembered(int reloadAt)
    {
        Twins(Stoppable, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 25)
                screen.Scene.Ship.Press("Stop Button");
        });
    }

    /// <summary>A cycle cut short by switching its logic system off says where it stopped, and keeps saying it once switched on.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 250, 9)]
    public void ASwitchOffPartWayIsRemembered(int reloadAt)
    {
        Twins(Cycle, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 25)
                screen.Editor.Program.Systems[0].Enabled = false;
            if (tick == 140)
                screen.Editor.Program.Systems[0].Enabled = true;
        });
    }

    /// <summary>A vent sent the other way while its step waits ends the run, and what it said is kept, at its step and naming it.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 250, 9)]
    public void AVentSentTheOtherWayIsRemembered(int reloadAt)
    {
        Twins(Vented, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 30)
                Overruled(screen);
        });
    }

    /// <summary>The scenes the twins compare do what the twins are about: the switch is said and the vent's overruling is kept.</summary>
    [Fact]
    public void TheScenesMeetWhatTheyAreAbout()
    {
        var cut = Screen.Fresh();
        Cycle(cut);
        cut.Scene.Ship.Press("Airlock Button");
        cut.Run(13);
        cut.Editor.Program.Systems[0].Enabled = false;
        cut.Run(110);
        var cycle = cut.Editor.Program.Sequences[0];
        Assert.Equal(Stopper.Switch, cut.Scene.Ship.Runner.Get(cycle, cycle.System.First)!.StoppedBy);

        var vented = Screen.Fresh();
        Vented(vented);
        vented.Scene.Ship.Press("Airlock Button");
        vented.Run(18);
        Overruled(vented);
        vented.Run(100);
        var cycled = vented.Editor.Program.Sequences[0];
        var fault = SequenceSummary.Fault(vented.Scene.Ship.Runner.Get(cycled, cycled.System.First));
        Assert.Equal(SequenceProblem.Conflicted, fault.Problem);
        Assert.Equal("Airlock Vent", fault.Which);
    }

    /// <summary>Something other than the cycle tells the airlock's vent to fill the room.</summary>
    static void Overruled(Screen screen)
    {
        var vent = screen.Scene.Ship.Machine.Fitted.First(fitting => fitting.Name == "Airlock Vent");
        vent.Perform("Depressurize_Off", new List<Literal>());
    }

    /// <summary>A construct taken outside its sharing pauses, and stays paused, its cycle held where it was.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 120, 7)]
    public void APauseIsRemembered(int reloadAt)
    {
        Twins(Owned, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 20)
                screen.Scene.Ship.Machine.Own(Bert);
        });
    }

    /// <summary>A vent emptying the room and filling it again, waited on each time, with a pause between.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 400, 11)]
    public void AVentCycleCarriesOn(int reloadAt)
    {
        Twins(Vented, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
        });
    }

    /// <summary>A cycle that meets a door ground off before its step comes: what it met is said, and stays said.</summary>
    [Theory]
    [MemberData(nameof(Every), 3, 300, 9)]
    public void WhatARunMetIsRemembered(int reloadAt)
    {
        Twins(Cycle, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 30)
                screen.Scene.Ship.Machine.Grind(screen.Scene.Ship.Machine.Fitted.First(fitting => fitting.Name == "Airlock Outer Door"));
        });
    }

    /// <summary>
    /// The same, in a world with settings of its own, which the editor of a world opened again is given after it is made: what a
    /// run met is said as soon either way.
    /// </summary>
    [Theory]
    [MemberData(nameof(Every), 3, 300, 9)]
    public void WhatARunMetIsRememberedInAWorldWithItsOwnSettings(int reloadAt)
    {
        Twins(delegate(Screen screen)
        {
            screen.Scene.Ship.Settle(new Settings(Layer.Empty, new Layer(true, null, Waiting.Step, false)));
            Cycle(screen);
        }, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12)
                screen.Scene.Ship.Press("Airlock Button");
            if (tick == 30)
                screen.Scene.Ship.Machine.Grind(screen.Scene.Ship.Machine.Fitted.First(fitting => fitting.Name == "Airlock Outer Door"));
        });
    }

    /// <summary>A button pressed again while its cycle runs is skipped and counted, and the count stays.</summary>
    [Theory]
    [MemberData(nameof(Every), 15, 120, 6)]
    public void ASkippedPressIsCounted(int reloadAt)
    {
        Twins(Cycle, reloadAt, delegate(Screen screen, int tick)
        {
            if (tick == 12 || tick == 20 || tick == 45)
                screen.Scene.Ship.Press("Airlock Button");
        });
    }

    /// <summary>Reload points from <paramref name="from" /> to <paramref name="to" />, every <paramref name="step" /> ticks.</summary>
    public static IEnumerable<object[]> Every(int from, int to, int step)
    {
        for (var tick = from; tick <= to; tick += step)
            yield return new object[] { tick };
    }

    static void Vented(Screen screen)
    {
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        Task(editor, "Airlock Vent", "depressurize");
        editor.PressAdd("delay");
        editor.GiveLength("1");
        Task(editor, "Airlock Vent", "pressurize");
        editor.SwitchOn();
        editor.GoToRoot();
        screen.Run(2);
    }

    const long Anna = 144115188075855873;
    const long Bert = 144115188075855874;

    static void Flasher(Screen screen)
    {
        var editor = screen.Editor;
        editor.NewSystem("Lights");
        editor.PressStart("every");
        screen.Run(1);
        editor.TimerRate().Give("2", false);
        editor.PressPrimary();
        Light(editor, "on");
        editor.PressAdd("delay");
        editor.GiveLength("0.5");
        Light(editor, "off");
        editor.SwitchOn();
        editor.GoToRoot();
        screen.Run(2);
    }

    static void Watched(Screen screen)
    {
        Cycle(screen);
        var editor = screen.Editor;
        editor.NewSystem("Watch");
        editor.PressStart("when");
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("finishes closing");
        editor.PressPrimary();
        Light(editor, "off");
        editor.SwitchOn();
        editor.GoToRoot();
        screen.Run(2);
    }

    static void Stoppable(Screen screen)
    {
        screen.Scene.Ship.Machine.Panel(31, "Stop Button");
        screen.Run(1);
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        Task(editor, "Airlock Inner Door", "close");
        editor.PressAdd("delay");
        editor.GiveLength("2");
        Task(editor, "Airlock Outer Door", "open");
        editor.Back();
        editor.PressStart(SentenceText.WhenWord);
        editor.PressBlock("Stop Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressChoice("stop");
        editor.PressChoice("Sequence 1");
        editor.PressPrimary();
        editor.SwitchOn();
        editor.GoToRoot();
        screen.Run(2);
    }

    static void Owned(Screen screen)
    {
        Cycle(screen);
        screen.Scene.Ship.Machine.Own(Anna);
        screen.Run(2);
    }

    static void Light(SprocketEditor editor, string power)
    {
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = power;
        editor.PressPrimary();
    }

    /// <summary>The airlock's button system with its cycle, switched on, back on the home screen.</summary>
    static void Cycle(Screen screen)
    {
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        Task(editor, "Airlock Inner Door", "close");
        editor.PressAdd("delay");
        editor.GiveLength("2");
        Task(editor, "Airlock Outer Door", "open");
        editor.PressAdd("delay");
        editor.GiveLength("1.5");
        Task(editor, "Airlock Outer Door", "close");
        editor.SwitchOn();
        editor.GoToRoot();
        screen.Run(2);
    }

    static void Task(SprocketEditor editor, string block, string action)
    {
        editor.PressAdd("task");
        editor.PressBlock(block);
        editor.PressChoice(action);
        editor.PressPrimary();
    }

    /// <summary>
    /// Runs the scenario twice, doing <paramref name="act" /> at each tick, the second time saved and loaded again before
    /// tick <paramref name="reloadAt" />; fails at the first tick after it where the two runs differ.
    /// </summary>
    /// <remarks>
    /// One after the other rather than side by side: the harness finds a block by its id, and two of the same rig would
    /// answer for each other's blocks.
    /// </remarks>
    static void Twins(Action<Screen> build, int reloadAt, Action<Screen, int> act)
    {
        var kept = Record(build, reloadAt, false, reloadAt + After, act);
        var reloaded = Record(build, reloadAt, true, reloadAt + After, act);

        for (var tick = reloadAt; tick <= reloadAt + After; tick++)
        {
            var was = kept[tick];
            var now = reloaded[tick];
            Assert.True(was == now, "reloaded before tick " + reloadAt + ", the twins differ at tick " + tick + ":\n" + Difference(was, now));
        }
    }

    /// <summary>
    /// What a person could notice at each tick of the scenario, saved and loaded again before tick <paramref name="at" /> where
    /// <paramref name="reloads" /> says, and only looked at again there otherwise.
    /// </summary>
    /// <remarks>A load closes the screen, so both twins are looked at on a screen opened afresh from the tick it happens.</remarks>
    static string[] Record(Action<Screen> build, int at, bool reloads, int ticks, Action<Screen, int> act)
    {
        var screen = Screen.Fresh();
        build(screen);

        var seen = new string[ticks + 1];
        for (var tick = 1; tick <= ticks; tick++)
        {
            if (tick == at && reloads)
                screen.Scene.Reload();
            else if (tick == at)
                screen.Scene.Reopen();

            act(screen, tick);
            screen.Run(1);
            seen[tick] = Seen(screen);
        }

        return seen;
    }

    /// <summary>Everything a person could notice: what the blocks have been told, what each run is doing, and the home screen.</summary>
    static string Seen(Screen screen)
    {
        var seen = new StringBuilder();
        foreach (var line in screen.Scene.Ship.Machine.Log)
            seen.Append("block: ").AppendLine(line);

        var runner = screen.Scene.Ship.Runner;
        foreach (var sequence in screen.Editor.Program.Sequences)
        {
            foreach (var instance in sequence.System.Uses)
            {
                var run = runner.Get(sequence, instance);
                seen.Append("run ").Append(sequence.Number).Append('/').Append(instance.Number).Append(": ")
                    .AppendLine(run == null ? "none" : Run(run));
            }
        }

        var program = screen.Editor.Program;
        seen.Append("paused: ").Append(program.Paused).Append(" live: ").Append(runner.Live).AppendLine();

        foreach (var line in screen.Legible)
            seen.Append("screen: ").AppendLine(line);

        return seen.ToString();
    }

    static string Run(SequenceRunner run) =>
        (run.IsRunning ? "running" : "idle") + " step " + run.CurrentStep + " wait " + run.WaitTicks + (run.IsWaiting ? " waiting" : "")
        + " fired " + run.FiredCount + " skipped " + run.SkippedCount + " refused " + run.RefusedCount + " stopped " + run.StoppedBy
        + " at " + run.StoppedAt + " fault " + Fault(SequenceSummary.Fault(run));

    static string Fault(SequenceFault fault) =>
        fault.Problem + " step " + fault.Step + " block " + fault.Which + " because " + fault.Because.Cause + " " + fault.Because.About;

    /// <summary>The first line the two differ on, with each side's.</summary>
    static string Difference(string was, string now)
    {
        var a = was.Split('\n');
        var b = now.Split('\n');
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var left = i < a.Length ? a[i].TrimEnd('\r') : "(nothing)";
            var right = i < b.Length ? b[i].TrimEnd('\r') : "(nothing)";
            if (left != right)
                return "  kept:     " + left + "\n  reloaded: " + right;
        }

        return "  (the same, line by line)";
    }
}
