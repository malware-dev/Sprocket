using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// What is true of a sequence right now, for every place one is listed.
    /// </summary>
    public static class SequenceSummary
    {
        /// <summary>Whether <paramref name="sequence" /> names the variable or slot <paramref name="name" />, in what starts it or in a step.</summary>
        public static bool References(Sequence sequence, string name)
        {
            return Automation.References(sequence, name);
        }

        /// <summary>
        /// The state to show for a sequence on a screen that is not about one of its system.
        /// </summary>
        /// <remarks>Whichever installation is running it, and the first otherwise.</remarks>
        public static SequenceRunner Live(AutomationRunner runner, Sequence sequence)
        {
            var system = sequence.System;
            if (system == null)
                return runner.Get(sequence);

            var instances = system.Uses;
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
        /// No trigger at all is trouble only once nothing calls it either, and a trigger with nothing in it only once the
        /// sequence has steps.
        /// </remarks>
        static SequenceFault StartTrouble(GridWorld world, Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var system = sequence.System;
            if (system == null)
                return SequenceFault.None;

            var starts = 0;
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (!triggers[i].IsAbout(sequence))
                    continue;

                // A trigger that stops it is wired like one that starts it, so it is checked below all the same, and
                // starts nothing.
                if (!triggers[i].Stops)
                    starts++;

                // A signal part left unanswered is said by Empty; whether the slots it is given answer is the
                // construct's to say below. One that names no part yet is somebody still building it.
                var required = triggers[i] as SignalTrigger;
                if (required != null)
                {
                    // A block that never sends the change Sprocket watches for can never start it, which is said rather than
                    // waited on forever.
                    if (required.IsWatched)
                    {
                        var sending = SystemInstance.Through(through, required.Sources);
                        foreach (var source in sending)
                        {
                            GridEntity block = bindings.Entity(source);
                            if (block != null && Probe.Watching(block, required.Provider, required.EventId) == null)
                                return SequenceFault.Unstarted(Unwired.NoMoment, block.Name);
                        }
                    }

                    if (required.Sources.Count > 0)
                        continue;

                    return sequence.Steps.Count == 0
                        ? SequenceFault.None
                        : SequenceFault.Unstarted(Unwired.NoBlock, null);
                }

                var moment = triggers[i] as MomentTrigger;
                if (moment == null)
                    continue;

                // Nothing named and no moment either is a whole sentence, and which block starts it is the host's to answer.
                if (moment.Sources.Count == 0 && string.IsNullOrEmpty(moment.EventId))
                    continue;

                if (moment.Sources.Count == 0 || string.IsNullOrEmpty(moment.EventId))
                {
                    if (sequence.Steps.Count == 0)
                        return SequenceFault.None;

                    if (moment.Sources.Count == 0)
                        return SequenceFault.Unstarted(Unwired.NoBlock, null);

                    var named = SystemInstance.Through(through, moment.Sources);
                    var entity = named.Count == 0 ? null : bindings.Entity(named[0]);
                    return SequenceFault.Unstarted(Unwired.NoMoment, entity?.Name);
                }

                // A block whose member can never be read as the when compares it can never start it, which is said rather
                // than waited on forever.
                var when = moment as WhenTrigger;
                if (when == null)
                    continue;

                var watched = SystemInstance.Through(through, when.Sources);
                foreach (var source in watched)
                {
                    GridEntity block = bindings.Entity(source);
                    if (block != null && Probe.Of(block, when.Provider, when.EventId, when.Arguments, when.Quantifier,
                            when.Relation, when.Value) == null)
                        return SequenceFault.Unstarted(Unwired.NoMoment, block.Name);
                }
            }

            // Nothing starts it at all: a warning rather than a fault, and nothing to say while it has no steps.
            if (starts == 0)
            {
                return sequence.Steps.Count > 0 && !system.Called(sequence.Number)
                    ? new SequenceFault(SequenceProblem.NothingStarts, SequenceFault.Trigger)
                    : SequenceFault.None;
            }

            if (world.CanStart(sequence, through))
                return SequenceFault.None;

            var why = world.Why(sequence, through);
            return why == null ? SequenceFault.Unstarted(Unwired.Unknown, null) : SequenceFault.Unstarted(why.Why, why.Block);
        }

        /// <summary>Ticks since it last ran, or -1 if it never has.</summary>
        public static int TicksSinceRun(SequenceRunner state, int tick)
        {
            if (state == null || state.LastFiredTick < 0)
                return -1;
            return tick - state.LastFiredTick;
        }

        /// <summary>What <paramref name="sequence" /> is doing, as <paramref name="state" /> says it runs and its switch allows.</summary>
        public static SequenceStatus Status(Sequence sequence, SequenceRunner state)
        {
            // What is happening outranks what is allowed: a switched-off sequence run by hand is running.
            if (state != null && state.IsRunning)
                return state.IsWaiting ? SequenceStatus.RunningWaiting : SequenceStatus.Running;

            if (!sequence.CanRun(null))
                return SequenceStatus.Off;

            if (state == null)
                return SequenceStatus.NeverRun;

            if (state.StoppedBy != StopCause.None)
                return SequenceStatus.Stopped;

            return state.LastFiredTick < 0 ? SequenceStatus.NeverRun : SequenceStatus.Waiting;
        }

        /// <summary>What is known to be wrong with a sequence on one of its system, as its runner holds it, or none.</summary>
        /// <remarks>A lookup: the faults are found where they happen, and nothing is worked out here.</remarks>
        public static SequenceFault Fault(SequenceRunner state)
        {
            return state?.Fault ?? SequenceFault.None;
        }

        /// <summary>What the program says is wrong with a sequence on <paramref name="through" />, or none.</summary>
        /// <remarks>Asked when the program or its wiring changes, and on no tick.</remarks>
        public static SequenceFault Checked(GridWorld world, Bindings bindings, Sequence sequence, SystemInstance through)
        {
            SequenceFault empty = Empty(sequence, through);
            if (empty.Exists)
                return empty;

            SequenceFault circular = Circular(sequence);
            if (circular.Exists)
                return circular;

            SequenceFault unoffered = Unoffered(bindings, sequence, through);
            if (unoffered.Exists)
                return unoffered;

            SequenceFault missing = MissingFacet(bindings, sequence, through);
            if (missing.Exists)
                return missing;

            SequenceFault unfilled = Unfilled(bindings, sequence, through);
            if (unfilled.Exists)
                return unfilled;

            SequenceFault mistyped = Mistyped(bindings, sequence, through);
            if (mistyped.Exists)
                return mistyped;

            // Last, because it is about the sequence as a whole rather than about something it names; a warning about a step
            // is said ahead of one about the whole, a fault behind none.
            SequenceFault start = StartTrouble(world, bindings, sequence, through);
            if (start.Exists && !start.IsWarning)
                return start;

            SequenceFault idle = Idle(bindings, sequence, through);
            return idle.Exists ? idle : start;
        }

        /// <summary>The first await with nothing to wait for, no step since the await before it taking time, or none.</summary>
        /// <remarks>An await after an if has nothing to wait for only where no way through the if gives it anything.</remarks>
        static SequenceFault Idle(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var steps = sequence.Steps;
            var pending = false;
            var before = new List<bool>();
            var after = new List<bool>();
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (step is If && step.IsOff)
                {
                    i = sequence.EndOf(i);
                    continue;
                }

                if (step is If)
                {
                    before.Add(pending);
                    after.Add(false);
                    continue;
                }

                var top = before.Count - 1;
                if (step is Else && top >= 0)
                {
                    after[top] = after[top] || pending;
                    pending = before[top];
                    continue;
                }

                if (step is EndIf && top >= 0)
                {
                    pending = pending || after[top] || before[top];
                    before.RemoveAt(top);
                    after.RemoveAt(top);
                    continue;
                }

                if (step.IsOff)
                    continue;

                if (step is Await)
                {
                    if (!pending)
                        return new SequenceFault(SequenceProblem.NothingToAwait, i);

                    pending = false;
                    continue;
                }

                var run = step as Run;
                var act = step as Act;
                if (run != null && !run.Stops || act != null && Lasts(bindings, act, through))
                    pending = true;
            }

            return SequenceFault.None;
        }

        /// <summary>Whether what the step does to any of its blocks takes time to finish.</summary>
        static bool Lasts(Bindings bindings, Act act, SystemInstance through)
        {
            var targets = SystemInstance.Through(through, act.Targets);
            foreach (var target in targets)
            {
                GridEntity entity = bindings.Entity(target);
                if (entity == null)
                    continue;

                EntityAction doing = act.Names(entity.Kind.Actions);
                if (doing != null && doing.Lasts)
                    return true;
            }

            return false;
        }

        /// <summary>The first block a sequence's steps name on <paramref name="through" /> that is gone, or may have been swapped for another, or none.</summary>
        /// <remarks>A slot left empty is skipped.</remarks>
        public static SequenceFault Looked(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var system = sequence.System;
            var adrift = SequenceFault.None;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                var lost = step is Act ? SequenceProblem.MissingTarget : SequenceProblem.MissingGate;
                for (var s = 0; s < step.Targets.Count; s++)
                {
                    var slot = step.Targets[s];
                    var part = system?.FindPart(slot);
                    var filled = through?.FilledWith(slot);
                    if (part != null && through != null && (filled == null || filled.Count == 0))
                        continue;

                    var count = filled?.Count ?? 1;
                    for (var n = 0; n < count; n++)
                    {
                        var name = filled == null ? slot : filled[n];
                        var points = bindings.State(name);
                        if (points == RefState.Lost && (part == null || !part.Optional))
                            return new SequenceFault(lost, i, BlockTrouble.None, name);
                        if (points == RefState.Adrift && !adrift.Exists)
                            adrift = new SequenceFault(SequenceProblem.Adrift, i, BlockTrouble.None, name);
                    }
                }
            }

            // Only once nothing is outright missing, because being gone is the worse news and a line can only say one thing.
            return adrift;
        }

        /// <summary>The first slot, of what starts it and then what its steps act on, that <paramref name="through" /> leaves empty and may not, or none.</summary>
        static SequenceFault Empty(Sequence sequence, SystemInstance through)
        {
            var system = sequence.System;
            if (system == null || through == null)
                return SequenceFault.None;

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var moment = triggers[i] as MomentTrigger;
                if (moment == null || !moment.IsAbout(sequence))
                    continue;

                var slot = EmptyOf(system, through, moment.Sources);
                if (slot != null)
                    return new SequenceFault(SequenceProblem.EmptySlot, SequenceFault.Trigger, BlockTrouble.None, slot);
            }

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var slot = EmptyOf(system, through, sequence.Steps[i].Targets);
                if (slot != null)
                    return new SequenceFault(SequenceProblem.EmptySlot, i, BlockTrouble.None, slot);
            }

            return SequenceFault.None;
        }

        static string EmptyOf(AutomationSystem system, SystemInstance through, IList<string> names)
        {
            for (var i = 0; i < names.Count; i++)
            {
                var part = system.FindPart(names[i]);
                if (part == null || part.Optional)
                    continue;

                var filled = through.FilledWith(names[i]);
                if (filled == null || filled.Count == 0)
                    return names[i];
            }

            return null;
        }

        /// <summary>The test a <see cref="Branch" /> makes, or null for every other step and an <c>else</c>.</summary>
        static Comparison TestOf(Step step)
        {
            var branch = step as Branch;
            return branch?.Test;
        }

        /// <summary>The first step that runs a sequence which, directly or through the sequences it runs, starts this one again, or none.</summary>
        /// <remarks>Only <c>run</c> counts, because a sequence may <c>stop</c> itself. Asked when the program is checked, and on no tick.</remarks>
        static SequenceFault Circular(Sequence sequence)
        {
            var system = sequence.System;
            if (system == null || sequence.IsAlone)
                return SequenceFault.None;

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var run = steps[i] as Run;
                if (run != null && !run.Stops && !run.IsOff && Reaches(system, run.Number, sequence.Number, new List<int>()))
                    return new SequenceFault(SequenceProblem.Circular, i);
            }

            return SequenceFault.None;
        }

        /// <summary>Whether sequence <paramref name="from" />, or any it runs, is sequence <paramref name="to" />.</summary>
        static bool Reaches(AutomationSystem system, int from, int to, List<int> seen)
        {
            if (from == to)
                return true;

            if (seen.Contains(from))
                return false;

            seen.Add(from);
            var called = system.FindSequence(from);
            var steps = called?.Steps;
            for (var i = 0; steps != null && i < steps.Count; i++)
            {
                var run = steps[i] as Run;
                if (run != null && !run.Stops && !run.IsOff && Reaches(system, run.Number, to, seen))
                    return true;
            }

            return false;
        }

        /// <summary>The first step that has not been given a value its action or function needs, or none.</summary>
        static SequenceFault Unfilled(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var test = TestOf(sequence.Steps[i]);
                if (test != null)
                {
                    var read = SystemInstance.Through(through, test.Targets);
                    foreach (var source in read)
                    {
                        GridEntity entity = bindings.Entity(source);
                        var value = entity == null ? null : test.Names(entity.Kind.Actions);
                        var left = value == null || value.Being == null ? null : value.Being.Missing(test.Arguments.Count);
                        if (left != null)
                            return new SequenceFault(SequenceProblem.Unfilled, i, BlockTrouble.None, left);
                    }

                    continue;
                }

                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                foreach (var target in targets)
                {
                    GridEntity entity = bindings.Entity(target);
                    if (entity == null)
                        continue;

                    EntityAction doing = act.Names(entity.Kind.Actions);
                    if (doing == null)
                        continue;

                    var missing = doing.Missing(act.Arguments.Count);
                    if (missing != null)
                        return new SequenceFault(SequenceProblem.Unfilled, i, BlockTrouble.None, missing);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>The first step given a value its action takes as another kind, or none.</summary>
        static SequenceFault Mistyped(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var compared = new List<Literal>(1);
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var test = TestOf(sequence.Steps[i]);
                if (test != null)
                {
                    compared.Clear();
                    compared.Add(test.Value);
                    if (!TestTakes(bindings, test, through, compared))
                        return new SequenceFault(SequenceProblem.Mistyped, i);

                    continue;
                }

                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction || act.Arguments.Count == 0)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                foreach (var target in targets)
                {
                    GridEntity entity = bindings.Entity(target);
                    if (entity == null)
                        continue;

                    EntityAction doing = act.Names(entity.Kind.Actions);
                    var refused = doing?.Refused(act.Arguments);
                    if (refused != null)
                        return new SequenceFault(SequenceProblem.Mistyped, i, BlockTrouble.None, refused);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>
        /// Whether every block a test reads a member on takes what it is compared with as the kind the member is, and can be
        /// compared as the test compares it.
        /// </summary>
        static bool TestTakes(Bindings bindings, Comparison test, SystemInstance through, IList<Literal> compared)
        {
            var targets = SystemInstance.Through(through, test.Targets);
            foreach (var target in targets)
            {
                GridEntity entity = bindings.Entity(target);
                if (entity == null)
                    continue;

                EntityAction value = test.Names(entity.Kind.Actions);
                if (value != null && !value.Takes(compared))
                    return false;

                if (value != null && value.Being != null && value.Being.Refused(test.Arguments) != null)
                    return false;

                var offered = value != null;
                if (offered && Probe.Of(entity, test.Provider, test.MemberId, test.Arguments, test.Quantifier, test.Relation,
                        test.Value) == null)
                    return false;
            }

            return true;
        }

        /// <summary>The first step with a block lacking the facet <paramref name="through" /> chose of it in a slot, naming that block; or none.</summary>
        static SequenceFault MissingFacet(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            if (through == null)
                return SequenceFault.None;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var test = TestOf(sequence.Steps[i]);
                var act = sequence.Steps[i] as Act;
                var targets = test != null ? test.Targets : act?.Targets;
                if (targets == null)
                    continue;

                foreach (var name in SystemInstance.Through(through, targets))
                {
                    GridEntity entity = bindings.Entity(name);
                    if (entity == null)
                        continue;

                    var member = test != null ? test.Names(entity.Kind.Actions) : act.Names(entity.Kind.Actions);
                    if (member?.Type == null)
                        continue;

                    var number = through.FacetOf(targets, name);
                    if (number > 0 && member.Type.Find(entity.Kind, number) == null)
                        return new SequenceFault(SequenceProblem.MissingFacet, i, BlockTrouble.None, entity.Name);
                }
            }

            return SequenceFault.None;
        }

        /// <summary>The first step whose blocks do not offer what it does, or none.</summary>
        /// <remarks>Every block the step names has to offer it.</remarks>
        static SequenceFault Unoffered(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var test = TestOf(sequence.Steps[i]);
                if (test != null)
                {
                    var read = SystemInstance.Through(through, test.Targets);
                    foreach (var source in read)
                    {
                        GridEntity entity = bindings.Entity(source);
                        if (entity != null && test.Names(entity.Kind.Actions) == null)
                            return new SequenceFault(SequenceProblem.Unoffered, i);
                    }

                    continue;
                }

                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction)
                    continue;

                var targets = SystemInstance.Through(through, act.Targets);
                foreach (var target in targets)
                {
                    GridEntity entity = bindings.Entity(target);
                    if (entity != null && act.Names(entity.Kind.Actions) == null)
                        return new SequenceFault(SequenceProblem.Unoffered, i);
                }
            }

            return SequenceFault.None;
        }
    }
}
