using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    partial class StageColumn
    {
        /// <summary>How tall a column's heading line is: as tall as the tallest thing it holds, a link to another view.</summary>
        public const float HeadHigh = 28f;

        RichText _title;
        HStack _heads;
        HStack _viewLinks;
        Label _subtitle;
        HStack _search;
        TextBox _narrow;
        Glyph _narrowClear;
        HStack _kindHead;
        HStack _kindLine;
        Label _kindWord;
        Glyph _kindClear;
        Glyph _hidden;
        Label _showing;
        HStack _options;
        VStack _given;
        Label _note;
        VStack _rows;
        Label _empty;

        Popup _kinds;
        TextBox _kindNarrow;
        Glyph _kindNarrowClear;
        VStack _kindRows;

        /// <summary>Makes the heading and whatever narrows the column, then the rows under it with the way to finish at their foot.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;
            Style = Styles.Column;

            // The eye stands on a line that is always shown, so it is there even when everything it would show is hidden. The line
            // is one height whatever it holds, so every column's heading and search stand level.
            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Placement = FixedSize.Height(HeadHigh),
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Items },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new RichText { Style = Styles.H1Term }.As(out _title),
                            new HStack { IsVisible = false }.As(out _heads)
                        }
                    },
                    new HStack().As(out _viewLinks),
                    new Glyph { Symbol = Symbols.HiddenHidden, Style = Styles.Icon, Placement = FixedSize.Size(28f, 24f), IsVisible = false }
                        .As(out _hidden)
                }
            });
            // Its line is held whether or not there is anything to say on it, so every column's box lines up.
            Children.Add(new Label { Style = Styles.ColumnNote }.As(out _subtitle));

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Within },
                IsVisible = false,
                Children =
                {
                    new TextBox { Style = Styles.Search, Focusable = true, Hint = "Search", Placement = Flex.Grow }.As(out _narrow),

                    // Made invisible rather than taken out, so nothing moves when the first letter is typed.
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(28f, 28f), IsVisible = false }
                        .As(out _narrowClear)
                }
            }.As(out _search));

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Within },
                IsVisible = false,
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Style = Styles.ListLine,
                        Children =
                        {
                            new Label { Style = Styles.OptionWord }.As(out _kindWord),
                            ListField.Fold()
                        }
                    }.As(out _kindLine),
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(28f, 28f), IsVisible = false }
                        .As(out _kindClear)
                }
            }.As(out _kindHead));

            Children.Add(new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _showing));

            // Over the choices and the rows, in the order the sentence says them, so a column further on stays free.
            Children.Add(new VStack { IsVisible = false }.As(out _given));

            Children.Add(new HStack { Base = { Spacing = Gaps.Parts }, IsVisible = false }.As(out _options));

            Children.Add(new ScrollView
            {
                Style = Styles.ScrollView,
                Placement = Flex.Grow,
                Content = new VStack
                {
                    Base = { Spacing = Gaps.Within },
                    Children =
                    {
                        new Label { Style = Styles.Note, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _note),
                        new VStack { Base = { Spacing = Gaps.Within } }.As(out _rows),
                        new Label { Style = Styles.Nothing, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _empty)
                    }
                }
            });

            _kinds = new Popup
            {
                Style = Styles.Popup,
                Anchor = _kindLine,
                Base = { Gap = 4f },
                MatchAnchorWidth = true,
                Content = new Menu
                {
                    Base = { Spacing = Gaps.Within },
                    Children =
                    {
                        new HStack
                        {
                            Base = { Spacing = Gaps.Within },
                            Children =
                            {
                                new TextBox { Style = Styles.Search, Focusable = true, Hint = "Search", Placement = Flex.Grow }
                                    .As(out _kindNarrow),
                                new Glyph
                                {
                                    Symbol = Symbols.Close,
                                    Style = Styles.Icon,
                                    Placement = FixedSize.Size(28f, 28f),
                                    IsVisible = false
                                }.As(out _kindNarrowClear)
                            }
                        },
                        // As tall as its kinds until they run past the room on the screen, and scrolled from there.
                        new ScrollView { Style = Styles.ScrollView, FitsContent = true, Placement = Flex.Grow, Content = new VStack().As(out _kindRows) }
                    }
                }
            };
        }
    }
}
