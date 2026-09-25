using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>A banner that slides in, can be opened for the rest of it, and slides out when it is done.</summary>
    /// <remarks>
    ///     Its height is animated both ways, so nothing below it jumps under the pointer. The slide is stepped
    ///     from the tick it is handed, so it takes the same time whatever the game's update rate is doing.
    /// </remarks>
    public sealed class NoticeView : ComponentView
    {
        /// <summary>How many of the game's frames a slide takes, arriving, leaving or opening.</summary>
        const float Frames = 12f;

        readonly NoticeComponent _component;

        readonly Reveal _reveal;
        readonly VStack _column;

        /// <summary>The opening half, which slides on its own.</summary>
        readonly Reveal _more;

        readonly HStack _heading;
        readonly Label _text;
        readonly Glyph _chevron;
        readonly Label _detail;
        readonly Label _diagnostic;
        readonly HStack _actions;
        readonly List<Button> _buttons = new List<Button>();

        int _stepped = -1;
        int _shown = -1;

        /// <summary>How far along the slide is, before the easing is applied to it.</summary>
        float _progress;

        /// <summary>The same for the opening, which moves at the same rate and is a separate thing.</summary>
        float _opening;

        /// <summary>A view of <paramref name="component" />.</summary>
        public NoticeView(NoticeComponent component)
        {
            _component = component;

            _reveal = new Reveal
            {
                Child = new VStack
                {
                    Children =
                    {
                        new HStack
                        {
                            Spacing = 8f,
                            Children =
                            {
                                new Label { Wraps = true, Placement = Flex.Grow }.As(out _text),
                                new Glyph { Symbol = Symbols.Down }.As(out _chevron)
                            }
                        }.As(out _heading),
                        new Reveal
                        {
                            Child = new VStack
                            {
                                Children =
                                {
                                    new Label { Style = Styles.Opened, Wraps = true }.As(out _detail),
                                    new Label { Style = Styles.Diagnostic, Wraps = true }.As(out _diagnostic),
                                    new HStack { Spacing = 8f, Padding = new Thickness(16f, 0f, 16f, 8f) }.As(out _actions)
                                }
                            }
                        }.As(out _more)
                    }
                }.As(out _column)
            };
        }

        /// <summary>Told rather than asked: everything a banner says announces itself, and the slide is not a value.</summary>
        protected override bool Stale
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override Element Root
        {
            get { return _reveal; }
        }

        /// <inheritdoc />
        protected override void Refresh()
        {
            var problem = _component.Level == NoticeLevel.Problem;

            _column.Style = problem ? Styles.Problem : Styles.Notice;
            _heading.Style = problem ? Styles.ProblemStrip : Styles.NoticeStrip;

            _text.Style = problem ? Styles.ProblemText : Styles.NoticeText;
            _text.Text = _component.Text;

            _detail.Text = _component.Detail;
            _detail.Collapsed = _component.Detail.Length == 0;

            _diagnostic.Text = _component.Diagnostic;
            _diagnostic.Collapsed = _component.Diagnostic.Length == 0;

            _chevron.Symbol = _component.Expanded ? Symbols.Up : Symbols.Down;
            _chevron.Style = problem ? Styles.ProblemChevron : Styles.NoticeChevron;
            _chevron.Collapsed = _component.ExpandCommand == 0;

            // Reached only once the slide has finished, because a reveal answers nothing while it moves.
            _heading.Press = Deed.Sends(_component.ExpandCommand);

            Buttons(problem);
        }

        /// <summary>The slide, which is the one thing here that happens without anybody doing it.</summary>
        protected override void Moved(int tick)
        {
            Slide(tick);
        }

        /// <summary>Moves the banner towards where it should be, by however many ticks have passed.</summary>
        /// <remarks>Costs one comparison a tick once it is there: a reveal invalidates only when its openness changes.</remarks>
        void Slide(int tick)
        {
            var since = _stepped < 0 ? 1 : tick - _stepped;
            _stepped = tick;

            if (since < 1)
                return;

            var step = since / Frames;

            _progress = Towards(_progress, _component.Leaving ? 0f : 1f, step);
            _opening = Towards(_opening, _component.Expanded ? 1f : 0f, step);

            _reveal.Openness = Eased(_progress);
            _more.Openness = Eased(_opening);

            // Whoever raised the banner waits for this to drop it.
            if (_component.Leaving && _reveal.Shut)
                _component.Closed = true;
        }

        void Buttons(bool problem)
        {
            var style = problem ? Styles.ProblemAction : Styles.NoticeAction;

            _actions.Collapsed = _component.Actions.Count == 0;

            if (_shown != _component.Actions.Count)
            {
                _shown = _component.Actions.Count;
                _buttons.Clear();
                _actions.Children.Clear();

                for (var i = 0; i < _component.Actions.Count; i++)
                {
                    var button = new Button();
                    _buttons.Add(button);
                    _actions.Children.Add(button);
                }
            }

            for (var i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Style = style;
                _buttons[i].Text = _component.Actions[i].Text;
                _buttons[i].Press = Deed.Sends(_component.Actions[i].Command);
            }
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
