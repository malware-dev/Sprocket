using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    public sealed partial class SlotPage
    {
        Label _locked;
        Label _purposeHeading;
        Composer _composer;
        RichText _purpose;
        Section _naming;
        Label _shortHeading;
        TextBox _short;
        Label _shownShort;
        Label _expectsHeading;
        Label _expects;
        SlotChip _expectsPart;
        Switch _required;

        /// <summary>What the slot is for, in its box or as it reads, then the kinds of block it expects.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new Label { Style = Styles.Aside, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _locked),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Sections },
                        Children =
                        {
                            new Section
                            {
                                Children =
                                {
                                    new Label { Style = Styles.H2 }.As(out _purposeHeading),
                                    new Composer(160f, Styles.SlotDescription).As(out _composer),
                                    new RichText { Style = Styles.SlotDescription, IsVisible = false }.As(out _purpose)
                                }
                            },
                            new Section
                            {
                                IsVisible = false,
                                Children =
                                {
                                    new Label { Style = Styles.H2 }.As(out _shortHeading),
                                    new TextBox { Style = Styles.Search, Focusable = true, IsVisible = false, MaxLength = ShortLongest, Placement = FixedSize.Width(460f) }.As(out _short),
                                    new Label { Style = Styles.Phrase, IsVisible = false }.As(out _shownShort)
                                }
                            }.As(out _naming),
                            new Section
                            {
                                Children =
                                {
                                    new Label { Style = Styles.H2 }.As(out _expectsHeading),
                                    new Label { Style = Styles.Phrase, TextWrapping = TextWrapping.Wrap }.As(out _expects),
                                    new SlotChip { IsVisible = false, Align = Align.Left }.As(out _expectsPart)
                                }
                            },
                            // Hidden: the runtime runs no installation with an optional slot left empty.
                            new Switch { Style = Styles.Switch, IsVisible = false }.As(out _required)
                        }
                    }
                }
            };
        }
    }
}
