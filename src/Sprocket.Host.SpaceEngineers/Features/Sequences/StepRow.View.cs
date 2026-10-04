using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    partial class StepRow
    {
        Grip _grip;
        Label _number;
        Flow _sentence;
        Glyph _leadWaits;
        Label _lead;
        Glyph _pause;
        Glyph _delete;

        /// <summary>Makes the grip and the step's number, the sentence, the pause and the bin.</summary>
        void CreateView()
        {
            Spacing = 12f;
            Padding = new Thickness(16f, 8f);

            Children.Add(new HStack
            {
                Spacing = 4f,
                Children =
                {
                    new Grip { Color = Palette.Faint, Placement = Fixed.Size(24f, 24f) }.As(out _grip),
                    new Label { Style = Styles.Aside, Placement = Fixed.Wide(36f) }.As(out _number)
                }
            });

            Children.Add(new Flow
            {
                Spacing = 8f,
                LineSpacing = 8f,
                Placement = Flex.Grow,
                Children =
                {
                    // Before its word, as it stands before a member's name: running another sequence lasts until it finishes.
                    new Glyph { Symbol = Symbols.Finish, Style = Styles.Waits, IsVisible = false }.As(out _leadWaits),
                    new Label { Style = Styles.Phrase }.As(out _lead)
                }
            }.As(out _sentence));

            Children.Add(new Glyph { Symbol = Symbols.Pause, Style = Styles.Holds, Placement = Fixed.Size(32f, 32f) }.As(out _pause));

            // The one control on the row that destroys something, so the one place that warns under the pointer.
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = Fixed.Size(32f, 32f) }.As(out _delete));
        }
    }
}
