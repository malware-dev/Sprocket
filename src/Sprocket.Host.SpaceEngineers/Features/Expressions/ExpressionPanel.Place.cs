using Lumel;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    public partial class ExpressionPanel
    {
        // A place on the board, or the bin, that a piece dragged in the panel can be dropped on.
        sealed class Place : IDropTarget
        {
            readonly ExpressionPanel _panel;
            readonly DraftPiece _piece;
            readonly Element _shown;
            readonly bool _bin;

            public Place(ExpressionPanel panel, DraftPiece piece, Element shown, bool bin)
            {
                _panel = panel;
                _piece = piece;
                _shown = shown;
                _bin = bin;
            }

            /// <inheritdoc />
            public bool CanDrop(object data)
            {
                var carried = data as Dragged;
                return carried != null && _panel.Fits(carried, _piece, _bin);
            }

            /// <inheritdoc />
            public void Drop(object data)
            {
                _panel.Dropped((Dragged)data, _piece, _shown, _bin);
            }
        }
    }
}
