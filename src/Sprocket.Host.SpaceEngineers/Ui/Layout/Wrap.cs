using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>Puts its children in a line and starts a new one when the line runs out.</summary>
    /// <remarks>
    ///     A sentence is what this is for: a trigger reads "When the hangar door finishes opening" with the
    ///     nouns as chips, and a stack would run that off the right-hand edge the moment somebody picked a
    ///     block with a long name.
    ///     <para>
    ///         Children keep the size they asked for and are never stretched. A child wider than the whole line
    ///         is put on a line of its own and left to overflow, which is visible and therefore fixable, rather
    ///         than being silently shrunk into something unreadable.
    ///     </para>
    /// </remarks>
    public class Wrap : Element
    {
        float _itemSpacing;
        float _lineSpacing;

        public Wrap()
        {
            Children = new ElementCollection(this);
        }

        public ElementCollection Children { get; }

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
            var lineWidth = 0f;
            var lineHeight = 0f;
            var widest = 0f;
            var total = 0f;
            var first = true;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                child.Measure(g, available);

                if (child.Collapsed)
                    continue;

                var width = child.DesiredSize.X;
                var advance = lineWidth > 0f ? width + ItemSpacing : width;

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
            var x = bounds.X;
            var y = bounds.Y;
            var lineWidth = 0f;
            var lineHeight = 0f;
            var first = true;
            var lineFrom = 0;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if (child.Collapsed)
                    continue;

                var width = child.DesiredSize.X;
                var advance = lineWidth > 0f ? width + ItemSpacing : width;

                // The same sum, added up the same way, as measuring did: worked out from the position
                // instead, a line measured as exactly fitting came out a hair over and dropped its last
                // child onto a line of its own.
                if (!first && lineWidth > 0f && lineWidth + advance > bounds.Width)
                {
                    // The line just finished is now known, so its short children can be dropped onto its
                    // middle before the next one starts.
                    Centre(lineFrom, i, y, lineHeight);

                    y += lineHeight + LineSpacing;
                    x = bounds.X;
                    lineWidth = 0f;
                    lineHeight = 0f;
                    lineFrom = i;
                    advance = width;
                }

                var left = lineWidth > 0f ? x + ItemSpacing : x;
                child.Arrange(new RectangleF(left, y, width, child.DesiredSize.Y));

                x = left + width;
                lineWidth += advance;
                lineHeight = Wider(lineHeight, child.DesiredSize.Y);
                first = false;
            }

            Centre(lineFrom, Children.Count, y, lineHeight);
        }

        /// <summary>
        ///     Drops everything on one line onto that line's middle.
        /// </summary>
        /// <remarks>
        ///     A second pass, because a line's height is not known until the child that ends it has been
        ///     seen. Without it a sentence hangs its light and its lead-in from the top of the tallest chip
        ///     on the line, which reads as a stray mark above the words rather than as part of them.
        /// </remarks>
        void Centre(int from, int to, float y, float lineHeight)
        {
            for (var i = from; i < to && i < Children.Count; i++)
            {
                var child = Children[i];

                if (child.Collapsed)
                    continue;

                var height = child.DesiredSize.Y;
                if (height <= 0f || height >= lineHeight)
                    continue;

                var placed = child.Bounds;
                child.Arrange(new RectangleF(
                    placed.X - child.Offset.X,
                    y + (lineHeight - height) * 0.5f,
                    placed.Width,
                    height));
            }
        }

        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(Children, point);
        }

        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i].Draw(g);
        }

        static float Wider(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}