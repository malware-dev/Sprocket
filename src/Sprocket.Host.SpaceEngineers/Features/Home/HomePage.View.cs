using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    sealed partial class HomePage
    {
        Button _new;
        Button _load;
        VStack _plain;
        Label _empty;
        VStack _plainRows;
        VStack _blueprintGroups;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    new VStack
                    {
                        Children =
                        {
                            // What adds stands on the left with its plus; the way to the library goes somewhere, so it stands on
                            // the right as a button of its own, ending where the rows' controls do.
                            new HStack
                            {
                                Spacing = 8f,
                                Padding = new Thickness(0f, 0f, 16f, 0f),
                                Children =
                                {
                                    new Button { Style = Styles.Start }.As(out _new),
                                    new HStack { Placement = Flex.Grow },
                                    new Button { Style = Styles.Button }.As(out _load)
                                }
                            },
                            new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) }
                        }
                    },

                    // Logic systems of their own first, with nothing over them: a heading of theirs would be the
                    // one thing on the screen asking to be understood before anything is made. The blueprints
                    // follow, so a row of its own is never read as belonging to the blueprint above it.
                    new VStack
                    {
                        Children =
                        {
                            new Label { Style = Styles.Empty, Wraps = true }.As(out _empty),
                            new VStack { Spacing = 8f }.As(out _plainRows)
                        }
                    }.As(out _plain),
                    new VStack().As(out _blueprintGroups)
                }
            };
        }
    }
}
