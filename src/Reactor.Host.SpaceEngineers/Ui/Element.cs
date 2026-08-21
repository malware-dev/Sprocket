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
        protected static readonly MyStringId Fill = MyStringId.GetOrCompute("Phosphor_Fill");

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
        /// Whether children and content are cut to <see cref="Bounds" />. Off by default: most elements are
        /// laid out to fit and clipping them would only cost a pair of calls to prove it.
        /// </summary>
        public bool ClipsContent { get; set; }

        /// <summary>What the last measure asked for. Meaningless before one has happened.</summary>
        public Vector2 DesiredSize { get; private set; }

        /// <summary>What the last arrange handed over.</summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>Asks how much room this would like, given <paramref name="available" />.</summary>
        public void Measure(IGraphics g, Vector2 available)
        {
            DesiredSize = MeasureContent(g, available);
        }

        /// <summary>Tells this what it got. A parent may hand over less than was asked for.</summary>
        public void Arrange(RectangleF bounds)
        {
            Bounds = bounds;
            ArrangeContent(bounds);
        }

        /// <summary>
        /// Draws the background, then the content, with the clip in place if one was asked for. Not virtual:
        /// the pairing of push and pop is the thing this method exists to guarantee, so a subclass overrides
        /// <see cref="DrawContent" /> instead and cannot get between them.
        /// </summary>
        public void Draw(IGraphics g)
        {
            if (Background.A > 0)
                g.DrawSprite(Fill, Bounds, Background, 0f);

            if (!ClipsContent)
            {
                DrawContent(g);
                return;
            }

            g.PushClip(Bounds);
            DrawContent(g);
            g.PopClip();
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
