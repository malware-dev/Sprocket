using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>
    ///     A name that can be changed where it stands: the words, a pencil beside them, and the box that
    ///     replaces the words while somebody is typing.
    /// </summary>
    public class Nameplate : Element
    {
        readonly Label _label = new Label();

        /// <summary>The hint, held for measuring, since measuring from a builder allocates nothing.</summary>
        readonly StringBuilder _measuring = new StringBuilder(32);

        readonly Glyph _pen;

        float _gap;
        float _leastWidth;
        Color _penColor;

        /// <summary>
        ///     Makes a nameplate whose pencil is the symbol <paramref name="pen" /> at <paramref name="penSize" />.
        /// </summary>
        public Nameplate(string pen, float penSize)
        {
            var children = new ElementCollection(this);

            // A box cannot trim, so what is typed runs on past the end and the clip stops it.
            ClipsContent = true;

            _label.Wraps = false;
            _label.Trims = true;

            // No padding and no edge, so the box stands exactly where the words stood.
            Box.Padding = new Thickness();
            Box.Edge = new Color(0, 0, 0, 0);

            _pen = new Glyph(pen, penSize, Color.White);

            children.Add(_label);
            children.Add(Box);
            children.Add(_pen);
        }

        /// <summary>How far the pencil is lifted off the middle of its room, as <see cref="Glyph.Lift" />.</summary>
        public float PenLift
        {
            get { return _pen.Lift; }
            set { _pen.Lift = value; }
        }

        /// <summary>The name, as it reads when nobody is editing it.</summary>
        /// <remarks>
        ///     Never written through to the box, which holds the truth while an edit runs; writing over it
        ///     would fight the caret on every keystroke.
        /// </remarks>
        public string Text
        {
            get { return _label.Text; }
            set
            {
                if (_label.Text == value)
                    return;

                _label.Text = value;
            }
        }

        /// <summary>What the box says, which is what the edit will commit.</summary>
        public string Editing => Box.Text;

        /// <summary>The box itself, for whoever gives it the keyboard.</summary>
        public TextBox Box { get; } = new TextBox();

        /// <summary>The font of the words, in both modes.</summary>
        public string Font
        {
            get { return _label.Font; }
            set
            {
                _label.Font = value;
                Box.Font = value;
            }
        }

        /// <summary>How tall a line of the words is, in UI units, in both modes.</summary>
        public float FontSize
        {
            get { return _label.FontSize; }
            set
            {
                _label.FontSize = value;
                Box.FontSize = value;
            }
        }

        /// <summary>The colour of the words in both modes, and of the caret.</summary>
        public Color Color
        {
            get { return _label.Color; }
            set
            {
                _label.Color = value;
                Box.Color = value;
                Box.CaretColor = value;
            }
        }

        /// <summary>The pencil's colour, transparent for a name that cannot be changed.</summary>
        public Color PenColor
        {
            get { return _penColor; }
            set
            {
                if (_penColor.R == value.R && _penColor.G == value.G
                                           && _penColor.B == value.B && _penColor.A == value.A)
                    return;

                _penColor = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The colour behind text selected in the box.</summary>
        public Color SelectionColor
        {
            get { return Box.SelectionColor; }
            set { Box.SelectionColor = value; }
        }

        /// <summary>Shown in place of a name that has not been given yet.</summary>
        public string Hint
        {
            get { return Box.Hint; }
            set
            {
                if (Box.Hint == value)
                    return;

                Box.Hint = value;
                _measuring.Length = 0;
                InvalidateMeasure();

                if (!string.IsNullOrEmpty(value))
                    _measuring.Append(value);
            }
        }

        /// <summary>The colour the hint is drawn in.</summary>
        public Color HintColor
        {
            get { return Box.HintColor; }
            set { Box.HintColor = value; }
        }

        /// <summary>What the words sit in while they are being typed.</summary>
        public Color Well { get; set; }

        /// <summary>Whether what is in the box cannot be accepted as it stands.</summary>
        public bool Wrong { get; set; }

        /// <summary>The well while <see cref="Wrong" />.</summary>
        public Color WrongWell { get; set; }

        /// <summary>A line under the words while <see cref="Wrong" />.</summary>
        /// <remarks>
        ///     Under the words rather than an edge round the box, which with no padding would touch the
        ///     first letter.
        /// </remarks>
        public Color WrongEdge { get; set; }

        /// <summary>How thick that line is.</summary>
        public float WrongEdgeThickness { get; set; }

        /// <summary>Room between the name and the pencil.</summary>
        public float Gap
        {
            get { return _gap; }
            set
            {
                if (_gap == value)
                    return;

                _gap = value;
                InvalidateMeasure();
            }
        }

        /// <summary>How narrow the name is allowed to get, one line's height when left at zero.</summary>
        public float LeastWidth
        {
            get { return _leastWidth; }
            set
            {
                if (_leastWidth == value)
                    return;

                _leastWidth = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether the box is up.</summary>
        public bool IsEditing { get; private set; }

        /// <summary>Puts the box up, holding the name it started with and all of it selected.</summary>
        public void Begin()
        {
            IsEditing = true;
            InvalidateMeasure();

            Box.Text = _label.Text;
            Box.SelectAll();
            Box.IsEditing = true;
        }

        /// <summary>Puts the box away; keeping what was typed is the caller's business.</summary>
        public void End()
        {
            IsEditing = false;
            Box.IsEditing = false;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _pen.Measure(g, available);

            // Room for the pencil only where it can be drawn, and kept while editing, when it is hidden,
            // so the words do not jog sideways as typing starts.
            var pen = IsEditing || PenColor.A > 0;
            var aside = pen ? _pen.DesiredSize.X + Gap : 0f;
            var room = new Vector2(available.X - aside, available.Y);
            if (room.X < 0f)
                room.X = 0f;

            // Both are measured because both are arranged. The box counts toward the width only while
            // editing, since it keeps what was last typed into it; then both count, so the plate never
            // narrows under the pointer.
            _label.Measure(g, room);
            Box.Measure(g, room);

            var line = _label.FontSize;

            var name = _label.DesiredSize.X;
            if (IsEditing && Box.DesiredSize.X > name)
                name = Box.DesiredSize.X;

            // An empty box measures nothing, so the hint it shows is measured here or the clip cuts it off.
            if (IsEditing && _measuring.Length > 0 && Box.Text.Length == 0)
            {
                var hint = g.MeasureText(_label.Font, _measuring, _label.FontSize).X;
                if (hint > name)
                    name = hint;
            }

            var least = LeastWidth > 0f ? LeastWidth : line;
            if (name < least)
                name = least;

            // Always one line of this font at this size, whatever the parts measure: a row centres what is
            // in it, so a taller plate would sit its words above its neighbours'. A symbol measures its
            // whole square cell, which is taller than the line.
            return new Vector2(name + aside, line);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var pen = IsEditing || PenColor.A > 0 ? _pen.DesiredSize.X : 0f;
            var name = bounds.Width - pen - (pen > 0f ? Gap : 0f);
            if (name < 0f)
                name = 0f;

            // Every part is centred on the line, so a part taller than the line overhangs both ends evenly.
            Centre(_label, bounds, bounds.X, name);
            Centre(Box, bounds, bounds.X, name);
            Centre(_pen, bounds, bounds.X + bounds.Width - pen, pen);
        }

        /// <summary>Puts one part on the middle of the plate's line, at its own height.</summary>
        static void Centre(Element part, RectangleF bounds, float x, float width)
        {
            var height = part.DesiredSize.Y;
            if (height <= 0f)
                height = bounds.Height;

            part.Arrange(new RectangleF(x, bounds.Y + (bounds.Height - height) * 0.5f, width, height));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (IsEditing)
            {
                Box.Background = Wrong && WrongWell.A > 0 ? WrongWell : Well;
                Box.Draw(g);

                if (Wrong && WrongEdge.A > 0)
                {
                    var thick = WrongEdgeThickness > 0f ? WrongEdgeThickness : 1f;
                    var box = Box.Bounds;

                    g.DrawSprite(Solid, new RectangleF(
                        box.X, box.Y + box.Height - thick, box.Width, thick), WrongEdge, 0f);
                }
            }
            else
                _label.Draw(g);

            _pen.Tint = IsEditing ? new Color(0, 0, 0, 0) : PenColor;
            _pen.Draw(g);
        }
    }
}