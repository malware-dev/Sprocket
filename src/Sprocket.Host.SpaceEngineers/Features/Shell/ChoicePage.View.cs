using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    public sealed partial class ChoicePage
    {
        Label _empty;
        VStack _answers;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.Empty, Wraps = true, IsVisible = false }.As(out _empty),
                    new VStack { Spacing = 8f }.As(out _answers)
                }
            };
        }
    }
}
