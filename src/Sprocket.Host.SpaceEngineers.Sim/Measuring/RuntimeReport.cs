using Door = Sprocket.Sim.Machine.Door;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using Ship = Sprocket.Sim.Machine.Simulation;

namespace Sprocket.Sim.Measuring;

/// <summary>
/// What the runtime costs a world per tick: idle, waiting, running and changing, and how each grows with the number of
/// constructs and of airlocks on each.
/// </summary>
/// <remarks>
/// Every construct carries one airlock logic system with an installation per airlock: a button, a door finishing closing,
/// the room's oxygen polled every ten ticks, and a cycle through both doors. Only the session's tick is timed; the rig
/// moving its doors is not.
/// <para>
/// <b>The game's calls are counted, not timed.</b> Every call into the game here goes through a stand-in that costs far
/// more than the game's own, so the time and the bytes a tick costs carry the stand-ins' share. The calibration line
/// gives what one stand-in call costs, and the own columns take that share out. What the game's calls cost in the game
/// is measured in the game.
/// </para>
/// <para>
/// <b>These numbers are from this machine, not from the game.</b> Run it in Release; a Debug figure measures the JIT
/// declining to optimise.
/// </para>
/// </remarks>
public static class RuntimeReport
{
    const int Settle = 3 * Rota.Hundred;

    const int Window = 10 * Rota.Hundred;

    const int Windows = 5;

    const int Cycle = 6 * Rota.Ten * 10;

    const string Guid = "6b0f3d2e-41a7-4c9b-8e5d-1f2a3b4c5d6e";

    /// <summary>The player every construct belongs to.</summary>
    const long Player = 144115188075855873;

    /// <summary>The report, as a table per state of the world.</summary>
    public static string Render()
    {
        AppDomain.MonitoringIsEnabled = true;
        var report = new StringBuilder();
        report.AppendLine("Runtime cost per tick, Release only, on the framework the game runs.");
        report.AppendLine();

        var call = Calibrate();
        report.AppendLine("one stand-in call: " + Span(call.Micros * 1000) + " ns, " + Whole((long)call.Bytes) + " bytes");
        report.AppendLine();

        report.AppendLine("state       ships  airlocks  extra   us/tick   own us   calls   bytes   own bytes   shifts");
        report.AppendLine(new string('-', 96));

        foreach (var state in new[] { State.Idle, State.Waiting, State.Running })
        {
            Measure(report, call, state, 1, 1, 0);
            Measure(report, call, state, 1, 20, 0);
            Measure(report, call, state, 10, 5, 0);
            Measure(report, call, state, 50, 5, 0);
        }

        Measure(report, call, State.Showing, 1, 20, 0);
        Measure(report, call, State.Clocks, 50, 5, 0);
        Measure(report, call, State.Idle, 1, 20, 2000);
        report.AppendLine();

        report.AppendLine("The tick after a block is welded on, and after the grid says a block changed hands: one construct of 20 airlocks");
        report.AppendLine("and as many other blocks as said.");
        report.AppendLine();
        report.AppendLine("extra   us/weld   calls   bytes       us/hands   calls    bytes");
        report.AppendLine(new string('-', 70));
        Weld(report, 0);
        Weld(report, 500);
        Weld(report, 2000);
        report.AppendLine();

        report.AppendLine("Once, not per tick: one construct of as many airlocks as said.");
        report.AppendLine();
        report.AppendLine("airlocks   rebuilt us   checked us   saved us   rebuilt bytes   checked bytes   saved bytes");
        report.AppendLine(new string('-', 92));
        Once(report, 1);
        Once(report, 5);
        Once(report, 20);

        return report.ToString();
    }

    /// <summary>
    /// What a construct costs where it is built again from its program as a client's edit arrives, checked after its wiring
    /// changes, and written into its block as the world saves.
    /// </summary>
    static void Once(StringBuilder report, int airlocks)
    {
        var rig = new Rig();
        Build(rig, 1, airlocks, 0, Airlock);
        var sim = Ship.On(rig);
        var frame = Quiet(sim, false);
        for (var i = 0; i < Settle; i++)
            Step(sim, rig, ref frame, false, false);

        const int times = 20;
        var rebuilt = new Stopwatch();
        var checkedClock = new Stopwatch();
        var saved = new Stopwatch();
        long rebuiltBytes = 0, checkedBytes = 0, savedBytes = 0;
        for (var i = 0; i < times; i++)
        {
            var unit = Unit(sim);
            string program;
            string state;
            unit.Written(out program, out state);

            // The server builds the construct's Sprocket again for a client's edit, and the next tick checks it.
            var allocated = Allocated();
            rebuilt.Start();
            sim.Roster.Replace(unit, program, state);
            sim.Roster.Tick(++frame);
            rebuilt.Stop();
            rebuiltBytes += Allocated() - allocated;

            for (var t = 0; t < Rota.Ten; t++)
                Step(sim, rig, ref frame, false, false);

            Unit(sim).Program.Rewire();
            allocated = Allocated();
            checkedClock.Start();
            sim.Roster.Tick(++frame);
            checkedClock.Stop();
            checkedBytes += Allocated() - allocated;

            allocated = Allocated();
            saved.Start();
            sim.Roster.Flush();
            saved.Stop();
            savedBytes += Allocated() - allocated;

            for (var t = 0; t < Rota.Ten; t++)
                Step(sim, rig, ref frame, false, false);
        }

        report.AppendLine(Pad(Whole(airlocks), 11) + Pad(Span(Elapsed(rebuilt) / times), 13) + Pad(Span(Elapsed(checkedClock) / times), 13)
            + Pad(Span(Elapsed(saved) / times), 11) + Pad(Whole(rebuiltBytes / times), 16) + Pad(Whole(checkedBytes / times), 16)
            + Whole(savedBytes / times));
    }

    /// <summary>The construct with airlocks of the harness's world: every one but the empty first one.</summary>
    static SprocketUnit Unit(Ship sim)
    {
        foreach (var unit in sim.Roster.All)
        {
            if (unit.Program.Systems.Count > 0)
                return unit;
        }

        throw new InvalidOperationException("no construct has a logic system");
    }

    static double Elapsed(Stopwatch clock)
    {
        return clock.Elapsed.TotalMilliseconds * 1000;
    }

    /// <summary>What the world is doing while it is measured.</summary>
    enum State
    {
        /// <summary>Nothing running: only the watches look.</summary>
        Idle,

        /// <summary>Every cycle started and held at its first await, its door never moving.</summary>
        Waiting,

        /// <summary>Every cycle going round, its doors moving, started again as it finishes.</summary>
        Running,

        /// <summary>Idle, with an editor open on the first construct.</summary>
        Showing,

        /// <summary>Idle, every installation watching only a clock that does not come round while it is measured, so nothing asks the game anything.</summary>
        Clocks
    }

    /// <summary>What one call through a stand-in costs here, in microseconds and bytes.</summary>
    struct Cost
    {
        public double Micros;
        public double Bytes;
    }

    /// <summary>Times a read of a block's working flag through its stand-in, the commonest call a look makes.</summary>
    static Cost Calibrate()
    {
        var rig = new Rig();
        var door = rig.Door(1, "Door");
        var block = door.Block;
        const int reads = 200000;
        var micros = Time(() =>
        {
            var working = 0;
            for (var i = 0; i < reads; i++)
                working += block.IsWorking ? 1 : 0;

            GC.KeepAlive(working);
        });

        var sink = 0;
        var bytes = Allocated();
        for (var i = 0; i < reads; i++)
            sink += block.IsWorking ? 1 : 0;

        bytes = Allocated() - bytes;
        GC.KeepAlive(sink);
        return new Cost { Micros = micros / reads, Bytes = (double)bytes / reads };
    }

    static void Measure(StringBuilder report, Cost call, State state, int ships, int airlocks, int extra)
    {
        var rig = new Rig();
        var doors = Build(rig, ships, airlocks, extra, state == State.Clocks ? Clocks : Airlock);
        var sim = Ship.On(rig);
        var frame = Quiet(sim, state == State.Showing);
        var running = state == State.Running;
        if (state == State.Waiting || running)
            Start(sim);

        for (var i = 0; i < Settle; i++)
            Step(sim, rig, ref frame, running, state != State.Idle && state != State.Showing);

        // A cycle started where it is waited on: its first await holds on a door the rig never moves.
        var rota = sim.Unit.Runner.Rota;
        var ticks = 0L;
        var calls = 0L;
        var shifts = 0L;
        var bytes = 0L;
        var times = new double[Windows];
        for (var w = 0; w < Windows; w++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var asked = Pretend.Asked;
            var done = rota.Done;
            var allocated = Allocated();
            var clock = new Stopwatch();
            for (var i = 0; i < Window; i++)
            {
                if (running)
                {
                    rig.Tick();
                    if (frame % Cycle == 0)
                        Start(sim);
                }

                frame++;
                clock.Start();
                sim.Roster.Tick(frame);
                clock.Stop();
            }

            allocated = Allocated() - allocated;
            times[w] = clock.Elapsed.TotalMilliseconds * 1000 / Window;
            ticks += Window;
            calls += Pretend.Asked - asked;
            shifts += rota.Done - done;
            bytes += allocated;
        }

        // The rig's own calls while it moves its doors are not the runtime's, and were not timed either.
        Array.Sort(times);
        var tick = times[Windows / 2];
        var perCall = (double)calls / ticks;
        var perBytes = (double)bytes / ticks;
        GC.KeepAlive(doors);

        report.AppendLine(Pad(state.ToString().ToLowerInvariant(), 12) + Pad(Whole(ships), 7) + Pad(Whole(airlocks), 10)
            + Pad(Whole(extra), 8) + Pad(Span(tick), 10) + Pad(Span(Math.Max(0, tick - perCall * call.Micros)), 9)
            + Pad(Span(perCall), 8) + Pad(Span(perBytes), 8) + Pad(Span(Math.Max(0, perBytes - perCall * call.Bytes)), 12)
            + Span((double)shifts / ticks));
    }

    /// <summary>
    /// Welds a light onto the last construct, then times the tick that follows, many times over; and the same for the grid
    /// saying a block changed hands, as it does after any block's owner changes or a block comes to work or stops.
    /// </summary>
    static void Weld(StringBuilder report, int extra)
    {
        var rig = new Rig();
        Build(rig, 1, 20, extra, Airlock);
        var sim = Ship.On(rig);
        var frame = Quiet(sim, false);
        for (var i = 0; i < Settle; i++)
            Step(sim, rig, ref frame, false, false);

        var welded = 0;
        var weld = Changed(sim, rig, ref frame, () => rig.Light(900000 + welded++, "Welded " + welded));
        var handed = Changed(sim, rig, ref frame, () => Unit(sim).World.Reshare());

        report.AppendLine(Pad(Whole(extra), 8) + Pad(Span(weld.Micros), 10) + Pad(Span(weld.Calls), 8) + Pad(Span(weld.Bytes), 12)
            + Pad(Span(handed.Micros), 11) + Pad(Span(handed.Calls), 9) + Span(handed.Bytes));
    }

    /// <summary>What the tick after <paramref name="change" /> costs, on average over many, each with ticks between.</summary>
    static Spent Changed(Ship sim, Rig rig, ref int frame, Action change)
    {
        const int changes = 50;
        var clock = new Stopwatch();
        var calls = 0L;
        var bytes = 0L;
        for (var i = 0; i < changes + 5; i++)
        {
            change();

            // The first few are not counted, so what is timed is the code and not its first compiling.
            var counted = i >= 5;
            var asked = Pretend.Asked;
            var allocated = Allocated();
            frame++;
            if (counted)
                clock.Start();

            sim.Roster.Tick(frame);
            clock.Stop();
            if (counted)
            {
                bytes += Allocated() - allocated;
                calls += Pretend.Asked - asked;
            }

            // Ticks between, so a change is measured alone rather than on top of the last one's work.
            for (var t = 0; t < Rota.Ten; t++)
                Step(sim, rig, ref frame, false, false);
        }

        return new Spent { Micros = Elapsed(clock) / changes, Calls = (double)calls / changes, Bytes = (double)bytes / changes };
    }

    /// <summary>What something cost on average: time, calls into the game, and bytes.</summary>
    struct Spent
    {
        public double Micros;
        public double Calls;
        public double Bytes;
    }

    /// <summary>A tick of the world: the rig moves where the doors are to, then the session ticks.</summary>
    static void Step(Ship sim, Rig rig, ref int frame, bool running, bool started)
    {
        if (running)
            rig.Tick();

        frame++;
        if (started && frame % Cycle == 0)
            Start(sim);

        sim.Roster.Tick(frame);
    }

    /// <summary>Starts the cycle on every installation of every construct; one already going counts it as skipped.</summary>
    static void Start(Ship sim)
    {
        foreach (var unit in sim.Roster.All)
        {
            foreach (var system in unit.Program.Systems)
            {
                var cycle = system.FindSequence(1);
                if (cycle == null)
                    continue;

                foreach (var instance in system.Uses)
                    unit.Runner.Call(cycle, instance);
            }
        }
    }

    /// <summary>
    /// Wires every button as filling its slot does, switches every logic system and installation on, and closes every screen
    /// but the first construct's where one is wanted.
    /// </summary>
    /// <remarks>A program read from storage takes a button slot it finds empty as one cleared by hand, and never writes it.</remarks>
    static int Quiet(Ship sim, bool showing)
    {
        var frame = sim.Now;
        foreach (var unit in sim.Roster.All)
        {
            foreach (var entity in unit.World.Entities)
            {
                if (entity.Name.StartsWith("Button ", StringComparison.Ordinal))
                    unit.Wire(entity.Id);
            }

            unit.Revisit();
        }

        sim.Roster.Tick(++frame);
        foreach (var unit in sim.Roster.All)
        {
            unit.Showing = showing && unit.Block.EntityId == Hero(0);

            // The editor is made as a screen first asks for it, on the first construct's home page.
            if (unit.Showing)
                _ = unit.Editor;
            foreach (var system in unit.Program.Systems)
            {
                system.Enabled = true;
                foreach (var instance in system.Uses)
                    instance.Enabled = true;
            }
        }

        sim.Roster.Tick(++frame);
        foreach (var unit in sim.Roster.All)
        {
            if (unit.Program.Systems.Count == 0 && unit.Block.EntityId != sim.Unit.Block.EntityId)
                throw new InvalidOperationException("construct " + unit.World.Name + " has no logic system: " + string.Join(" / ", sim.Log));

            foreach (var system in unit.Program.Systems)
            {
                foreach (var instance in system.Uses)
                {
                    foreach (var sequence in system.Sequences)
                    {
                        var fault = unit.Runner.Get(sequence, instance)?.Fault ?? SequenceFault.None;
                        if (!instance.Enabled || fault.Exists)
                            throw new InvalidOperationException($"{unit.World.Name} {sequence.Name} on {instance.Name}: {fault.Problem} {fault.Which}");
                    }
                }
            }
        }

        return frame;
    }

    /// <summary>Welds <paramref name="ships" /> constructs, each apart from the rest with <paramref name="airlocks" /> airlocks, <paramref name="extra" /> other blocks and a programmable block holding their program.</summary>
    static List<Door> Build(Rig rig, int ships, int airlocks, int extra, string blueprint)
    {
        var doors = new List<Door>();
        for (var s = 0; s < ships; s++)
        {
            rig.Apart("Ship " + s);
            var hero = rig.Programmable(Hero(s), "Automation " + s);
            var names = new StringBuilder();
            var instances = new StringBuilder();
            for (var a = 0; a < airlocks; a++)
            {
                var at = Hero(s) + 10 * (a + 1);
                var button = rig.Panel(at + 1, $"Button {s}.{a}");
                var inner = rig.Door(at + 2, $"Inner {s}.{a}");
                var outer = rig.Door(at + 3, $"Outer {s}.{a}");
                var vent = rig.Vent(at + 4, $"Vent {s}.{a}");
                var lamp = rig.Light(at + 5, $"Lamp {s}.{a}");
                doors.Add(inner);
                doors.Add(outer);

                foreach (Fitting fitting in new Fitting[] { button, inner, outer, vent, lamp })
                    names.Append("let '").Append(fitting.Name).Append("' = block '").Append(Kind(fitting)).Append("' [ ")
                        .Append(fitting.Id.ToString(CultureInfo.InvariantCulture)).Append(", \"").Append(fitting.Name).Append("\" ]\n");

                instances.Append("instance ").Append(a + 1).Append(" of \"").Append(Guid).Append("\" as '").Append(a + 1).Append("' {\n")
                    .Append("    button = [ sprocket.toolbar(1, 1, 1) on [ '").Append(button.Name).Append("' ] ]\n")
                    .Append("    inner = [ '").Append(inner.Name).Append("' ]\n")
                    .Append("    outer = [ '").Append(outer.Name).Append("' ]\n")
                    .Append("    vent = [ '").Append(vent.Name).Append("' ]\n")
                    .Append("    lamp = [ '").Append(lamp.Name).Append("' ]\n")
                    .Append("}\n\n");
            }

            for (var e = 0; e < extra; e++)
                rig.Light(Hero(s) + 5000 + e, $"Light {s}.{e}");

            var store = Store.Nowhere();
            store.SetValue(BlockStorage.ProgramKey, "sprocket 1 alpha 17\n\n" + names + "\n" + blueprint + "\n" + instances);
            hero.Storage = store;
        }

        // One player's, as a ship is, so whether its owner may use each block is asked as it is in the game.
        foreach (var fitting in rig.InWorld)
            fitting.Owner = Player;

        // Whatever is welded next goes onto the last construct.
        return doors;
    }

    /// <summary>The airlock every construct runs, on slots each installation fills.</summary>
    const string Airlock = "system blueprint \"" + Guid + "\" as Bench.Airlock {\n"
        + "    required slot button expects signal\n"
        + "    required slot inner expects Door\n"
        + "    required slot outer expects Door\n"
        + "    required slot vent expects AirVent\n"
        + "    required slot lamp\n\n"
        + "    whenever button then run 1\n"
        + "    whenever sprocket.finishes_closing on [ inner ] then run 2\n"
        + "    when any sprocket.oxygen_level on [ vent ] < 0.5 then run 3\n\n"
        + "    sequence 1 as Cycle {\n"
        + "        call sprocket.close on [ inner ]\n"
        + "        await\n"
        + "        call sprocket.open on [ outer ]\n"
        + "        await\n"
        + "        wait 60t\n"
        + "        call sprocket.close on [ outer ]\n"
        + "        await\n"
        + "        call sprocket.open on [ inner ]\n"
        + "        await\n"
        + "    }\n\n"
        + "    sequence 2 as Shut {\n"
        + "        set sprocket.power on [ lamp ] to on\n"
        + "    }\n\n"
        + "    sequence 3 as Thin {\n"
        + "        set sprocket.power on [ lamp ] to off\n"
        + "    }\n"
        + "}\n";

    /// <summary>The same slots as <see cref="Airlock" />, watched by nothing but a clock of a minute and a half.</summary>
    const string Clocks = "system blueprint \"" + Guid + "\" as Bench.Clock {\n"
        + "    required slot button expects signal\n"
        + "    required slot inner expects Door\n"
        + "    required slot outer expects Door\n"
        + "    required slot vent expects AirVent\n"
        + "    required slot lamp\n\n"
        + "    every 6000t run 1\n\n"
        + "    sequence 1 as Light {\n"
        + "        set sprocket.power on [ lamp ] to on\n"
        + "    }\n"
        + "}\n";

    /// <summary>The id the programmable block of construct <paramref name="ship" /> has.</summary>
    static long Hero(int ship)
    {
        return 1000000L * (ship + 1);
    }

    /// <summary>How a program names the type of <paramref name="fitting" />.</summary>
    static string Kind(Fitting fitting)
    {
        var type = fitting.Definition.TypeIdString;
        const string prefix = "MyObjectBuilder_";
        if (type.StartsWith(prefix, StringComparison.Ordinal))
            type = type.Substring(prefix.Length);

        return type + "/" + fitting.Definition.SubtypeName;
    }

    static long Allocated()
    {
        return GC.GetAllocatedBytesForCurrentThread();
    }

    /// <summary>The median of seven windows of <paramref name="work" />, in microseconds a run.</summary>
    static double Time(Action work)
    {
        work();
        var windows = new double[7];
        for (var w = 0; w < windows.Length; w++)
        {
            var clock = Stopwatch.StartNew();
            work();
            windows[w] = clock.Elapsed.TotalMilliseconds * 1000;
        }

        Array.Sort(windows);
        return windows[windows.Length / 2];
    }

    static string Span(double value)
    {
        return value.ToString(value < 10 ? "0.000" : "0.0", CultureInfo.InvariantCulture);
    }

    static string Whole(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static string Pad(string value, int width)
    {
        return value.Length >= width ? value + " " : value + new string(' ', width - value.Length);
    }
}
