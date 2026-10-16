using System;
using System.Collections.Generic;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>Children in a line across that carries on onto the next line when it runs out of room, each centered down its line.</summary>
    /// <remarks>A child wider than the room has a line to itself, and is given its own width.</remarks>
    public class Flow : Panel
    {
        readonly List<Vector2> _sizes = new List<Vector2>();

        float _lineSpacing;
        float _spacing;

        /// <summary>Blank room left between one child and the next along a line.</summary>
        public float Spacing
        {
            get { return _spacing; }
            set
            {
                if (_spacing == value)
                    return;

                _spacing = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Blank room left between one line and the next.</summary>
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

        /// <summary>Each line centers what it holds down.</summary>
        protected override Align Aligns => Align.Center;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var size = _sizes[i];
                Children[i].Measure(g, new Vector2(Given(size.X, available.X), Given(size.Y, available.Y)));
            }

            var widest = 0f;
            var height = 0f;
            var from = 0;

            while (from < Children.Count)
            {
                float width;
                float tall;
                var to = Line(from, available.X, out width, out tall);

                if (width > 0f || tall > 0f)
                {
                    if (height > 0f)
                        height += _lineSpacing;

                    height += tall;
                    widest = Math.Max(widest, width);
                }

                from = to;
            }

            return new Vector2(widest, height);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var y = bounds.Y;
            var from = 0;
            var placed = false;

            while (from < Children.Count)
            {
                float width;
                float tall;
                var to = Line(from, bounds.Width, out width, out tall);

                if (width > 0f || tall > 0f)
                {
                    if (placed)
                        y += _lineSpacing;

                    placed = true;
                    Place(from, to, bounds.X, y, tall);
                    y += tall;
                }

                from = to;
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            foreach (var child in Children)
                child.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(Children, point);
        }

        /// <summary>Refuses a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(
                    GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <inheritdoc />
        protected override void Joined(Element child, int index)
        {
            _sizes.Insert(index, Resolve(child.Placement));
        }

        /// <inheritdoc />
        protected override void Released(Element child, int index)
        {
            _sizes.RemoveAt(index);
        }

        /// <inheritdoc />
        protected override void Replaced(Element child)
        {
            var index = Children.IndexOf(child);
            if (index >= 0)
                _sizes[index] = Resolve(child.Placement);
        }

        int Line(int from, float room, out float width, out float tall)
        {
            // Measuring and arranging both break lines here, so a line measured as fitting cannot break
            // differently when it is placed.
            width = 0f;
            tall = 0f;
            var any = false;
            var at = from;

            for (; at < Children.Count; at++)
            {
                var child = Children[at];
                if (!child.IsVisible)
                    continue;

                var size = Size(at);
                var advance = any ? size.X + _spacing : size.X;
                if (any && width + advance > room)
                    break;

                width += advance;
                tall = Math.Max(tall, size.Y);
                any = true;
            }

            return at;
        }

        void Place(int from, int to, float x, float y, float tall)
        {
            var any = false;

            for (var i = from; i < to; i++)
            {
                var child = Children[i];
                if (!child.IsVisible)
                    continue;

                if (any)
                    x += _spacing;

                any = true;

                var size = Size(i);
                float at;
                float length;
                Span(child, false, y, tall, float.NaN, size.Y, out at, out length);
                child.Arrange(new RectangleF(x, at, size.X, length));
                x += size.X;
            }
        }

        Vector2 Size(int index)
        {
            var size = _sizes[index];
            var desired = Children[index].DesiredSize;
            return new Vector2(Given(size.X, desired.X), Given(size.Y, desired.Y));
        }

        static Vector2 Resolve(IPlacement placement)
        {
            var sized = placement as ISized;
            return sized == null ? new Vector2(float.NaN) : new Vector2(sized.Width, sized.Height);
        }

        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }
    }
}
