using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;

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

            // What answers it is on the ship's toolbars rather than in the system, so there is
            // nothing to name: the line says that something does.
            return trigger is RequiredTrigger ? "When something on this ship says so" : SentenceText.Empty;
        }


        /// <summary>What to call a sequence nobody has renamed: "Sequence 3".</summary>
        /// <remarks>
        /// Numbered, not described. Reactor used to word a row out of what the sequence did, which
        /// read well and made a row's identity change as somebody edited it. The game names a block
        /// and lets you overtype it, and everybody already knows that.
        /// </remarks>
        public string DefaultName(int number)
        {
            return "Sequence " + number;
        }


        public string Doing(Bindings bindings, Sequence sequence)
        {
            if (sequence.HasName)
                return sequence.Name;

            // The way out for a sequence the editor did not make - the model still allows a nameless
            // one, so this stays rather than leaving a blank row behind. No longer the intended path:
            // anything built through the editor is named from birth.
            if (sequence.Steps.Count == 0)
                return "nothing yet";

            var first = Step(bindings, sequence, sequence.Steps[0]);
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
            if (problem == SequenceProblem.NeverTriggered)
                return "Nothing can start this.";
            if (problem == SequenceProblem.MissingGate)
                return "This waits on something that isn't here any more.";

            return "";
        }


        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Three sorts of line, because a sequence has three: something done to some blocks, waiting
        /// for what has been started, and spending time.
        /// </remarks>
        string Step(Bindings bindings, Sequence sequence, Step step)
        {
            var wait = step as Wait;
            if (wait != null)
                return "wait " + TimeText.Duration(wait.Ticks);

            var run = step as Run;
            if (run != null)
            {
                var system = sequence.System;
                var called = system == null ? null : system.FindSequence(run.Number);
                return "run " + (called == null || !called.HasName
                    ? DefaultName(run.Number)
                    : called.Name);
            }

            var act = step as Act;
            return act == null ? "wait for it to finish" : Action(bindings, act);
        }


        static string Action(Bindings bindings, Act step)
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
    }
}
