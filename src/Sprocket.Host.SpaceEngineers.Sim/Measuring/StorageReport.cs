using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Sim.Measuring;

/// <summary>
/// How long it takes to write a program out and read it back, at sizes nobody will ever build.
/// </summary>
/// <remarks>
/// Reading a program happens once per Sprocket block when a world loads, so it lands inside the time
/// somebody spends staring at a loading screen and blaming whichever mod they least understand. That is
/// the only reason this exists: the question is not whether the parser is elegant, it is whether fifty
/// Sprockets on a station cost anything a person could notice.
/// <para>
/// <b>These numbers are from this machine and this runtime, not from the game.</b> The mod is compiled
/// against an older framework and the game brings its own load, so treat the absolute figures as
/// indicative and the shape - whether cost grows with the program or with the square of it - as the
/// thing worth trusting. Run it in Release; a Debug figure measures the JIT declining to optimise.
/// </para>
/// </remarks>
public static class StorageReport
{
    /// <summary>How many Sprockets a busy station might carry, for the column that multiplies.</summary>
    const int Sprockets = 50;

    /// <summary>Long enough that the clock's own resolution stops mattering.</summary>
    const int MinimumMilliseconds = 60;

    /// <summary>How many separate windows a figure is the median of.</summary>
    const int Windows = 7;

    public static string Render()
    {
        var report = new StringBuilder();
        report.AppendLine("Storage timing, Release only, on the framework the game runs.");
        report.AppendLine();
        report.AppendLine("shape        bytes  stmts   save ms   load ms    lex ms  parse ms  model ms"
            + "   x50 ms   KB/load   collections");
        report.AppendLine(new string('-', 114));

        Measure(report, "his world", Shape.His());
        Measure(report, "plausible", new Shape(40, 8, 3, 6, 6, 2));
        Measure(report, "large", new Shape(200, 20, 5, 20, 10, 3));
        Measure(report, "huge", new Shape(1000, 50, 10, 40, 20, 4));
        Measure(report, "wide lists", Shape.Wide());
        Measure(report, "wordy", Shape.Wordy());
        Measure(report, "quote-heavy", Shape.Quoted());

        report.AppendLine();
        Headroom(report);
        report.AppendLine();
        Broken(report);
        return report.ToString();
    }

    static void Measure(StringBuilder report, string name, Shape shape)
    {
        var values = new Values();
        var program = shape.Build(values);
        var text = ProgramText.Save(program, values);

        var save = Time(() => ProgramText.Save(program, values));
        var load = Time(() => Read(text));
        var lex = Time(() => Tokens(text));
        var parse = Time(() => new Parser(text).Read());
        var bytes = Allocated(() => Read(text));
        var swept = Collections(() => Read(text));

        report.AppendLine(
            Pad(name, 12) +
            Pad(Whole(text.Length), 6) +
            Pad(Whole(Statements(text)), 7) +
            Pad(Span(save), 10) +
            Pad(Span(load), 10) +
            Pad(Span(lex), 10) +
            Pad(Span(parse), 10) +
            Pad(Span(load - parse), 10) +
            Pad(Span(load * Sprockets), 9) +
            Pad(Whole(bytes / 1024), 10) +
            swept);
    }

    /// <summary>
    /// The same shape at doubling sizes, so the curve can be read rather than argued about.
    /// </summary>
    /// <remarks>
    /// The column that matters is the cost per statement. While it holds steady the reader is linear and
    /// a bigger program only costs proportionally more; where it starts climbing, the collector has begun
    /// running inside a load, and everything past that point costs more than it looks like it should.
    /// <para>
    /// It is here because the language is going to grow. Expressions, branching and values on actions all
    /// add statements and add values to each one, so the size that is absurd today is the size that is
    /// ordinary later, and the point to know is where the floor gives way.
    /// </para>
    /// </remarks>
    static void Headroom(StringBuilder report)
    {
        report.AppendLine("Headroom, one shape at doubling sizes.");
        report.AppendLine();
        report.AppendLine("stmts    bytes     load ms   us/stmt   x50 ms    collections");
        report.AppendLine(new string('-', 66));

        for (var systems = 2; systems <= 64; systems *= 2)
        {
            var values = new Values();
            var shape = new Shape(systems * 8, systems, 3, 10, 8, 3);
            var text = ProgramText.Save(shape.Build(values), values);

            var load = Time(() => Read(text));
            var statements = Statements(text);

            report.AppendLine(
                Pad(Whole(statements), 9) +
                Pad(Whole(text.Length), 10) +
                Pad(Span(load), 10) +
                Pad(Span(load * 1000 / statements), 10) +
                Pad(Span(load * Sprockets), 10) +
                Collections(() => Read(text)));
        }
    }

    /// <summary>
    /// What a damaged save costs, which is the one path a person meets while something is wrong.
    /// </summary>
    /// <remarks>
    /// Recovery skips to the end of the line and carries on, so the interesting question is whether a
    /// file that is wrong at the top costs the same as one that is right. If it did not, a corrupt save
    /// would be a way to make a world load slowly.
    /// </remarks>
    static void Broken(StringBuilder report)
    {
        var values = new Values();
        var program = new Shape(200, 20, 5, 20, 10, 3).Build(values);
        var whole = ProgramText.Save(program, values);
        var hurt = whole.Replace("] wait", "] wait [").Replace("sequence 1 on", "sequence 1 on \"");

        report.AppendLine("A 200-declaration program with every step and sequence line broken: "
            + Span(Time(() => Read(hurt))) + " ms against " + Span(Time(() => Read(whole)))
            + " ms intact.");
    }

    static void Read(string text)
    {
        ProgramText.Load(text, new Automation(), new Values());
    }

    /// <summary>The lexer on its own, so the parser's share of a load can be told from the reader's.</summary>
    static void Tokens(string text)
    {
        var lexer = new Lexer(text);
        while (lexer.Next().Kind != TokenKind.End)
        {
        }
    }

    static int Statements(string text)
    {
        var lines = 0;
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '\n')
                lines++;

        return lines;
    }

    /// <summary>
    /// Milliseconds per call: the median of several windows, each started from a swept heap.
    /// </summary>
    /// <remarks>
    /// <b>A single mean was not good enough, and gave answers that could not be true</b> - a load reported
    /// as faster than the parse inside it, and a program twice the size reading faster than half of it. A
    /// collection costs milliseconds and lands in whichever window it lands in, so one unlucky window moved
    /// a figure by more than the change being measured. Sweeping first means a window pays only for the
    /// garbage its own calls made, and the median ignores the window that caught a collection anyway.
    /// <para>
    /// Warmed up first, because the first call through a method pays for the JIT compiling it, and that is
    /// paid once per session rather than once per Sprocket.
    /// </para>
    /// </remarks>
    static double Time(Action work)
    {
        for (var i = 0; i < 3; i++)
            work();

        var windows = new double[Windows];
        for (var w = 0; w < Windows; w++)
        {
            Sweep();

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

    static void Sweep()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    static long Allocated(Action work)
    {
        work();
        var before = GC.GetAllocatedBytesForCurrentThread();
        work();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>
    /// How many times the collector ran per load, by generation.
    /// </summary>
    /// <remarks>
    /// The column that says whether a slow load is slow arithmetic or slow allocation. A parser doing
    /// linear work in a quadratic-looking time is usually neither: it is the collector being handed more
    /// garbage than it can sweep between calls, and that is a different fix entirely.
    /// </remarks>
    static string Collections(Action work)
    {
        work();
        Sweep();

        var runs = 20;
        var zero = GC.CollectionCount(0);
        var one = GC.CollectionCount(1);
        var two = GC.CollectionCount(2);

        for (var i = 0; i < runs; i++)
            work();

        return Share(GC.CollectionCount(0) - zero, runs) + " / "
            + Share(GC.CollectionCount(1) - one, runs) + " / "
            + Share(GC.CollectionCount(2) - two, runs);
    }

    static string Share(int count, int runs)
    {
        return ((double)count / runs).ToString("0.0", CultureInfo.InvariantCulture);
    }

    static string Span(double milliseconds)
    {
        return milliseconds.ToString(milliseconds < 10 ? "0.000" : "0.0",
            CultureInfo.InvariantCulture);
    }

    static string Whole(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static string Pad(string value, int width)
    {
        return value.Length >= width - 1 ? value + " " : value.PadRight(width);
    }

    /// <summary>
    /// A program built to a size, so cost can be read against something that grows.
    /// </summary>
    /// <remarks>
    /// Deliberately beyond what anybody will build by hand. The editor makes one sequence at a time
    /// through a terminal, so the honest ceiling is small - but systems are importable and a station is
    /// shared between people, so the answer has to hold when somebody is unreasonable.
    /// </remarks>
    sealed class Shape
    {
        readonly int _refs;
        readonly int _systems;
        readonly int _instances;
        readonly int _sequences;
        readonly int _steps;
        readonly int _targets;
        readonly string _annotation;
        readonly string _name;

        public Shape(int refs, int systems, int instances, int sequences, int steps, int targets,
            string annotation = "", string name = "")
        {
            _refs = refs;
            _systems = systems;
            _instances = instances;
            _sequences = sequences;
            _steps = steps;
            _targets = targets;
            _annotation = annotation;
            _name = name;
        }

        /// <summary>What is actually in the test world, as the floor to read the rest against.</summary>
        public static Shape His()
        {
            return new Shape(3, 1, 1, 3, 1, 1);
        }

        /// <summary>Few statements, enormous lists: the other axis the format can grow along.</summary>
        public static Shape Wide()
        {
            return new Shape(500, 1, 1, 20, 20, 200);
        }

        /// <summary>Every construct annotated and named at length, which is the writer's slow path.</summary>
        public static Shape Wordy()
        {
            return new Shape(200, 20, 5, 20, 10, 3,
                "A paragraph about what this does and why it was built this way.\n\n"
                + "A second one, because somebody who writes one usually writes two.",
                "a name that goes on rather longer");
        }

        /// <summary>Names full of quotes, so every value pays for doubling them.</summary>
        public static Shape Quoted()
        {
            return new Shape(200, 20, 5, 20, 10, 3, "", "the \"\"\"big\"\"\" airlock \"2\"");
        }

        public Automation Build(Values values)
        {
            var program = new Automation();

            for (var i = 0; i < _refs; i++)
            {
                var declared = new EntityRef("entity" + i, "MyObjectBuilder_Door/LargeBlockDoor");
                declared.Keys.Add(new EntityKey(ValueKind.Integer,
                    EntityValue.Of(74857693847562934L + i)));
                declared.Keys.Add(new EntityKey(ValueKind.Text, values.Text("Airlock Door " + i)));
                program.Restore(declared);
            }

            for (var s = 0; s < _systems; s++)
                BuildSystem(program, s);

            return program;
        }

        void BuildSystem(Automation program, int number)
        {
            var system = program.AddSystem();
            // Numbered even when every other name is the same, because a system is its name and two alike
            // would be a program that does not read cleanly - which would time the wrong thing.
            system.Namespace = "MalTek";
            system.Name = Named("Airlock") + " " + number;
            system.Annotation = _annotation;

            var part = system.PartFor(Entity(0));
            part.Annotation = _annotation;
            part.Expects.Add("Door");

            for (var i = 1; i < _instances; i++)
                system.AddInstance().Name = Named("Forward " + i);

            for (var i = 0; i < _instances; i++)
                for (var f = 0; f < 4 && f < _refs; f++)
                    system.Instances[i].Fill(Entity(f), new[] { Entity(f) });

            for (var q = 0; q < _sequences; q++)
                BuildSequence(program, system, q);
        }

        void BuildSequence(Automation program, AutomationSystem system, int number)
        {
            var trigger = new WhenTrigger("pressed_1");
            trigger.Sources.Add(Entity(number));

            var sequence = program.AddSequence(system, trigger);
            sequence.Name = Named("Let me out " + number);
            sequence.Annotation = _annotation;

            for (var i = 0; i < _steps; i++)
            {
                var step = new Act(i % 3 == 0 ? "open" : "close");
                step.Annotation = _annotation;
                for (var t = 0; t < _targets; t++)
                    step.Targets.Add(Entity(t));

                sequence.Steps.Add(step);

                // The shape a sequence written in the editor has: something started, then waited for,
                // with the occasional deliberate pause.
                sequence.Steps.Add(new Await());
                if (i % 3 == 1)
                    sequence.Steps.Add(new Wait(60 + i));
            }
        }

        string Entity(int number)
        {
            return "entity" + number % Math.Max(1, _refs);
        }

        string Named(string plain)
        {
            return _name.Length > 0 ? _name : plain;
        }
    }
}
