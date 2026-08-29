using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Puts its children in a line and starts a new one when the line runs out.
    /// <para>
    /// A sentence is what this is for. A trigger reads "When the hangar door finishes opening" with the
    /// nouns as chips, and a stack would run that off the right-hand edge the moment somebody picked a
    /// block with a long name. Flowing onto the next line is the only arrangement that survives names
    /// people actually type.
    /// </para>
    /// <para>
    /// Children keep the size they asked for and are never stretched, because every one of them is a word
    /// or a chip. A child wider than the whole line is put on a line of its own and left to overflow,
    /// which is visible and therefore fixable, rather than being silently shrunk into something unreadable.
    /// </para>
    /// </summary>
    public class Wrap : Element
    {
        private float _itemSpacing;
        private float _lineSpacing;

        private readonly ElementCollection _children;

        public Wrap()
        {
            _children = new ElementCollection(this);
        }

        public ElementCollection Children
        {
            get { return _children; }
        }

        /// <summary>Blank room between one child and the next along a line.</summary>
        public float ItemSpacing
        {
            get { return _itemSpacing; }
            set
            {
                if (_itemSpacing == value)
                    return;

                _itemSpacing = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Blank room between one line and the next.</summary>
        public float LineSpacing
        {
            get { return _lineSpacing; }
            set
            {
                if (_lineSpacing == value)
                    return;

                _lineSpacing = value;
                InvalidateMeasure();
            }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float lineWidth = 0f;
            float lineHeight = 0f;
            float widest = 0f;
            float total = 0f;
            bool first = true;

            for (int i = 0; i < _children.Count; i++)
            {
                Element child = _children[i];
                child.Measure(g, available);

                if (child.Collapsed)
                    continue;

                float width = child.DesiredSize.X;
                float advance = lineWidth > 0f ? width + ItemSpacing : width;

                if (!first && lineWidth > 0f && lineWidth + advance > available.X)
                {
                    // This one starts the next line, so the one just finished is now known.
                    total += lineHeight + (total > 0f ? LineSpacing : 0f);
                    widest = Wider(widest, lineWidth);
                    lineWidth = width;
                    lineHeight = child.DesiredSize.Y;
                    continue;
                }

                lineWidth += advance;
                lineHeight = Wider(lineHeight, child.DesiredSize.Y);
                first = false;
            }

            if (lineHeight > 0f || lineWidth > 0f)
            {
                total += lineHeight + (total > 0f ? LineSpacing : 0f);
                widest = Wider(widest, lineWidth);
            }

            return new Vector2(widest, total);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float x = bounds.X;
            float y = bounds.Y;
            float lineHeight = 0f;
            bool first = true;
            int lineFrom = 0;

            for (int i = 0; i < _children.Count; i++)
            {
                Element child = _children[i];

                if (child.Collapsed)
                    continue;

                float width = child.DesiredSize.X;
                float advance = x > bounds.X ? width + ItemSpacing : width;

                if (!first && x > bounds.X && x - bounds.X + advance > bounds.Width)
                {
                    // The line just finished is now known, so its short children can be dropped onto its
                    // middle before the next one starts.
                    Centre(lineFrom, i, y, lineHeight);

                    y += lineHeight + LineSpacing;
                    x = bounds.X;
                    lineHeight = 0f;
                    lineFrom = i;
                    advance = width;
                }

                float left = x > bounds.X ? x + ItemSpacing : x;
                child.Arrange(new RectangleF(left, y, width, child.DesiredSize.Y));

                x = left + width;
                lineHeight = Wider(lineHeight, child.DesiredSize.Y);
                first = false;
            }

            Centre(lineFrom, _children.Count, y, lineHeight);
        }

        /// <summary>
        /// Drops everything on one line onto that line's middle.
        /// </summary>
        /// <remarks>
        /// A second pass, because a line's height is not known until the child that ends it has been
        /// seen. Without it a sentence hangs its light and its lead-in from the top of the tallest chip
        /// on the line, which reads as a stray mark above the words rather than as part of them.
        /// </remarks>
        private void Centre(int from, int to, float y, float lineHeight)
        {
            for (int i = from; i < to && i < _children.Count; i++)
            {
                Element child = _children[i];

                if (child.Collapsed)
                    continue;

                float height = child.DesiredSize.Y;
                if (height <= 0f || height >= lineHeight)
                    continue;

                RectangleF placed = child.Bounds;
                child.Arrange(new RectangleF(
                    placed.X - child.Offset.X,
                    y + (lineHeight - height) * 0.5f,
                    placed.Width,
                    height));
            }
        }

        protected override void DrawContent(IGraphics g)
        {
            for (int i = 0; i < _children.Count; i++)
                _children[i].Draw(g);
        }

        private static float Wider(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}
