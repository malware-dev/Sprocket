using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public sealed partial class LibraryPage
    {
        TextBox _hereSearch;
        VStack _here;
        ScrollView _hereScroll;
        LibraryMoves _moves;
        TextBox _search;
        VStack _list;
        ScrollView _listScroll;
        Gutter _opened;
        BlueprintDetail _detail;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            new Gutter(Sizes.Indent, Gaps.Items) { Body = { Children = { new BlueprintDetail(Editor).As(out _detail) } } }.As(out _opened);

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
                            new TextBox { Style = Styles.Search, Focusable = true }.As(out _search),
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
