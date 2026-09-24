using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// What a trigger is chosen to do, in the column beside what starts it: a single action, a new
    /// sequence, or a sequence the system has, to run or to stop.
    /// </summary>
    /// <remarks>
    /// One for every page a trigger is made or changed on, so what it does is chosen the same way
    /// wherever it is met. It opens on what the trigger does already, and a new sequence where it does
    /// nothing yet, which is what a trigger usually does.
    /// </remarks>
    sealed class Aim
    {
        readonly SprocketEditor _editor;

        /// <summary>The sequences it may run or stop: the ones that stand on their own.</summary>
        readonly List<Sequence> _offered = new List<Sequence>();

        readonly Outcome _was;
        readonly int _wasSequence;
        readonly bool _wasStops;

        /// <summary>Opens on what <paramref name="trigger" /> does now, or on a new sequence where it is null or does nothing.</summary>
        public Aim(SprocketEditor editor, AutomationSystem system, Trigger trigger)
        {
            _editor = editor;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].HasName)
                    _offered.Add(sequences[i]);
            }

            Outcome = Outcome.New;
            Sequence running = trigger == null || trigger.Runs < 1 ? null : system.FindSequence(trigger.Runs);
            if (running != null)
            {
                Outcome = running.HasName ? Outcome.Existing : Outcome.One;
                Sequence = running.HasName ? running.Number : 0;
                Stops = running.HasName && trigger.Stops;
            }

            _was = Outcome;
            _wasSequence = Sequence;
            _wasStops = Stops;
        }

        /// <summary>What the trigger is chosen to do.</summary>
        public Outcome Outcome { get; private set; }

        /// <summary>The sequence it runs or stops, for <see cref="Outcome.Existing" />.</summary>
        public int Sequence { get; private set; }

        /// <summary>Whether it stops that sequence rather than running it.</summary>
        public bool Stops { get; private set; }

        /// <summary>Whether it says what the trigger does, which is what finishing needs.</summary>
        public bool Chosen => Outcome != Outcome.None;

        /// <summary>Whether it says something other than what the trigger did when it opened.</summary>
        public bool Moved => Outcome != _was || Sequence != _wasSequence || Stops != _wasStops;

        /// <summary>
        /// Whether the page it is chosen on is finished there, which a single action is not: choosing
        /// one goes on to a page of its own.
        /// </summary>
        public bool Finishes => Chosen && Outcome != Outcome.One;

        /// <summary>What choosing a single action goes on to, which is the page after this one.</summary>
        public Action Onward { get; set; }

        /// <summary>
        /// The page the single action is chosen on, kept while going back to look at what starts the
        /// trigger, and let go once it does something else.
        /// </summary>
        public PickPage Following { get; set; }

        /// <summary>
        /// Whether the page after is being gone straight to, because the part pressed to open the pick
        /// stands for something on it.
        /// </summary>
        public bool Straight { get; set; }

        /// <summary>
        /// The pick what the trigger does is chosen in, which the page the single action is chosen on
        /// goes on in.
        /// </summary>
        /// <remarks>
        /// So what a trigger does, and then the action it does, are one pick: the stages already
        /// answered stay where going back finds them, and nothing is kept until the last page is done.
        /// </remarks>
        public PickComponent Pick { get; set; }

        /// <summary>Which column the page the single action is chosen on opens on.</summary>
        public Opening Opens { get; set; }

        /// <summary>What finishing the page the single action is chosen on does, as things stand now.</summary>
        /// <remarks>
        /// Set again each time that page is gone on to, because what starts the trigger may have been
        /// changed on the way back, and finishing carries whatever it is by then.
        /// </remarks>
        public Action<PickAnswer> Finish { get; set; }

        /// <summary>
        /// Says what the trigger may do in <paramref name="stage" />, and answers whether any of it changed.
        /// </summary>
        /// <remarks>
        /// Running and stopping are a choice about the rest, as any and all are about the blocks, so
        /// stopping is one choice and one row rather than a second list. There is nothing new to stop,
        /// and a single action is done rather than stopped, so stopping lists the sequences alone; and
        /// a system with none has nothing to stop.
        /// </remarks>
        public bool Fill(PickStage stage)
        {
            // Nothing is finished from here where what is chosen goes on to another page.
            var changed = stage.Finishes != Finishes;
            stage.Finishes = Finishes;

            var asks = _offered.Count > 0;
            changed |= PickPage.Resize(stage.Options, asks ? 2 : 0);
            if (asks)
            {
                changed |= PickPage.Apply(stage.Options[0], "run", !Stops,
                    _editor.Register(Cmd.TriggerRuns, delegate { Stopping(false); }));
                changed |= PickPage.Apply(stage.Options[1], "stop", Stops,
                    _editor.Register(Cmd.TriggerStops, delegate { Stopping(true); }));
            }

            var heading = !Stops && _offered.Count > 0;
            changed |= PickPage.Resize(stage.Entries, _offered.Count + (Stops ? 0 : 2) + (heading ? 1 : 0));

            // The two that make something first, and then under their own heading the sequences, which
            // are as many as the system has and so go last where their number cannot push anything else
            // out of sight.
            var at = 0;
            if (!Stops)
            {
                changed |= PickPage.Apply(stage.Entries[at++], "A single action…", "", "", false,
                    Outcome == Outcome.One,
                    _editor.Register(Cmd.TriggerDoes, 1, delegate
                    {
                        Does(Outcome.One, 0);
                        if (Onward != null)
                            Onward();
                    }));
                changed |= PickPage.Apply(stage.Entries[at++], "A new sequence", "", "", false,
                    Outcome == Outcome.New,
                    _editor.Register(Cmd.TriggerDoes, 0, delegate { Does(Outcome.New, 0); }));
            }

            if (heading)
                changed |= PickPage.Apply(stage.Entries[at++], "Sequences", "", "", false, false, 0, heading: true);

            for (var i = 0; i < _offered.Count; i++)
            {
                var number = _offered[i].Number;
                changed |= PickPage.Apply(stage.Entries[at++], _offered[i].Name, "", "", false,
                    Outcome == Outcome.Existing && Sequence == number,
                    _editor.Register(Cmd.TriggerDoes, i + 2, delegate { Does(Outcome.Existing, number); }));
            }

            return changed;
        }

        void Does(Outcome outcome, int sequence)
        {
            Outcome = outcome;
            Sequence = sequence;
            if (outcome != Outcome.One)
                Following = null;

            _editor.Invalidate();
        }

        /// <summary>Changes between running and stopping, keeping a sequence already chosen.</summary>
        void Stopping(bool stops)
        {
            Stops = stops;
            if (Outcome != Outcome.Existing)
                Outcome = stops ? Outcome.None : Outcome.New;

            if (Outcome != Outcome.One)
                Following = null;

            _editor.Invalidate();
        }
    }
}
