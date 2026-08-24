using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Anything that occupies a rectangle and draws in it.
    /// <para>
    /// Laying out happens in two passes, the arrangement every layout in this shape uses: <b>measure</b> asks
    /// how much room a thing would like given how much is going spare, and <b>arrange</b> tells it what it
    /// actually got. They are separate because a parent cannot decide what to give until it has heard from
    /// all of its children, and a child cannot always answer without knowing the width it has to wrap into.
    /// </para>
    /// <para>
    /// Clipping lives here rather than in each container that wants it, because a clip is a property of a
    /// rectangle rather than of any particular widget, and because doing it in one place is what guarantees
    /// every push is matched by a pop even when a child throws its own drawing away.
    /// </para>
    /// </summary>
    public abstract class Element
    {
        /// <summary>A flat white texture, tinted to whatever colour is wanted. The only way to fill a
        /// rectangle, since the seam draws sprites and knows nothing of solid colours.</summary>
        /// <remarks>
        /// Named for the texture rather than for the act, because <c>Fill</c> is what a container calls
        /// the child that takes the slack and one of the two had to give way.
        /// </remarks>
        protected static readonly MyStringId Solid = Symbols.Fill;

        /// <summary>Set when this is added to another element's children. Null while it is loose.</summary>
        public Element Parent { get; internal set; }

        /// <summary>
        /// The collection this sits in, so being added somewhere else can take it out of where it was. Kept
        /// here rather than reached through the parent, because that would mean every element having
        /// children in order that a label could be moved.
        /// </summary>
        internal ElementCollection Holder { get; set; }

        /// <summary>
        /// Painted across <see cref="Bounds" /> before anything else. Transparent by default, so an element
        /// that has not been given one costs no draw call rather than a clear one.
        /// </summary>
        public Color Background { get; set; }

        /// <summary>
        /// A hairline drawn around <see cref="Bounds" />, inside it. Transparent by default.
        /// </summary>
        /// <remarks>
        /// Here rather than in each container that wants one, for the same reason padding and clipping
        /// are: it is a property of a rectangle. It is four sprites and it is drawn only when a colour has
        /// been set, so an element that has not asked for one costs a comparison.
        /// <para>
        /// Four rectangles rather than a border, because the seam has no borders - a rectangle of colour
        /// is the whole of what either target can be asked for.
        /// </para>
        /// </remarks>
        public Color Edge { get; set; }

        /// <summary>
        /// Painted instead of <see cref="Background" /> while the pointer is resting on this.
        /// </summary>
        /// <remarks>
        /// Transparent by default, and that default is load-bearing: <b>setting one of these is how a
        /// thing declares that it can be pressed.</b> An element that answers to nothing never lights up,
        /// so the pointer is a way of asking the screen what is real - and the same rule read the other
        /// way says a row that lights up had better do something when it is pressed.
        /// </remarks>
        public Color Highlight { get; set; }

        /// <summary>Drawn instead of <see cref="Edge" /> while the pointer is resting on this.</summary>
        /// <remarks>
        /// Separate from <see cref="Highlight" /> because some controls are mostly outline - a switch is
        /// two fills of its own and a border, and brightening the border is the only response it has room
        /// for. The game does the same thing: its highlight textures differ from its normal ones by the
        /// edge far more than by the middle.
        /// </remarks>
        public Color HighlightEdge { get; set; }

        /// <summary>
        /// Whether the pointer is on this right now. Set by whatever owns the screen, once a frame.
        /// </summary>
        /// <remarks>
        /// A property rather than something worked out here, because an element knows its own rectangle
        /// and nothing else - not whether something is drawn over it, not whether it has been scrolled out
        /// of sight, and not whether a popup has taken the screen. The thing holding the list of what can
        /// be pressed knows all three.
        /// </remarks>
        public bool IsHot { get; set; }

        /// <summary>How thick <see cref="Edge" /> is drawn.</summary>
        public float EdgeThickness { get; set; }

        /// <summary>
        /// Whether children and content are cut to <see cref="Bounds" />. Off by default: most elements are
        /// laid out to fit and clipping them would only cost a pair of calls to prove it.
        /// </summary>
        public bool ClipsContent { get; set; }

        /// <summary>
        /// Room held clear inside <see cref="Bounds" />, which content is measured and placed within.
        /// </summary>
        /// <remarks>
        /// On the element rather than on each container that wants it, for the same reason clipping is:
        /// it is a property of a rectangle rather than of any particular widget. A background painted
        /// across the whole of <see cref="Bounds" /> while the words sit inside the padding is what makes
        /// a chip or a row look like one at all.
        /// </remarks>
        public Thickness Padding { get; set; }

        /// <summary>
        /// Moved by this much after being placed, without anything having measured it there.
        /// </summary>
        /// <remarks>
        /// What a render transform is for elsewhere: the layout still believes the thing is where it put
        /// it, and only the drawing disagrees. Dragging a row needs exactly that - the row follows the
        /// pointer while the list underneath keeps its own arithmetic, and letting go simply stops lying.
        /// </remarks>
        public Vector2 Offset { get; set; }

        /// <summary>
        /// Whether this is out of the layout entirely, rather than merely invisible.
        /// </summary>
        /// <remarks>
        /// Not the same as drawing nothing. A label with no words still holds a line, which is exactly
        /// right for anything that must not move when its value comes and goes, and exactly wrong for a
        /// warning that is usually absent - a row that reserves three lines to show one is a row with a
        /// strip of empty panel under it on every screen. Containers skip a collapsed child completely,
        /// spacing included, so nothing is left behind to say it was there.
        /// </remarks>
        public bool Collapsed { get; set; }

        /// <summary>What the last measure asked for, padding included.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>What the last arrange handed over.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>What is left of <see cref="Bounds" /> once the padding is taken out.</summary>
        public RectangleF ContentBounds { get; private set; }

        /// <summary>Asks how much room this would like, given <paramref name="available" />.</summary>
        public void Measure(IGraphics g, Vector2 available)
        {
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
        /// Draws the background, then the content, with the clip in place if one was asked for. Not virtual:
        /// the pairing of push and pop is the thing this method exists to guarantee, so a subclass overrides
        /// <see cref="DrawContent" /> instead and cannot get between them.
        /// </summary>
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
    }
}
