using System.Text;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// A name that can be changed where it stands: the words, a pencil beside them, and the box that
    /// replaces the words while somebody is typing.
    /// <para>
    /// One element rather than three arranged by whoever wanted them, because the awkward part is not any
    /// of the three - it is that the box has to land exactly where the label was. Every caller that built
    /// this by hand ended up rediscovering the same two facts: the box takes no padding of its own because
    /// it stands where the words stood, and it takes no border because the words had none. Getting either
    /// wrong moves the heading sideways at the moment somebody starts typing into it.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The pencil keeps its room while the box is up rather than being taken out of the layout. A glyph
    /// drawn in nothing already does exactly that - see <see cref="Glyph.Tint" /> - and it is the whole of
    /// why the flip between the two modes moves nothing at all.
    /// <para>
    /// It knows nothing about keyboards or focus. Something else decides that an edit has begun and that it
    /// has finished; this owns only what the two states look like. That is the same split
    /// <see cref="TextBox" /> already makes and for the same reason.
    /// </para>
    /// </remarks>
    public class Nameplate : Element
    {
        private readonly ElementCollection _children;
        private readonly Label _label = new Label();
        private readonly TextBox _box = new TextBox();
        private readonly Glyph _pen;

        /// <summary>Reused for measuring the hint, so a frame of drawing one costs no allocation.</summary>
        private readonly StringBuilder _measuring = new StringBuilder(32);

        private bool _editing;

        public Nameplate(string pen, float penScale)
        {
            _children = new ElementCollection(this);

            // A label can be told to trim and a box cannot: what somebody is typing has to stay where they
            // typed it, so it runs on past the end and the clip is what stops it. Without this a long name
            // wrote itself out across the chrome and over whatever was beside it.
            ClipsContent = true;

            _label.Wraps = false;
            _label.Trims = true;

            // Neither of these is a default worth having anywhere else. They are what makes the box stand
            // where the words stand, which is this element's entire reason for existing.
            _box.Padding = new Thickness();
            _box.Edge = new Color(0, 0, 0, 0);

            _pen = new Glyph(pen, penScale, Color.White);

            _children.Add(_label);
            _children.Add(_box);
            _children.Add(_pen);
        }

        /// <summary>The name, as it reads when nobody is editing it.</summary>
        /// <remarks>
        /// Not written through to the box while an edit is running. Whatever is in the box is the truth for
        /// as long as somebody is typing, and putting the model's answer back over the top would fight the
        /// caret on every keystroke.
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

        /// <summary>What the box says while it is up. The words the edit will commit.</summary>
        public string Editing
        {
            get { return _box.Text; }
        }

        /// <summary>
        /// The box itself, for whoever gives it the keyboard.
        /// </summary>
        /// <remarks>
        /// Handed out rather than wrapped in forwarding methods. The thing driving it needs a box and this
        /// has one; a plate that reimplemented caret movement so as not to admit that would be worse.
        /// </remarks>
        public TextBox Box
        {
            get { return _box; }
        }

        public string Font
        {
            get { return _label.Font; }
            set
            {
                _label.Font = value;
                _box.Font = value;
            }
        }

        public float Scale
        {
            get { return _label.Scale; }
            set
            {
                _label.Scale = value;
                _box.Scale = value;
            }
        }

        /// <summary>The words, in both modes. The caret takes the same colour.</summary>
        public Color Color
        {
            get { return _label.Color; }
            set
            {
                _label.Color = value;
                _box.Color = value;
                _box.CaretColor = value;
            }
        }

        /// <summary>
        /// The pencil. Transparent for a name that cannot be changed, which then draws as a plain heading.
        /// </summary>
        /// <remarks>
        /// Its room is kept either way, so a screen where some names can be renamed and others cannot does
        /// not have its headings start at two different places.
        /// </remarks>
        public Color PenColor { get; set; }

        public Color SelectionColor
        {
            get { return _box.SelectionColor; }
            set { _box.SelectionColor = value; }
        }

        /// <summary>Shown in place of a name that has not been given yet.</summary>
        public string Hint
        {
            get { return _box.Hint; }
            set
            {
                if (_box.Hint == value)
                    return;

                _box.Hint = value;
                _measuring.Length = 0;

                if (!string.IsNullOrEmpty(value))
                    _measuring.Append(value);
            }
        }

        public Color HintColor
        {
            get { return _box.HintColor; }
            set { _box.HintColor = value; }
        }

        /// <summary>
        /// What the words sit in while they are being typed. Transparent for a plate that shows no well.
        /// </summary>
        /// <remarks>
        /// Only ever painted while the box is up, because only the box is drawn then - so this is what
        /// tells somebody the heading has become a field, and it is the only thing that does. It covers
        /// the words and not the pencil, which is right: the pencil is not part of what you are typing.
        /// </remarks>
        public Color Well { get; set; }

        /// <summary>
        /// Whether what is in the box cannot be accepted as it stands.
        /// </summary>
        /// <remarks>
        /// Set by whoever knows the rule; this only wears it. A plate that decided for itself what counted
        /// as a good name would be a plate that had to be told about systems, sequences and everything
        /// after them.
        /// </remarks>
        public bool Wrong { get; set; }

        /// <summary>The well while <see cref="Wrong" />.</summary>
        public Color WrongWell { get; set; }

        /// <summary>
        /// A line under the words while <see cref="Wrong" />, and the only mark that has to be loud.
        /// </summary>
        /// <remarks>
        /// Under rather than around, which is the same choice a slot in a sentence makes and for the same
        /// reason: a border one unit wide is drawn inside the box, and with no padding to give it room it
        /// would sit against the first letter. A rule beneath touches nothing.
        /// </remarks>
        public Color WrongEdge { get; set; }

        /// <summary>How thick that line is.</summary>
        public float WrongEdgeThickness { get; set; }

        /// <summary>Room between the name and the pencil.</summary>
        public float Gap { get; set; }

        /// <summary>
        /// How narrow the name is allowed to get.
        /// </summary>
        /// <remarks>
        /// A name somebody has emptied measures nothing, and a box with no width is a box nobody can find
        /// to type into. That is the whole of what this is for.
        /// <para>
        /// It is <i>not</i> what keeps the plate steady while somebody deletes - the measure already takes
        /// the wider of the label and the box, and the label goes on holding the name the edit started
        /// with. This only matters where there was little or nothing there to begin with.
        /// </para>
        /// </remarks>
        public float LeastWidth { get; set; }

        /// <summary>Whether the box is up. Set it through <see cref="Begin" /> and <see cref="End" />.</summary>
        public bool IsEditing
        {
            get { return _editing; }
        }

        /// <summary>Puts the box up, holding the name it started with and all of it selected.</summary>
        /// <remarks>
        /// Selected rather than with the caret parked at the end, because renaming something is usually
        /// replacing the name rather than appending to it - and the one case is a keystroke away from the
        /// other, where the reverse costs a select-all nobody thought to do.
        /// </remarks>
        public void Begin()
        {
            _editing = true;
            _box.Text = _label.Text;
            _box.SelectAll();

            // The box draws its caret only while it believes an edit is running, and the plate is the one
            // that knows. Left to the caller this was a line easy to leave out, and leaving it out gives a
            // field somebody can type into with nothing marking where the next letter goes.
            _box.IsEditing = true;
        }

        /// <summary>Puts the box away. Whether anything was kept is the caller's business, not this one's.</summary>
        public void End()
        {
            _editing = false;
            _box.IsEditing = false;
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _pen.Measure(g, available);

            float aside = _pen.DesiredSize.X + Gap;
            Vector2 room = new Vector2(available.X - aside, available.Y);
            if (room.X < 0f)
                room.X = 0f;

            // Both, every time, and the wider of the two wins. Measuring only the one on show would let the
            // plate change width at the moment of the flip, which is the one thing this element exists to
            // prevent - and while an edit is running the two disagree by however much has been typed.
            _label.Measure(g, room);
            _box.Measure(g, room);

            float name = _label.DesiredSize.X > _box.DesiredSize.X
                ? _label.DesiredSize.X
                : _box.DesiredSize.X;

            // A box measures the words in it and an empty one measures nothing, so a hint drawn in an
            // empty box is drawn in a plate that made no room for it - which is how the first hint here
            // came to be written out across the chrome, and how the clip then came to cut it off instead.
            // Neither is the answer. If it is going to be shown, it is part of how wide this is.
            if (_editing && _measuring.Length > 0 && _box.Text.Length == 0)
            {
                float hint = g.MeasureText(_label.Font, _measuring, _label.Scale).X;
                if (hint > name)
                    name = hint;
            }

            if (name < LeastWidth)
                name = LeastWidth;

            float height = _label.DesiredSize.Y > _box.DesiredSize.Y
                ? _label.DesiredSize.Y
                : _box.DesiredSize.Y;

            if (_pen.DesiredSize.Y > height)
                height = _pen.DesiredSize.Y;

            return new Vector2(name + aside, height);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float pen = _pen.DesiredSize.X;
            float name = bounds.Width - pen - Gap;
            if (name < 0f)
                name = 0f;

            var where = new RectangleF(bounds.X, bounds.Y, name, bounds.Height);
            _label.Arrange(where);
            _box.Arrange(where);

            // On the middle of the line, the way a stack would put it. A pencil hung from the top of a
            // heading reads as a superscript rather than as part of the name.
            float top = bounds.Y + (bounds.Height - _pen.DesiredSize.Y) * 0.5f;
            _pen.Arrange(new RectangleF(
                bounds.X + bounds.Width - pen, top, pen, _pen.DesiredSize.Y));
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_editing)
            {
                _box.Background = Wrong && WrongWell.A > 0 ? WrongWell : Well;
                _box.Draw(g);

                if (Wrong && WrongEdge.A > 0)
                {
                    float thick = WrongEdgeThickness > 0f ? WrongEdgeThickness : 1f;
                    RectangleF box = _box.Bounds;

                    g.DrawSprite(Solid, new RectangleF(
                        box.X, box.Y + box.Height - thick, box.Width, thick), WrongEdge, 0f);
                }
            }
            else
            {
                _label.Draw(g);
            }

            // Nothing while the box is up: the box says it is being edited far better than a picture of a
            // pencil beside it could, and two ways of saying it is one too many.
            _pen.Tint = _editing ? new Color(0, 0, 0, 0) : PenColor;
            _pen.Draw(g);
        }
    }
}
