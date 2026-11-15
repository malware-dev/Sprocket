using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Images;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers
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
        DragHandle _grip;

        /// <summary>Makes the sentence with its opening and joining words, the badge and the problem under it, and the bin.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;

            // Shown only where the row's place can be changed, as a sequence's grip is.
            Children.Add(new DragHandle { IsVisible = false, Align = Align.Center, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            Children.Add(new Glyph { Style = Styles.Waits, Symbol = Symbols.Trigger, Align = Align.Center, Placement = FixedSize.Width(16f) });
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
                            new Label { Style = Styles.Phrase }.As(out _lead),
                            new Label { Style = Styles.Phrase }.As(out _between),
                            new Label { Style = Styles.Live }.As(out _live)
                        }
                    }.As(out _sentence),

                    new Label { Style = Styles.Problem, TextWrapping = TextWrapping.Wrap }.As(out _problem)
                }
            });

            Children.Add(new Glyph { Symbol = Symbols.Duplicate, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f) }.As(out _duplicate));
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _delete));
        }
    }
}
