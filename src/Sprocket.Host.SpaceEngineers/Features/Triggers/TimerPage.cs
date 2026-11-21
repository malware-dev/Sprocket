using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using Lumel;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers
{
    /// <summary>How often a trigger on a timer fires, and what it then does, side by side.</summary>
    public sealed class TimerPage : EditorPage
    {
        readonly OutcomeColumn _aim;
        readonly Action<Duration> _done;
        readonly Duration _start;

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
        public override string PrimaryText => Editor.Texts.ChromeDone;

        /// <inheritdoc />
        public override bool CanPrimary => _rate == null || _rate.Length.HasValue;

        /// <inheritdoc />
        public override string PrimaryAbout => "";

        /// <inheritdoc />
        public override string PrimaryWaits => Editor.Texts.Codex.PickDoneFirst;

        /// <inheritdoc />
        public override bool ScrollsItself => true;

        /// <summary>The strip the pick is drawn in.</summary>
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

            if (_strip.Stages.Count == 0)
            {
                _strip.Add(_rates);
                _strip.Add(_does);
            }

            _strip.ShowsArguments = false;

            _rates.ShowEmpty();
            _aim.Fill(_does);

            _strip.Show();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            _strip.Tick();
        }

        /// <summary>The strip of the pick.</summary>
        protected override Element CreateView()
        {
            return _strip;
        }
    }
}
