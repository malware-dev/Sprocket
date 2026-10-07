using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Text;
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
    /// of it - including the word order, which is the part core must never own. A line written the
    /// way the program writes it stays in the program's words; everything else is said in the
    /// player's language.
    /// </remarks>
    public static class SequenceText
    {
        /// <summary>The program's own word for a sequence nobody has named, in the lines written as the program writes them.</summary>
        const string Written = "Sequence ";


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

            return (trigger.Stops ? "stops " : "runs ") + (sequence.HasName ? sequence.Name : Written + sequence.Number);
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
                    when != null && when.RequiresAll) + " " + Happening(bindings, Vocabulary.EventOf(null, bindings, null, moment, through), sources);
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + TimeText.Rate(every.IntervalTicks);

            // A required trigger that names nothing: whatever answers it is on the ship's toolbars
            // and not in the system, so there is nothing to name and the line says that.
            return trigger is SignalTrigger ? "When something on this construct starts it" : SentenceText.Empty;
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
        public static string DefaultName(Texts texts, int number)
        {
            return Template.Fill(texts.SequenceNumbered, "number", number.ToString(CultureInfo.InvariantCulture));
        }


        /// <summary>What a sequence is called: its name, or its number where it has none.</summary>
        public static string Called(Texts texts, Sequence sequence)
        {
            return sequence.HasName ? sequence.Name : DefaultName(texts, sequence.Number);
        }


        /// <summary>What <paramref name="status" /> is said as, in <paramref name="texts" />.</summary>
        public static string Status(Texts texts, SequenceStatus status)
        {
            if (status == SequenceStatus.Off)
                return texts.SequenceOff;
            if (status == SequenceStatus.Running)
                return texts.SequenceRunning;
            if (status == SequenceStatus.RunningWaiting)
                return texts.SequenceRunningWaiting;
            if (status == SequenceStatus.Stopped)
                return texts.SequenceStopped;

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
            return texts.SequenceReady;
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
        static string Wants(Texts texts, int values)
        {
            if (values <= 1)
                return texts.PickWantsOne;

            return Template.Fill(texts.PickWantsMany, "count", Counted(texts, values));
        }

        /// <summary>
        /// The same, said as what is actually being asked for where somebody has described it.
        /// </summary>
        /// <remarks>
        /// A count is what can be said when the game is the only source, and it stops being the whole
        /// truth the moment a description says these three values are one colour. Then the form shows
        /// one control and asking for three values would be counting something nobody can see.
        /// </remarks>
        public static string Wants(Texts texts, IList<Field> fields)
        {
            if (fields.Count == 1 && fields[0].Edits == Editor.Colour)
                return texts.PickWantsColor;

            return Wants(texts, fields.Count);
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
        public static string Starts(Texts texts)
        {
            return texts.SequenceRunNow;
        }

        /// <summary>The same button, facing the other way.</summary>
        public static string Stops(Texts texts)
        {
            return texts.SequenceStopNow;
        }

        /// <summary>The row that opens the field, which says what pressing it is for.</summary>
        public static string Giving()
        {
            return "Say what to give it";
        }

        static string Counted(Texts texts, int values)
        {
            if (values == 2)
                return texts.NumberTwo;
            if (values == 3)
                return texts.NumberThree;
            if (values == 4)
                return texts.NumberFour;

            return values.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>What stopped the last run of <paramref name="sequence" />, and at which step: "stopped at step 4 by Cycle Out".</summary>
        public static string CalledOff(Texts texts, Sequence sequence, SequenceRunner state)
        {
            var step = Row(sequence, state.StoppedAt).ToString(CultureInfo.InvariantCulture);
            if (state.StoppedBy == Stopper.Hand)
                return Template.Fill(texts.SequenceStoppedByHand, "step", step);

            if (state.StoppedBy == Stopper.Trigger)
                return Template.Fill(texts.SequenceStoppedByTrigger, "step", step);

            var system = sequence.System;
            var from = system == null ? null : system.FindSequence(state.StoppedFrom);
            return Fill(texts.SequenceStoppedBySequence, "step", step, "sequence",
                from == null ? DefaultName(texts, state.StoppedFrom) : Called(texts, from));
        }

        /// <summary>What is wrong with <paramref name="sequence" />, letting whatever is at fault speak for itself where it can.</summary>
        public static string Problem(Texts texts, SequenceFault fault, Sequence sequence)
        {
            if (fault.Problem == SequenceProblem.EmptySlot)
                return Template.Fill(texts.ProblemEmptySlot, "slot", fault.Which);

            if (fault.Problem == SequenceProblem.NeverTriggered)
                return Unstarted(texts, fault.Unwired, fault.Which);

            if (fault.Problem == SequenceProblem.Unable && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemBlockOff, "block", fault.Which);

            if (fault.Problem == SequenceProblem.Stalled && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemBlockStalled, "block", fault.Which);

            return fault.Because.Exists ? TroubleText.Of(texts, fault.Because) : Problem(texts, fault.Problem);
        }

        /// <summary>What is wrong with <paramref name="sequence" /> and where, for a line about the whole of its logic system.</summary>
        public static string Failing(Texts texts, SequenceFault fault, Sequence sequence)
        {
            // A trigger's own act has no steps to count, and no name.
            var problem = Problem(texts, fault, sequence);
            if (sequence.IsAlone)
                return problem;

            var row = Row(sequence, fault.Step);
            var step = row.ToString(CultureInfo.InvariantCulture);
            if (sequence.HasName)
            {
                return row > 0
                    ? Fill(texts.ProblemAtNamedStep, "step", step, "problem", problem, "sequence", sequence.Name)
                    : Fill(texts.ProblemInSequence, "problem", problem, "sequence", sequence.Name);
            }

            return row > 0 ? Fill(texts.ProblemAtStep, "step", step, "problem", problem) : problem;
        }

        /// <summary>Why nothing can start a sequence, naming the block whose button it is where that is known.</summary>
        static string Unstarted(Texts texts, Unwired why, string block)
        {
            if (why == Unwired.NoBlock)
                return texts.ProblemNoBlock;
            if (why == Unwired.NoMoment)
                return string.IsNullOrEmpty(block)
                    ? texts.ProblemNoMoment
                    : Template.Fill(texts.ProblemNoMomentOf, "block", block);
            if (string.IsNullOrEmpty(block))
                return texts.ProblemNeverTriggered;
            if (why == Unwired.Cleared)
                return Template.Fill(texts.ProblemCleared, "block", block);
            if (why == Unwired.Taken)
                return Template.Fill(texts.ProblemTaken, "block", block);

            return Template.Fill(texts.ProblemUnset, "block", block);
        }

        /// <summary>Why Sprocket switched off <paramref name="system" />, or one of it, as <paramref name="halt" /> recorded it.</summary>
        public static string Stopped(Texts texts, Halt halt, AutomationSystem system)
        {
            if (halt == null || halt.Problem == SequenceProblem.None)
                return texts.StoppedUnknown;

            var sequence = system == null ? null : system.FindSequence(halt.Sequence);
            var row = Row(sequence, halt.Step);
            var step = row.ToString(CultureInfo.InvariantCulture);
            var reason = halt.Because.Exists ? TroubleText.Of(texts, halt.Because) : Because(texts, halt.Problem, halt.Block);

            if (sequence == null || !sequence.HasName)
                return row > 0 ? Fill(texts.StoppedAtStep, "step", step, "reason", reason) : Template.Fill(texts.StoppedPlain, "reason", reason);

            return row > 0
                ? Fill(texts.StoppedAtStepOf, "step", step, "reason", reason, "sequence", sequence.Name)
                : Fill(texts.StoppedIn, "reason", reason, "sequence", sequence.Name);
        }

        static string Because(Texts texts, SequenceProblem problem, string block)
        {
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.EmptySlot)
                return Template.Fill(texts.StoppedEmptySlot, "slot", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.NeverTriggered)
                return Template.Fill(texts.StoppedButtonFailed, "block", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.MissingTarget)
                return Template.Fill(texts.StoppedBlockGone, "block", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Adrift)
                return Template.Fill(texts.StoppedBlockReplaced, "block", block);

            if (problem == SequenceProblem.NeverTriggered)
                return texts.StoppedNeverTriggered;
            if (problem == SequenceProblem.MissingSource)
                return texts.StoppedMissingSource;
            if (problem == SequenceProblem.MissingTarget)
                return texts.StoppedMissingTarget;
            if (problem == SequenceProblem.MissingGate)
                return texts.StoppedMissingGate;
            if (problem == SequenceProblem.Unoffered)
                return texts.StoppedUnoffered;
            if (problem == SequenceProblem.Unfilled)
                return texts.StoppedUnfilled;
            if (problem == SequenceProblem.Mistyped)
                return texts.StoppedMistyped;
            if (problem == SequenceProblem.Unawaited)
                return texts.StoppedUnawaited;
            if (problem == SequenceProblem.Circular)
                return texts.StoppedCircular;
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Unable)
                return Template.Fill(texts.StoppedBlockOff, "block", block);
            if (problem == SequenceProblem.Unable)
                return texts.StoppedUnable;
            if (problem == SequenceProblem.Adrift)
                return texts.StoppedAdrift;

            return texts.StoppedFailed;
        }

        static string Fill(string template, string name, string value, string other, string said)
        {
            return Template.Fill(Template.Fill(template, name, value), other, said);
        }

        static string Fill(string template, string name, string value, string other, string said, string last, string lastSaid)
        {
            return Template.Fill(Fill(template, name, value, other, said), last, lastSaid);
        }

        /// <summary>The number a step's row shows, counting past the waits for what a step starts, or 0 where there is none.</summary>
        /// <remarks>
        /// Counted as the sequence's screen numbers its rows: a wait for a step is part of the step's row, and the line closing an
        /// if has no number. Zero where the line has no row of its own to point at.
        /// </remarks>
        static int Row(Sequence sequence, int step)
        {
            if (sequence == null || step < 0 || step >= sequence.Steps.Count)
                return 0;

            while (step >= 0 && sequence.Steps[step] is Await)
                step--;

            if (step < 0 || sequence.Steps[step] is EndIf)
                return 0;

            var row = 0;
            for (var i = 0; i <= step; i++)
            {
                if (!(sequence.Steps[i] is Await) && !(sequence.Steps[i] is EndIf))
                    row++;
            }

            return row;
        }

        /// <summary>What <paramref name="problem" /> is said as, in <paramref name="texts" />.</summary>
        public static string Problem(Texts texts, SequenceProblem problem)
        {
            if (problem == SequenceProblem.MissingSource)
                return texts.ProblemMissingSource;
            if (problem == SequenceProblem.MissingTarget)
                return texts.ProblemMissingTarget;
            if (problem == SequenceProblem.NeverTriggered)
                return texts.ProblemNeverTriggered;
            if (problem == SequenceProblem.NothingStarts)
                return texts.ProblemNothingStarts;
            if (problem == SequenceProblem.MissingGate)
                return texts.ProblemMissingGate;
            if (problem == SequenceProblem.Unoffered)
                return texts.ProblemUnoffered;
            if (problem == SequenceProblem.Unfilled)
                return texts.ProblemUnfilled;
            if (problem == SequenceProblem.Mistyped)
                return texts.ProblemMistyped;
            if (problem == SequenceProblem.Unawaited)
                return texts.ProblemUnawaited;
            if (problem == SequenceProblem.Stuck)
                return texts.ProblemStuck;
            if (problem == SequenceProblem.Conflicted)
                return texts.ProblemConflicted;
            if (problem == SequenceProblem.Circular)
                return texts.ProblemCircular;
            if (problem == SequenceProblem.Unable)
                return texts.ProblemUnable;
            if (problem == SequenceProblem.Stalled)
                return texts.ProblemStalled;

            return "";
        }


        /// <summary>One numbered line of the sequence, in the words its row reads in; the line closing an if says nothing.</summary>
        public static string Step(Bindings bindings, Sequence sequence, Step step, SystemInstance through)
        {
            var wait = step as Wait;
            if (wait != null)
                return "wait " + TimeText.Duration(wait.Ticks);

            var run = step as SpaceEngineers.Sequences.Run;
            if (run != null)
            {
                var system = sequence.System;
                var called = system == null ? null : system.FindSequence(run.Number);
                return (run.Stops ? "stop " : "run ") + (called == null || !called.HasName
                    ? Written + run.Number
                    : called.Name);
            }

            var branch = step as Branch;
            if (branch != null)
            {
                if (branch.Test == null)
                    return "otherwise";

                return (step is If ? "if " : "otherwise if ") + Tested(bindings, branch.Test, through);
            }

            if (step is EndIf)
                return "";

            var act = step as Act;
            return act == null ? "wait for it to finish" : Action(bindings, act, through);
        }

        /// <summary>A test as its row reads: the blocks and the state they are read for, or the value on the blocks and what it is compared with.</summary>
        static string Tested(Bindings bindings, Comparison test, SystemInstance through)
        {
            var targets = SystemInstance.Through(through, test.Targets);
            var blocks = SentenceText.Named(bindings, targets, test.RequiresAll);
            if (test.IsMoment)
            {
                var events = Vocabulary.EventsFor(bindings, targets);
                for (var i = 0; i < events.Count; i++)
                {
                    if (events[i].Id == test.MemberId && events[i].IsState)
                        return blocks + " " + (test.Value.Flag ? events[i].State : events[i].Unstate);
                }

                return blocks + " " + SentenceText.Empty;
            }

            EntityAction value = test.Names(Vocabulary.ActionsFor(bindings, targets));
            if (value == null)
                return SentenceText.Empty + " on " + blocks;

            var fields = new List<Field>();
            value.Fields(fields);
            return value.Named + " on " + blocks + " is " + SentenceText.Given(new ValuesSlot(new[] { test.Value }, fields, null));
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
            var slot = new ValuesSlot(step.Values, fields, null);
            var given = SentenceText.Given(slot);

            if (step.Sets)
                return SentenceText.Setting(named.Named, blocks, given);

            return step.Values.Count == 0 || slot.Quiet ? named.Text + " " + blocks : named.Text + " " + blocks + " " + given;
        }
    }
}
