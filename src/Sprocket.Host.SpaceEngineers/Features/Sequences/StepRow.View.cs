using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Controls;
using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    partial class StepRow
    {
        DragHandle _grip;
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
            Base.Spacing = Gaps.Parts;

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Within },
                Children =
                {
                    new DragHandle { Placement = FixedSize.Size(24f, 24f) }.As(out _grip),
                    new Label { Style = Styles.Aside, Placement = FixedSize.Width(36f) }.As(out _number)
                }
            });

            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Within },
                Placement = Flex.Grow,
                Children =
                {
                    new Flow
                    {
                        Base = { Spacing = Gaps.Parts, LineSpacing = Gaps.Parts, Indent = Sizes.Indent },
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Awaited, Style = Styles.Waits, IsVisible = false, Halo = Symbols.AwaitedOutline, Base = { HaloSize = 20f * Sizes.Halo } }
                                .As(out _leadWaits),
                            new RichText { Style = Styles.PhraseTerm }.As(out _lead)
                        }
                    }.As(out _sentence),

                    new Label { Style = Styles.Problem, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _problem)
                }
            });

            Children.Add(new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f) }.As(out _duplicate));
            Children.Add(new Glyph { Symbol = Symbols.Pause, Style = Styles.Holds, Placement = FixedSize.Size(32f, 32f) }.As(out _pause));

            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _delete));
        }
    }
}
