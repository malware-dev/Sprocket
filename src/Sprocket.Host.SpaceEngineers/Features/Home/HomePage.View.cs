using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public sealed partial class HomePage
    {
        Label _heading;
        Label _empty;
        VStack _list;
        Button _new;

        /// <inheritdoc />
        /// <remarks>
        /// One list, the way a sequence lays out its steps: a heading, what is there, and the way to add more on the line
        /// under it. Each logic system is its group, its installations under it.
        /// </remarks>
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    new Label { Style = Styles.Heading }.As(out _heading),
                    new Label { Style = Styles.Empty, Wraps = true }.As(out _empty),
                    new VStack().As(out _list),
                    new HStack { Padding = new Thickness(0f, 8f, 0f, 0f), Children = { new Button { Style = Styles.Start }.As(out _new) } }
                }
            };
        }
    }
}
