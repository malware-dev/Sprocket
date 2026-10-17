using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>What a construct's automation amounts to, one line per system.</summary>
    public sealed class StatusOverview
    {
        /// <summary>One system, as a line.</summary>
        public struct Row
        {
            /// <summary>What the player called the system.</summary>
            public string Name;

            /// <summary>How many ways it runs on this construct: a blueprint's instances, or one for a system.</summary>
            public int Instances;

            /// <summary>The most telling of its sequences' states.</summary>
            public SequenceStatus Status;

            /// <summary>How many of its sequences are broken.</summary>
            public int Problems;

            /// <summary>How many of its sequences have a warning.</summary>
            public int Warnings;

            /// <summary>Whether the system's own switch is off, which outranks everything below it.</summary>
            public bool Parked;
        }

        readonly List<Row> _rows = new List<Row>();

        /// <summary>One row for each logic system on the construct, in order.</summary>
        public IReadOnlyList<Row> Rows => _rows;

        /// <summary>How many systems are doing something this instant.</summary>
        public int Running { get; private set; }

        /// <summary>How many problems the logic systems on the construct have between them.</summary>
        public int Problems { get; private set; }

        /// <summary>How many warnings the logic systems on the construct have between them.</summary>
        public int Warnings { get; private set; }

        /// <summary>Works out every row again.</summary>
        /// <remarks>Not for every frame: a fault can send the host off to read a toolbar, so ask once a second or slower.</remarks>
        public void Fill(GridWorld world, Automation program, AutomationRunner runner)
        {
            Clear();
            Include(world, program, runner);
        }

        /// <summary>Empties every row and count.</summary>
        public void Clear()
        {
            _rows.Clear();
            Running = 0;
            Problems = 0;
            Warnings = 0;
        }

        /// <summary>Adds one Sprocket's systems to what is already here.</summary>
        public void Include(GridWorld world, Automation program, AutomationRunner runner)
        {
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _rows.Add(Line(runner, systems[i]));
        }

        Row Line(AutomationRunner runner, AutomationSystem system)
        {
            var row = new Row();
            row.Name = system.Name;
            row.Instances = system.Uses.Count;
            row.Parked = Off(system);
            row.Status = SequenceStatus.Off;

            var sequences = system.Sequences;
            var triggers = system.Triggers;
            for (var i = 0; i < sequences.Count + triggers.Count; i++)
            {
                // The sequences, and then what each trigger that acts itself runs.
                var sequence = i < sequences.Count ? sequences[i] : runner.Alone(system, triggers[i - sequences.Count]);
                if (sequence == null)
                    continue;

                var state = runner.Get(sequence);

                if (Louder(SequenceSummary.Status(sequence, state), row.Status))
                    row.Status = SequenceSummary.Status(sequence, state);

                var fault = SequenceSummary.Fault(state);
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

        /// <summary>Whether <paramref name="system" /> is switched off, as a whole or every one of it.</summary>
        static bool Off(AutomationSystem system)
        {
            if (!system.Enabled)
                return true;

            var uses = system.Uses;
            for (var i = 0; i < uses.Count; i++)
            {
                if (uses[i].Enabled)
                    return false;
            }

            return uses.Count > 0;
        }

        /// <summary>Whether one state is more worth showing than another.</summary>
        static bool Louder(SequenceStatus now, SequenceStatus than)
        {
            return Rank(now) > Rank(than);
        }

        static int Rank(SequenceStatus status)
        {
            if (status == SequenceStatus.Running || status == SequenceStatus.RunningWaiting)
                return 4;

            if (status == SequenceStatus.Waiting || status == SequenceStatus.Stopped)
                return 3;

            return status == SequenceStatus.NeverRun ? 2 : 1;
        }
    }
}
