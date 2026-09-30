using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Values;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// The words for a sequence where a person can only read it: a list row, a heading, a caption.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="SentenceText" />. A sentence is slots because every part of one
    /// is somebody's to change; these are labels, so they are one string each and this decides all
    /// of it - including the word order, which is the part core must never own.
    /// </remarks>
    public static class SequenceText
    {


        /// <summary>
        /// What a trigger does: run a sequence, stop one, or the one thing it does itself.
        /// </summary>
        /// <remarks>
        /// A named sequence is said by name, because the name is what somebody chose for it and a
        /// list of triggers is a list of what they set up. A trigger that does one thing has nothing
        /// to name, so its own sentence is the answer - "open Airlock Inner Door" - which is also
        /// what it reads as in the construct's own wiring, where it ends up.
        /// <para>
        /// Its blocks are named as <paramref name="through" /> fills them, or as written where that is null.
        /// </para>
        /// </remarks>
        public static string Runs(Bindings bindings, AutomationSystem system, Trigger trigger, SystemInstance through)
        {
            var act = trigger.Acts;
            if (act != null)
                return Action(bindings, act, through);

            Sequence sequence = system == null ? null : system.FindSequence(trigger.Runs);
            if (sequence == null)
                return trigger.Stops ? "stops a sequence that is gone" : "runs a sequence that is gone";

            return (trigger.Stops ? "stops " : "runs ") + (sequence.HasName ? sequence.Name : DefaultName(sequence.Number));
        }


        /// <summary>
        /// The whole first line: "When the hangar door finishes opening".
        /// </summary>
        /// <remarks>
        /// The same line whether Sprocket watches for the moment or the construct announces it. That
        /// difference is real and it is not a difference in what somebody asked for, so it is not
        /// something to read about on every row.
        /// <para>
        /// Its blocks are named as <paramref name="through" /> fills them, or as written where that is null.
        /// </para>
        /// </remarks>
        public static string Trigger(Bindings bindings, Trigger trigger, SystemInstance through)
        {
            var moment = trigger as MomentTrigger;
            if (moment != null && moment.Sources.Count > 0)
            {
                var when = moment as WhenTrigger;
                var sources = SystemInstance.Through(through, moment.Sources);
                return "When " + SentenceText.Named(bindings, sources,
                    when != null && when.RequiresAll) + " " + Happening(bindings, Vocabulary.EventOf(moment, through), sources);
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + TimeText.Rate(every.IntervalTicks);

            // A required trigger that names nothing: whatever answers it is on the ship's toolbars
            // and not in the system, so there is nothing to name and the line says that.
            return trigger is RequiredTrigger ? "When something on this ship says so" : SentenceText.Empty;
        }


        /// <summary>The words for the moment <paramref name="eventId" /> on <paramref name="sources" />, or an unfilled gap.</summary>
        static string Happening(Bindings bindings, string eventId, IList<string> sources)
        {
            var events = Vocabulary.EventsFor(bindings, sources);
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == eventId)
                    return events[i].Text;
            }

            return SentenceText.Empty;
        }


        /// <summary>What to call a sequence nobody has renamed: "Sequence 3".</summary>
        /// <remarks>
        /// Numbered, not described. A row worded out of what the sequence does reads well and makes
        /// a row's identity change as somebody edits it. The game names a block and lets the name be
        /// overtyped, and everybody already knows that.
        /// </remarks>
        public static string DefaultName(int number)
        {
            return "Sequence " + number;
        }


        /// <summary>What a sequence is called: its name, or its number where it has none.</summary>
        public static string Called(Sequence sequence)
        {
            return sequence.HasName ? sequence.Name : DefaultName(sequence.Number);
        }


        public static string Status(SequenceStatus status)
        {
            if (status == SequenceStatus.Off)
                return "switched off";
            if (status == SequenceStatus.Running)
                return "running";
            if (status == SequenceStatus.RunningWaiting)
                return "running - waiting for this step";

            // Not "waiting", which is what a sequence held mid-run says two lines above. The same
            // word for a sequence doing nothing and for one stopped part-way through is the one
            // reading somebody most needs to tell apart.
            //
            // Ready rather than idle, because a sequence with a trigger is not merely doing
            // nothing: it is loaded and pointed at a button, and whether that button will work is
            // what somebody who has just built one is asking.
            // Both, and the same word, because the difference between them is not known. Having
            // never run is worth saying and would be said here - but only a run in progress is
            // written to the save, so after a world is loaded every sequence looks like one that
            // has never fired, including the one that has cycled an airlock every day for a month.
            return "ready";
        }


        /// <summary>
        /// How many values an action is waiting for, said as a sentence.
        /// </summary>
        /// <remarks>
        /// The count and nothing else, because the count is all the game declares: an action says
        /// how many values it wants and never what they are for. Saying more would be Sprocket
        /// inventing knowledge it does not have, on the screen where somebody is deciding what to
        /// type.
        /// </remarks>
        static string Wants(int values)
        {
            if (values <= 1)
                return "This needs one value.";

            return "This needs " + Counted(values) + " values.";
        }

        /// <summary>
        /// The same, said as what is actually being asked for where somebody has described it.
        /// </summary>
        /// <remarks>
        /// A count is what can be said when the game is the only source, and it stops being the whole
        /// truth the moment a description says these three values are one colour. Then the form shows
        /// one control and asking for three values would be counting something nobody can see.
        /// </remarks>
        public static string Wants(IList<Field> fields)
        {
            if (fields.Count == 1 && fields[0].Edits == Editor.Colour)
                return "This needs a colour.";

            return Wants(fields.Count);
        }

        /// <summary>What the picker's column of values is headed.</summary>
        public static string ValuesTitle()
        {
            return "Values";
        }

        /// <summary>
        /// What the one button that starts and stops a sequence promises, whichever way it faces.
        /// </summary>
        /// <remarks>
        /// Two screens carry that button and it is the same button on both, so the words are here
        /// rather than written out in each view. Starting says what it is <em>not</em> - a preview -
        /// because "run" and "play" are not the same promise; stopping says what it leaves behind,
        /// because a machine halfway through a cycle stays halfway through it.
        /// </remarks>
        public static string Starts()
        {
            return "Run it now, as its trigger would";
        }

        public static string Stops()
        {
            return "Stop it, leaving everything where it is";
        }

        /// <summary>The row that opens the field, which says what pressing it is for.</summary>
        public static string Giving()
        {
            return "Say what to give it";
        }

        static string Counted(int values)
        {
            if (values == 2)
                return "two";
            if (values == 3)
                return "three";
            if (values == 4)
                return "four";

            return values.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>What is wrong with <paramref name="sequence" />, letting whatever is at fault speak for itself where it can.</summary>
        internal static string Problem(SequenceFault fault, Sequence sequence)
        {
            if (fault.Problem == SequenceProblem.EmptySlot)
                return "The " + fault.Which + " slot is empty. Fill it on the configuration panel.";

            if (fault.Problem == SequenceProblem.NeverTriggered)
                return Unstarted(fault.Unwired, fault.Which);

            if (fault.Problem == SequenceProblem.Unable && !string.IsNullOrEmpty(fault.Which))
                return fault.Which + " is switched off. Switch it on first.";

            if (fault.Problem == SequenceProblem.Stalled && !string.IsNullOrEmpty(fault.Which))
                return fault.Which + " isn't working, so this waits for it.";

            return string.IsNullOrEmpty(fault.Because) ? Problem(fault.Problem) : fault.Because;
        }

        /// <summary>What is wrong with <paramref name="sequence" /> and where, for a line about the whole of its logic system.</summary>
        internal static string Failing(SequenceFault fault, Sequence sequence)
        {
            // A trigger's own act has no steps to count, and no name.
            if (sequence.IsAlone)
                return Problem(fault, sequence);

            var row = Row(sequence, fault.Step);
            var named = sequence.HasName ? sequence.Name : "";
            var at = row > 0 ? (named.Length > 0 ? ", step " : "Step ") + row.ToString(CultureInfo.InvariantCulture) : "";

            return named + at + (named.Length + at.Length > 0 ? ": " : "") + Problem(fault, sequence);
        }

        /// <summary>Why nothing can start a sequence, naming the block whose button it is where that is known.</summary>
        static string Unstarted(Unwired why, string block)
        {
            if (why == Unwired.NoBlock)
                return "No block starts this. Choose one.";
            if (why == Unwired.NoMoment)
                return string.IsNullOrEmpty(block)
                    ? "Choose what the block must do to start this."
                    : "Choose what " + block + " must do to start this.";
            if (string.IsNullOrEmpty(block))
                return "Nothing can start this.";
            if (why == Unwired.Cleared)
                return block + "'s button was cleared, so it can't start this.";
            if (why == Unwired.Taken)
                return block + "'s button does something else, so it can't start this.";

            return block + "'s button isn't set to start this.";
        }

        /// <summary>Why Sprocket switched off <paramref name="system" />, or one of it, as <paramref name="halt" /> recorded it.</summary>
        public static string Stopped(Halt halt, AutomationSystem system)
        {
            if (halt == null || halt.Problem == SequenceProblem.None)
                return "Stopped for an unknown reason.";

            var sequence = system == null ? null : system.FindSequence(halt.Sequence);
            var row = Row(sequence, halt.Step);
            var at = row > 0 ? " at step " + row.ToString(CultureInfo.InvariantCulture) : "";
            var of = sequence == null || !sequence.HasName ? "" : (row > 0 ? " of " : " in ") + sequence.Name;

            return "Stopped" + at + of + ": " + (string.IsNullOrEmpty(halt.Said) ? Because(halt.Problem, halt.Block) : halt.Said);
        }

        static string Because(SequenceProblem problem, string block)
        {
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.EmptySlot)
                return "the " + block + " slot was empty.";
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.NeverTriggered)
                return block + "'s button couldn't start it.";
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.MissingTarget)
                return block + " was gone.";
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Adrift)
                return block + " may have been replaced.";

            if (problem == SequenceProblem.NeverTriggered)
                return "nothing could start it.";
            if (problem == SequenceProblem.MissingSource)
                return "a block that starts it was gone.";
            if (problem == SequenceProblem.MissingTarget)
                return "a block it acts on was gone.";
            if (problem == SequenceProblem.MissingGate)
                return "a block it waits on was gone.";
            if (problem == SequenceProblem.Unoffered)
                return "its blocks no longer offered the action.";
            if (problem == SequenceProblem.Unfilled)
                return "a value was missing.";
            if (problem == SequenceProblem.Mistyped)
                return "a value didn't fit the action.";
            if (problem == SequenceProblem.Unawaited)
                return "it didn't wait for what it started.";
            if (problem == SequenceProblem.Circular)
                return "it would have started itself again.";
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Unable)
                return block + " was switched off.";
            if (problem == SequenceProblem.Unable)
                return "a block it acts on was switched off.";
            if (problem == SequenceProblem.Adrift)
                return "a block may have been replaced.";

            return "it could not work.";
        }

        /// <summary>The number a step's row shows, counting past the waits for what a step starts, or 0 where there is none.</summary>
        static int Row(Sequence sequence, int step)
        {
            if (sequence == null || step < 0 || step >= sequence.Steps.Count || sequence.Steps[step] is Await)
                return 0;

            var row = 0;
            for (var i = 0; i <= step; i++)
            {
                if (!(sequence.Steps[i] is Await))
                    row++;
            }

            return row;
        }

        public static string Problem(SequenceProblem problem)
        {
            if (problem == SequenceProblem.MissingSource)
                return "This listens to something that isn't here any more.";
            if (problem == SequenceProblem.MissingTarget)
                return "This affects something that isn't here any more.";
            if (problem == SequenceProblem.NeverTriggered)
                return "Nothing can start this.";
            if (problem == SequenceProblem.NothingStarts)
                return "Nothing starts this yet.";
            if (problem == SequenceProblem.MissingGate)
                return "This waits on something that isn't here any more.";
            if (problem == SequenceProblem.Unoffered)
                return "This does something these blocks no longer offer. Replace this step.";
            if (problem == SequenceProblem.Unfilled)
                return "This step is missing a value it needs. Fill it in.";
            if (problem == SequenceProblem.Mistyped)
                return "This step has a value its action can't take. Fill it in again.";
            if (problem == SequenceProblem.Unawaited)
                return "This step doesn't wait for what it starts, which this version can't show. Replace this step.";
            if (problem == SequenceProblem.Stuck)
                return "This is waiting for something that cannot finish.";
            if (problem == SequenceProblem.Conflicted)
                return "This stopped: something else took over one of its blocks.";
            if (problem == SequenceProblem.Circular)
                return "This runs a sequence that runs this one again. Remove one of the two.";
            if (problem == SequenceProblem.Unable)
                return "A block this step acts on is switched off. Switch it on first.";
            if (problem == SequenceProblem.Stalled)
                return "A block this waits for isn't working, so this waits for it.";

            return "";
        }


        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Three sorts of line, because a sequence has three: something done to some blocks, waiting
        /// for what has been started, and spending time.
        /// </remarks>
        public static string Step(Bindings bindings, Sequence sequence, Step step, SystemInstance through)
        {
            var wait = step as Wait;
            if (wait != null)
                return "wait " + TimeText.Duration(wait.Ticks);

            var run = step as Run;
            if (run != null)
            {
                var system = sequence.System;
                var called = system == null ? null : system.FindSequence(run.Number);
                return (run.Stops ? "stop " : "run ") + (called == null || !called.HasName
                    ? DefaultName(run.Number)
                    : called.Name);
            }

            var act = step as Act;
            return act == null ? "wait for it to finish" : Action(bindings, act, through);
        }


        static string Action(Bindings bindings, Act step, SystemInstance through)
        {
            if (step.Targets.Count == 0)
                return SentenceText.Empty;

            var targets = SystemInstance.Through(through, step.Targets);
            EntityAction named = step.Names(Vocabulary.ActionsFor(bindings, targets));
            var blocks = SentenceText.Named(bindings, targets, true);
            if (named == null)
                return SentenceText.Empty + " " + blocks;

            // The values in the words the step shows them in: a choice as its word, three numbers as
            // one colour. After what it acts on, because the blocks are what somebody is looking for
            // in a list of steps and the values are the detail.
            var fields = new List<Field>();
            named.Fields(fields);
            var given = SentenceText.Given(new ValuesSlot(step.Values, fields, null));

            if (step.Sets)
                return SentenceText.Setting(named.Named, blocks, given);

            return step.Values.Count == 0 ? named.Text + " " + blocks : named.Text + " " + blocks + " " + given;
        }
    }
}
