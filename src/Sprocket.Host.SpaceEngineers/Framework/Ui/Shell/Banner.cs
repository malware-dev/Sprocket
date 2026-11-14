using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Images;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>A notice as a banner that slides in, opens for the rest of its text, and slides out.</summary>
    public class Banner : Reveal
    {
        readonly VStack _column;

        readonly Reveal _more;

        readonly HStack _heading;
        readonly RelayCommand _expands;
        readonly RelayCommand _offers;
        readonly Label _text;
        readonly Glyph _chevron;
        readonly Label _detail;
        readonly Label _diagnostic;
        readonly HStack _actions;
        readonly Button _offer;

        Notice _notice;

        /// <summary>A banner showing nothing, shut.</summary>
        public Banner()
        {
            _expands = new RelayCommand(() => Carry(_notice?.Expands));
            _offers = new RelayCommand(() => Carry(_notice?.Offers));

            // The gap under the banner is part of what slides, so it opens and closes with the banner.
            Content = new VStack
            {
                Base = { Padding = new Thickness(0f, 0f, 0f, Gaps.Items) },
                Children =
                {
                    new VStack
                    {
                        Children =
                        {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Command = _expands,
                        Children =
                        {
                            new Label { Style = Styles.Name, TextWrapping = TextWrapping.Wrap, Placement = Flex.Grow }.As(out _text),
                            new Glyph { Symbol = Symbols.Down, Style = Styles.Chevron, Placement = FixedSize.Size(32f, 32f) }.As(out _chevron)
                        }
                    }.As(out _heading),
                    new Reveal
                    {
                        Content = new VStack
                        {
                            Children =
                            {
                                new Label { Style = Styles.Opened, TextWrapping = TextWrapping.Wrap }.As(out _detail),
                                new Label { Style = Styles.Diagnostic, TextWrapping = TextWrapping.Wrap }.As(out _diagnostic),
                                new HStack
                                {
                                    Base = { Spacing = Gaps.Parts, Padding = new Thickness(16f, 0f, 16f, 8f) },
                                    Children = { new Button { Style = Styles.Button, Command = _offers }.As(out _offer) }
                                }.As(out _actions)
                            }
                        }
                    }.As(out _more)
                        }
                    }.As(out _column)
                }
            };
        }

        /// <summary>Shows <paramref name="notice" />, or slides away where there is none.</summary>
        /// <remarks>Called every frame; costs a handful of comparisons once the slide has settled.</remarks>
        public void Show(Notice notice)
        {
            _notice = notice;

            if (notice != null)
            {
                IsVisible = true;
                Say(notice);
            }

            Slide();

            if (notice == null && IsClosed)
                IsVisible = false;
        }

        void Say(Notice notice)
        {
            var ink = RemarkLook.Of(notice.Level, Palette.Ink);

            RemarkLook.Ground(_column, notice.Level, Palette.Panel);
            _heading.Style = notice.Level == NoticeLevel.Problem
                ? Styles.ProblemStrip
                : notice.Level == NoticeLevel.Warning
                    ? Styles.NoticeStrip
                    : Styles.InfoStrip;
            _text.Base.Color = ink;
            _text.Text = notice.Text;
            _detail.Text = notice.Detail;
            _detail.IsVisible = notice.Detail.Length != 0;
            _diagnostic.Text = notice.Diagnostic;
            _diagnostic.IsVisible = notice.Diagnostic.Length != 0;
            _chevron.Symbol = notice.Expanded ? Symbols.Up : Symbols.Down;
            _chevron.Base.Tint = ink;
            _chevron.IsVisible = notice.Expands != null;

            _expands.CanExecute = notice.Expands != null;

            var offers = notice.Offers != null;
            _actions.IsVisible = offers;
            _offers.CanExecute = offers;
            _offer.Text = notice.OfferText;
        }

        /// <summary>Opens the banner while it has a notice that is not leaving, and its rest while that is expanded.</summary>
        void Slide()
        {
            IsOpen = _notice != null && !_notice.Leaving;
            _more.IsOpen = _notice != null && _notice.Expanded;

            // Whoever raised the banner waits for this to drop it.
            if (_notice != null && _notice.Leaving && IsClosed)
                _notice.Closed = true;
        }

        static void Carry(ICommand command)
        {
            if (command != null && command.CanExecute)
                command.Execute();
        }
    }
}
