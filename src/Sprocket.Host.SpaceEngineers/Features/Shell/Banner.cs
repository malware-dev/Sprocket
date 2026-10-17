using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Images;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A notice as a banner that slides in, opens for the rest of its text, and slides out.</summary>
    public class Banner : Reveal
    {
        const float Frames = 12f;

        readonly VStack _column;

        readonly Reveal _more;

        readonly HStack _heading;
        readonly Relay _expands;
        readonly Relay _offers;
        readonly Label _text;
        readonly Glyph _chevron;
        readonly Label _detail;
        readonly Label _diagnostic;
        readonly HStack _actions;
        readonly Button _offer;

        Notice _notice;
        int _stepped = -1;

        float _progress;

        float _opening;

        /// <summary>A banner showing nothing, shut.</summary>
        public Banner()
        {
            _expands = new Relay(() => Carry(_notice?.Expands));
            _offers = new Relay(() => Carry(_notice?.Offers));

            Openness = 0f;
            Content = new VStack
            {
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Command = _expands,
                        Children =
                        {
                            new Label { Style = Styles.Name, Wraps = true, Placement = Flex.Grow }.As(out _text),
                            new Glyph { Symbol = Symbols.Down, Style = Styles.Chevron, Placement = FixedSize.Size(32f, 32f) }.As(out _chevron)
                        }
                    }.As(out _heading),
                    new Reveal
                    {
                        Content = new VStack
                        {
                            Children =
                            {
                                new Label { Style = Styles.Opened, Wraps = true }.As(out _detail),
                                new Label { Style = Styles.Diagnostic, Wraps = true }.As(out _diagnostic),
                                new HStack
                                {
                                    Spacing = 8f,
                                    Padding = new Thickness(16f, 0f, 16f, 8f),
                                    Children = { new Button { Style = Styles.Button, Command = _offers }.As(out _offer) }
                                }.As(out _actions)
                            }
                        }
                    }.As(out _more)
                }
            }.As(out _column);
        }

        /// <summary>Shows <paramref name="notice" />, or slides away where there is none, moved along to <paramref name="tick" />.</summary>
        /// <remarks>Called every frame; costs a handful of comparisons once the slide has settled.</remarks>
        public void Show(Notice notice, int tick)
        {
            _notice = notice;

            if (notice != null)
            {
                IsVisible = true;
                Say(notice);
            }

            Slide(tick);

            if (notice == null && Shut)
                IsVisible = false;
        }

        void Say(Notice notice)
        {
            var ink = RemarkColors.Of(notice.Level, Palette.Ink);

            _column.Background = notice.Level == NoticeLevel.Problem
                ? Palette.ProblemWash
                : notice.Level == NoticeLevel.Warning
                    ? Palette.NoticeWash
                    : Palette.Panel;
            _heading.Style = notice.Level == NoticeLevel.Problem
                ? Styles.ProblemStrip
                : notice.Level == NoticeLevel.Warning
                    ? Styles.NoticeStrip
                    : Styles.InfoStrip;
            _text.Color = ink;
            _text.Text = notice.Text;
            _detail.Text = notice.Detail;
            _detail.IsVisible = notice.Detail.Length != 0;
            _diagnostic.Text = notice.Diagnostic;
            _diagnostic.IsVisible = notice.Diagnostic.Length != 0;
            _chevron.Symbol = notice.Expanded ? Symbols.Up : Symbols.Down;
            _chevron.Tint = ink;
            _chevron.IsVisible = notice.Expands != null;

            _expands.CanExecute = notice.Expands != null;

            var offers = notice.Offers != null;
            _actions.IsVisible = offers;
            _offers.CanExecute = offers;
            _offer.Text = notice.OfferText;
        }

        /// <summary>Moves the banner towards where it should be, by however many ticks have passed.</summary>
        /// <remarks>Costs one comparison a tick once the banner has settled.</remarks>
        void Slide(int tick)
        {
            var since = _stepped < 0 ? 1 : tick - _stepped;
            _stepped = tick;

            if (since < 1)
                return;

            var step = since / Frames;
            var shown = _notice != null && !_notice.Leaving;

            _progress = Towards(_progress, shown ? 1f : 0f, step);
            _opening = Towards(_opening, _notice != null && _notice.Expanded ? 1f : 0f, step);

            Openness = Eased(_progress);
            _more.Openness = Eased(_opening);

            // Whoever raised the banner waits for this to drop it.
            if (_notice != null && _notice.Leaving && Shut)
                _notice.Closed = true;
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }

        static float Towards(float value, float wanted, float step)
        {
            return value < wanted ? Least(value + step, wanted) : Most(value - step, wanted);
        }

        /// <summary>Most of the movement early, settling rather than stopping: the remaining distance, cubed.</summary>
        static float Eased(float progress)
        {
            var left = 1f - progress;
            return 1f - (left * left * left);
        }

        static float Least(float value, float bound)
        {
            return value < bound ? value : bound;
        }

        static float Most(float value, float bound)
        {
            return value > bound ? value : bound;
        }
    }
}
