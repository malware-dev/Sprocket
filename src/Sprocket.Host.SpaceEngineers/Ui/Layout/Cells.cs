namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>What sits in each cell of a grid, placed by row and column.</summary>
    public sealed class Cells
    {
        readonly Grid _grid;

        /// <summary>The cells of <paramref name="grid" />.</summary>
        internal Cells(Grid grid)
        {
            _grid = grid;
        }

        /// <summary>The element in the cell at <paramref name="row" /> and <paramref name="column" />, or null.</summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that cell; null empties it.</remarks>
        public Element this[int row, int column]
        {
            get { return _grid.At(row, column); }
            set { _grid.Place(value, row, column, 1, 1); }
        }

        /// <summary>
        ///     The element spanning <paramref name="rows" /> rows and <paramref name="columns" /> columns from
        ///     <paramref name="row" /> and <paramref name="column" />, or null.
        /// </summary>
        /// <remarks>Setting puts one there, replacing whatever held exactly that span; null empties it.</remarks>
        public Element this[int row, int column, int rows, int columns]
        {
            get { return _grid.At(row, column); }
            set { _grid.Place(value, row, column, rows, columns); }
        }
    }
}
