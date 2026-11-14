using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
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
            Base.Spacing = Gaps.Items;
            Style = Styles.Answer;

            Children.Add(new Glyph { Style = Styles.Waits, IsVisible = false }.As(out _mark));
            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Within },
                Placement = Flex.Grow,
                Children =
                {
                    new Label { Style = Styles.Name }.As(out _words),

                    new Label { Style = Styles.WarningLine, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _caution)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Check, Style = Styles.Icon, Placement = FixedSize.Size(24f, 24f), IsVisible = false }.As(out _given));
        }
    }
}
