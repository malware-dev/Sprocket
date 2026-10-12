using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Ownership
{
    public sealed partial class PausedPage
    {
        /// <summary>How wide the column it says everything in stands, centered on the page.</summary>
        const float Column = 640f;

        RichText _body;
        Button _resume;
        Button _leave;
        Button _remove;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            // Centered down the page by the room either side of it, which the two spacers share.
            return new VStack
            {
                Align = Align.Fill,
                Children =
                {
                    new Label { Placement = Flex.Grow },
                    new VStack
                    {
                        Align = Align.Center,
                        Placement = Fixed.Wide(Column),
                        Spacing = 16f,
                        Children =
                        {
                            new RichText { Style = Styles.Codex }.As(out _body),
                            new HStack
                            {
                                Align = Align.Center,
                                Spacing = 8f,
                                Children =
                                {
                                    new Button { Style = Styles.Button }.As(out _resume),
                                    new Button { Style = Styles.Button }.As(out _leave),
                                    new Button { Style = Styles.Button }.As(out _remove)
                                }
                            }
                        }
                    },
                    new Label { Placement = Flex.Grow }
                }
            };
        }
    }
}
