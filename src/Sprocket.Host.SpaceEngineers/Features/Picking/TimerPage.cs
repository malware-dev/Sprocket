using System;
using Lumel;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>How often a trigger on a timer fires, and what it then does, side by side.</summary>
    /// <remarks>
    /// The timer's own picker. A trigger on a block picks the block and what happens to it; this gives the rate in the panel a
    /// length is given in everywhere, and the column saying what the trigger does is the same one, so it is chosen the same way
    /// whichever kind of trigger it is.
    /// </remarks>
    public sealed class TimerPage : EditorPage
    {
        readonly OutcomeColumn _aim;
        readonly Action<Duration> _done;
        readonly Duration _start;

        bool _goesOn;
        PickStrip _strip;
        StageColumn _rates;
        StageColumn _does;
        DurationPanel _rate;
        ICommand _finish;

        /// <summary>Opens on <paramref name="start" />, and hands the rate given to <paramref name="done" />.</summary>
        public TimerPage(SprocketEditor editor, Duration start, OutcomeColumn aim, Action<Duration> done) : base(editor)
        {
            _start = start;
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
        public override bool Asks => true;

        /// <inheritdoc />
        public override string PrimaryText => _aim.Outcome != Outcome.One ? Editor.Texts.ChromeDone : "";

        /// <inheritdoc />
        public override bool CanPrimary => _aim.Finishes && (_rate == null || _rate.Length.HasValue);

        /// <inheritdoc />
        public override string PrimaryAbout => Editor.Texts.PickDoneAbout;

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.PickDoneFirst;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in, which the page the single action is chosen on goes on in.</summary>
        public PickStrip Strip => _strip;

        /// <summary>The panel the rate is given in, once the page is built.</summary>
        public DurationPanel Rate => _rate;

        /// <inheritdoc />
        public override void Primary()
        {
            var rate = _rate?.Length;
            _done(rate ?? _start);
        }

        /// <inheritdoc />
        public override void Build()
        {
            if (_strip == null)
            {
                _strip = new PickStrip(Editor.Texts);
                _finish = Editor.Act(CommandKind.Primary, Primary);

                _rates = new StageColumn(_strip.Width, false, null, Editor.Texts);
                _rates.Title = Editor.Texts.PickHowOften;
                _rate = new DurationPanel(Editor.Texts, Editor.Input, _start) { Cautions = true };
                _rate.Switched += Editor.Prefer;
                _rate.Submitted += delegate
                {
                    if (_finish != null && CanPrimary)
                        _finish.Execute();
                };
                _rates.Options.Add(_rate);

                _does = new StageColumn(_strip.Width, false, null, Editor.Texts);
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
            _strip.ShowsArguments = false;
            _strip.Onward = _aim.Outcome == Outcome.One ? Editor.Act(CommandKind.PageOn, Primary) : null;

            _rates.ShowEmpty();
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
        public void GoOn()
        {
            _goesOn = true;
        }

        /// <summary>The strip of the pick, which the page the single action is chosen on shares.</summary>
        protected override Element CreateView()
        {
            return _strip;
        }
    }
}
