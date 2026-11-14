using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
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
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Label { Style = Styles.Empty, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _empty),
                    new VStack { Base = { Spacing = Gaps.Parts } }.As(out _answers)
                }
            };
        }
    }
}
