using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    partial class ChoiceRow
    {
        Glyph _mark;
        Label _words;
        Label _caution;
        Glyph _given;

        /// <summary>Makes the mark of what the answer names, its words with the caution under them, and the mark of the answer already given.</summary>
        void CreateView()
        {
            Spacing = 16f;
            Style = Styles.Answer;

            Children.Add(new Glyph { Style = Styles.Waits, IsVisible = false }.As(out _mark));
            Children.Add(new VStack
            {
                Spacing = 4f,
                Placement = Flex.Grow,
                Children =
                {
                    new Label { Style = Styles.Name }.As(out _words),

                    // Told as well as shown, and told before the answer is given rather than after.
                    new Label { Style = Styles.Caution, Wraps = true, IsVisible = false }.As(out _caution)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Check, Style = Styles.Icon, Placement = FixedSize.Size(24f, 24f), IsVisible = false }.As(out _given));
        }
    }
}
