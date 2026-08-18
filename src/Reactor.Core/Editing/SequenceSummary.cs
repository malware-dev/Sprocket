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
        /// <summary>Lit for this many ticks after a sequence fires.</summary>
        const int PulseTicks = 10;

        public static bool References(Sequence sequence, long deviceId)
        {
            return Automation.References(sequence, deviceId);
        }

        /// <summary>Updates one entry per sequence, in place.</summary>
        public static void Fill(ObservableList<SequenceEntry> entries, List<Sequence> sequences, IWorld world,
            AutomationRunner runner, int tick)
        {
            var count = entries.Count < sequences.Count ? entries.Count : sequences.Count;
            for (var i = 0; i < count; i++)
            {
                var sequence = sequences[i];
                var entry = entries[i];
                var state = runner.Get(sequence);

                entry.Summary = Describe(world, sequence);
                entry.Status = Status(sequence, state);
                entry.Pulse = state != null && state.LastFiredTick >= 0
                    && tick - state.LastFiredTick < PulseTicks;
                entry.Problem = Problem(world, sequence, state);

                entry.LastRun = Elapsed.Ago(TicksSinceRun(state, tick));
            }
        }

        /// <summary>
        /// What to call this sequence: its name if it has one, otherwise the whole
        /// thing read as a sentence.
        /// </summary>
        /// <remarks>
        /// The name wins everywhere it exists, which is the entire point of allowing
        /// one - a list of twenty sentences is unreadable, a list of twenty names is
        /// not. Everything unnamed still reads perfectly well on its own.
        /// </remarks>
        public static string Describe(IWorld world, Sequence sequence)
        {
            if (sequence.HasName)
                return sequence.Name;

            var trigger = SequenceText.DescribeTrigger(world, sequence.Trigger);
            if (sequence.Steps.Count == 0)
                return trigger + " → nothing yet";

            var first = SequenceText.DescribeStep(world, sequence.Steps[0]);
            if (sequence.Steps.Count == 1)
                return trigger + " → " + first;

            return trigger + " → " + first + " (+" + (sequence.Steps.Count - 1) + " more)";
        }

        /// <summary>
        /// What this sequence is doing, in a handful of words.
        /// </summary>
        /// <remarks>
        /// Every answer is a constant, deliberately. This is read on every tick for
        /// whatever is on screen, so anything built from a number would allocate
        /// sixty times a second inside the game's update loop.
        ///
        /// Nothing is lost by it. The step it is on is already shown by that step
        /// being lit, and a count of ticks since it last ran is a number nobody
        /// reads - the indicator flashing when it fires is the part that carries.
        /// </remarks>
        /// <summary>Ticks since it last ran, or -1 if it never has.</summary>
        public static int TicksSinceRun(SequenceRunner state, int tick)
        {
            if (state == null || state.LastFiredTick < 0)
                return -1;
            return tick - state.LastFiredTick;
        }

        public static string Status(Sequence sequence, SequenceRunner state)
        {
            if (!sequence.Enabled)
                return "switched off";

            if (state == null)
                return "waiting to happen";

            if (state.IsRunning)
                return state.IsWaiting ? "running — waiting for this step" : "running";

            return state.LastFiredTick < 0 ? "waiting to happen" : "waiting";
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
