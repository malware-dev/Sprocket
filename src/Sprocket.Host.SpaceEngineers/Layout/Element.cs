using VRage.Utils;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>Anything that occupies a rectangle and draws in it.</summary>
    /// <remarks>
    /// Laying out happens in two passes: <b>measure</b> asks how much room a thing would like given how
    /// much is going spare, and <b>arrange</b> tells it what it actually got. A parent cannot decide what
    /// to give until it has heard from all of its children, and a child cannot always answer without
    /// knowing the width it has to wrap into.
    /// <para>
    /// Clipping lives here rather than in each container, which is what guarantees every push is matched
    /// by a pop even when a child throws its own drawing away.
    /// </para>
    /// </remarks>
    public abstract class Element
    {
        /// <summary>
        /// A flat white texture, tinted to whatever colour is wanted. The only way to fill a rectangle,
        /// since the seam draws sprites and knows nothing of solid colours.
        /// </summary>
        /// <remarks>Named for the texture rather than the act, because <c>Fill</c> is taken by
        /// <see cref="Dock.Fill" />.</remarks>
        protected static readonly MyStringId Solid = Symbols.Fill;

        /// <summary>Set when this is added to another element's children. Null while it is loose.</summary>
        public Element Parent { get; internal set; }

        /// <summary>The collection this sits in, so being added somewhere else can take it out of where it was.</summary>
        internal ElementCollection Holder { get; set; }

        /// <summary>
        /// Painted across <see cref="Bounds" /> before anything else. Transparent by default, so an
        /// element that has not been given one costs no draw call rather than a clear one.
        /// </summary>
        public Color Background { get; set; }

        /// <summary>A hairline drawn around <see cref="Bounds" />, inside it. Transparent by default.</summary>
        /// <remarks>
        /// Four rectangles rather than a border, because the seam has no borders - a rectangle of colour
        /// is the whole of what either target can be asked for. Drawn only when a colour has been set.
        /// </remarks>
        public Color Edge { get; set; }

        /// <summary>Painted instead of <see cref="Background" /> while the pointer is resting on this.</summary>
        /// <remarks>
        /// Transparent by default, and that default is load-bearing: <b>setting one of these is how a
        /// thing declares that it can be pressed.</b> Read the other way, a row that lights up had better
        /// do something when it is pressed.
        /// </remarks>
        public Color Highlight { get; set; }

        /// <summary>Drawn instead of <see cref="Edge" /> while the pointer is resting on this.</summary>
        /// <remarks>
        /// Separate from <see cref="Highlight" /> because some controls are mostly outline. The game does
        /// the same: its highlight textures differ from its normal ones by the edge far more than by the
        /// middle.
        /// </remarks>
        public Color HighlightEdge { get; set; }

        /// <summary>Whether the pointer is on this right now. Set by whatever owns the screen, once a frame.</summary>
        /// <remarks>
        /// Not worked out here. <see cref="Under" /> answers what the pointer reaches, but only one thing
        /// on a screen is ever lit and only the screen knows which tree the pointer is in - a popup owns
        /// it while it is up, whatever is underneath.
        /// </remarks>
        public bool IsHot { get; set; }

        /// <summary>
        /// What pressing this means. <see cref="Means.Nothing" /> for anything only there to be read.
        /// </summary>
        /// <remarks>
        /// <b>What makes an element a target for the pointer at all.</b> Whoever builds a screen says
        /// here what each control does, and the walk in <see cref="Under" /> is then the whole of
        /// finding it - there is no second list of rectangles to be kept in step with the layout.
        /// <para>
        /// Neither this nor <see cref="Tip" /> moves anything, so setting one invalidates nothing -
        /// which is what lets a view hand its controls their meaning again on every tick, as one does
        /// whenever what a control means can change under it.
        /// </para>
        /// </remarks>
        public Deed Press
        {
            get { return _press; }
            set { _press = value; }
        }

        /// <summary>
        /// What to say while the pointer rests on this. Null for most things.
        /// </summary>
        /// <remarks>
        /// <b>Only for a control that cannot say what it is.</b> A row reading "Airlock 1" needs no note
        /// saying it is Airlock 1, and a screen that explained every word on it would be one nobody
        /// could scan. What earns one is a picture: a bin, an arrow that turns back on itself, a switch
        /// whose two words say the state rather than the act.
        /// <para>
        /// A note without a press is how something says what is wrong with it while it is being typed
        /// into, so this stands on its own rather than sitting inside <see cref="Deed" />.
        /// </para>
        /// </remarks>
        public string Tip { get; set; }

        /// <summary>
        /// Whether the pointer has any business here: something to do, or something to say.
        /// </summary>
        /// <remarks>
        /// Reads the field rather than the property. <see cref="Under" /> asks this of every element it
        /// visits on every frame, and asking a property for one field of a struct copies the whole
        /// struct to get at it.
        /// </remarks>
        private bool Answers
        {
            get { return _press.Means != Means.Nothing || !string.IsNullOrEmpty(Tip); }
        }

        /// <summary>How thick <see cref="Edge" /> is drawn.</summary>
        public float EdgeThickness { get; set; }

        /// <summary>
        /// Whether children and content are cut to <see cref="Bounds" />. Off by default: most elements
        /// are laid out to fit, and clipping them would only cost a pair of calls to prove it.
        /// </summary>
        public bool ClipsContent { get; set; }

        /// <summary>Room held clear inside <see cref="Bounds" />, which content is measured and placed within.</summary>
        /// <remarks>
        /// <see cref="Background" /> is painted across the whole of <see cref="Bounds" /> while the
        /// content sits inside the padding, which is what makes a chip or a row look like one at all.
        /// </remarks>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Moved by this much after being placed, without anything having measured it there.</summary>
        /// <remarks>
        /// The layout still believes the thing is where it put it, and only the drawing disagrees, which
        /// is what a dragged row needs: it follows the pointer while the list underneath keeps its own
        /// arithmetic.
        /// </remarks>
        public Vector2 Offset
        {
            get { return _offset; }
            set
            {
                // Field by field, because the substitute maths the simulator builds against does not
                // define the operators the game's does, and a seam has to compile on both.
                if (_offset.X == value.X && _offset.Y == value.Y)
                    return;

                _offset = value;
                InvalidateArrange();
            }
        }

        /// <summary>Whether this is out of the layout entirely, rather than merely invisible.</summary>
        /// <remarks>
        /// Not the same as drawing nothing: a label with no words still holds a line. Containers skip a
        /// collapsed child completely, spacing included.
        /// </remarks>
        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                if (_collapsed == value)
                    return;

                _collapsed = value;
                InvalidateMeasure();
            }
        }

        private Deed _press;
        private Thickness _padding;
        private Vector2 _offset;
        private bool _collapsed;

        private bool _measureDirty = true;
        private bool _arrangeDirty = true;

        private Vector2 _measuredFor;
        private RectangleF _arrangedTo;

        /// <summary>Says this needs measuring again, and everything above it with it.</summary>
        /// <remarks>
        /// A screen is drawn sixty times a second and used perhaps twice a minute. The game answers a
        /// text measurement by walking the string a character at a time with kerning, so laying out every
        /// frame asks it the same question about the same words for as long as somebody is looking.
        /// <para>
        /// Upwards only, and it stops at the first parent that is already dirty, so a change costs the
        /// depth of the tree once and nothing at all when several things change in the same frame.
        /// </para>
        /// <para>
        /// <b>The rule for a property is what it can move.</b> Anything that changes how big this wants
        /// to be belongs here; anything that only changes where it sits within what it was given belongs
        /// in <see cref="InvalidateArrange" />; and colour changes nothing at all, which matters because
        /// the views set colours on every element on every tick.
        /// </para>
        /// </remarks>
        public void InvalidateMeasure()
        {
            // A size that changed has to be placed again, so this implies the other.
            _arrangeDirty = true;

            if (_measureDirty)
                return;

            _measureDirty = true;

            if (Parent != null)
                Parent.InvalidateMeasure();
        }

        /// <summary>Says this needs placing again, without its size having changed.</summary>
        public void InvalidateArrange()
        {
            if (_arrangeDirty)
                return;

            _arrangeDirty = true;

            if (Parent != null)
                Parent.InvalidateArrange();
        }

        /// <summary>What the last measure asked for, padding included.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>What the last arrange handed over.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>What is left of <see cref="Bounds" /> once the padding is taken out.</summary>
        public RectangleF ContentBounds { get; private set; }

        /// <summary>Asks how much room this would like, given <paramref name="available" />.</summary>
        public void Measure(IGraphics g, Vector2 available)
        {
            // Safe to stop here rather than walking on down: a child that had changed would have said so,
            // and saying so reaches every parent between it and the root.
            if (!_measureDirty && _measuredFor.X == available.X && _measuredFor.Y == available.Y)
                return;

            _measureDirty = false;
            _measuredFor = available;

            if (Collapsed)
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            Vector2 inner = new Vector2(
                available.X - Padding.Horizontal,
                available.Y - Padding.Vertical);

            if (inner.X < 0f)
                inner.X = 0f;
            if (inner.Y < 0f)
                inner.Y = 0f;

            Vector2 content = MeasureContent(g, inner);
            DesiredSize = new Vector2(content.X + Padding.Horizontal, content.Y + Padding.Vertical);
        }

        /// <summary>Tells this what it got. A parent may hand over less than was asked for.</summary>
        public void Arrange(RectangleF bounds)
        {
            if (!_arrangeDirty
                && _arrangedTo.X == bounds.X && _arrangedTo.Y == bounds.Y
                && _arrangedTo.Width == bounds.Width && _arrangedTo.Height == bounds.Height)
                return;

            _arrangeDirty = false;
            _arrangedTo = bounds;

            bounds = new RectangleF(bounds.X + Offset.X, bounds.Y + Offset.Y, bounds.Width, bounds.Height);
            Bounds = bounds;

            float width = bounds.Width - Padding.Horizontal;
            float height = bounds.Height - Padding.Vertical;

            ContentBounds = new RectangleF(
                bounds.X + Padding.Left,
                bounds.Y + Padding.Top,
                width > 0f ? width : 0f,
                height > 0f ? height : 0f);

            ArrangeContent(ContentBounds);
        }

        /// <summary>
        /// Draws the background, then the content, with the clip in place if one was asked for.
        /// </summary>
        /// <remarks>
        /// Not virtual: pairing the push with the pop is what this method exists to guarantee, so a
        /// subclass overrides <see cref="DrawContent" /> and cannot get between them.
        /// </remarks>
        public void Draw(IGraphics g)
        {
            if (Collapsed)
                return;

            Color fill = IsHot && Highlight.A > 0 ? Highlight : Background;
            if (fill.A > 0)
                g.DrawSprite(Solid, Bounds, fill, 0f);

            Color edge = IsHot && HighlightEdge.A > 0 ? HighlightEdge : Edge;
            if (edge.A > 0)
                Outline(g, Bounds, edge, EdgeThickness > 0f ? EdgeThickness : 1f);

            if (!ClipsContent)
            {
                DrawContent(g);
                return;
            }

            g.PushClip(Bounds);
            DrawContent(g);
            g.PopClip();
        }

        /// <summary>
        /// The element a press or a hover at <paramref name="point" /> reaches, or null for none.
        /// </summary>
        /// <remarks>
        /// The reverse of the way it was drawn, so what is on top is what answers: children before the
        /// parent that holds them, and the last child drawn before the ones it covers. An element with
        /// something to do answers only once nothing inside it has, which is what lets the switch in a
        /// row be pressed without pressing the row.
        /// <para>
        /// <b>Clipping is the authority on what can be reached.</b> A row scrolled up behind a header
        /// keeps the rectangle it was arranged at, and only the clip knows it is out of sight - so a
        /// clipped element that does not hold the point answers nothing and neither does anything
        /// inside it. An unclipped one asks its children anyway, because a child moved by
        /// <see cref="Offset" /> is drawn outside its parent and can be pressed there too.
        /// </para>
        /// </remarks>
        public Element Under(Vector2 point)
        {
            if (Collapsed)
                return null;

            if (ClipsContent && !Holds(point))
                return null;

            Element inner = UnderContent(point);
            if (inner != null)
                return inner;

            return Answers && Holds(point) ? this : null;
        }

        /// <summary>
        /// Whether <paramref name="point" /> is inside <see cref="Bounds" />.
        /// </summary>
        /// <remarks>
        /// A rectangle and nothing else. Whether the thing in that rectangle can actually be reached
        /// there is a different question, and <see cref="Reaches" /> is the one that answers it.
        /// </remarks>
        public bool Holds(Vector2 point)
        {
            return point.X >= Bounds.X && point.X <= Bounds.X + Bounds.Width
                && point.Y >= Bounds.Y && point.Y <= Bounds.Y + Bounds.Height;
        }

        /// <summary>
        /// Whether this is on show at <paramref name="point" />: it holds it, and nothing above it has
        /// folded it away, cut it off, or not finished bringing it into view.
        /// </summary>
        /// <remarks>
        /// For the things that are taken hold of rather than pressed: a grip and a scrollbar carry no
        /// <see cref="Press" />, so the walk in <see cref="Under" /> never offers them, and testing a
        /// rectangle alone would let somebody drag a row that has been scrolled out of sight behind a
        /// header.
        /// <para>
        /// <b>It is not the same question as <see cref="Under" />.</b> That one answers what is on top
        /// at a point, walking down from the root; this answers whether one particular thing is on
        /// show, walking up from it - so it knows nothing about a sibling drawn over it, and nothing
        /// about the popups that hang outside the layout. Whoever asks this has to have settled that
        /// already, which is what <c>Owner</c> on the screen is for.
        /// </para>
        /// </remarks>
        public bool Reaches(Vector2 point)
        {
            if (Collapsed || !Holds(point))
                return false;

            for (Element above = Parent; above != null; above = above.Parent)
            {
                if (above.Collapsed || !above.Passes)
                    return false;

                if (above.ClipsContent && !above.Holds(point))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether anything inside this may be reached at all.
        /// </summary>
        /// <remarks>
        /// True for everything that simply holds its children. A container that is part way through
        /// bringing them into view says no, and says it once for both the press path and the grab.
        /// </remarks>
        internal virtual bool Passes
        {
            get { return true; }
        }

        /// <summary>Draws a hairline just inside <paramref name="bounds" />, as four thin rectangles.</summary>
        protected static void Outline(IGraphics g, RectangleF bounds, Color color, float thickness)
        {
            g.DrawSprite(Solid, new RectangleF(bounds.X, bounds.Y, bounds.Width, thickness), color, 0f);
            g.DrawSprite(Solid, new RectangleF(
                bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), color, 0f);
            g.DrawSprite(Solid, new RectangleF(bounds.X, bounds.Y, thickness, bounds.Height), color, 0f);
            g.DrawSprite(Solid, new RectangleF(
                bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), color, 0f);
        }

        /// <summary>How much room this wants. Measuring must not change anything a later pass reads.</summary>
        protected abstract Vector2 MeasureContent(IGraphics g, Vector2 available);

        /// <summary>Places whatever is inside. Nothing to do for an element with no children.</summary>
        protected virtual void ArrangeContent(RectangleF bounds)
        {
        }

        /// <summary>Draws whatever is inside, background already painted and any clip already pushed.</summary>
        protected abstract void DrawContent(IGraphics g);

        /// <summary>
        /// Whatever inside this answers a press at <paramref name="point" />, or null. Nothing to do for
        /// an element with no children.
        /// </summary>
        /// <remarks>
        /// Asked in the reverse of the order <see cref="DrawContent" /> draws in, so the thing on top is
        /// the thing that answers. A control made of parts nobody presses separately - a switch, a
        /// nameplate - leaves this alone and is pressed as one thing.
        /// </remarks>
        protected virtual Element UnderContent(Vector2 point)
        {
            return null;
        }

        /// <summary>
        /// The last of <paramref name="children" /> that answers, which is the one drawn over the rest.
        /// </summary>
        /// <remarks>
        /// For a container that draws its children in order and stacks nothing on purpose. A container
        /// whose parts have an order of their own - a bar, a dock - walks them itself, in the reverse of
        /// the order it draws them.
        /// </remarks>
        protected static Element OnTop(ElementCollection children, Vector2 point)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                Element found = children[i].Under(point);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
