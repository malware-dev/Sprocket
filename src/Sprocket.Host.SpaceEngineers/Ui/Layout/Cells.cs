using System;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>What sits in each cell of a grid, placed by row and column.</summary>
    public sealed class Cells
    {
        readonly Func<int, int, Element> _at;
        readonly Action<Element, int, int, int, int> _place;

        /// <summary>
        ///     Cells read through <paramref name="at" />, by row and column, and filled through <paramref name="place" />,
        ///     by the element, its row and column, and how many rows and columns it spans.
        /// </summary>
        public Cells(Func<int, int, Element> at, Action<Element, int, int, int, int> place)
        {
            _at = at;
            _place = place;
        }

        /// <summary>The element in the cell at <paramref name="row" /> and <paramref name="column" />, or null.</summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that cell; null empties it.</remarks>
        public Element this[int row, int column]
        {
            get { return _at(row, column); }
            set { _place(value, row, column, 1, 1); }
        }

        /// <summary>
        ///     The element spanning <paramref name="rows" /> rows and <paramref name="columns" /> columns from
        ///     <paramref name="row" /> and <paramref name="column" />, or null.
        /// </summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that span; null empties it.</remarks>
        public Element this[int row, int column, int rows, int columns]
        {
            get { return _at(row, column); }
            set { _place(value, row, column, rows, columns); }
        }
    }
}
