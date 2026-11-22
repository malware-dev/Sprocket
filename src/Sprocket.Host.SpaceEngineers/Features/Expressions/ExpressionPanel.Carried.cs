namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    public partial class ExpressionPanel
    {
        /// <summary>What is being dragged: a calculation from the tray, a kind of value from it, or a part of the expression.</summary>
        sealed class Carried
        {
            /// <summary>The calculation dragged from the tray, where it is one.</summary>
            public JoinKind? Kind;

            /// <summary>The kind of value dragged from the tray, where it is one.</summary>
            public ValueSource Source;

            /// <summary>The kind of value typed in, where the kind of value dragged is one typed in.</summary>
            public KindClass Typed;

            /// <summary>The part of the expression dragged, where it is one.</summary>
            public DraftPiece Piece;
        }
    }
}
