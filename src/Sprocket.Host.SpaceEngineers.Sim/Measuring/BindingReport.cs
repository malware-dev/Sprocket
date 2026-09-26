using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Ship = Sprocket.Sim.Machine.Simulation;

namespace Sprocket.Sim.Measuring;

/// <summary>
/// What it costs to work out what every declaration points at, and what of that cost is the kind check.
/// </summary>
/// <remarks>
/// A declaration says which sort of block it points at, and that sort is a string: the game's own
/// definition id. The game has a typed id of its own with cheaper equality, and reaching for it would
/// put a parser in the load path and a struct the harness cannot resolve outside the game. So the
/// question this answers is whether the string costs anything worth that.
/// <para>
/// <b>Two numbers, not one.</b> Rebinding is what the string sits inside, and rebinding happens when a
/// block is welded on or a declaration changes - never on an idle tick, which
/// <c>RefTests.TenSecondsOfIdleTicksMatchNothing</c> holds to. So the share matters as much as the
/// span: a comparison that is a rounding error inside an operation that is itself rare is not a
/// performance question at all.
/// </para>
/// <para>
/// <b>These numbers are from this machine, not from the game.</b> Run it in Release; a Debug figure
/// measures the JIT declining to optimise.
/// </para>
/// </remarks>
public static class BindingReport
{
    const int MinimumMilliseconds = 60;

    const int Windows = 7;

    public static string Render()
    {
        var report = new StringBuilder();
        report.AppendLine("Binding timing, Release only, on the framework the game runs.");
        report.AppendLine();
        report.AppendLine("construct      blocks  refs   rebind ms   per ref us"
            + "   kind compares   compare ms   share");
        report.AppendLine(new string('-', 100));

        Measure(report, "his world", 40, 12);
        Measure(report, "plausible", 200, 40);
        Measure(report, "large", 800, 200);
        Measure(report, "absurd", 3000, 800);

        report.AppendLine();
        report.AppendLine("A rebind happens when the world or the program changes, and nothing else.");
        report.AppendLine("Idle ticks cost two integer comparisons and do none of the above.");
        return report.ToString();
    }

    static void Measure(StringBuilder report, string name, int blocks, int refs)
    {
        var rig = new Rig();
        for (var i = 0; i < blocks; i++)
            rig.Door(100 + i, "Door " + i);

        Ship sim = Ship.On(rig);
        sim.Tick();

        GridWorld world = sim.World;
        Automation program = sim.Editor.Program;

        // Every nth block, so the declarations are spread through the world rather than clustered at
        // the front of it: matching walks the blocks in order and a run of early hits would flatter it.
        var step = blocks / refs;
        for (var i = 0; i < refs; i++)
        {
            GridEntity entity = world.FindEntity(100 + (i * step));
            if (entity != null)
                program.Declare(world, entity);
        }

        var bindings = new Bindings();
        var rebind = Time(() => bindings.Resolve(world, program.Refs));

        // The same comparisons the rebind makes, on the same strings, with nothing else in the loop.
        // Held in arrays rather than read back off the model so the loop measures the comparison and
        // not the walk that gathers them.
        var mine = new List<string>();
        var theirs = new List<string>();
        for (var i = 0; i < program.Refs.Count; i++)
        {
            GridEntity found = bindings.Entity(program.Refs[i].Name);
            if (found == null)
                continue;

            mine.Add(program.Refs[i].KindId);
            theirs.Add(found.Kind.Id);
        }

        var compares = mine.Count;
        var compare = Time(() => Same(mine, theirs));

        report.AppendLine(Pad(name, 15) + Pad(Whole(blocks), 8) + Pad(Whole(refs), 6)
            + Pad(Span(rebind), 12) + Pad(Span(rebind * 1000 / Math.Max(1, refs)), 13)
            + Pad(Whole(compares), 16) + Pad(Span(compare), 13)
            + (rebind <= 0 ? "-" : (compare / rebind).ToString("0.00%", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// The kind check, in a loop, kept honest.
    /// </summary>
    /// <remarks>
    /// The result is added up and handed back so the comparison cannot be optimised away, and the
    /// strings are equal in every pair, which is the expensive case: an ordinal compare that differs in
    /// the first character never reads the second.
    /// </remarks>
    static int Same(List<string> mine, List<string> theirs)
    {
        var same = 0;
        for (var i = 0; i < mine.Count; i++)
        {
            if (mine[i].Length > 0 && theirs[i] != mine[i])
                same++;
        }

        return same;
    }

    static double Time(Action work)
    {
        for (var i = 0; i < 3; i++)
            work();

        var windows = new double[Windows];
        for (var w = 0; w < Windows; w++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var clock = Stopwatch.StartNew();
            var runs = 0;
            while (clock.ElapsedMilliseconds < MinimumMilliseconds)
            {
                work();
                runs++;
            }

            windows[w] = clock.Elapsed.TotalMilliseconds / runs;
        }

        Array.Sort(windows);
        return windows[Windows / 2];
    }

    static double Time(Func<int> work)
    {
        var sink = 0;
        return Time(delegate { sink += work(); }) + (sink == int.MinValue ? 1 : 0);
    }

    static string Span(double milliseconds)
    {
        return milliseconds.ToString(milliseconds < 10 ? "0.0000" : "0.0",
            CultureInfo.InvariantCulture);
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
