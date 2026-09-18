using Reactor.Host.SpaceEngineers.Editing;
using Reactor.Host.SpaceEngineers.Runtime;
using Reactor.Host.SpaceEngineers.Sequences;

namespace Reactor.Host.SpaceEngineers.Words
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
        /// </remarks>
        public static string Runs(Bindings bindings, AutomationSystem system, Trigger trigger)
        {
            Sequence sequence = system == null ? null : system.FindSequence(trigger.Runs);
            if (sequence == null)
                return trigger.Stops ? "stops a sequence that is gone" : "runs a sequence that is gone";

            if (sequence.HasName)
                return (trigger.Stops ? "stops " : "runs ") + sequence.Name;

            return trigger.Stops ? "stops " + Doing(bindings, sequence) : Doing(bindings, sequence);
        }


        /// <summary>
        /// The whole first line: "When the hangar door finishes opening".
        /// </summary>
        /// <remarks>
        /// The same line whether Reactor watches for the moment or the construct announces it. That
        /// difference is real and it is not a difference in what somebody asked for, so it is not
        /// something to read about on every row.
        /// </remarks>
        public static string Trigger(Bindings bindings, Trigger trigger)
        {
            var moment = trigger as MomentTrigger;
            if (moment != null && moment.Sources.Count > 0)
            {
                var events = Vocabulary.EventsFor(bindings, moment.Sources);
                var text = SentenceText.Empty;
                for (var i = 0; i < events.Count; i++)
                {
                    if (events[i].Id == moment.EventId)
                    {
                        text = events[i].Text;
                        break;
                    }
                }

                var when = moment as WhenTrigger;
                return "When " + SentenceText.Named(bindings, moment.Sources,
                    when != null && when.RequiresAll) + " " + text;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + TimeText.Rate(every.IntervalTicks);

            // A required trigger that names nothing: whatever answers it is on the ship's toolbars
            // and not in the system, so there is nothing to name and the line says that.
            return trigger is RequiredTrigger ? "When something on this ship says so" : SentenceText.Empty;
        }


        /// <summary>What to call a sequence nobody has renamed: "Sequence 3".</summary>
        /// <remarks>
        /// Numbered, not described. Reactor used to word a row out of what the sequence did, which
        /// read well and made a row's identity change as somebody edited it. The game names a block
        /// and lets you overtype it, and everybody already knows that.
        /// </remarks>
        public static string DefaultName(int number)
        {
            return "Sequence " + number;
        }


        public static string Doing(Bindings bindings, Sequence sequence)
        {
            if (sequence.HasName)
                return sequence.Name;

            // The way out for a sequence the editor did not make - the model still allows a nameless
            // one, so this stays rather than leaving a blank row behind. No longer the intended path:
            // anything built through the editor is named from birth.
            if (sequence.Steps.Count == 0)
                return "nothing yet";

            // Awaits are not counted. The editor writes one after every step, so counting them would
            // say a sequence of one action has two lines - and an await is not a line on any screen:
            // a step and the await that follows it are one row, moved and deleted together.
            Step told = null;
            var lines = 0;
            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i] is Await)
                    continue;

                if (told == null)
                    told = steps[i];

                lines++;
            }

            if (told == null)
                return "nothing yet";

            var first = Step(bindings, sequence, told);
            return lines == 1 ? first : first + " (+" + (lines - 1) + " more)";
        }


        public static string Status(SequenceStatus status)
        {
            if (status == SequenceStatus.Off)
                return "switched off";
            if (status == SequenceStatus.Running)
                return "running";
            if (status == SequenceStatus.RunningWaiting)
                return "running - waiting for this step";

            return status == SequenceStatus.NeverRun ? "waiting to happen" : "waiting";
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

            return "";
        }


        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Three sorts of line, because a sequence has three: something done to some blocks, waiting
        /// for what has been started, and spending time.
        /// </remarks>
        static string Step(Bindings bindings, Sequence sequence, Step step)
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
