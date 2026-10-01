using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    partial class Prompt
    {
        Label _question;
        TextBox _box;
        Button _take;

        /// <summary>Makes the question, and under it the box with the way on beside it.</summary>
        void CreateView()
        {
            Style = Styles.Prompt;
            Placement = Fixed.Wide(520f);
            Gap = 4f;

            Content = new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.ColumnNote, Wraps = true }.As(out _question),
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new TextBox { Style = Styles.Value, Focusable = true, Placement = Flex.Grow }.As(out _box),
                            new Button { Style = Styles.Button }.As(out _take)
                        }
                    }
                }
            };
        }
    }
}
