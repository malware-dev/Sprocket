using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>The ways to start something new, as a row of buttons with a line closing them off.</summary>
    /// <remarks>Core's labels carry no plus; this target draws one on everything that adds.</remarks>
    public sealed class StartView : ComponentView
    {
        /// <summary>What goes before the words of anything that adds.</summary>
        public const string Plus = "+ ";

        readonly StartComponent _component;
        readonly VStack _stack;
        readonly HStack _buttons;
        readonly List<Button> _rows = new List<Button>();

        int _count = -1;

        /// <summary>A view of <paramref name="component" />.</summary>
        public StartView(StartComponent component)
        {
            _component = component;

            _stack = new VStack
            {
                Children =
                {
                    new HStack { Spacing = 8f }.As(out _buttons),
                    new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) }
                }
            };
        }

        /// <summary>Told rather than asked: a page writes its ways to start once, when it builds the block.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override Element Root
        {
            get { return _stack; }
        }

        /// <inheritdoc />
        protected override void Refresh()
        {
            Rebuild();
        }

        void Rebuild()
        {
            if (_count == _component.Options.Count)
                return;

            _count = _component.Options.Count;
            _rows.Clear();
            _buttons.Children.Clear();

            for (var i = 0; i < _component.Options.Count; i++)
            {
                var option = _component.Options[i];

                // The ellipsis is the desktop convention for a button that asks something before it acts.
                var at = i;
                var button = new Button
                {
                    Style = Styles.Way,
                    Text = Plus + option.Label + (option.Asks ? "…" : ""),
                    RichTip = option.About,
                    Command = new Relay(() => Start(at))
                };

                _rows.Add(button);
                _buttons.Children.Add(button);
            }
        }

        void Start(int at)
        {
            var option = _component.Options[at];
            Host.Send(_rows[at], option.Command, option.Question);
        }
    }
}
