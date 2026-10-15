using System;
using System.Collections.Generic;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>Rows of a label and the field it names, side by side where both fit and the field under its label where they do not.</summary>
    /// <remarks>
    /// Decided row by row from the width the form is given, so nothing in it runs past its edge: a long label or a wide field
    /// moves its field onto a line of its own instead. Side by side, a field keeps to the right-hand edge, so the fields of a
    /// form end together; under its label it starts at the left. A row can ask for two lines whatever the width, for text and
    /// anything else that wants the whole width, or be one element across the whole width with no label at all.
    /// <para>
    /// A child's own alignment wins over either, and a fixed width from its placement is kept as long as there is room for it.
    /// </para>
    /// </remarks>
    public class Form : Container
    {
        readonly Collection _children;
        readonly List<Vector2> _labels = new List<Vector2>();
        readonly List<Vector2> _fields = new List<Vector2>();
        readonly List<float> _heights = new List<float>();

        float _columnSpacing = 8f;
        float _rowSpacing = 8f;
        float _labelSpacing = 4f;

        /// <summary>A form with no rows yet.</summary>
        public Form()
        {
            _children = Hold();
            Rows = new FormRows(Join, Release);
        }

        /// <summary>The rows, top to bottom.</summary>
        public FormRows Rows { get; }

        /// <summary>Blank room between a label and its field when they sit side by side.</summary>
        public float ColumnSpacing
        {
            get { return _columnSpacing; }
            set
            {
                if (_columnSpacing == value)
                    return;

                _columnSpacing = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Blank room between one row and the next.</summary>
        public float RowSpacing
        {
            get { return _rowSpacing; }
            set
            {
                if (_rowSpacing == value)
                    return;

                _rowSpacing = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Blank room between a label and the field under it.</summary>
        public float LabelSpacing
        {
            get { return _labelSpacing; }
            set
            {
                if (_labelSpacing == value)
                    return;

                _labelSpacing = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Side by side, a field keeps to the right-hand edge.</summary>
        protected override Align Aligns => Align.Right;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var room = available.X;
            var bounded = room < Unbounded;
            var wide = 0f;
            var high = 0f;
            var placed = false;

            _labels.Clear();
            _fields.Clear();
            _heights.Clear();
            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                var label = Vector2.Zero;
                var field = Vector2.Zero;
                var tall = 0f;

                if (!row.Field.IsVisible)
                {
                    Keep(label, field, tall);
                    continue;
                }

                if (row.Label != null && row.Label.IsVisible)
                {
                    row.Label.Measure(g, new Vector2(room, Unbounded));
                    label = row.Label.DesiredSize;
                }

                var fixedWidth = FixedWidth(row.Field);
                row.Field.Measure(g, new Vector2(float.IsNaN(fixedWidth) ? Unbounded : fixedWidth, Unbounded));
                var natural = Width(row.Field, row.Field.DesiredSize.X);

                var alone = row.Label == null;
                var beside = !alone && row.Lines == Lines.Fit
                             && (!bounded || label.X + ColumnSpacing + natural <= room);
                row.Stacked = !alone && !beside;

                if (beside)
                {
                    // Measured again in the width it will have where it stretches, so it is laid out for that width.
                    if (Stretches(row.Field, Align.Right) && bounded)
                        row.Field.Measure(g, new Vector2(room - label.X - ColumnSpacing, Unbounded));

                    field = new Vector2(natural, row.Field.DesiredSize.Y);
                    tall = Math.Max(label.Y, field.Y);
                    wide = Math.Max(wide, label.X + ColumnSpacing + natural);
                }
                else
                {
                    row.Field.Measure(g, new Vector2(bounded ? room : Unbounded, Unbounded));
                    field = new Vector2(Math.Min(Width(row.Field, row.Field.DesiredSize.X), room), row.Field.DesiredSize.Y);
                    tall = alone ? field.Y : label.Y + LabelSpacing + field.Y;
                    wide = Math.Max(wide, Math.Max(label.X, field.X));
                }

                Keep(label, field, tall);
                if (placed)
                    high += RowSpacing;

                placed = true;
                high += tall;
            }

            return new Vector2(bounded ? room : wide, high);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var y = bounds.Y;
            var placed = false;
            for (var i = 0; i < Rows.Count && i < _heights.Count; i++)
            {
                var row = Rows[i];
                var field = _fields[i];
                var height = _heights[i];
                if (!row.Field.IsVisible)
                    continue;

                if (placed)
                    y += RowSpacing;

                placed = true;

                if (row.Label == null)
                {
                    Put(row.Field, Align.Wide, bounds.X, bounds.Width, y, field);
                }
                else if (!row.Stacked)
                {
                    var label = _labels[i];
                    row.Label.Arrange(new RectangleF(bounds.X, y + (height - label.Y) * 0.5f, label.X, label.Y));

                    var from = bounds.X + label.X + ColumnSpacing;
                    Put(row.Field, Align.Right, from, bounds.X + bounds.Width - from, y + (height - field.Y) * 0.5f, field);
                }
                else
                {
                    var label = _labels[i];
                    row.Label.Arrange(new RectangleF(bounds.X, y, Math.Min(label.X, bounds.Width), label.Y));
                    Put(row.Field, row.Lines == Lines.Two ? Align.Wide : Align.Left, bounds.X, bounds.Width,
                        y + label.Y + LabelSpacing, field);
                }

                y += height;
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < _children.Count; i++)
                _children[i].Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(_children, point);
        }

        /// <summary>Refuses a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <summary>Places <paramref name="field" /> across <paramref name="room" /> from <paramref name="from" /> as its alignment says, else as <paramref name="fallback" /> does.</summary>
        static void Put(Element field, Align fallback, float from, float room, float y, Vector2 size)
        {
            var edges = field.Align & Align.Wide;
            if (edges == Align.Default)
                edges = (field.Align & Align.Center) != 0 ? Align.Center : fallback;

            var stretched = edges == Align.Wide && float.IsNaN(FixedWidth(field));
            var width = stretched ? room : Math.Min(size.X, room);
            var x = edges == Align.Right ? from + room - width
                : edges == Align.Center ? from + (room - width) * 0.5f
                : from;

            field.Arrange(new RectangleF(x, y, width, size.Y));
        }

        /// <summary>Whether <paramref name="field" /> takes the whole of the room it is given, where its own alignment leaves it to <paramref name="fallback" />.</summary>
        static bool Stretches(Element field, Align fallback)
        {
            var edges = field.Align & Align.Wide;
            return (edges == Align.Default ? fallback : edges) == Align.Wide && float.IsNaN(FixedWidth(field));
        }

        /// <summary>The width a child's placement fixes, or NaN.</summary>
        static float FixedWidth(Element child)
        {
            var sized = child.Placement as ISized;
            return sized != null ? sized.Width : float.NaN;
        }

        /// <summary>A child's width: the one its placement fixes, else <paramref name="desired" />.</summary>
        static float Width(Element child, float desired)
        {
            var fixedWidth = FixedWidth(child);
            return float.IsNaN(fixedWidth) ? desired : fixedWidth;
        }

        /// <summary>Holds what one row measured: its label, its field and its whole height, read again when it is placed.</summary>
        void Keep(Vector2 label, Vector2 field, float height)
        {
            _labels.Add(label);
            _fields.Add(field);
            _heights.Add(height);
        }

        void Join(Element child)
        {
            _children.Add(child);
        }

        void Release(Element child)
        {
            _children.Remove(child);
        }
    }
}
