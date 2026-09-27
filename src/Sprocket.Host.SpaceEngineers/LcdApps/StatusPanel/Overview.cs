using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>
    /// What a construct's automation amounts to, one line per system.
    /// </summary>
    /// <remarks>
    /// For a screen somebody glances at rather than works in: a row says what a system is doing right
    /// now and whether it needs attention, and the totals say whether the whole construct does. Nothing
    /// here is interactive, so nothing here is a command.
    /// <para>
    /// <b>Facts and existing words only.</b> The state of a row is a <see cref="SequenceStatus" />, the
    /// same one a sequence carries in the editor, so the display says "running" and "switched off" in
    /// the words the editor already uses - a glance and a look agree, and the coming pass over the words
    /// has one place to change.
    /// </para>
    /// </remarks>
    public sealed class Overview
    {
        /// <summary>One system, as a line.</summary>
        public struct Row
        {
            /// <summary>What the player called the system.</summary>
            public string Name;

            /// <summary>How many instances of it this construct has.</summary>
            public int Instances;

            /// <summary>The most telling of its sequences' states.</summary>
            public SequenceStatus Status;

            /// <summary>How many of its sequences are broken.</summary>
            public int Problems;

            /// <summary>How many are worth a remark rather than a mark.</summary>
            public int Warnings;

            /// <summary>Whether the system's own switch is off, which outranks everything below it.</summary>
            public bool Parked;
        }

        readonly List<Row> _rows = new List<Row>();

        public IReadOnlyList<Row> Rows => _rows;

        /// <summary>How many systems are doing something this instant.</summary>
        public int Running { get; private set; }

        public int Problems { get; private set; }

        public int Warnings { get; private set; }

        /// <summary>
        /// Works out every row again.
        /// </summary>
        /// <remarks>
        /// <b>Not for a frame.</b> A fault can send the host off to read a toolbar, so this belongs on
        /// the same sort of cadence the editor's screens use - once a second or slower. A surface script
        /// asking on <c>Update100</c> is well inside that.
        /// </remarks>
        public void Fill(GridWorld world, Automation program, AutomationRunner runner)
        {
            Clear();
            Include(world, program, runner);
        }

        public void Clear()
        {
            _rows.Clear();
            Running = 0;
            Problems = 0;
            Warnings = 0;
        }

        /// <summary>
        /// Adds one Sprocket's systems to what is already here.
        /// </summary>
        /// <remarks>
        /// A construct may carry more than one Sprocket, and a person looking at a panel wants the ship
        /// rather than whichever block the panel happens to sit nearest. So the rows and the totals are
        /// the construct's, and every Sprocket on it adds to them.
        /// </remarks>
        public void Include(GridWorld world, Automation program, AutomationRunner runner)
        {
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _rows.Add(Line(world, runner, systems[i]));
        }

        Row Line(GridWorld world, AutomationRunner runner, AutomationSystem system)
        {
            var row = new Row();
            row.Name = system.Name;
            row.Instances = system.Instances.Count;
            row.Parked = Off(system);
            row.Status = SequenceStatus.Off;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var sequence = sequences[i];
                var state = runner.Get(sequence);

                if (Louder(SequenceSummary.Status(sequence, state), row.Status))
                    row.Status = SequenceSummary.Status(sequence, state);

                var fault = SequenceSummary.Fault(world, runner.Bindings, sequence, state, system.First);
                if (!fault.Exists)
                    continue;

                if (fault.IsWarning)
                    row.Warnings++;
                else
                    row.Problems++;
            }

            Problems += row.Problems;
            Warnings += row.Warnings;

            if (row.Status == SequenceStatus.Running || row.Status == SequenceStatus.RunningWaiting)
                Running++;

            return row;
        }

        /// <summary>
        /// Whether one state is more worth showing than another.
        /// </summary>
        /// <remarks>
        /// A system is a row, and a row says one thing. Something happening beats something waiting,
        /// and anything at all beats a rule somebody parked - which is the least interesting fact about
        /// a system where the others are running.
        /// </remarks>
        /// <summary>Whether every one of <paramref name="system" /> is switched off.</summary>
        static bool Off(AutomationSystem system)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                if (instances[i].Enabled)
                    return false;
            }

            return instances.Count > 0;
        }

        static bool Louder(SequenceStatus now, SequenceStatus than)
        {
            return Rank(now) > Rank(than);
        }

        static int Rank(SequenceStatus status)
        {
            if (status == SequenceStatus.Running || status == SequenceStatus.RunningWaiting)
                return 4;

            if (status == SequenceStatus.Waiting)
                return 3;

            return status == SequenceStatus.NeverRun ? 2 : 1;
        }
    }
}
