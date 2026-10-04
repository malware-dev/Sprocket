using System;
using System.Collections.Generic;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>Elements in cells of rows and columns, each track sized outright, by its largest cell, or by a share of what is left.</summary>
    /// <remarks>One element to a cell, stretched across it and centred down it.</remarks>
    public class Grid : Container
    {
        readonly List<Cell> _cells = new List<Cell>();
        readonly Collection _children;

        int _columnCount;
        bool _columnsShare;
        float _columnSpacing;
        float[] _heights = new float[0];
        int _rowCount;
        bool _rowsShare;
        float _rowSpacing;
        float[] _widths = new float[0];
        float[] _xs = new float[0];
        float[] _ys = new float[0];

        /// <summary>An empty grid, every track of it auto.</summary>
        public Grid()
        {
            _children = Hold();
            Columns = new Tracks(InvalidateMeasure);
            Rows = new Tracks(InvalidateMeasure);
            Cells = new Cells(At, Place);
        }

        /// <summary>How wide each column is, left to right.</summary>
        public Tracks Columns { get; }

        /// <summary>How tall each row is, top to bottom.</summary>
        public Tracks Rows { get; }

        /// <summary>What is in each cell.</summary>
        public Cells Cells { get; }

        /// <summary>Blank room left between one column and the next.</summary>
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

        /// <summary>Blank room left between one row and the next.</summary>
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

        /// <summary>A cell stretches what it holds across and centers it down.</summary>
        protected override Align Aligns => Align.Wide | Align.Center;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            Count();

            var boundedAcross = available.X < Unbounded;
            var boundedDown = available.Y < Unbounded;

            for (var c = 0; c < _columnCount; c++)
                _widths[c] = Columns[c].IsFixed ? Columns[c].Size : 0f;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var track = Columns[cell.Column];

                if (cell.Columns == 1 && (track.IsFixed || (track.IsFlex && boundedAcross)))
                    continue;

                cell.Element.Measure(g, new Vector2(Given(cell.Width, available.X), Given(cell.Height, available.Y)));

                if (!cell.Element.IsVisible)
                    continue;

                var wanted = Given(cell.Width, cell.Element.DesiredSize.X);

                if (cell.Columns == 1)
                {
                    if (wanted > _widths[cell.Column])
                        _widths[cell.Column] = wanted;
                }
                else
                {
                    Widen(_widths, Columns, cell.Column, cell.Columns, _columnSpacing, wanted);
                }
            }

            _columnsShare = Share(_widths, Columns, _columnCount, available.X, _columnSpacing);

            for (var r = 0; r < _rowCount; r++)
                _heights[r] = Rows[r].IsFixed ? Rows[r].Size : 0f;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var room = Span(_widths, cell.Column, cell.Columns, _columnSpacing);

                cell.Element.Measure(g, new Vector2(Given(cell.Width, room), Given(cell.Height, available.Y)));

                if (!cell.Element.IsVisible)
                    continue;

                var track = Rows[cell.Row];
                if (cell.Rows == 1 && (track.IsFixed || (track.IsFlex && boundedDown)))
                    continue;

                var wanted = Given(cell.Height, cell.Element.DesiredSize.Y);

                if (cell.Rows == 1)
                {
                    if (wanted > _heights[cell.Row])
                        _heights[cell.Row] = wanted;
                }
                else
                {
                    Widen(_heights, Rows, cell.Row, cell.Rows, _rowSpacing, wanted);
                }
            }

            _rowsShare = Share(_heights, Rows, _rowCount, available.Y, _rowSpacing);

            return new Vector2(
                _columnsShare ? available.X : Span(_widths, 0, _columnCount, _columnSpacing),
                _rowsShare ? available.Y : Span(_heights, 0, _rowCount, _rowSpacing));
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_columnsShare)
                Share(_widths, Columns, _columnCount, bounds.Width, _columnSpacing);

            if (_rowsShare)
                Share(_heights, Rows, _rowCount, bounds.Height, _rowSpacing);

            Offsets(_xs, _widths, _columnCount, bounds.X, _columnSpacing);
            Offsets(_ys, _heights, _rowCount, bounds.Y, _rowSpacing);

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (!cell.Element.IsVisible)
                    continue;

                var width = Span(_widths, cell.Column, cell.Columns, _columnSpacing);
                var height = Span(_heights, cell.Row, cell.Rows, _rowSpacing);

                float x;
                float wide;
                float y;
                float tall;
                Span(cell.Element, true, _xs[cell.Column], width, cell.Width, cell.Element.DesiredSize.X, out x, out wide);
                Span(cell.Element, false, _ys[cell.Row], height, cell.Height, cell.Element.DesiredSize.Y, out y, out tall);
                cell.Element.Arrange(new RectangleF(x, y, wide, tall));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < _cells.Count; i++)
                _cells[i].Element.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            for (var i = _cells.Count - 1; i >= 0; i--)
            {
                var found = _cells[i].Element.Under(point);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Refuses a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(
                    GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <inheritdoc />
        protected override void Released(Element child, int index)
        {
            for (var i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].Element != child)
                    continue;

                _cells.RemoveAt(i);
                return;
            }
        }

        /// <inheritdoc />
        protected override void Replaced(Element child)
        {
            for (var i = 0; i < _cells.Count; i++)
                if (_cells[i].Element == child)
                    _cells[i].Resolve(child.Placement);
        }

        Element At(int row, int column)
        {
            for (var i = 0; i < _cells.Count; i++)
                if (_cells[i].Covers(row, column))
                    return _cells[i].Element;

            return null;
        }

        void Place(Element child, int row, int column, int rows, int columns)
        {
            if (row < 0 || column < 0 || rows < 1 || columns < 1)
                throw new ArgumentException("A cell needs a row and column from zero and a span of at least one.");

            var placing = new Cell(child, row, column, rows, columns);
            Cell same = null;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (cell.Element == child)
                    continue;

                if (cell.Row == row && cell.Column == column && cell.Rows == rows && cell.Columns == columns)
                    same = cell;
                else if (cell.Overlaps(placing))
                    throw new InvalidOperationException(
                        "The cell at row " + row + ", column " + column + " overlaps one already holding something.");
            }

            if (child != null)
                Admit(child, child.Placement);

            if (same != null)
                _children.Remove(same.Element);

            if (child == null)
                return;

            _children.Add(child);
            placing.Resolve(child.Placement);
            _cells.Add(placing);
        }

        /// <summary>Works out how many columns and rows there are, and makes room to hold their sizes.</summary>
        void Count()
        {
            _columnCount = Columns.Count;
            _rowCount = Rows.Count;

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];

                if (cell.Column + cell.Columns > _columnCount)
                    _columnCount = cell.Column + cell.Columns;

                if (cell.Row + cell.Rows > _rowCount)
                    _rowCount = cell.Row + cell.Rows;
            }

            Hold(ref _widths, _columnCount);
            Hold(ref _xs, _columnCount);
            Hold(ref _heights, _rowCount);
            Hold(ref _ys, _rowCount);
        }

        static void Hold(ref float[] sizes, int count)
        {
            if (sizes.Length < count)
                sizes = new float[count];
        }

        /// <summary>Shares what the fixed and auto tracks leave of <paramref name="room" /> among the flex ones, and says whether it did.</summary>
        static bool Share(float[] sizes, Tracks tracks, int count, float room, float spacing)
        {
            if (room >= Unbounded)
                return false;

            var weight = 0f;
            var used = count > 1 ? spacing * (count - 1) : 0f;

            for (var i = 0; i < count; i++)
            {
                if (tracks[i].IsFlex)
                    weight += tracks[i].Weight;
                else
                    used += sizes[i];
            }

            if (!(weight > 0f))
                return false;

            var spare = room - used;
            if (spare < 0f)
                spare = 0f;

            for (var i = 0; i < count; i++)
                if (tracks[i].IsFlex)
                    sizes[i] = spare * tracks[i].Weight / weight;

            return true;
        }

        /// <summary>Makes a span of tracks at least <paramref name="wanted" /> long, adding what is missing to its last track that is not fixed.</summary>
        static void Widen(float[] sizes, Tracks tracks, int from, int count, float spacing, float wanted)
        {
            var missing = wanted - Span(sizes, from, count, spacing);
            if (!(missing > 0f))
                return;

            for (var i = from + count - 1; i >= from; i--)
            {
                if (tracks[i].IsFixed)
                    continue;

                sizes[i] += missing;
                return;
            }
        }

        static float Span(float[] sizes, int from, int count, float spacing)
        {
            var total = count > 1 ? spacing * (count - 1) : 0f;

            for (var i = from; i < from + count; i++)
                total += sizes[i];

            return total;
        }

        static void Offsets(float[] at, float[] sizes, int count, float start, float spacing)
        {
            for (var i = 0; i < count; i++)
            {
                at[i] = start;
                start += sizes[i] + spacing;
            }
        }

        /// <summary><paramref name="size" /> where the placement gives one, and <paramref name="otherwise" /> where it is <c>float.NaN</c>.</summary>
        static float Given(float size, float otherwise)
        {
            return float.IsNaN(size) ? otherwise : size;
        }

        /// <summary>Where one element sits, and what its placement says about its size.</summary>
        sealed class Cell
        {
            public readonly int Column;
            public readonly int Columns;
            public readonly Element Element;
            public readonly int Row;
            public readonly int Rows;
            public float Height = float.NaN;
            public float Width = float.NaN;

            public Cell(Element element, int row, int column, int rows, int columns)
            {
                Element = element;
                Row = row;
                Column = column;
                Rows = rows;
                Columns = columns;
            }

            public bool Covers(int row, int column)
            {
                return row >= Row && row < Row + Rows && column >= Column && column < Column + Columns;
            }

            public bool Overlaps(Cell other)
            {
                return Row < other.Row + other.Rows && other.Row < Row + Rows
                    && Column < other.Column + other.Columns && other.Column < Column + Columns;
            }

            public void Resolve(IPlacement placement)
            {
                var sized = placement as ISized;
                Width = sized == null ? float.NaN : sized.Width;
                Height = sized == null ? float.NaN : sized.Height;
            }
        }
    }
}
