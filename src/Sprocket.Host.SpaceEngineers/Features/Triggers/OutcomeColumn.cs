using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers
{
    /// <summary>
    /// What a trigger is chosen to do, in the column beside what starts it: a new sequence, or a sequence the system has, to
    /// run or to stop.
    /// </summary>
    public sealed class OutcomeColumn
    {
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

        /// <summary>What choosing a sequence twice in quick succession does, which is finishing the page as Done would.</summary>
        public Action MovesOn { get; set; }

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
                _runs.OnText = SentenceText.RunWord;
                _runs.OffText = SentenceText.StopWord;
                stage.Options.Add(_runs);
            }

            if (asks)
            {
                _runs.IsOn = !Stops;
                _runs.Tip = CodexTip.For(null, Stops ? texts.Codex.PickStopAbout : texts.Codex.PickRunAbout);
            }

            _keys.Clear();
            if (!Stops)
                _keys.Add(New);

            if (!Stops && _offered.Count > 0)
                _keys.Add(Heading);

            foreach (var sequence in _offered)
                _keys.Add(Key(sequence.Number));

            _rows.Show(_keys);

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

        /// <summary>Takes what the row under <paramref name="key" /> offers.</summary>
        void Take(string key, int presses)
        {
            Action does;
            if (key == New)
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
            if (presses > 1 && MovesOn != null)
                MovesOn();
        }

        void Does(Outcome outcome, int sequence)
        {
            Outcome = outcome;
            Sequence = sequence;
            _editor.Invalidate();
        }

        /// <summary>Changes between running and stopping, keeping a sequence already chosen.</summary>
        void Stopping(bool stops)
        {
            Stops = stops;
            if (Outcome != Outcome.Existing)
                Outcome = stops ? Outcome.None : Outcome.New;

            _editor.Invalidate();
        }
    }
}
