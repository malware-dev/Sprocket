using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Runtime;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// What is true of a sequence right now, for every place one is listed.
    /// </summary>
    /// <remarks>
    /// Facts only. This works out which state a sequence is in and what is wrong with it; what those
    /// read as belongs to <see cref="ISequenceText" />, so a sequence reads identically wherever it is
    /// seen without core deciding what it says.
    /// </remarks>
    static class SequenceSummary
    {
        /// <summary>How long after firing a sequence still counts as having just run.</summary>
        const int JustRanTicks = 10;

        public static bool References(Sequence sequence, string name)
        {
            return Automation.References(sequence, name);
        }

        /// <summary>Updates one entry per sequence, in place.</summary>
        public static void Fill(ObservableList<StatusEntry> entries, List<Sequence> sequences, Bindings bindings,
            AutomationRunner runner, int tick, ISequenceText wording, ITimeText time)
        {
            var count = entries.Count < sequences.Count ? entries.Count : sequences.Count;
            for (var i = 0; i < count; i++)
            {
                var sequence = sequences[i];
                var entry = entries[i];
                var running = runner.Get(sequence);

                entry.Summary = wording.Sequence(bindings, sequence);
                entry.Status = wording.Status(Status(sequence, running));
                entry.JustRan = running != null && running.LastFiredTick >= 0
                    && tick - running.LastFiredTick < JustRanTicks;
                entry.Problem = wording.Problem(Problem(bindings, sequence, running));

                entry.LastRun = time.Ago(TicksSinceRun(running, tick));
            }
        }

        /// <summary>Ticks since it last ran, or -1 if it never has.</summary>
        public static int TicksSinceRun(SequenceRunner state, int tick)
        {
            if (state == null || state.LastFiredTick < 0)
                return -1;
            return tick - state.LastFiredTick;
        }

        public static SequenceStatus Status(Sequence sequence, SequenceRunner state)
        {
            if (!sequence.Enabled)
                return SequenceStatus.Off;

            if (state == null)
                return SequenceStatus.NeverRun;

            if (state.IsRunning)
                return state.IsWaiting ? SequenceStatus.RunningWaiting : SequenceStatus.Running;

            return state.LastFiredTick < 0 ? SequenceStatus.NeverRun : SequenceStatus.Waiting;
        }

        /// <summary>Why this sequence cannot work, or <see cref="SequenceProblem.None" />.</summary>
        public static SequenceProblem Problem(Bindings bindings, Sequence sequence, SequenceRunner state)
        {
            if (state != null && state.HasMissingSources)
                return SequenceProblem.MissingSource;

            var adrift = false;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];

                for (var t = 0; t < step.Targets.Count; t++)
                {
                    var points = bindings.State(step.Targets[t]);
                    if (points == RefState.Lost)
                        return SequenceProblem.MissingTarget;
                    if (points == RefState.Adrift)
                        adrift = true;
                }

                var gate = step.Gate as OnceGate;
                var condition = gate != null ? gate.Condition : null;
                if (condition == null)
                {
                    var guard = step.Gate as OnlyIfGate;
                    condition = guard != null ? guard.Condition : null;
                }

                if (condition == null)
                    continue;

                for (var t = 0; t < condition.Entities.Count; t++)
                {
                    var held = bindings.State(condition.Entities[t]);
                    if (held == RefState.Lost)
                        return SequenceProblem.MissingGate;
                    if (held == RefState.Adrift)
                        adrift = true;
                }
            }

            // Reported only once nothing is outright missing, because being gone is the worse news
            // and a line can only say one thing.
            return adrift ? SequenceProblem.Adrift : SequenceProblem.None;
        }
    }
}
