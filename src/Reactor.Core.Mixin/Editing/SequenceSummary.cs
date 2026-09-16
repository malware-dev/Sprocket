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

                entry.LastRun = time.Ago(TicksSinceRun(running, tick));
            }
        }

        /// <summary>
        /// Whether nothing can start this sequence.
        /// </summary>
        /// <remarks>
        /// A sequence nothing starts at all is not judged: it may be there to be <c>run</c> by another
        /// one, which is an ordinary way to build. What is judged is a sequence something is supposed
        /// to start - a button that was cleared, or a trigger the construct never answered.
        /// <para>
        /// An unfinished trigger is not unreachable either. Somebody half way through picking blocks
        /// has a sequence that cannot fire yet, and saying so would be scolding them for not having
        /// finished - unless they have already built steps, in which case a trigger with nothing in
        /// it is a sequence that cannot run however it got that way.
        /// </para>
        /// <para>
        /// One question to the host rather than one per source, for the rest. The host knows what it
        /// arranged and for which sequence; core walking the sources and asking about each in turn was
        /// both dearer and weaker, since it could only ask whether <i>something</i> was wired there.
        /// </para>
        /// </remarks>
        static bool NeverTriggered(IWorld world, Sequence sequence)
        {
            var system = sequence.System;
            if (system == null)
                return false;

            var starts = 0;
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].Runs != sequence.Number)
                    continue;

                starts++;

                var when = triggers[i] as WhenTrigger;
                if (when != null && (when.Sources.Count == 0 || string.IsNullOrEmpty(when.EventId)))
                    return sequence.Steps.Count > 0;
            }

            return starts > 0 && !world.CanStart(sequence);
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

        /// <summary>
        /// What is wrong with this sequence and where, or <see cref="SequenceFault.None" />.
        /// </summary>
        /// <remarks>
        /// <b>Not free, and not for a tick.</b> Reachability can send a host off to read something
        /// expensive - a toolbar, in Space Engineers - so every screen asks this on a cadence, which
        /// is what <see cref="EditorPage.SecondPassed" /> is for. Nothing that breaks a sequence
        /// breaks it faster than a person can do it by hand, so a second is soon enough.
        /// </remarks>
        public static SequenceFault Fault(IWorld world, Bindings bindings, Sequence sequence,
            SequenceRunner state)
        {
            if (state != null && state.HasMissingSources)
                return new SequenceFault(SequenceProblem.MissingSource, SequenceFault.Trigger);

            var adrift = SequenceFault.None;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null)
                    continue;

                for (var t = 0; t < act.Targets.Count; t++)
                {
                    var points = bindings.State(act.Targets[t]);
                    if (points == RefState.Lost)
                        return new SequenceFault(SequenceProblem.MissingTarget, i);
                    if (points == RefState.Adrift && !adrift.Exists)
                        adrift = new SequenceFault(SequenceProblem.Adrift, i);
                }
            }

            // Reported only once nothing is outright missing, because being gone is the worse news
            // and a line can only say one thing.
            if (adrift.Exists)
                return adrift;

            // Last, because it is the only one that is about the sequence as a whole rather than
            // about something it names, and anything missing is the more useful thing to say.
            return NeverTriggered(world, sequence)
                ? new SequenceFault(SequenceProblem.NeverTriggered, SequenceFault.Trigger)
                : SequenceFault.None;
        }

    }
}
