using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Rich;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>A section's name, in capitals.</summary>
    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly VStack _line;
        readonly RichText _words;

        /// <summary>What it was last given, so the words are made when they change.</summary>
        string _said;

        bool _follows;

        /// <summary>A view of <paramref name="component" />.</summary>
        public HeadingView(HeadingComponent component)
        {
            _component = component;

            _line = new VStack
            {
                Children =
                {
                    new RichText { Style = Styles.Section }.As(out _words)
                }
            };

            Spaced();
        }

        /// <summary>Told rather than asked: the one thing it shows announces itself.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override Element Root
        {
            get { return _line; }
        }

        /// <inheritdoc />
        protected override void Refresh()
        {
            if (_follows != _component.Follows)
                Spaced();

            if (ReferenceEquals(_said, _component.Text))
                return;

            _said = _component.Text;
            _words.Document = Markdown.Parse(Markdown.Escape(Widgets.Caps(_component.Text)));
        }

        /// <summary>Room above a heading that follows other content, so it reads as the start of what comes after, and room below it before its rows.</summary>
        void Spaced()
        {
            _follows = _component.Follows;
            _line.Padding = new Thickness(4f, _follows ? 32f : 8f, 0f, 8f);
        }
    }
}
