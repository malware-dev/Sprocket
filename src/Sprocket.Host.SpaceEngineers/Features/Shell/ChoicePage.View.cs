using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    sealed partial class ChoicePage
    {
        Label _empty;
        VStack _answers;
        HStack _typed;
        TextBox _box;
        Button _give;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.Empty, Wraps = true, IsVisible = false }.As(out _empty),
                    new VStack { Spacing = 8f }.As(out _answers),

                    // Typed in the list itself, beside the answers that are listed, rather than on a screen of its own.
                    new HStack
                    {
                        Spacing = 16f,
                        Style = Styles.Answer,
                        IsVisible = false,
                        Children =
                        {
                            new TextBox { Style = Styles.Field, Focusable = true, Placement = Flex.Grow }.As(out _box),
                            new Button { Style = Styles.Button }.As(out _give)
                        }
                    }.As(out _typed)
                }
            };
        }
    }
}
