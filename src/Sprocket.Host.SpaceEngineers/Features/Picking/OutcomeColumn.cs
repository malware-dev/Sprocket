using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What a trigger is chosen to do, in the column beside what starts it: a single action, a new sequence, or a sequence the
    /// system has, to run or to stop.
    /// </summary>
    public sealed class OutcomeColumn
    {
        const string One = "one";
        const string New = "new";
        const string Heading = "#";

        readonly SprocketEditor _editor;
        readonly List<Sequence> _offered = new List<Sequence>();
        readonly List<string> _keys = new List<string>();
        readonly Outcome _was;
        readonly int _wasSequence;
        readonly bool _wasStops;

        StageColumn _filled;
        KeyedChildren<string, PickRow> _rows;
        Switch _runs;

        /// <summary>Opens on what <paramref name="trigger" /> does now, or on a new sequence where it is null or does nothing.</summary>
        public OutcomeColumn(SprocketEditor editor, AutomationSystem system, Trigger trigger)
        {
            _editor = editor;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _offered.Add(sequences[i]);

            Outcome = Outcome.New;
            var running = trigger == null || trigger.Runs < 1 ? null : system.FindSequence(trigger.Runs);
            if (running != null)
            {
                Outcome = Outcome.Existing;
                Sequence = running.Number;
                Stops = trigger.Stops;
            }
            else if (trigger != null && trigger.Acts != null)
            {
                Outcome = Outcome.One;
            }

            _was = Outcome;
            _wasSequence = Sequence;
            _wasStops = Stops;
        }

        /// <summary>What the trigger is chosen to do.</summary>
        public Outcome Outcome { get; private set; }

        /// <summary>The sequence it runs or stops, for <see cref="Picking.Outcome.Existing" />.</summary>
        public int Sequence { get; private set; }

        /// <summary>Whether it stops that sequence rather than running it.</summary>
        public bool Stops { get; private set; }

        /// <summary>Whether it says what the trigger does, which is what finishing needs.</summary>
        public bool Chosen => Outcome != Outcome.None;

        /// <summary>Whether it says something other than what the trigger did when it opened.</summary>
        public bool Moved => Outcome != _was || Sequence != _wasSequence || Stops != _wasStops;

        /// <summary>Whether the page it is chosen on is finished there, which a single action is not: choosing one goes on to a page of its own.</summary>
        public bool Finishes => Chosen && Outcome != Outcome.One;

        /// <summary>What choosing a single action goes on to, which is the page after this one.</summary>
        public Action Onward { get; set; }

        /// <summary>What choosing anything else twice in quick succession does, which is finishing the page as Done would.</summary>
        public Action MovesOn { get; set; }

        /// <summary>The page the single action is chosen on, kept while going back to look at what starts the trigger, and let go once it does something else.</summary>
        public PickPage Following { get; set; }

        /// <summary>Whether the page after is being gone straight to, because the part pressed to open the pick stands for something on it.</summary>
        public bool Straight { get; set; }

        /// <summary>The strip what the trigger does is chosen in, which the page the single action is chosen on goes on in.</summary>
        public PickStrip Pick { get; set; }

        /// <summary>Which column the page the single action is chosen on opens on.</summary>
        public OpeningColumn Opens { get; set; }

        /// <summary>What finishing the page the single action is chosen on does, as things stand now.</summary>
        /// <remarks>
        /// Set again each time that page is gone on to, since what starts the trigger may have changed on the way back.
        /// </remarks>
        public Action<PickAnswer> Finish { get; set; }

        /// <summary>Says what the trigger may do in <paramref name="stage" />.</summary>
        public void Fill(StageColumn stage)
        {
            var texts = _editor.Texts;
            if (stage != _filled)
            {
                _filled = stage;
                _rows = new KeyedChildren<string, PickRow>(stage.Rows, Row);
                _runs = null;
            }


            var asks = _offered.Count > 0;
            if (asks && _runs == null)
            {
                _runs = new Switch { Style = Styles.Switch, Command = _editor.Act(CommandKind.TriggerStops, () => Stopping(!Stops)) };
                _runs.Says(SentenceText.RunWord, SentenceText.StopWord);
                stage.Options.Add(_runs);
            }

            if (asks)
            {
                _runs.IsOn = !Stops;
                _runs.Tip = Tip.For(null, Stops ? texts.Codex.PickStopAbout : texts.Codex.PickRunAbout);
            }

            var offersOne = !Stops && (_editor.DeveloperMode || Outcome == Outcome.One);

            _keys.Clear();
            if (offersOne)
                _keys.Add(One);

            if (!Stops)
                _keys.Add(New);

            if (!Stops && _offered.Count > 0)
                _keys.Add(Heading);

            foreach (var sequence in _offered)
                _keys.Add(Key(sequence.Number));

            _rows.Show(_keys);

            if (offersOne)
            {
                _rows[One].Mark = Symbols.Action;
                Show(_rows[One], texts.PickSingleAction, null, Outcome == Outcome.One, false);
            }

            if (!Stops)
            {
                _rows[New].Mark = Symbols.Sequence;
                Show(_rows[New], texts.PickNewSequence, null, Outcome == Outcome.New, false);
            }

            if (!Stops && _offered.Count > 0)
                Show(_rows[Heading], texts.PickSequences, null, false, true);

            foreach (var sequence in _offered)
            {
                var number = sequence.Number;
                _rows[Key(number)].Mark = Symbols.Sequence;
                Show(_rows[Key(number)], sequence.Name, null, Outcome == Outcome.Existing && Sequence == number, false);
            }

            stage.ShowEmpty();
        }

        static string Key(int number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        static void Show(PickRow row, string text, string about, bool chosen, bool heading)
        {
            row.Text = text;
            row.About = about;
            row.IsChosen = chosen;
            row.IsHeading = heading;
            row.Show();
        }

        PickRow Row(string key)
        {
            var row = new PickRow();
            row.Picked += delegate(HeldModifier held, int presses) { Take(key, presses); };
            return row;
        }

        /// <summary>Takes what the row under <paramref name="key" /> offers; a single action goes straight on to the page it is chosen on.</summary>
        void Take(string key, int presses)
        {
            Action does;
            if (key == One)
            {
                does = delegate
                {
                    Does(Outcome.One, 0);
                    if (Onward != null)
                        Onward();
                };
            }
            else if (key == New)
            {
                does = () => Does(Outcome.New, 0);
            }
            else
            {
                int number;
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    return;

                does = () => Does(Outcome.Existing, number);
            }

            var act = _editor.Act(CommandKind.TriggerDoes, does);
            if (act == null)
                return;

            act.Execute();
            if (presses > 1 && key != One && MovesOn != null)
                MovesOn();
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
