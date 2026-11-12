using Sprocket.Host.SpaceEngineers.Repairs;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// The words for a sequence where a person can only read it: a list row, a heading, a caption.
    /// </summary>
    public static class SequenceText
    {
        // The program's own word for a sequence nobody has named.
        const string Written = "Sequence ";


        /// <summary>
        /// What a trigger does: run a sequence, stop one, or the one thing it does itself.
        /// </summary>
        /// <remarks>
        /// Its blocks are named as <paramref name="through" /> fills them, or as written where that is null.
        /// </remarks>
        public static string Runs(Bindings bindings, AutomationSystem system, Trigger trigger, SystemInstance through)
        {
            var act = trigger.Acts;
            if (act != null)
                return Action(bindings, act, through, false);

            Sequence sequence = system?.FindSequence(trigger.Runs);
            if (sequence == null)
                return trigger.Stops ? "stops a sequence that was deleted" : "runs a sequence that was deleted";

            return (trigger.Stops ? "stops " : "runs ") + (sequence.HasName ? sequence.Name : Written + sequence.Number);
        }


        /// <summary>
        /// The whole first line: "When the hangar door finishes opening".
        /// </summary>
        /// <remarks>
        /// Its blocks are found through <paramref name="bindings" /> as <paramref name="through" /> fills the slots, or as written
        /// where that is null, and named by those blocks, or by the slots' own names where <paramref name="slotted" />, as a row
        /// names them.
        /// </remarks>
        public static string Trigger(Automation program, Bindings bindings, AutomationSystem system, Trigger trigger,
            SystemInstance through, bool slotted = false)
        {
            var moment = trigger as MomentTrigger;
            if (moment != null && moment.Sources.Count > 0)
            {
                var when = moment as WhenTrigger;
                var sources = SystemInstance.Through(through, moment.Sources);
                var named = slotted ? moment.Sources : null;
                if (when != null && !Watched(bindings, when, sources))
                    return $"When {Compared(bindings, when, sources, named)}";

                var all = when != null && when.Quantifier == Quantifier.All;
                var happening = Happening(bindings, Vocabulary.EventOf(program, bindings, system, moment, through), sources);
                return $"When {Named(bindings, sources, all, named)} {(all && sources.Count > 1 ? SentenceText.Plural(happening) : happening)}";
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return $"Every {TimeText.Rate(every.Interval)}";

            // A signal trigger naming nothing is answered from the construct's toolbars, so the line says so.
            return trigger is SignalTrigger ? "When something on this construct starts it" : SentenceText.Empty;
        }


        /// <summary>The words for the moment <paramref name="eventId" /> on <paramref name="sources" />, or an unfilled gap.</summary>
        static string Happening(Bindings bindings, string eventId, IList<string> sources)
        {
            var happening = Vocabulary.Find(Vocabulary.EventsFor(bindings, sources), eventId);
            return happening == null ? SentenceText.Empty : happening.Text;
        }


        /// <summary>What to call a sequence nobody has renamed: "Sequence 3".</summary>
        public static string DefaultName(Texts texts, int number)
        {
            return Template.Fill(texts.SequenceNumbered, "number", number.ToString(CultureInfo.InvariantCulture));
        }


        /// <summary>The name a new sequence of <paramref name="system" /> is given: "Sequence 4", the first number free, counting on from the sequences there are.</summary>
        public static string NewName(Texts texts, AutomationSystem system)
        {
            var sequences = system.Sequences;
            return NumberedName.Free(texts.SequenceNumbered, sequences.Count, wanted =>
            {
                foreach (var sequence in sequences)
                {
                    if (sequence.Name == wanted)
                        return true;
                }

                return false;
            });
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

            // One word for idle and for never run: only a run in progress is written to the save, so
            // after a load the two cannot be told apart.
            return texts.SequenceReady;
        }


        /// <summary>
        /// What the one button that starts and stops a sequence promises while the sequence is not running, facing the way that starts it.
        /// </summary>
        public static string Starts(Texts texts)
        {
            return texts.Codex.SequenceRunNow;
        }

        /// <summary>Why the button that starts <paramref name="sequence" /> on <paramref name="instance" /> cannot be pressed: the switch that is off, or null where both are on.</summary>
        public static string RunLocked(Texts texts, Sequence sequence, SystemInstance instance)
        {
            if (sequence.CanRun(instance))
                return null;

            return sequence.System != null && !sequence.System.Enabled ? texts.Codex.SequenceRunSystemOff : texts.Codex.SequenceRunInstallationOff;
        }

        /// <summary>What the one button that starts and stops a sequence promises while the sequence is running, facing the way that stops it.</summary>
        public static string Stops(Texts texts)
        {
            return texts.Codex.SequenceStopNow;
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
            if (state.StoppedBy == StopCause.Hand)
                return Template.Fill(texts.SequenceStoppedByHand, "step", step);

            if (state.StoppedBy == StopCause.Trigger)
                return Template.Fill(texts.SequenceStoppedByTrigger, "step", step);

            if (state.StoppedBy == StopCause.Switch)
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
                return Template.Fill(fault.Step == SequenceFault.Trigger ? texts.ProblemTriggerOptionMissing : texts.ProblemOptionMissing, "name", fault.Which);

            if (fault.Problem == SequenceProblem.Mistyped && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(fault.Step == SequenceFault.Trigger ? texts.ProblemTriggerOptionWrong : texts.ProblemOptionWrong, "name", fault.Which);

            if (fault.Problem == SequenceProblem.PassedOver)
                return Template.Fill(texts.ProblemPassedOver, "block", fault.Which);

            if (fault.Problem == SequenceProblem.MissingFacet)
                return Template.Fill(texts.ProblemMissingScreen, "block", fault.Which);

            var repair = ProgramRepairs.Of(fault.Problem);
            if (repair != null)
                return repair.Warning(texts, fault);

            if (fault.Problem == SequenceProblem.MissingSource && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemSourceLost, "block", fault.Which);

            if ((fault.Problem == SequenceProblem.MissingTarget || fault.Problem == SequenceProblem.MissingGate) && !string.IsNullOrEmpty(fault.Which))
                return Template.Fill(texts.ProblemBlockLost, "block", fault.Which);

            return fault.Because.Exists ? TroubleText.Of(texts, fault.Because, fault.Which) : Problem(texts, fault.Problem);
        }

        /// <summary>What is wrong with <paramref name="sequence" /> and where, for a line about the whole of its logic system.</summary>
        public static string Failing(Texts texts, SequenceFault fault, Sequence sequence)
        {
            return Failing(texts, fault, sequence.HasName ? sequence.Name : "", sequence.IsAlone, Row(sequence, fault.Step));
        }

        /// <summary>
        /// What is wrong and where, for a line about the whole of a logic system: in the sequence called
        /// <paramref name="sequence" />, or one with no name where it is empty, at the step whose row is <paramref name="row" />,
        /// or none where it is 0, or in a trigger's own act where <paramref name="alone" />.
        /// </summary>
        public static string Failing(Texts texts, SequenceFault fault, string sequence, bool alone, int row)
        {
            // A trigger's own act has no steps to count, and no name.
            var problem = Problem(texts, fault, null);
            if (alone)
                return problem;

            var step = row.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(sequence))
            {
                return row > 0
                    ? Fill(texts.ProblemAtNamedStep, "step", step, "problem", problem, "sequence", sequence)
                    : Fill(texts.ProblemInSequence, "problem", problem, "sequence", sequence);
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

        /// <summary>How <paramref name="fault" /> is marked where it is shown.</summary>
        public static NoticeLevel Level(SequenceFault fault)
        {
            return fault.IsNote ? NoticeLevel.Info : fault.IsWarning ? NoticeLevel.Warning : NoticeLevel.Problem;
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
            var sequence = halt == null ? null : system?.FindSequence(halt.Sequence);
            return Stopped(texts, halt, sequence != null && sequence.HasName ? sequence.Name : "", halt == null ? 0 : Row(sequence, halt.Step),
                restored);
        }

        /// <summary>
        /// Why Sprocket switched a logic system off, or one of it, as <paramref name="halt" /> recorded it: in the sequence
        /// called <paramref name="sequence" />, or one with no name where it is empty, at the step whose row is
        /// <paramref name="row" />, or none where it is 0, and that a lost block is back where <paramref name="restored" />.
        /// </summary>
        public static string Stopped(Texts texts, Halt halt, string sequence, int row, bool restored)
        {
            if (halt == null || halt.Problem == SequenceProblem.None)
                return texts.StoppedUnknown;

            var step = row.ToString(CultureInfo.InvariantCulture);
            var reason = restored && Lost(halt)
                ? Template.Fill(texts.StoppedBlockRestored, "block", halt.Block)
                : halt.Because.Exists
                    ? TroubleText.Of(texts, halt.Because, halt.Block)
                    : Because(texts, halt.Problem, halt.Block);

            if (string.IsNullOrEmpty(sequence))
                return row > 0 ? Fill(texts.StoppedAtStep, "step", step, "reason", reason) : Template.Fill(texts.StoppedPlain, "reason", reason);

            return row > 0
                ? Fill(texts.StoppedAtStepOf, "step", step, "reason", reason, "sequence", sequence)
                : Fill(texts.StoppedIn, "reason", reason, "sequence", sequence);
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
            if (problem == SequenceProblem.NothingToAverage)
                return texts.StoppedNothingToAverage;

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
        /// Counted as the sequence's screen numbers its rows, where the line closing an if has no number.
        /// </remarks>
        public static int Row(Sequence sequence, int step)
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
            if (problem == SequenceProblem.NothingToAverage)
                return texts.ProblemNothingToAverage;

            return "";
        }


        /// <summary>One numbered line of the sequence, in the words its row reads in; the line closing an if says nothing.</summary>
        /// <remarks>
        /// Its blocks are found through <paramref name="bindings" /> as <paramref name="through" /> fills the slots, or as written
        /// where that is null, and named by those blocks, or by the slots' own names where <paramref name="slotted" />, as a row
        /// names them.
        /// </remarks>
        public static string Step(Bindings bindings, Sequence sequence, Step step, SystemInstance through, bool slotted = false)
        {
            var wait = step as Wait;
            if (wait != null)
                return $"{SentenceText.WaitWord} {TimeText.Duration(wait.Length)}";

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

                var compared = branch.Test as Comparison;
                var tested = compared != null ? Tested(bindings, compared, through, slotted) : Tested((StateTest)branch.Test);
                return (step is If ? "if " : "otherwise if ") + tested;
            }

            if (step is EndIf)
                return "";

            var set = step as SetState;
            if (set != null)
                return $"{SentenceText.SetWord} {set.State} to {(set.From.Length > 0 ? set.From : set.Value.Said)}";

            var act = step as Act;
            return act == null ? "wait for it to finish" : Action(bindings, act, through, slotted);
        }

        /// <summary>A test of a state as its row reads: the state, how it is compared, and with what: "where is inside".</summary>
        static string Tested(StateTest test)
        {
            var with = test.Other.Length > 0 ? test.Other : test.Value.Said;
            return $"{test.State} {ComparisonQuestion.Said(test.Relation)} {with}";
        }

        /// <summary>A test as its row reads: the blocks, the value unless it is their status, and how it is compared with what.</summary>
        static string Tested(Bindings bindings, Comparison test, SystemInstance through, bool slotted)
        {
            var targets = SystemInstance.Through(through, test.Targets);
            var value = EntityAction.Value(Vocabulary.ActionsFor(bindings, targets), test.Provider, test.MemberId);
            var shape = TestShape.Of(test.Quantifier, value == null || !value.IsStatus, slotted || targets.Count > 1, slotted, ComparisonQuestion.Join(value, test.Arguments));
            var blocks = Named(bindings, targets, shape.JoinsAll, slotted ? test.Targets : null);
            if (value == null)
                return shape.Says(SentenceText.Empty, blocks);

            var fields = ComparisonQuestion.Fields(value, Vocabulary.EventsFor(bindings, targets), test.Arguments);
            var given = SentenceText.Given(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation), test.Value }, fields, null,
                ArgumentRole.Checked, shape.IsPlural));
            return $"{shape.Says(value.IsStatus ? null : SentenceText.Value(value), blocks, Taking(value, test.Arguments))} {given}";
        }

        /// <summary>
        /// What <paramref name="when" /> compares on the blocks <paramref name="targets" />, as a sentence reads it: the blocks
        /// and the state they are read for, for one of Sprocket's own, or the value on the blocks and what it is compared with.
        /// </summary>
        static string Compared(Bindings bindings, WhenTrigger when, IList<string> targets, IList<string> slots)
        {
            var blocks = Named(bindings, targets, when.Quantifier != Quantifier.Any, slots);
            if (when.Provider == BlockKinds.Own)
            {
                bool inState;
                var state = Vocabulary.Change(Vocabulary.EventsFor(bindings, targets), when.EventId, when.Relation, when.Value, out inState);
                if (state != null)
                {
                    var said = inState ? state.State : state.Unstate;
                    return $"{blocks} {(when.Quantifier == Quantifier.All && targets.Count > 1 ? SentenceText.Plural(said) : said)}";
                }
            }

            var value = EntityAction.Value(Vocabulary.ActionsFor(bindings, targets), when.Provider, when.EventId);
            var shape = TestShape.Of(when.Quantifier, true, slots != null || targets.Count > 1, slots != null, ComparisonQuestion.Join(value, when.Arguments));
            var read = Named(bindings, targets, shape.JoinsAll, slots);

            if (value == null)
                return shape.Says(SentenceText.Empty, read);

            var fields = new List<Field>();
            value.Fields(fields);
            ComparisonQuestion.Measure(value, fields, when.Arguments);
            return $"{shape.Says(SentenceText.Value(value), read, Taking(value, when.Arguments))} {Compared(when.Relation)} {SentenceText.Given(new ArgumentsSlot(new[] { when.Value }, fields, null))}";
        }

        /// <summary>The <paramref name="arguments" /> <paramref name="value" /> is read with, as a sentence says them, or null where it takes none.</summary>
        static string Taking(EntityAction value, IList<Literal> arguments)
        {
            var parameters = ComparisonQuestion.Parameters(value);
            return !ComparisonQuestion.SaysAny(parameters, arguments) ? null : SentenceText.Given(new ArgumentsSlot(arguments, parameters, null));
        }

        /// <summary>The blocks <paramref name="targets" />, or the slots <paramref name="slots" /> by their names where there are slots.</summary>
        static string Named(Bindings bindings, IList<string> targets, bool requiresAll, IList<string> slots)
        {
            return slots != null ? SentenceText.Slots(slots) : SentenceText.Named(bindings, targets, requiresAll);
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


        static string Action(Bindings bindings, Act step, SystemInstance through, bool slotted)
        {
            if (step.Targets.Count == 0)
                return SentenceText.Empty;

            var targets = SystemInstance.Through(through, step.Targets);
            EntityAction named = step.Names(Vocabulary.ActionsFor(bindings, targets));
            var blocks = Named(bindings, targets, true, slotted ? step.Targets : null);
            if (named == null)
                return $"{SentenceText.Empty} {blocks}";

            var fields = new List<Field>();
            named.Fields(fields);
            var ahead = new List<Literal>();
            var aheadFields = new List<Field>();
            var after = new List<Literal>();
            var afterFields = new List<Field>();
            ArgumentsSlot.Split(step.Arguments, fields, ahead, aheadFields, after, afterFields);
            var slot = new ArgumentsSlot(after, afterFields, null);
            var given = SentenceText.Given(slot);

            var verb = step.Sets ? SentenceText.Switching(named, step.Arguments) : "";
            if (verb.Length != 0)
                return SentenceText.Switched(verb, SentenceText.Value(named), blocks);

            if (step.Sets)
                return SentenceText.Setting(SentenceText.Value(named), blocks, given);

            var leads = aheadFields.Count == 0 ? named.Text : $"{named.Text} {SentenceText.Given(new ArgumentsSlot(ahead, aheadFields, null))}";
            if (named.Join.Length != 0)
                leads = $"{leads} {named.Join}";

            return step.Arguments.Count == 0 || slot.Quiet ? $"{leads} {blocks}" : $"{leads} {blocks} {given}";
        }
    }
}
