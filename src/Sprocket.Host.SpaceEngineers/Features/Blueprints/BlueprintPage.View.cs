using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    sealed partial class BlueprintPage
    {
        Label _madeHeading;
        VStack _made;
        StatusRow _another;
        StatusRow _chosen;
        Label _partsHeading;
        VStack _partList;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new Tops
            {
                Spacing = 32f,
                Children =
                {
                    // What is made from it, with the way to make another at the foot.
                    new VStack
                    {
                        Spacing = 4f,
                        Placement = Fixed.Wide(380f),
                        Children =
                        {
                            new Label { Style = Styles.Heading }.As(out _madeHeading),
                            new VStack { Spacing = 4f }.As(out _made),
                            new StatusRow().As(out _another)
                        }
                    },

                    // The chosen one at the head of its blocks, and then the parts.
                    new VStack
                    {
                        Spacing = 4f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new StatusRow().As(out _chosen),
                            new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) },
                            new Label { Style = Styles.Heading }.As(out _partsHeading),
                            new VStack().As(out _partList)
                        }
                    }
                }
            };
        }
    }
}
