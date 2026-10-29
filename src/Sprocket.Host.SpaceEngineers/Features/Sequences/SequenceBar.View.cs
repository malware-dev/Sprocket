using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    partial class SequenceBar
    {
        Glyph _light;
        Label _status;
        Label _problem;
        Label _lastRun;
        Label _skipped;

        /// <summary>Makes the state on the left and the news of what was skipped after it.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Items;

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Glyph { Symbol = Symbols.Disc, Style = Styles.Light, Placement = FixedSize.Size(16f, 16f) }.As(out _light),
                    new Label { Style = Styles.Status }.As(out _status),
                    new Label { Style = Styles.Status, IsVisible = false }.As(out _problem),
                    new Label { Style = Styles.Aside }.As(out _lastRun)
                }
            });

            Children.Add(new HStack
            {
                Placement = Flex.Grow,
                Children = { new Label { Style = Styles.Status, IsVisible = false }.As(out _skipped) }
            });
        }
    }
}
