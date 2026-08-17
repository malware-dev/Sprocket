using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Runtime;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// Words a sequence's one-line form and its live state.
    /// </summary>
    /// <remarks>
    /// Shared by every place a sequence is listed, so a sequence reads identically whether
    /// it is seen from the block it affects or from the full list. Wording that a
    /// target had to supply would drift between targets, and worse, between screens.
    /// </remarks>
    static class SequenceSummary
    {
        /// <summary>Lit for this many beats after a sequence fires.</summary>
        const int PulseBeats = 10;

        public static bool References(Sequence sequence, long deviceId)
        {
            return Automation.References(sequence, deviceId);
        }

        /// <summary>Updates one entry per sequence, in place.</summary>
        public static void Fill(List<SequenceEntry> entries, List<Sequence> sequences, IWorld world,
            AutomationRunner runner, int beat)
        {
            var count = entries.Count < sequences.Count ? entries.Count : sequences.Count;
            for (var i = 0; i < count; i++)
            {
                var sequence = sequences[i];
                var entry = entries[i];
                var state = runner.Get(sequence);

                entry.Summary = Describe(world, sequence);
                entry.Status = Status(sequence, state, beat);
                entry.Pulse = state != null && state.LastFiredBeat >= 0
                    && beat - state.LastFiredBeat < PulseBeats;
                entry.Problem = Problem(world, sequence, state);
            }
        }

        /// <summary>The whole sequence as one sentence, for lists and headings.</summary>
        public static string Describe(IWorld world, Sequence sequence)
        {
            var trigger = SequenceText.DescribeTrigger(world, sequence.Trigger);
            if (sequence.Steps.Count == 0)
                return trigger + " → nothing yet";

            var first = SequenceText.DescribeStep(world, sequence.Steps[0]);
            if (sequence.Steps.Count == 1)
                return trigger + " → " + first;

            return trigger + " → " + first + " (+" + (sequence.Steps.Count - 1) + " more)";
        }

        public static string Status(Sequence sequence, SequenceRunner state, int beat)
        {
            if (!sequence.Enabled)
                return "switched off";

            if (state == null)
                return "waiting to happen";

            if (state.IsRunning)
            {
                var step = state.CurrentStep + 1;
                return state.IsWaiting
                    ? "running — waiting on step " + step
                    : "running — step " + step;
            }

            if (state.LastFiredBeat < 0)
                return "waiting to happen";

            return "waiting — last ran " + (beat - state.LastFiredBeat) + " beats ago";
        }

        /// <summary>
        /// Why this sequence cannot work, or empty.
        /// </summary>
        /// <remarks>
        /// A sequence pointing at a block that got shot off must never simply do nothing.
        /// Silence is indistinguishable from working, and that is precisely the kind
        /// of failure a non-programmer has no way to diagnose.
        /// </remarks>
        public static string Problem(IWorld world, Sequence sequence, SequenceRunner state)
        {
            if (state != null && state.HasMissingSources)
                return "This listens to something that isn't on the ship any more.";

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];

                for (var t = 0; t < step.Targets.Count; t++)
                {
                    if (world.FindDevice(step.Targets[t]) == null)
                        return "This affects something that isn't on the ship any more.";
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

                for (var t = 0; t < condition.Devices.Count; t++)
                {
                    if (world.FindDevice(condition.Devices[t]) == null)
                        return "This waits on something that isn't on the ship any more.";
                }
            }

            return "";
        }
    }
}
