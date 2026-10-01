using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    sealed partial class HomePage
    {
        Label _plainHeading;
        Label _empty;
        VStack _plainRows;
        Button _new;
        Label _blueprintsHeading;
        VStack _blueprintGroups;

        /// <inheritdoc />
        /// <remarks>
        /// Two lists, each the way a sequence lays out its steps: a heading, what is there, and the ways to add more on
        /// the line under it. Logic systems of their own first, then the blueprints, so a row of its own is never read as
        /// belonging to the blueprint above it. A blueprint is added from a logic system, or from the library in the page's
        /// header, so the second list ends with its last blueprint.
        /// </remarks>
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    new Label { Style = Styles.Heading }.As(out _plainHeading),
                    new VStack
                    {
                        Children =
                        {
                            new Label { Style = Styles.Empty, Wraps = true }.As(out _empty),
                            new VStack { Spacing = 8f }.As(out _plainRows),
                            new HStack { Padding = new Thickness(0f, 8f, 0f, 0f), Children = { new Button { Style = Styles.Start }.As(out _new) } }
                        }
                    },

                    new Label { Style = Styles.NextHeading }.As(out _blueprintsHeading),
                    new VStack().As(out _blueprintGroups)
                }
            };
        }
    }
}
