using System.Collections.Generic;
using VRageMath;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A banner that slides in, can be opened for the rest of it, and slides out when it is done.
    /// </summary>
    /// <remarks>
    /// <b>Nothing on this screen appears or vanishes.</b> A banner that took its full height on the
    /// frame it arrived would push every row below it down by that much, and a list that moves under a
    /// pointer is a list somebody presses the wrong row in. So it is drawn inside a
    /// <see cref="Reveal" /> and the height is animated, both ways.
    /// <para>
    /// The speed is in openness per tick rather than per call, so it takes the same time whatever the
    /// game's update rate is doing - and it is stepped from the tick it was handed, not from a clock of
    /// its own.
    /// </para>
    /// </remarks>
    public sealed class NoticeView : ComponentView
    {
        /// <summary>
        /// How many of the game's frames the slide takes.
        /// </summary>
        /// <remarks>
        /// The session ticks every frame, so this is a fifth of a second at sixty. One number for
        /// arriving, leaving and opening, so the banner never has two speeds.
        /// </remarks>
        const float Frames = 12f;

        readonly NoticeComponent _component;

        readonly Reveal _reveal = new Reveal();
        readonly Stack _column = Widgets.Column(0f);

        /// <summary>
        /// The opening half, which is its own slide.
        /// </summary>
        /// <remarks>
        /// Two reveals rather than one: the outer one is the banner arriving, the inner one is somebody
        /// opening it. Collapsing the detail instead of revealing it changed the height between two
        /// frames, which is the same jump the outer one exists to avoid - and the one that gets noticed,
        /// because it happens under a pointer that has just pressed something.
        /// </remarks>
        readonly Reveal _more = new Reveal();

        readonly Stack _rest = Widgets.Column(0f);
        readonly Bar _heading = new Bar();
        readonly Label _text = Widgets.Label("", Palette.Problem, Lettering.Small);
        readonly Glyph _chevron = Widgets.Symbol(Symbols.Down, Palette.Problem);
        readonly Label _detail = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly Label _diagnostic = Widgets.Label("", Palette.Dim, Lettering.Tiny);
        readonly Stack _actions = Widgets.RowOf(Space.Item);
        readonly List<Label> _buttons = new List<Label>();

        int _stepped = -1;
        int _shown = -1;

        /// <summary>How far along the slide is, before the easing is applied to it.</summary>
        float _progress;

        /// <summary>The same for the opening, which moves at the same rate and is a separate thing.</summary>
        float _opening;

        public NoticeView(NoticeComponent component)
        {
            _component = component;

            // The chevron at the far edge rather than beside the words, because the whole strip is the
            // control: a banner is pressed anywhere along it, and a mark tucked against the text reads
            // as punctuation.
            _heading.Fill = _text;
            _heading.Right = _chevron;
            _heading.Spacing = Space.Item;
            _heading.Padding = new Thickness(Space.ChipX, Space.ChipY * 1.6f);

            _detail.Padding = new Thickness(Space.ChipX, 0f, Space.ChipX, Space.ChipY * 1.6f);

            _diagnostic.Padding = new Thickness(Space.ChipX, 0f, Space.ChipX, Space.ChipY * 1.6f);

            _rest.Children.Add(_detail);
            _rest.Children.Add(_diagnostic);
            _rest.Children.Add(_actions);
            _more.Child = _rest;

            _column.Children.Add(_heading);
            _column.Children.Add(_more);

            _reveal.Child = _column;
        }

        public override Element Root
        {
            get { return _reveal; }
        }

        public override void Update(int tick)
        {
            bool problem = _component.Level == NoticeLevel.Problem;
            Color ink = problem ? Palette.Problem : Palette.Notice;
            Color wash = problem ? Palette.ProblemWash : Palette.NoticeWash;

            // The wash goes on the whole block rather than on the words, so it runs the width of the
            // screen: a banner that stopped where its sentence stopped read as a chip somebody had left
            // at the top of the page.
            _column.Background = wash;

            _text.Text = _component.Text;
            _text.Color = ink;

            _detail.Text = _component.Detail;
            _detail.Collapsed = _component.Detail.Length == 0;

            _diagnostic.Text = _component.Diagnostic;
            _diagnostic.Collapsed = _component.Diagnostic.Length == 0;

            // Pointing the way it will go, which is how every disclosure in every product says it.
            _chevron.Symbol = _component.Expanded ? Symbols.Up : Symbols.Down;
            _chevron.Tint = ink;
            _chevron.Collapsed = _component.ExpandCommand == 0;

            // Both of these sit inside the reveal, which answers nothing while it is moving - a button
            // still arriving is a button pressed by accident on the way to what was underneath it.
            _heading.Press = Deed.Sends(_component.ExpandCommand);

            Buttons(ink);
            Slide(tick);
        }

        /// <summary>
        /// Moves the banner towards where it should be, by however many ticks have passed.
        /// </summary>
        /// <remarks>
        /// Idle once it is there: the reveal only invalidates a measure when the number actually
        /// changes, so a banner that has finished arriving costs one comparison a tick.
        /// </remarks>
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

            // Said once it is all the way out, because whoever raised it is waiting to drop it and has
            // no other way to know the slide is over.
            if (_component.Leaving && _reveal.Shut)
                _component.Closed = true;
        }

        void Buttons(Color ink)
        {

            _actions.Collapsed = _component.Actions.Count == 0;

            if (_shown == _component.Actions.Count)
            {
                for (int i = 0; i < _buttons.Count; i++)
                {
                    _buttons[i].Text = _component.Actions[i].Text;
                    _buttons[i].Color = ink;
                    _buttons[i].Press = Deed.Sends(_component.Actions[i].Command);
                }

                return;
            }

            _shown = _component.Actions.Count;
            _buttons.Clear();
            _actions.Children.Clear();
            _actions.Padding = new Thickness(Space.ChipX, 0f, Space.ChipX, Space.ChipY);

            for (int i = 0; i < _component.Actions.Count; i++)
            {
                // Filled, because it is standing on the banner's wash rather than on the page. At the
                // size everything else in here is: a banner is a line and a half, not a dialog.
                Label button = Widgets.Filled(Widgets.Ghost(_component.Actions[i].Text));
                button.Color = ink;
                button.Press = Deed.Sends(_component.Actions[i].Command);
                _buttons.Add(button);
                _actions.Children.Add(button);
            }
        }

        static float Towards(float value, float wanted, float step)
        {
            return value < wanted ? Least(value + step, wanted) : Most(value - step, wanted);
        }

        /// <summary>
        /// Most of the movement early, settling rather than stopping.
        /// </summary>
        /// <remarks>
        /// A linear slide this short reads as something being dragged. Cubed, so the last tenth of the
        /// distance takes about as long as the first half and the whole thing looks like it arrived
        /// rather than that it was moved.
        /// </remarks>
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
