using System.Collections.Generic;
using System.Text;
using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Host.SpaceEngineers.Presentation.Words
{
    /// <summary>
    /// The words for a sequence where a person can only read it: a list row, a heading, a caption.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="SentenceText" />. A sentence is slots because every part of one
    /// is somebody's to change; these are labels, so they are one string each and this decides all
    /// of it - including the word order, which is the part core must never own.
    /// </remarks>
    public sealed class SequenceText : ISequenceText
    {


        /// <summary>
        /// What starts it, then what it does or is called.
        /// </summary>
        /// <remarks>
        /// A joined string rather than parts, unlike the heading, and that is not an oversight. A row
        /// is a label: there is nothing on it to press, no part of it is somebody's to edit, and one
        /// line has to stand for the whole sequence.
        /// </remarks>
        public string Sequence(Bindings bindings, Sequence sequence)
        {
            return Trigger(bindings, sequence.Trigger) + " » " + Doing(bindings, sequence);
        }


        /// <summary>The whole first line: "When the hangar door finishes opening".</summary>
        public string Trigger(Bindings bindings, Trigger trigger)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
            {
                var events = Vocabulary.EventsFor(bindings, when.Sources);
                var text = SentenceText.Empty;
                for (var i = 0; i < events.Count; i++)
                {
                    if (events[i].Id == when.EventId)
                    {
                        text = events[i].Text;
                        break;
                    }
                }

                return "When " + SentenceText.Named(bindings, when.Sources, when.RequiresAll) + " " + text;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + TimeText.Rate(every.IntervalTicks);

            return SentenceText.Empty;
        }


        public string Doing(Bindings bindings, Sequence sequence)
        {
            if (sequence.HasName)
                return sequence.Name;

            if (sequence.Steps.Count == 0)
                return "nothing yet";

            var first = Step(bindings, sequence.Steps[0]);
            return sequence.Steps.Count == 1
                ? first
                : first + " (+" + (sequence.Steps.Count - 1) + " more)";
        }


        public string Status(SequenceStatus status)
        {
            if (status == SequenceStatus.Off)
                return "switched off";
            if (status == SequenceStatus.Running)
                return "running";
            if (status == SequenceStatus.RunningWaiting)
                return "running - waiting for this step";

            return status == SequenceStatus.NeverRun ? "waiting to happen" : "waiting";
        }


        public string Problem(SequenceProblem problem)
        {
            if (problem == SequenceProblem.MissingSource)
                return "This listens to something that isn't here any more.";
            if (problem == SequenceProblem.MissingTarget)
                return "This affects something that isn't here any more.";
            if (problem == SequenceProblem.MissingGate)
                return "This waits on something that isn't here any more.";

            return "";
        }


        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Ordinarily just "close the inner door". Nothing says when it happens, because the list
        /// being a list already does - a step waits for its own action to finish before the next one
        /// starts. A gate only appears when the step waits on something this sequence did not cause.
        /// </remarks>
        static string Step(Bindings bindings, Step step)
        {
            // A step with a duration and nothing to do is simply a wait, and reads better as one than
            // as "after 5 seconds - nothing".
            if (!step.HasAction)
            {
                var pause = step.Gate as AfterGate;
                if (pause != null)
                    return "wait " + TimeText.Duration(pause.Ticks);
            }

            var body = Action(bindings, step);
            var gate = Gate(bindings, step.Gate);
            return gate.Length == 0 ? body : gate + " - " + body;
        }


        static string Action(Bindings bindings, Step step)
        {
            if (step.Targets.Count == 0)
                return SentenceText.Empty;

            var actions = Vocabulary.ActionsFor(bindings, step.Targets);
            var text = SentenceText.Empty;
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == step.ActionId)
                {
                    text = actions[i].Text;
                    break;
                }
            }

            return text + " " + SentenceText.Named(bindings, step.Targets, true);
        }


        /// <summary>The "once ..." or "only if ..." prefix, or empty for the usual case.</summary>
        static string Gate(Bindings bindings, StepGate gate)
        {
            var once = gate as OnceGate;
            if (once != null)
                return "once " + Condition(bindings, once.Condition);

            var only = gate as OnlyIfGate;
            if (only != null)
                return "only if " + Condition(bindings, only.Condition);

            var after = gate as AfterGate;
            if (after != null)
                return "after " + TimeText.Rate(after.Ticks);

            return "";
        }


        static string Condition(Bindings bindings, Condition condition)
        {
            if (condition == null)
                return SentenceText.Empty;

            var text = SentenceText.Empty;
            for (var i = 0; i < condition.Entities.Count; i++)
            {
                var entity = bindings.Entity(condition.Entities[i]);
                if (entity == null)
                    continue;

                var state = entity.Kind.FindState(condition.StateId);
                if (state == null)
                    continue;

                text = state.Text;
                break;
            }

            return SentenceText.Named(bindings, condition.Entities, true) + " " + text;
        }
    }
}
