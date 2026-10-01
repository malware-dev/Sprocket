using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>How often a trigger on a timer fires, and what it then does, side by side.</summary>
    /// <remarks>
    /// The timer's own picker. A trigger on a block picks the block and what happens to it; this picks the rate, and the
    /// column saying what the trigger does is the same one, so it is chosen the same way whichever kind of trigger it is.
    /// </remarks>
    sealed class TimerPage : EditorPage
    {
        readonly Aim _aim;
        readonly Action<int> _done;
        readonly List<int> _keys = new List<int>();

        int _ticks;
        bool _goesOn;
        PickStrip _strip;
        StageColumn _rates;
        StageColumn _does;
        Keyed<int, PickRow> _rows;
        ICommand _finish;

        /// <summary>Opens on <paramref name="ticks" />, and hands the rate chosen to <paramref name="done" />.</summary>
        public TimerPage(SprocketEditor editor, int ticks, Aim aim, Action<int> done) : base(editor)
        {
            _ticks = ticks;
            _aim = aim;
            aim.Onward = Primary;
            aim.MovesOn = delegate
            {
                if (_finish != null && CanPrimary)
                    _finish.Execute();
            };
            _done = done;
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.PickHowOftenQuestion;

        /// <inheritdoc />
        public override string PrimaryText => _aim.Outcome != Outcome.One ? Editor.Texts.ChromeDone : "";

        /// <inheritdoc />
        public override bool CanPrimary => _aim.Finishes;

        /// <inheritdoc />
        public override string PrimaryAbout => Editor.Texts.PickDoneAbout;

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.PickDoneFirst;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in, which the page the single action is chosen on goes on in.</summary>
        public PickStrip Strip => _strip;

        /// <inheritdoc />
        public override void Primary()
        {
            _done(_ticks);
        }

        /// <inheritdoc />
        public override void Build()
        {
            if (_strip == null)
            {
                _strip = new PickStrip(Editor.Texts, delegate { return Editor.Windows; });
                _finish = Editor.Act(Cmd.Primary, Primary);

                _rates = new StageColumn(_strip.Width, false, null, Editor.Texts, delegate { return Editor.Windows; });
                _rates.Title = Editor.Texts.PickHowOften;
                _rows = new Keyed<int, PickRow>(_rates.Rows, Rate);

                _does = new StageColumn(_strip.Width, false, null, Editor.Texts, delegate { return Editor.Windows; });
                _does.Title = Editor.Texts.PickWhatItDoes;
            }

            _aim.Pick = _strip;

            // Back from the page after this one, whatever that page asked goes with it.
            _strip.Return(0, 2);
            if (_strip.Stages.Count == 0)
            {
                _strip.Add(_rates);
                _strip.Add(_does);
            }

            _strip.Returns = null;
            _strip.ShowsValues = false;
            _strip.Onward = _aim.Outcome == Outcome.One ? Editor.Act(Cmd.PageOn, Primary) : null;

            FillRates();
            _aim.Fill(_does);

            _strip.Show();

            if (!_goesOn)
                return;

            _goesOn = false;
            Primary();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>Goes straight on to the page after this one as soon as this is built.</summary>
        internal void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>The strip of the pick, which the page the single action is chosen on shares.</summary>
        protected override Element CreateView()
        {
            return _strip;
        }

        PickRow Rate(int ticks)
        {
            var row = new PickRow();
            row.Picked += delegate
            {
                var choose = Editor.Act(Cmd.Choice, delegate
                {
                    _ticks = ticks;
                    Editor.Invalidate();
                });

                if (choose != null)
                    choose.Execute();
            };

            return row;
        }

        /// <summary>Every rate there is, the one chosen marked, and what the costly ones cost said on the pointer.</summary>
        void FillRates()
        {
            var cadences = Cadences.All;
            _keys.Clear();
            for (var i = 0; i < cadences.Count; i++)
                _keys.Add(cadences[i].Ticks);

            _rows.Show(_keys);

            for (var i = 0; i < cadences.Count; i++)
            {
                var ticks = cadences[i].Ticks;
                var row = _rows[ticks];

                row.Text = ticks == 1
                    ? cadences[i].Name(Editor.Texts)
                    : Template.Fill(Template.Fill(Editor.Texts.PickRate, "ticks", ticks.ToString(CultureInfo.InvariantCulture)),
                        "name", cadences[i].Name(Editor.Texts));
                row.IsChosen = ticks == _ticks;
                row.Why = null;
                row.Show();

                // Said on the pointer rather than withheld: a costly rate can still be chosen.
                var warning = cadences[i].Warning(Editor.Texts);
                row.Tip = Tip.For(row.Tip, warning.Length == 0 ? null : warning);
            }
        }
    }
}
