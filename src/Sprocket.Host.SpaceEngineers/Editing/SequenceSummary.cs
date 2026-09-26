using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
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
        public static bool References(Sequence sequence, string name)
        {
            return Automation.References(sequence, name);
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
        public static SequenceRunner Live(AutomationRunner runner, Sequence sequence)
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
        static SequenceFault Stuck(Bindings bindings, Sequence sequence, SequenceRunner state,
            SystemInstance through)
        {
            if (state == null)
                return SequenceFault.None;

            // What ended the last run, which outlives the run itself: a sequence that gave up is
            // not running and has nothing outstanding to be asked, so this is the only record of
            // it. It stands until the sequence runs again.
            if (!string.IsNullOrEmpty(state.Quit))
                return new SequenceFault(SequenceProblem.Conflicted, SequenceFault.Trigger,
                    state.Quit);

            if (!state.IsWaiting)
                return SequenceFault.None;

            IList<Act> outstanding = state.Outstanding;


            for (var i = 0; i < outstanding.Count; i++)
            {
                Act act = outstanding[i];
                var targets = SystemInstance.Through(through, act.Targets);
                for (var t = 0; t < targets.Count; t++)
                {
                    GridEntity entity = bindings.Entity(targets[t]);
                    if (entity == null)
                        continue;

                    // About the state this step is waiting for, not about the block. One vent
                    // serves the cycle out and the cycle in, and they are waiting for opposite
                    // things of it - so "what is wrong with the vent" has two answers and only the
                    // step knows which one it is owed.
                    EntityAction doing = act.Names(entity.Kind.Actions);
                    if (doing == null || string.IsNullOrEmpty(doing.LeadsToStateId))
                        continue;

                    Trouble said = entity.Trouble(doing.LeadsToStateId);
                    if (!said.Exists)
                        continue;

                    return new SequenceFault(SequenceProblem.Stuck, sequence.Steps.IndexOf(act),
                        said.Said);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>What is wrong with a sequence, or none, asking about its blocks as <paramref name="through" /> fills them.</summary>
        /// <remarks>A null <paramref name="through" /> asks about what the steps name as written.</remarks>
        public static SequenceFault Fault(GridWorld world, Bindings bindings, Sequence sequence,
            SequenceRunner state, SystemInstance through)
        {
            if (state != null && state.HasMissingSources)
                return new SequenceFault(SequenceProblem.MissingSource, SequenceFault.Trigger);

            var adrift = SequenceFault.None;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                for (var t = 0; t < targets.Count; t++)
                {
                    var points = bindings.State(targets[t]);
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

            SequenceFault unoffered = Unoffered(bindings, sequence, through);
            if (unoffered.Exists)
                return unoffered;

            SequenceFault unfilled = Unfilled(bindings, sequence, through);
            if (unfilled.Exists)
                return unfilled;

            SequenceFault mistyped = Mistyped(bindings, sequence, through);
            if (mistyped.Exists)
                return mistyped;

            SequenceFault unawaited = Unawaited(sequence);
            if (unawaited.Exists)
                return unawaited;

            // A step that is being waited on and cannot finish. Only the block knows - a vent with
            // nowhere to put the air looks exactly like one that is merely slow - so it is asked,
            // and what it says is what gets shown.
            SequenceFault stuck = Stuck(bindings, sequence, state, through);
            if (stuck.Exists)
                return stuck;

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

        /// <summary>The first action that is not followed by a wait for it, or none.</summary>
        static SequenceFault Unawaited(Sequence sequence)
        {
            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i] is Act && (i + 1 == steps.Count || !(steps[i + 1] is Await)))
                    return new SequenceFault(SequenceProblem.Unawaited, i);
            }

            return SequenceFault.None;
        }

        /// <summary>The first step that has not been given a value its action needs, or none.</summary>
        /// <remarks>
        /// Asked of the action the blocks offer, because what is needed is the action's to declare and
        /// a block may have been replaced by one that declares otherwise.
        /// </remarks>
        static SequenceFault Unfilled(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                for (var t = 0; t < targets.Count; t++)
                {
                    GridEntity entity = bindings.Entity(targets[t]);
                    if (entity == null)
                        continue;

                    EntityAction doing = act.Names(entity.Kind.Actions);
                    if (doing == null)
                        continue;

                    if (doing.Needs(act.Values.Count))
                        return new SequenceFault(SequenceProblem.Unfilled, i);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>The first step given a value its action takes as another kind, or none.</summary>
        /// <remarks>
        /// Asked of each block's own action, as <see cref="Unfilled" /> is: what a value has to be is
        /// the action's to declare.
        /// </remarks>
        static SequenceFault Mistyped(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction || act.Values.Count == 0)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                for (var t = 0; t < targets.Count; t++)
                {
                    GridEntity entity = bindings.Entity(targets[t]);
                    if (entity == null)
                        continue;

                    EntityAction doing = act.Names(entity.Kind.Actions);
                    if (doing != null && !doing.Takes(act.Values))
                        return new SequenceFault(SequenceProblem.Mistyped, i);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>The first step whose blocks do not offer what it does, or none.</summary>
        /// <remarks>
        /// Every block the step names has to offer it, because the step is one act done to all of
        /// them. Asked of each block's own list rather than of a list built for the purpose, since
        /// this is worked out every time a fault is.
        /// </remarks>
        static SequenceFault Unoffered(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                for (var t = 0; t < targets.Count; t++)
                {
                    GridEntity entity = bindings.Entity(targets[t]);
                    if (entity != null && act.Names(entity.Kind.Actions) == null)
                        return new SequenceFault(SequenceProblem.Unoffered, i);
                }
            }

            return SequenceFault.None;
        }
    }
}
