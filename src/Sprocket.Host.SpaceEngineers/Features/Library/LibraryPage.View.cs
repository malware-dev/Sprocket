using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public sealed partial class LibraryPage
    {
        TextBox _hereSearch;
        VStack _here;
        ScrollView _hereScroll;
        LibraryMoves _moves;
        TextBox _search;
        Button _import;
        VStack _list;
        ScrollView _listScroll;
        Gutter _opened;
        BlueprintDetail _detail;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            new Gutter(Sizes.Indent, Gaps.Items) { Spans = true, Body = { Children = { new BlueprintDetail(Editor).As(out _detail) } } }.As(out _opened);

            return new HStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new VStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new TextBox { Style = Styles.Search, Focusable = true }.As(out _hereSearch),
                            new ScrollView
                            {
                                Style = Styles.ScrollView,
                                Placement = Flex.Grow,
                                Content = new VStack { Base = { Spacing = Gaps.Sections } }.As(out _here)
                            }.As(out _hereScroll)
                        }
                    },
                    new LibraryMoves(Editor).As(out _moves),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new HStack
                            {
                                Base = { Spacing = Gaps.Parts },
                                Children =
                                {
                                    new TextBox { Style = Styles.Search, Focusable = true, Placement = Flex.Grow }.As(out _search),
                                    new Button { Style = Styles.Button }.As(out _import)
                                }
                            },
                            new ScrollView
                            {
                                Style = Styles.ScrollView,
                                Placement = Flex.Grow,
                                Content = new VStack { Base = { Spacing = Gaps.Sections } }.As(out _list)
                            }.As(out _listScroll)
                        }
                    }
                }
            };
        }
    }
}
