using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// What is true of a sequence right now, for every place one is listed.
    /// </summary>
    /// <remarks>
    /// Facts only. This works out which state a sequence is in and what is wrong with it; what those
    /// read as belongs to <see cref="SequenceText" />, so the words for one thing live in one place.
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
        public static void Fill(ObservableList<StatusEntry> entries, List<Sequence> sequences,
            AutomationRunner runner, int tick)
        {
            var count = entries.Count < sequences.Count ? entries.Count : sequences.Count;
            for (var i = 0; i < count; i++)
            {
                var sequence = sequences[i];
                var entry = entries[i];
                var running = Live(runner, sequence);

                // Said in the place the row already has for what it is doing, rather than on a line
                // of its own. The switch beside it is the fact and "switched off" beside a switch
                // that says off is a word spent on nothing; what follows from it is the thing
                // somebody looking for why their button did nothing needs to read. And a line that
                // is always there cannot move the row when the switch on it is pressed.
                var state = Status(sequence, running);
                entry.Status = state == SequenceStatus.Off && sequence.Steps.Count > 0
                    ? SequenceText.Problem(SequenceProblem.SwitchedOff)
                    : SequenceText.Status(state);
                entry.Running = running != null && running.IsRunning;
                entry.JustRan = running != null && running.LastFiredTick >= 0
                    && tick - running.LastFiredTick < JustRanTicks;

                // Every tick, and it costs nothing: every answer comes out of a table built once -
                // see TimeText.Ago, which was written to be called exactly like this.
                entry.LastRun = TimeText.Ago(TicksSinceRun(running, tick));
            }
        }

        /// <summary>
        /// The state to show for a sequence on a screen that is not about one of its system.
        /// </summary>
        /// <remarks>
        /// Whichever one of the system is going, and the first otherwise. A system's sequences belong
        /// to the system and run once for each one of it, so a list of them is about none of them in
        /// particular - and a row that only ever read the first would sit still while the port airlock
        /// cycled.
        /// </remarks>
        static SequenceRunner Live(AutomationRunner runner, Sequence sequence)
        {
            var system = sequence.System;
            if (system == null)
                return runner.Get(sequence);

            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var one = runner.Get(sequence, instances[i]);
                if (one != null && one.IsRunning)
                    return one;
            }

            return runner.Get(sequence);
        }

        /// <summary>
        /// What each row calls its sequence: its name, or what it does where it has none.
        /// </summary>
        /// <remarks>
        /// Not what starts it. That is a row of its own now, directly above this list, and a row
        /// saying both would say the trigger twice on the one screen where both are shown.
        /// <para>
        /// Separate from <see cref="Fill" /> because it is made of words rather than read off a state:
        /// joining a sentence out of a sequence's blocks and actions allocates, and the sentence changes
        /// when somebody edits it or renames a block it names - never on its own, and never per tick.
        /// </para>
        /// </remarks>
        public static void Word(ObservableList<StatusEntry> entries, List<Sequence> sequences,
            Bindings bindings)
        {
            var count = entries.Count < sequences.Count ? entries.Count : sequences.Count;
            for (var i = 0; i < count; i++)
                entries[i].Summary = SequenceText.Doing(bindings, sequences[i]);
        }

        /// <summary>
        /// What is wrong with what starts this sequence, if anything.
        /// </summary>
        /// <remarks>
        /// Two ways for nothing to start it. A trigger that is supposed to - a button whose slot was
        /// cleared, or a moment the construct never answered - and no trigger at all, which is a fault
        /// only once nothing calls it either. A sequence another one runs is an ordinary way to build,
        /// and until <c>run</c> existed that possibility meant this case could not be judged.
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
        static SequenceProblem StartTrouble(GridWorld world, Sequence sequence)
        {
            var system = sequence.System;
            if (system == null)
                return SequenceProblem.None;

            var starts = 0;
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].Runs != sequence.Number)
                    continue;

                starts++;

                var moment = triggers[i] as MomentTrigger;
                if (moment == null)
                    continue;

                // Nothing named and no moment either is a whole sentence: something on this ship
                // starts this, and which is the host's to answer rather than a hole in the trigger.
                if (moment.Sources.Count == 0 && string.IsNullOrEmpty(moment.EventId))
                    continue;

                if (moment.Sources.Count == 0 || string.IsNullOrEmpty(moment.EventId))
                    return sequence.Steps.Count > 0 ? SequenceProblem.NeverTriggered : SequenceProblem.None;
            }

            // Nothing starts it at all, which is a warning rather than a fault - and nothing to say
            // while it has no steps either, because somebody who has just made one has not finished.
            if (starts == 0)
            {
                return sequence.Steps.Count > 0 && !system.Called(sequence.Number)
                    ? SequenceProblem.NothingStarts
                    : SequenceProblem.None;
            }

            return world.CanStart(sequence) ? SequenceProblem.None : SequenceProblem.NeverTriggered;
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
            // What is happening outranks what is allowed to happen. A parked sequence somebody has
            // run by hand is running, and a screen saying "switched off" while its steps light up
            // one after another would be the screen arguing with itself.
            if (state != null && state.IsRunning)
                return state.IsWaiting ? SequenceStatus.RunningWaiting : SequenceStatus.Running;

            if (!sequence.Enabled)
                return SequenceStatus.Off;

            if (state == null)
                return SequenceStatus.NeverRun;

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
        public static SequenceFault Fault(GridWorld world, Bindings bindings, Sequence sequence,
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

            // These last, because they are the ones about the sequence as a whole rather than about
            // something it names, and anything missing is the more useful thing to say.
            var trouble = StartTrouble(world, sequence);
            if (trouble != SequenceProblem.None)
                return new SequenceFault(trouble, SequenceFault.Trigger);

            // Reported even while something starts it perfectly well, because that is the case it is
            // for: the button works, the trigger is right, and the sequence sits there. Not while it
            // is actually going, since somebody has run it by hand and can see that it ran, and not
            // before it does anything, because one nobody has written yet is not one to switch on.
            if (!sequence.Enabled && sequence.Steps.Count > 0 && (state == null || !state.IsRunning))
                return new SequenceFault(SequenceProblem.SwitchedOff, SequenceFault.Trigger);

            return SequenceFault.None;
        }

    }
}
