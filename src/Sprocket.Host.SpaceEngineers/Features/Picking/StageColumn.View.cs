using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Values;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    partial class StageColumn
    {
        Label _title;
        Label _subtitle;
        HStack _search;
        TextBox _narrow;
        Glyph _narrowClear;
        HStack _kindHead;
        HStack _kindLine;
        Label _kindWord;
        Glyph _kindClear;
        Grid _tabs;
        Label _showing;
        HStack _options;
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
            Spacing = 8f;
            Style = Styles.Column;

            Children.Add(new Label { Style = Styles.ColumnTitle }.As(out _title));
            // Its line is held whether or not there is anything to say on it, so every column's box lines up.
            Children.Add(new Label { Style = Styles.ColumnNote }.As(out _subtitle));

            // Wherever a column can be narrowed, and for the whole of its life: a box that came and went with how much
            // there is would leave the column with no shape to learn.
            Children.Add(new HStack
            {
                Spacing = 4f,
                IsVisible = false,
                Children =
                {
                    new TextBox { Style = Styles.Search, Focusable = true, Hint = "Search", Placement = Flex.Grow }.As(out _narrow),

                    // Made invisible rather than taken out, so nothing moves when the first letter is typed.
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = Fixed.Size(28f, 28f), IsVisible = false }
                        .As(out _narrowClear)
                }
            }.As(out _search));

            // One line rather than a row of chips: a large construct shows thirty kinds, and a wall of them crowds out
            // the box that answers the question in four keystrokes. The way back to every kind stands next to the word
            // it clears, where clearing is something to do without first opening the list that set it.
            Children.Add(new HStack
            {
                Spacing = 4f,
                IsVisible = false,
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Style = Styles.ListLine,
                        Children =
                        {
                            new Label { Style = Styles.OptionWord }.As(out _kindWord),
                            ListField.Fold()
                        }
                    }.As(out _kindLine),
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = Fixed.Size(28f, 28f), IsVisible = false }
                        .As(out _kindClear)
                }
            }.As(out _kindHead));

            // Equal plates butted edge to edge, as the game draws the tabs across the top of its terminal, so the strip
            // is the same shape whatever the views are called.
            Children.Add(new Grid { IsVisible = false }.As(out _tabs));

            Children.Add(new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _showing));

            // Across rather than stacked: there are two of them, three words each, and every line of height comes out of
            // the list.
            Children.Add(new HStack { Spacing = 8f, IsVisible = false }.As(out _options));

            Children.Add(new ScrollView
            {
                Style = Styles.ScrollView,
                Placement = Flex.Grow,
                Content = new VStack
                {
                    Spacing = 4f,
                    Children =
                    {
                        new Label { Style = Styles.Note, Wraps = true, IsVisible = false }.As(out _note),
                        new VStack { Spacing = 4f }.As(out _rows),
                        new Label { Style = Styles.Nothing, Wraps = true, IsVisible = false }.As(out _empty)
                    }
                }
            });

            _kinds = new Popup
            {
                Style = Styles.Menu,
                Anchor = _kindLine,
                Gap = 4f,
                Content = new Menu
                {
                    Spacing = 4f,
                    Children =
                    {
                        // Inside the list rather than beside the line, because it narrows what the list holds, and it is
                        // the first thing under the pointer once the list opens.
                        new HStack
                        {
                            Spacing = 4f,
                            Children =
                            {
                                new TextBox { Style = Styles.Search, Focusable = true, Hint = "Search", Placement = Flex.Grow }
                                    .As(out _kindNarrow),
                                new Glyph
                                {
                                    Symbol = Symbols.Close,
                                    Style = Styles.Icon,
                                    Placement = Fixed.Size(28f, 28f),
                                    IsVisible = false
                                }.As(out _kindNarrowClear)
                            }
                        },
                        new VStack().As(out _kindRows)
                    }
                }
            };
        }
    }
}
