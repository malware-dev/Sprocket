using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    public partial class ExpressionTray
    {
        Label _calculations;
        Flow _operators;
        Label _typedHeading;
        Flow _typed;
        Label _values;
        Flow _sources;
        HStack _bin;
        Label _binWords;

        /// <summary>Makes the tray left to right: the calculations, the fixed values, the kinds of value, and the bin.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Items;
            Align = Align.FillWidth;
            Children.Add(Shelf(out _calculations, out _operators));
            Children.Add(Shelf(out _typedHeading, out _typed));
            Children.Add(Shelf(out _values, out _sources));
            Children.Add(new HStack
            {
                Style = Styles.ExpressionBin,
                Align = Align.FillHeight,
                Children = { new Label { Style = Styles.Detail, Align = Align.Center }.As(out _binWords) }
            }.As(out _bin));
        }

        // A shelf: its heading over the pieces it holds, sharing the tray's width with the others.
        static VStack Shelf(out Label heading, out Flow pieces)
        {
            return new VStack
            {
                Style = Styles.ExpressionShelf,
                Placement = Flex.Grow,
                Align = Align.FillHeight,
                Children =
                {
                    new Label { Style = Styles.Detail }.As(out heading),
                    new Flow { Base = { Spacing = Gaps.Parts, LineSpacing = Gaps.Parts }, Align = Align.FillWidth }.As(out pieces)
                }
            };
        }
    }
}
