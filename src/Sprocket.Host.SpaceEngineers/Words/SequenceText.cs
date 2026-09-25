using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Words
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
            Sequence sequence = system == null ? null : system.FindSequence(trigger.Runs);
            if (sequence == null)
                return trigger.Stops ? "stops a sequence that is gone" : "runs a sequence that is gone";

            if (sequence.HasName)
                return (trigger.Stops ? "stops " : "runs ") + sequence.Name;

            return trigger.Stops
                ? "stops " + Doing(bindings, sequence, through)
                : Doing(bindings, sequence, through);
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
                    when != null && when.RequiresAll) + " " + Happening(bindings, moment, sources);
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return "Every " + TimeText.Rate(every.IntervalTicks);

            // A required trigger that names nothing: whatever answers it is on the ship's toolbars
            // and not in the system, so there is nothing to name and the line says that.
            return trigger is RequiredTrigger ? "When something on this ship says so" : SentenceText.Empty;
        }


        /// <summary>
        /// That the blocks a trigger names are spoken for: "Airlock Button is already in use".
        /// </summary>
        /// <remarks>
        /// The blocks rather than what holds them, because what holds them may be a system on the
        /// other side of the construct, and the fact somebody needs is the one in front of them: the
        /// button they just picked is not free.
        /// <para>
        /// The moment is left out of the words although it is part of what clashes - a panel's third
        /// button is free while its first is not. It cannot be said without the sentence coming
        /// apart, because a moment reads as a verb: "button 3 is pressed is already in use". What is
        /// on screen behind this question is the choice it is about.
        /// </para>
        /// <para>
        /// Its blocks are named as <paramref name="through" /> fills them, or as written where that is null.
        /// </para>
        /// </remarks>
        public static string Taken(Bindings bindings, Trigger trigger, SystemInstance through)
        {
            var moment = trigger as MomentTrigger;
            if (moment == null || moment.Sources.Count == 0)
                return "That is already in use";

            var when = moment as WhenTrigger;
            return SentenceText.Named(bindings, SystemInstance.Through(through, moment.Sources),
                when != null && when.RequiresAll) + " is already in use";
        }

        /// <summary>The words for the moment a trigger watches for on <paramref name="sources" />, or an unfilled gap.</summary>
        static string Happening(Bindings bindings, MomentTrigger moment, IList<string> sources)
        {
            var events = Vocabulary.EventsFor(bindings, sources);
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == moment.EventId)
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


        /// <summary>What a sequence does, in a line: its name, or its first step and how many follow.</summary>
        /// <remarks>Its blocks are named as <paramref name="through" /> fills them, or as written where that is null.</remarks>
        public static string Doing(Bindings bindings, Sequence sequence, SystemInstance through)
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

            var first = Step(bindings, sequence, told, through);
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

        /// <summary>The same, letting whatever is at fault speak for itself where it can.</summary>
        internal static string Problem(SequenceFault fault)
        {
            return string.IsNullOrEmpty(fault.Because) ? Problem(fault.Problem) : fault.Because;
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
            if (problem == SequenceProblem.SwitchedOff)
                return "This is switched off, so nothing starts it on its own.";
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

            return "";
        }


        /// <summary>
        /// One numbered line of the sequence.
        /// </summary>
        /// <remarks>
        /// Three sorts of line, because a sequence has three: something done to some blocks, waiting
        /// for what has been started, and spending time.
        /// </remarks>
        static string Step(Bindings bindings, Sequence sequence, Step step, SystemInstance through)
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
            var given = SentenceText.Given(new ValuesSlot(step.Values, fields, 0));

            if (step.Sets)
                return SentenceText.Setting(named.Named, blocks, given);

            return step.Values.Count == 0 ? named.Text + " " + blocks : named.Text + " " + blocks + " " + given;
        }
    }
}
