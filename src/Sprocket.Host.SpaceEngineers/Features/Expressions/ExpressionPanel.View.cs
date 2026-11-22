using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    public partial class ExpressionPanel
    {
        Label _calculations;
        Flow _operators;
        Label _typedHeading;
        Flow _typed;
        Label _values;
        Flow _sources;
        HStack _bin;
        Label _binWords;
        Label _hint;
        ScrollView _scroll;
        HStack _board;
        Button _optimize;
        Label _optimized;
        Label _why;
        Label _now;

        /// <summary>
        /// Makes the page top to bottom: the tray of pieces to drag with the bin beside it, the hint while dragging, the well
        /// the expression is built in, Optimize, what is wrong with the expression, and what it comes to now.
        /// </summary>
        /// <remarks>The tray stands above the expression, so it stays where it is as the expression grows.</remarks>
        void CreateView()
        {
            Base.Spacing = Gaps.Items;
            Base.Padding = new Thickness(Gaps.Sections);

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Items },
                Align = Align.FillWidth,
                Children =
                {
                    Shelf(out _calculations, out _operators),
                    Shelf(out _typedHeading, out _typed),
                    Shelf(out _values, out _sources),
                    new HStack
                    {
                        Style = Styles.ExpressionBin,
                        Align = Align.FillHeight,
                        Children = { new Label { Style = Styles.Detail, Align = Align.Center }.As(out _binWords) }
                    }.As(out _bin)
                }
            });
            Children.Add(new Label { Style = Styles.Detail }.As(out _hint));
            Children.Add(new HStack
            {
                Style = Styles.ExpressionLine,
                Align = Align.FillWidth,
                Children =
                {
                    // An expression wider than the page scrolls sideways, every box in it kept on the one line.
                    new ScrollView
                    {
                        Style = Styles.ScrollView,
                        ScrollsVertically = false,
                        ScrollsHorizontally = true,
                        FitsContent = true,
                        Placement = Flex.Grow,
                        Content = new HStack { Align = Align.Left | Align.Center }.As(out _board)
                    }.As(out _scroll)
                }
            });
            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Button { Style = Styles.Word }.As(out _optimize),
                    new Label { Style = Styles.Detail, Align = Align.Center }.As(out _optimized)
                }
            });
            Children.Add(new Label { Style = Styles.Problem, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _why));
            Children.Add(new Label { Style = Styles.ValueLabel }.As(out _now));
        }

        // A shelf of the tray: its heading over the pieces it holds, sharing the tray's width with the others.
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
