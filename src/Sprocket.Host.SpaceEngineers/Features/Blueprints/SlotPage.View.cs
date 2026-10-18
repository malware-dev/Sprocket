using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    public sealed partial class SlotPage
    {
        Label _locked;
        Label _purposeHeading;
        Composer _composer;
        RichText _purpose;
        Label _shortHeading;
        TextBox _short;
        Label _shownShort;
        Label _expectsHeading;
        Label _expects;
        Switch _required;

        /// <summary>What the slot is for, in its box or as it reads, then the kinds of block it expects.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 8f,
                Children =
                {
                    new Label { Style = Styles.Aside, Wraps = true, Padding = new Thickness(4f, 0f, 0f, 8f), IsVisible = false }.As(out _locked),

                    new Label { Style = Styles.Heading }.As(out _purposeHeading),
                    new Composer(160f, Styles.SlotDescription).As(out _composer),
                    new RichText { Style = Styles.SlotDescription, IsVisible = false }.As(out _purpose),

                    new Label { Style = Styles.NextHeading, IsVisible = false }.As(out _shortHeading),
                    new TextBox { Style = Styles.Search, Focusable = true, IsVisible = false, Placement = FixedSize.Wide(460f) }.As(out _short),
                    new Label { Style = Styles.Phrase, IsVisible = false }.As(out _shownShort),

                    new Label { Style = Styles.NextHeading }.As(out _expectsHeading),
                    new Label { Style = Styles.Phrase, Wraps = true }.As(out _expects),

                    new Switch { Style = Styles.Switch, IsVisible = false }.As(out _required)
                }
            };
        }
    }
}
