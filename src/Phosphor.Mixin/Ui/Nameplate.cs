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
        /// A plate with no pencil takes no room for one. That was the other way round for a while, on the
        /// reasoning that headings on different screens should start in the same place - which is wrong
        /// twice over: the pencil is a suffix, so dropping it moves nothing to its left, and a heading made
        /// of several parts had the un-nameable ones holding a pencil-shaped hole in the middle of the
        /// line. It read as forty units of unexplained space where a word gap is sixteen.
        /// <para>
        /// The room is kept while the box is up, though, because there the pencil really does come and go
        /// under somebody's hands - see <see cref="DrawContent" />.
        /// </para>
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
        /// How narrow the name is allowed to get. A line tall's worth when nobody says otherwise.
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

            // Nothing set aside for a pencil this plate will never draw. While it is being edited the
            // pencil is hidden rather than absent, and there the room has to stay or the words jog
            // sideways at the moment somebody starts typing.
            bool pen = _editing || PenColor.A > 0;
            float aside = pen ? _pen.DesiredSize.X + Gap : 0f;
            Vector2 room = new Vector2(available.X - aside, available.Y);
            if (room.X < 0f)
                room.X = 0f;

            // Both, every time, because the one that is not showing still has to be arranged. Only the
            // one on show gets a say in how wide this is, though - and while an edit is running, both do,
            // so the plate never narrows under the pointer as characters come out of the box.
            //
            // Letting the box speak while it was put away was a real fault and an invisible one. A box
            // keeps what was last typed into it, so a plate that had once held a longer name stayed as
            // wide as that name for the rest of the session: rename "Large Grid 6883" to "Test Rig A" and
            // the pencil stopped seventy units to the right of the words, holding a space for letters that
            // had been deleted.
            _label.Measure(g, room);
            _box.Measure(g, room);

            float line = g.LineHeight(_label.Font, _label.Scale);

            float name = _label.DesiredSize.X;
            if (_editing && _box.DesiredSize.X > name)
                name = _box.DesiredSize.X;

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

            // No wider than the plate is tall unless somebody insists. A field that has been emptied has
            // to stay findable and that is all it has to do; anything past a square of it is room held for
            // words that are not there, sitting between the name and the pencil.
            float least = LeastWidth > 0f ? LeastWidth : line;
            if (name < least)
                name = least;

            // One line of this font at this scale, and nothing in the plate can change it. Not the longest
            // name anybody types, not whether the pencil is showing, not which of the two modes is up.
            //
            // That is the invariant the whole thing rests on. A plate has to be the same height as every
            // other plate and as a plain label beside it, because a row centres what is in it - so a plate
            // a few units taller than its neighbour puts its words a couple of units above them, and a
            // heading reads as though somebody had nudged half of it. It was measured from the tallest of
            // the label, the box and the pencil, and the pencil won: a symbol measures its whole square
            // cell, and that cell stands clear of the text line at any size worth drawing it.
            return new Vector2(name + aside, line);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float pen = _editing || PenColor.A > 0 ? _pen.DesiredSize.X : 0f;
            float name = bounds.Width - pen - (pen > 0f ? Gap : 0f);
            if (name < 0f)
                name = 0f;

            // Everything on the middle of the line, including the words. Given a fixed height, centring is
            // what lets the parts disagree about their own without any of them moving: a pencil whose cell
            // is taller than the text, a box that measures a hair differently from the label it replaces.
            // Each one sits on the same middle and any excess hangs off both ends rather than pushing down
            // from the top.
            Centre(_label, bounds, bounds.X, name);
            Centre(_box, bounds, bounds.X, name);
            Centre(_pen, bounds, bounds.X + bounds.Width - pen, pen);
        }

        /// <summary>Puts one part on the middle of the plate's line, at its own height.</summary>
        static void Centre(Element part, RectangleF bounds, float x, float width)
        {
            float height = part.DesiredSize.Y;
            if (height <= 0f)
                height = bounds.Height;

            part.Arrange(new RectangleF(x, bounds.Y + (bounds.Height - height) * 0.5f, width, height));
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
