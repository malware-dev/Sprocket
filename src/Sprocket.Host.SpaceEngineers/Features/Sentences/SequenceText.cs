using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Fields;

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

            Sequence sequence = system?.FindSequence(trigger.Runs);
            if (sequence == null)
                return trigger.Stops ? "stops a sequence that was deleted" : "runs a sequence that was deleted";

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
        public static string Trigger(Automation program, Bindings bindings, AutomationSystem system, Trigger trigger,
            SystemInstance through)
        {
            var moment = trigger as MomentTrigger;
            if (moment != null && moment.Sources.Count > 0)
            {
                var when = moment as WhenTrigger;
                var sources = SystemInstance.Through(through, moment.Sources);
                if (when != null && !Watched(bindings, when, sources))
                    return $"When {Compared(bindings, when.Provider, when.EventId, when.Quantifier, when.Relation, when.Value, sources)}";

                var all = when != null && when.Quantifier == Quantifier.All;
                var happening = Happening(bindings, Vocabulary.EventOf(program, bindings, system, moment, through), sources);
                return $"When {SentenceText.Named(bindings, sources, all)} {(all && sources.Count > 1 ? SentenceText.Plural(happening) : happening)}";
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return $"Every {TimeText.Rate(every.Interval)}";

            // A required trigger that names nothing: whatever answers it is on the ship's toolbars
            // and not in the system, so there is nothing to name and the line says that.
            return trigger is SignalTrigger ? "When something on this construct starts it" : SentenceText.Empty;
        }


        /// <summary>The words for the moment <paramref name="eventId" /> on <paramref name="sources" />, or an unfilled gap.</summary>
        static string Happening(Bindings bindings, string eventId, IList<string> sources)
        {
            var happening = Vocabulary.Find(Vocabulary.EventsFor(bindings, sources), eventId);
            return happening == null ? SentenceText.Empty : happening.Text;
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

        /// <summary>What stopped the last run of <paramref name="sequence" />, and at which step: "stopped at step 4 by Cycle Out".</summary>
        public static string CalledOff(Texts texts, Sequence sequence, SequenceRunner state)
        {
            var step = Row(sequence, state.StoppedAt).ToString(CultureInfo.InvariantCulture);
            if (state.StoppedBy == Stopper.Hand)
                return Template.Fill(texts.SequenceStoppedByHand, "step", step);

            if (state.StoppedBy == Stopper.Trigger)
                return Template.Fill(texts.SequenceStoppedByTrigger, "step", step);

            if (state.StoppedBy == Stopper.Switch)
                return Template.Fill(texts.SequenceStoppedBySwitch, "step", step);

            var system = sequence.System;
            var from = system?.FindSequence(state.StoppedFrom);
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

            if (fault.Problem == SequenceProblem.Unfilled && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemOptionMissing, "name", fault.Which);

            if (fault.Problem == SequenceProblem.Mistyped && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemOptionWrong, "name", fault.Which);

            if (fault.Problem == SequenceProblem.MissingFacet)
                return Template.Fill(texts.ProblemMissingScreen, "block", fault.Which);

            if (fault.Problem == SequenceProblem.MissingSource && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemSourceLost, "block", fault.Which);

            if ((fault.Problem == SequenceProblem.MissingTarget || fault.Problem == SequenceProblem.MissingGate) && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemBlockLost, "block", fault.Which);

            return fault.Because.Exists ? TroubleText.Of(texts, fault.Because, fault.Which) : Problem(texts, fault.Problem);
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
            if (why == Unwired.Cleared || why == Unwired.Taken)
                return Template.Fill(texts.ProblemButtonChanged, "block", block);

            return Template.Fill(texts.ProblemUnset, "block", block);
        }

        /// <summary>Whether <paramref name="halt" /> stopped a run because a block was lost.</summary>
        public static bool Lost(Halt halt)
        {
            return halt != null && !halt.Because.Exists && !string.IsNullOrEmpty(halt.Block)
                && (halt.Problem == SequenceProblem.MissingTarget || halt.Problem == SequenceProblem.MissingGate
                    || halt.Problem == SequenceProblem.MissingSource);
        }

        /// <summary>Why Sprocket switched off <paramref name="system" />, or one of it, as <paramref name="halt" /> recorded it, and that a lost block is back where <paramref name="restored" />.</summary>
        public static string Stopped(Texts texts, Halt halt, AutomationSystem system, bool restored)
        {
            if (halt == null || halt.Problem == SequenceProblem.None)
                return texts.StoppedUnknown;

            var sequence = system?.FindSequence(halt.Sequence);
            var row = Row(sequence, halt.Step);
            var step = row.ToString(CultureInfo.InvariantCulture);
            var reason = restored && Lost(halt)
                ? Template.Fill(texts.StoppedBlockRestored, "block", halt.Block)
                : halt.Because.Exists
                    ? TroubleText.Of(texts, halt.Because, halt.Block)
                    : Because(texts, halt.Problem, halt.Block);

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
            if (!string.IsNullOrEmpty(block) && (problem == SequenceProblem.MissingTarget || problem == SequenceProblem.MissingGate
                    || problem == SequenceProblem.MissingSource))
                return Template.Fill(texts.StoppedBlockGone, "block", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Adrift)
                return Template.Fill(texts.StoppedBlockReplaced, "block", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Unfilled)
                return Template.Fill(texts.StoppedOptionMissing, "name", block);
            if (!string.IsNullOrEmpty(block) && problem == SequenceProblem.Mistyped)
                return Template.Fill(texts.StoppedOptionWrong, "name", block);
            if (problem == SequenceProblem.MissingFacet)
                return Template.Fill(texts.StoppedMissingScreen, "block", block);

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

        /// <summary>The number a step's row shows, or 0 where there is none.</summary>
        /// <remarks>
        /// Counted as the sequence's screen numbers its rows: a wait for what is running is a row of its own, and the line closing an
        /// if has no number. Zero where the line has no row of its own to point at.
        /// </remarks>
        static int Row(Sequence sequence, int step)
        {
            if (sequence == null || step < 0 || step >= sequence.Steps.Count)
                return 0;

            if (sequence.Steps[step] is EndIf)
                return 0;

            var row = 0;
            for (var i = 0; i <= step; i++)
            {
                var line = sequence.Steps[i];
                if (!(line is EndIf))
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
            if (problem == SequenceProblem.NothingToAwait)
                return texts.ProblemNothingToAwait;
            if (problem == SequenceProblem.MissingGate)
                return texts.ProblemMissingGate;
            if (problem == SequenceProblem.Unoffered)
                return texts.ProblemUnoffered;
            if (problem == SequenceProblem.Unfilled)
                return texts.ProblemUnfilled;
            if (problem == SequenceProblem.Mistyped)
                return texts.ProblemMistyped;
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
                return $"wait {TimeText.Duration(wait.Length)}";

            var run = step as SpaceEngineers.Sequences.Run;
            if (run != null)
            {
                var system = sequence.System;
                var called = system?.FindSequence(run.Number);
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

        /// <summary>A test as its row reads: the blocks, the value unless it is their status, and how it is compared with what.</summary>
        static string Tested(Bindings bindings, Comparison test, SystemInstance through)
        {
            var targets = SystemInstance.Through(through, test.Targets);
            var blocks = SentenceText.Named(bindings, targets, test.Quantifier != Quantifier.Any);
            var value = EntityAction.Value(Vocabulary.ActionsFor(bindings, targets), test.Provider, test.MemberId);
            if (value == null)
                return $"{SentenceText.Empty} on {blocks}";

            var fields = Comparing.Fields(value, Vocabulary.EventsFor(bindings, targets));
            var given = SentenceText.Given(new ArgumentsSlot(new[] { Comparing.Of(test.Relation), test.Value }, fields, null,
                Given.Checked, test.Quantifier == Quantifier.All && targets.Count > 1));
            var read = value.IsStatus ? blocks : $"{SentenceText.Value(value)} on {blocks}";
            return $"{(targets.Count > 1 ? $"{SentenceText.Quantified(test.Quantifier)} {read}" : read)} {given}";
        }

        /// <summary>
        /// A member compared on some blocks, as a sentence reads it: the blocks and the state they are read for, for one of
        /// Sprocket's own, or the value on the blocks and what it is compared with.
        /// </summary>
        static string Compared(Bindings bindings, string provider, string id, Quantifier quantifier, Relation relation,
            Literal compared, IList<string> targets)
        {
            var blocks = SentenceText.Named(bindings, targets, quantifier != Quantifier.Any);
            if (provider == BlockKinds.Own)
            {
                bool inState;
                var state = Vocabulary.Change(Vocabulary.EventsFor(bindings, targets), id, relation, compared, out inState);
                if (state != null)
                {
                    var said = inState ? state.State : state.Unstate;
                    return $"{blocks} {(quantifier == Quantifier.All && targets.Count > 1 ? SentenceText.Plural(said) : said)}";
                }
            }

            var value = EntityAction.Value(Vocabulary.ActionsFor(bindings, targets), provider, id);

            if (value == null)
                return $"{SentenceText.Empty} on {blocks}";

            var fields = new List<Field>();
            value.Fields(fields);
            var read = $"{SentenceText.Value(value)} on {blocks}";
            return $"{(quantifier == Quantifier.Sum || quantifier == Quantifier.Average ? $"{SentenceText.Quantified(quantifier)} {read}" : read)} {Compared(relation)} {SentenceText.Given(new ArgumentsSlot(new[] { compared }, fields, null))}";
        }

        /// <summary>Whether <paramref name="when" /> is a value of Sprocket's own coming to be what one of its signals says it becomes, which reads as that signal.</summary>
        static bool Watched(Bindings bindings, WhenTrigger when, IList<string> sources)
        {
            bool inState;
            return when.Provider == BlockKinds.Own
                   && Vocabulary.Change(Vocabulary.EventsFor(bindings, sources), when.EventId, when.Relation, when.Value, out inState) != null
                   && inState;
        }

        /// <summary>How a comparison reads between a value and what it is compared with.</summary>
        static string Compared(Relation relation)
        {
            switch (relation)
            {
                case Relation.IsNot: return "is not";
                case Relation.Above: return "is above";
                case Relation.AtLeast: return "is at least";
                case Relation.Below: return "is below";
                case Relation.AtMost: return "is at most";
            }

            return "is";
        }


        static string Action(Bindings bindings, Act step, SystemInstance through)
        {
            if (step.Targets.Count == 0)
                return SentenceText.Empty;

            var targets = SystemInstance.Through(through, step.Targets);
            EntityAction named = step.Names(Vocabulary.ActionsFor(bindings, targets));
            var blocks = SentenceText.Named(bindings, targets, true);
            if (named == null)
                return $"{SentenceText.Empty} {blocks}";

            // The values in the words the step shows them in: a choice as its word, three numbers as
            // one colour. After what it acts on, because the blocks are what somebody is looking for
            // in a list of steps and the values are the detail.
            var fields = new List<Field>();
            named.Fields(fields);
            var slot = new ArgumentsSlot(step.Arguments, fields, null);
            var given = SentenceText.Given(slot);

            var verb = step.Sets ? SentenceText.Switching(named, step.Arguments) : "";
            if (verb.Length != 0)
                return SentenceText.Switched(verb, SentenceText.Value(named), blocks);

            if (step.Sets)
                return SentenceText.Setting(SentenceText.Value(named), blocks, given);

            return step.Arguments.Count == 0 || slot.Quiet ? $"{named.Text} {blocks}" : $"{named.Text} {blocks} {given}";
        }
    }
}
