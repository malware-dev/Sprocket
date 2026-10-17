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
        Label _title;
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
            Spacing = 8f;
            Style = Styles.Column;

            // The way to the column's other view stands at the end of its heading, as a quiet link rather than a tab, so the
            // list under the heading is the column's list and the other is a step aside from it. The eye stands there too,
            // on a line that is always shown, so it is there even when everything it would show is hidden.
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.ColumnTitle, Placement = Flex.Grow }.As(out _title),
                    new HStack().As(out _viewLinks),
                    new Glyph { Symbol = Symbols.HiddenHidden, Style = Styles.Icon, Placement = FixedSize.Size(28f, 24f), IsVisible = false }
                        .As(out _hidden)
                }
            });
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
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(28f, 28f), IsVisible = false }
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
                    new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(28f, 28f), IsVisible = false }
                        .As(out _kindClear)
                }
            }.As(out _kindHead));

            Children.Add(new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _showing));

            // Over the choices and the rows, in the order the sentence says them, so a column further on stays free.
            Children.Add(new VStack { IsVisible = false }.As(out _given));

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
                Flush = true,
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
                                    Placement = FixedSize.Size(28f, 28f),
                                    IsVisible = false
                                }.As(out _kindNarrowClear)
                            }
                        },
                        // As tall as its kinds until they run past the room on the screen, and scrolled from there.
                        new ScrollView { Style = Styles.ScrollView, FitsContent = true, Content = new VStack().As(out _kindRows) }
                    }
                }
            };
        }
    }
}
