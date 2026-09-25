using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>Everything a value may be, laid out as a row of words or as a box that opens a list.</summary>
    public class Choices : Element
    {
        /// <summary>The most words shown as a row.</summary>
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

        /// <summary>Makes a set of choices whose words are drawn at <paramref name="fontSize" />.</summary>
        public Choices(float fontSize)
        {
            _scale = fontSize;
            var children = new ElementCollection(this);

            _row.ItemSpacing = Space.Row;
            _row.LineSpacing = Space.Row;

            _chosen = Widgets.Label("", Palette.Ink, fontSize);
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

        /// <summary>What there is to choose from.</summary>
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

                _words[i].Highlight = lit ? Palette.None : Palette.Panel;
                _lines[i].Color = lit ? Palette.Ink : Palette.Muted;
            }

            _chosen.Text = Holding(chosen) ? chosen : "";
        }

        /// <summary>Lights whichever part of this the pointer is resting on.</summary>
        /// <remarks>This control takes its own presses, so the screen's pointer walk never lights its parts.</remarks>
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

        /// <summary>Whether a point is on the open list, which is outside this control's own bounds.</summary>
        /// <remarks>Whatever decides where a press landed has to ask this as well as the bounds.</remarks>
        public bool Over(Vector2 point)
        {
            return IsOpen && !_asRow && _list.Holds(point);
        }

        /// <summary>Puts the list away, for a press that went somewhere else.</summary>
        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>Takes the press, and says which word it chose where it chose one.</summary>
        /// <remarks>True also for a press that only opened or shut the list, so that it goes no further.</remarks>
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

        /// <summary>Draws the open list, over everything the form has already drawn.</summary>
        /// <remarks>Called by whoever owns the form, after the form is arranged and drawn.</remarks>
        public void Hanging(IGraphics g)
        {
            if (!IsOpen || _asRow)
                return;

            _list.Measure(g, new Vector2(Bounds.Width, Bounds.Height * _shown + Bounds.Height));
            _list.Arrange(new RectangleF(Bounds.X, Bounds.Y + Bounds.Height,
                Bounds.Width, _list.DesiredSize.Y));

            _list.Draw(g);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _row.Measure(g, available);

            _asRow = _shown <= Few && _row.DesiredSize.X <= available.X;

            _row.Collapsed = !_asRow;
            _closed.Collapsed = _asRow;

            if (_asRow)
                return _row.DesiredSize;

            _closed.Measure(g, available);

            // As wide as the longest word, whichever is chosen, so choosing one does not resize the box.
            var widest = 0f;
            for (var i = 0; i < _row.Children.Count; i++)
            {
                if (_row.Children[i].DesiredSize.X > widest)
                    widest = _row.Children[i].DesiredSize.X;
            }

            // As tall as every other box a value is given in, whatever the mark is drawn at.
            var wide = widest + Space.Item + _mark.DesiredSize.X;
            var tall = _chosen.DesiredSize.Y + Space.FieldY * 2f;
            return new Vector2(wide < available.X ? wide : available.X, tall);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_asRow)
                _row.Arrange(bounds);
            else
                _closed.Arrange(bounds);
        }

        /// <inheritdoc />
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