using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// The shelves of the expression page that pieces are dragged from, the calculations, the comparisons and what joins flags,
    /// the fixed values to type and the kinds of value to choose, with the bin beside them that parts are dropped in to remove them.
    /// </summary>
    public partial class ExpressionTray : HStack
    {
        /// <summary>A tray whose pieces are each handed to <paramref name="grab" /> with their grip and what dragging them carries.</summary>
        public ExpressionTray(Action<DragHandle, Element, Dragged> grab)
        {
            CreateView();
            foreach (var kind in ExpressionOffers.Calculations)
                _operators.Children.Add(Piece(new Dragged { Kind = kind }, grab));

            foreach (var kind in ExpressionOffers.Conditions)
                _conditions.Children.Add(Piece(new Dragged { Kind = kind }, grab));

            foreach (var kind in ExpressionOffers.Typables)
                _typed.Children.Add(Piece(new Dragged { Source = ValueSource.Typed, Typed = kind }, grab));

            foreach (var source in ExpressionOffers.Sources)
                _sources.Children.Add(Piece(new Dragged { Source = source }, grab));
        }

        /// <summary>The bin parts are dropped in to remove them.</summary>
        public Element Bin => _bin;

        /// <summary>The piece for the calculation <paramref name="kind" />.</summary>
        public Element PieceFor(JoinKind kind)
        {
            var at = ExpressionOffers.PlaceOf(ExpressionOffers.Calculations, kind);
            return at >= 0 ? _operators.Children[at] : _conditions.Children[ExpressionOffers.PlaceOf(ExpressionOffers.Conditions, kind)];
        }

        /// <summary>The piece for the kind of value <paramref name="source" />.</summary>
        public Element PieceFor(ValueSource source) => _sources.Children[ExpressionOffers.PlaceOf(ExpressionOffers.Sources, source)];

        /// <summary>The piece for a value of <paramref name="kind" /> typed in.</summary>
        public Element PieceFor(KindClass kind) => _typed.Children[ExpressionOffers.PlaceOf(ExpressionOffers.Typables, kind)];

        /// <summary>Brings the words to <paramref name="texts" />.</summary>
        public void Show(Texts texts)
        {
            _calculations.Text = texts.ExpressionCalculations;
            _conditionsHeading.Text = texts.ExpressionCompare;
            _typedHeading.Text = texts.ExpressionTyped;
            _values.Text = texts.ExpressionCurrentValues;
            _binWords.Text = texts.ExpressionBin;
            _bin.Tip = CodexTip.For(null, texts.Codex.ExpressionBinAbout);
            for (var i = 0; i < ExpressionOffers.Calculations.Count; i++)
                Word(_operators, i, ExpressionWords.Symbol(texts, ExpressionOffers.Calculations[i]), ExpressionWords.About(texts, ExpressionOffers.Calculations[i]));

            for (var i = 0; i < ExpressionOffers.Conditions.Count; i++)
                Word(_conditions, i, ExpressionWords.Symbol(texts, ExpressionOffers.Conditions[i]), ExpressionWords.About(texts, ExpressionOffers.Conditions[i]));

            for (var i = 0; i < ExpressionOffers.Typables.Count; i++)
                Word(_typed, i, ExpressionWords.Said(texts, ExpressionOffers.Typables[i]), texts.Codex.ExpressionTypedAbout);

            for (var i = 0; i < ExpressionOffers.Sources.Count; i++)
                Word(_sources, i, ExpressionWords.Said(texts, ExpressionOffers.Sources[i]), ExpressionWords.About(texts, ExpressionOffers.Sources[i]));
        }

        /// <summary>
        /// Shows only the pieces that can go somewhere in a value of <paramref name="system" /> able to hold <paramref name="usable" />,
        /// as <paramref name="types" /> says what its values hold: the comparisons and what joins flags only where <paramref name="tests" />
        /// says a condition is built, no box at all where <paramref name="single" /> says the value is one alone, and a shelf
        /// only while it holds a piece.
        /// </summary>
        public void Offer(AutomationSystem system, KindClass usable, ITypeScope types, bool tests, bool single)
        {
            for (var i = 0; i < ExpressionOffers.Calculations.Count; i++)
                _operators.Children[i].IsVisible = !single && ExpressionOffers.Offers(ExpressionOffers.Calculations[i], usable, types);

            for (var i = 0; i < ExpressionOffers.Conditions.Count; i++)
                _conditions.Children[i].IsVisible = tests && !single && ExpressionOffers.Offers(ExpressionOffers.Conditions[i], usable, types);

            for (var i = 0; i < ExpressionOffers.Typables.Count; i++)
                _typed.Children[i].IsVisible = (ExpressionOffers.Typables[i] & usable) != KindClass.None;

            PieceFor(ValueSource.Setting).IsVisible = ExpressionOffers.Settings(system, usable, types).Count > 0;
            PieceFor(ValueSource.State).IsVisible = ExpressionOffers.States(system, usable, types).Count > 0;
            _operators.Parent.IsVisible = AnyShown(_operators);
            _conditions.Parent.IsVisible = AnyShown(_conditions);
            _typed.Parent.IsVisible = AnyShown(_typed);
        }

        // Puts words and tip on the piece at at among pieces.
        static void Word(Flow pieces, int at, string words, string tip)
        {
            var piece = (HStack)pieces.Children[at];
            ((Label)piece.Children[1]).Text = words;
            piece.Tip = CodexTip.For(null, tip);
        }

        static bool AnyShown(Flow pieces)
        {
            foreach (var piece in pieces.Children)
            {
                if (piece.IsVisible)
                    return true;
            }

            return false;
        }

        // A piece: a grip and the words, dragged as a copy of itself.
        static HStack Piece(Dragged dragged, Action<DragHandle, Element, Dragged> grab)
        {
            var piece = new HStack { Style = Styles.ExpressionPiece };
            var grip = new DragHandle { Placement = FixedSize.Size(16f, 24f), CanDrag = true };
            piece.Children.Add(grip);
            piece.Children.Add(new Label { Base = { Color = Palette.Ink } });
            grab(grip, piece, dragged);
            return piece;
        }
    }
}
