using Lumel.Controls;
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
        Label _problem;
        Glyph _leadWaits;
        RichText _lead;
        Glyph _duplicate;
        Glyph _pause;
        Glyph _delete;

        /// <summary>Makes the grip and the step's number, the sentence, the pause and the bin.</summary>
        void CreateView()
        {
            Spacing = 12f;
            Base.Padding = new Thickness(16f, 8f);

            Children.Add(new HStack
            {
                Spacing = 4f,
                Children =
                {
                    new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, Placement = FixedSize.Size(24f, 24f) }.As(out _grip),
                    new Label { Style = Styles.Aside, Placement = FixedSize.Wide(36f) }.As(out _number)
                }
            });

            Children.Add(new VStack
            {
                Spacing = 6f,
                Placement = Flex.Grow,
                Children =
                {
                    new Flow
                    {
                        Spacing = 8f,
                        LineSpacing = 8f,
                        Indent = 24f,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Awaited, Style = Styles.Waits, IsVisible = false, Halo = Symbols.AwaitedOutline, HaloSize = 20f * Sizes.Halo }
                                .As(out _leadWaits),
                            new RichText { Style = Styles.PhraseTerm }.As(out _lead)
                        }
                    }.As(out _sentence),

                    new Label { Style = Styles.Problem, Wraps = true, IsVisible = false }.As(out _problem)
                }
            });

            Children.Add(new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f) }.As(out _duplicate));
            Children.Add(new Glyph { Symbol = Symbols.Pause, Style = Styles.Holds, Placement = FixedSize.Size(32f, 32f) }.As(out _pause));

            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _delete));
        }
    }
}
