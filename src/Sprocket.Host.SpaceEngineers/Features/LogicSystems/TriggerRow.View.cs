using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class TriggerRow
    {
        Flow _sentence;
        Label _lead;
        Label _between;
        Label _live;
        Label _problem;
        Glyph _delete;
        Glyph _duplicate;

        /// <summary>Makes the sentence with its opening and joining words, the badge and the problem under it, and the bin.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(16f, 8f);

            Children.Add(new VStack
            {
                Spacing = 4f,
                Placement = Flex.Grow,
                Children =
                {
                    new Flow
                    {
                        Spacing = 8f,
                        LineSpacing = 8f,
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _lead),
                            new Label { Style = Styles.Phrase }.As(out _between),
                            new Label { Style = Styles.Live }.As(out _live)
                        }
                    }.As(out _sentence),

                    new Label { Style = Styles.Problem, Wraps = true }.As(out _problem)
                }
            });

            Children.Add(new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f) }.As(out _duplicate));
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _delete));
        }
    }
}
