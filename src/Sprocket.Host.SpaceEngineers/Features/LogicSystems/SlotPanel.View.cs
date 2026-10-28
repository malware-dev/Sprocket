using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    public partial class SlotPanel
    {
        Label _empty;
        VStack _emptyLine;
        Section _describing;
        Label _descriptionHeading;
        Composer _composer;
        RichText _description;
        PartList _slots;
        VStack _adds;
        Button _addCategory;
        Button _optimize;

        /// <summary>Makes the line said while there is nothing, the description under its heading, and the slots with the ways to group and tidy them.</summary>
        void CreateView()
        {
            Spacing = Gaps.Sections;

            Children.Add(new VStack
            {
                IsVisible = false,
                Children = { new Label { Style = Styles.Empty, Wraps = true }.As(out _empty) }
            }.As(out _emptyLine));
            Children.Add(new Section
            {
                IsVisible = false,
                Children =
                {
                    new Label { Style = Styles.H2 }.As(out _descriptionHeading),
                    new Composer(120f, Styles.Description).As(out _composer),
                    new RichText { Style = Styles.Description, IsVisible = false }.As(out _description)
                }
            }.As(out _describing));
            Children.Add(new VStack
            {
                Spacing = Gaps.Items,
                Children =
                {
                    new PartList { Spacing = Gaps.Items, IsVisible = false }.As(out _slots),
                    new VStack
                    {
                        IsVisible = false,
                        Children =
                        {
                            new HStack
                            {
                                Children =
                                {
                                    new Button { Style = Styles.Start }.As(out _addCategory),
                                    new HStack { Placement = Flex.Spare },
                                    new Button { Style = Styles.Button, Align = Align.Center }.As(out _optimize)
                                }
                            }
                        }
                    }.As(out _adds)
                }
            });
        }
    }
}
