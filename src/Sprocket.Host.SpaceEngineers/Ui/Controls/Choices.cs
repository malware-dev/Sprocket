using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>Everything a value may be, laid out to be picked from.</summary>
    /// <remarks>
    ///     Two shapes, and which one is used is decided here rather than by whoever puts one on a form,
    ///     because the only place the room available is known is where the measuring happens.
    ///     <para>
    ///         <b>A few short words are a row.</b> Drawn like the game's own switch, every word readable and
    ///         the one in force filled, so the choices can be read before one is made. That is the whole
    ///         reason not to use a box with the words listed beside it, where the only way to be wrong is to
    ///         be silently wrong.
    ///     </para>
    ///     <para>
    ///         <b>Anything longer is a closed box that opens a list.</b> Past about three the row stops being
    ///         something taken in at a glance and becomes a wall to search, and long words make a row wider
    ///         than the form whatever their number - an AI block carries several of these at once. A closed
    ///         box is one line whatever the list holds, and it is what a player has already met on the
    ///         terminal, where every one of these values is a drop-down.
    ///     </para>
    /// </remarks>
    public class Choices : Element
    {
        /// <summary>How many words still read as a row rather than as a list to search.</summary>
        const int Few = 3;

        readonly Label _chosen;
        readonly Bar _closed = new Bar();
        readonly List<Label> _lines = new List<Label>();
        readonly Stack _list;

        /// <summary>The mark on the shut box that says it opens.</summary>
        readonly Element _mark;

        readonly Wrap _row = new Wrap();
        readonly float _scale;
        readonly List<Label> _words = new List<Label>();
        bool _asRow = true;

        Element _hot;

        int _shown;

        public Choices(float scale)
        {
            _scale = scale;
            var children = new ElementCollection(this);

            _row.ItemSpacing = Space.Row;
            _row.LineSpacing = Space.Row;

            _chosen = Widgets.Label("", Palette.Ink, scale);
            _chosen.Wraps = false;

            _mark = Widgets.Centred(Symbols.Down, Palette.Faint);

            _closed.Spacing = Space.Item;
            _closed.Fill = _chosen;
            _closed.Right = _mark;
            _closed.Background = Palette.ControlHot;
            _closed.Highlight = Palette.Panel;
            _closed.HighlightEdge = Palette.Muted;
            _closed.Edge = Palette.Line;
            _closed.EdgeThickness = Space.Hair;
            _closed.Padding = new Thickness(Space.ChipX, Space.FieldY);

            _list = Widgets.Column(0f);
            _list.Background = Palette.ControlHot;
            _list.Edge = Palette.Line;
            _list.EdgeThickness = Space.Hair;

            children.Add(_row);
            children.Add(_closed);
        }

        /// <summary>Whether the list is hanging open under the box.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        ///     What there is to choose from, in the order the block listed them.
        /// </summary>
        /// <remarks>
        ///     Rebuilt only where the words have actually changed, because a question is dressed on every
        ///     tick and the words on one cannot move while it is open.
        /// </remarks>
        public void Says(IList<string> words)
        {
            if (Same(words))
                return;

            _row.Children.Clear();
            _list.Children.Clear();

            for (var i = 0; i < words.Count; i++)
            {
                while (_words.Count <= i)
                {
                    _words.Add(Chip());
                    _lines.Add(Line());
                }

                _words[i].Text = words[i];
                _lines[i].Text = words[i];

                _row.Children.Add(_words[i]);
                _list.Children.Add(_lines[i]);
            }

            _shown = words.Count;
            IsOpen = false;
            Lit(Vector2.Zero);
            InvalidateMeasure();
        }

        /// <summary>Marks the one in force, or none of them where nobody has chosen.</summary>
        public void Show(string chosen)
        {
            for (var i = 0; i < _shown && i < _words.Count; i++)
            {
                var lit = _words[i].Text == chosen;

                _words[i].Background = lit ? Palette.SwitchLit : Palette.SwitchDark;
                _words[i].Color = lit ? Palette.Ink : Palette.Faint;

                // Nothing on the one already in force: it is the answer rather than a way to
                // another one, and a highlight would offer to do what has already been done.
                _words[i].Highlight = lit ? Palette.None : Palette.Panel;
                _lines[i].Color = lit ? Palette.Ink : Palette.Muted;
            }

            _chosen.Text = Holding(chosen) ? chosen : "";
        }

        /// <summary>
        ///     Lights whichever part of this the pointer is resting on.
        /// </summary>
        /// <remarks>
        ///     Done here because the presses are taken here. The screen lights what a press would reach
        ///     by finding it the same way a press does, and this control is the one thing a press does
        ///     not reach that way - so it owes the other half of the bargain: everything in here that
        ///     answers a press says so under the pointer.
        /// </remarks>
        public void Lit(Vector2 point)
        {
            Element hot = null;

            if (_asRow)
                hot = Among(_words, point);
            else if (IsOpen && _list.Holds(point))
                hot = Among(_lines, point);
            else if (_closed.Holds(point))
                hot = _closed;

            if (ReferenceEquals(hot, _hot))
                return;

            if (_hot != null)
                _hot.IsHot = false;

            _hot = hot;

            if (_hot != null)
                _hot.IsHot = true;
        }

        static Element Among(List<Label> parts, Vector2 point)
        {
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].Collapsed && parts[i].Holds(point))
                    return parts[i];
            }

            return null;
        }

        /// <summary>
        ///     Whether a point is on the open list, which is outside this control's own bounds.
        /// </summary>
        /// <remarks>
        ///     The list hangs under the box rather than inside it, so anything asking what a press landed
        ///     on has to ask this as well - a form that only knew its own bounds would read a press on
        ///     the list as a press outside the question, and put the question away.
        /// </remarks>
        public bool Over(Vector2 point)
        {
            return IsOpen && !_asRow && _list.Holds(point);
        }

        /// <summary>Puts the list away, for a press that went somewhere else.</summary>
        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>
        ///     Takes the press, and says which word it chose where it chose one.
        /// </summary>
        /// <remarks>
        ///     Answers true for a press that only opened or shut the list as well, because the press did
        ///     land on this control and must not go on to mean something else as well.
        /// </remarks>
        public bool Took(Vector2 point, out string word)
        {
            word = "";
            if (Collapsed)
                return false;

            if (_asRow)
                return Picked(_words, point, out word);

            if (IsOpen && Picked(_lines, point, out word))
            {
                IsOpen = false;
                return true;
            }

            if (!_closed.Holds(point))
                return false;

            IsOpen = !IsOpen;
            return true;
        }

        /// <summary>
        ///     Draws the open list, over everything the form has already drawn.
        /// </summary>
        /// <remarks>
        ///     Drawn by whoever owns the form rather than in this control's own drawing, because a list
        ///     hanging under a box hangs over whatever the form put below it. Drawn where this control
        ///     ended up, which is only known once the form has been arranged.
        /// </remarks>
        public void Hanging(IGraphics g)
        {
            if (!IsOpen || _asRow)
                return;

            _list.Measure(g, new Vector2(Bounds.Width, Bounds.Height * _shown + Bounds.Height));
            _list.Arrange(new RectangleF(Bounds.X, Bounds.Y + Bounds.Height,
                Bounds.Width, _list.DesiredSize.Y));

            _list.Draw(g);
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _row.Measure(g, available);

            // Decided here and nowhere else: this is the one place that knows both how many words
            // there are and how much room they have been given.
            _asRow = _shown <= Few && _row.DesiredSize.X <= available.X;

            _row.Collapsed = !_asRow;
            _closed.Collapsed = _asRow;

            if (_asRow)
                return _row.DesiredSize;

            _closed.Measure(g, available);

            // As wide as the longest word it can hold and the mark beside it, whichever word is chosen,
            // so choosing one does not move the box and a box on the right of a line stays there.
            var widest = 0f;
            for (var i = 0; i < _row.Children.Count; i++)
            {
                if (_row.Children[i].DesiredSize.X > widest)
                    widest = _row.Children[i].DesiredSize.X;
            }

            // And as tall as a box, whatever the mark beside the word is drawn at, because everything a
            // value is given in is one height.
            var wide = widest + Space.Item + _mark.DesiredSize.X;
            var tall = _chosen.DesiredSize.Y + Space.FieldY * 2f;
            return new Vector2(wide < available.X ? wide : available.X, tall);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_asRow)
                _row.Arrange(bounds);
            else
                _closed.Arrange(bounds);
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_asRow)
                _row.Draw(g);
            else
                _closed.Draw(g);
        }

        bool Holding(string word)
        {
            for (var i = 0; i < _shown && i < _words.Count; i++)
            {
                if (_words[i].Text == word)
                    return true;
            }

            return false;
        }

        bool Picked(List<Label> among, Vector2 point, out string word)
        {
            word = "";
            for (var i = 0; i < _shown && i < among.Count; i++)
            {
                if (!among[i].Holds(point))
                    continue;

                word = among[i].Text;
                return true;
            }

            return false;
        }

        bool Same(IList<string> words)
        {
            if (words == null || words.Count != _shown)
                return false;

            for (var i = 0; i < words.Count && i < _words.Count; i++)
            {
                if (_words[i].Text != words[i])
                    return false;
            }

            return true;
        }

        Label Chip()
        {
            var made = Widgets.Label("", Palette.Faint, _scale);
            made.Padding = new Thickness(Space.ChipX, Space.FieldY);
            made.Wraps = false;
            made.Background = Palette.SwitchDark;
            made.Edge = Palette.Dim;
            made.EdgeThickness = Space.Hair;
            return made;
        }

        Label Line()
        {
            var made = Widgets.Label("", Palette.Muted, _scale);
            made.Padding = new Thickness(Space.ChipX, Space.FieldY);
            made.Wraps = false;
            made.Highlight = Palette.Panel;
            return made;
        }
    }
}