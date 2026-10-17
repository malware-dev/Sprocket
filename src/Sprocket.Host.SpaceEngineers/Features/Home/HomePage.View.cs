using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public sealed partial class HomePage
    {
        RichText _heading;
        Label _empty;
        VStack _list;
        Button _new;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    new RichText { Style = Styles.HeadingTerm }.As(out _heading),
                    new Label { Style = Styles.Empty, Wraps = true }.As(out _empty),
                    new VStack().As(out _list),
                    new HStack { Padding = new Thickness(0f, 8f, 0f, 0f), Children = { new Button { Style = Styles.Start }.As(out _new) } }
                }
            };
        }
    }
}
