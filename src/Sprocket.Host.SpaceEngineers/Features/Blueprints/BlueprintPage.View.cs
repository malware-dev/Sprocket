using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Writing;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    sealed partial class BlueprintPage
    {
        RichText _description;
        VStack _writing;
        Label _writingHeading;
        Composer _composer;
        Label _partsHeading;
        Label _needs;
        VStack _partList;

        /// <summary>What the blueprint's author wrote about it, or the box it is written in, then the slots with how many still need a block.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 4f,
                Children =
                {
                    new RichText { Style = Styles.Description, IsVisible = false }.As(out _description),

                    new VStack
                    {
                        Spacing = 8f,
                        IsVisible = false,
                        Padding = new Thickness(20f, 4f),
                        Children =
                        {
                            new Label { Style = Styles.Heading }.As(out _writingHeading),
                            new Composer(240f, Styles.Example, Windows).As(out _composer)
                        }
                    }.As(out _writing),
                    new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) },
                    new HStack
                    {
                        Spacing = 12f,
                        Children =
                        {
                            new Label { Style = Styles.Heading }.As(out _partsHeading),
                            new Label { Style = Styles.Needs, IsVisible = false }.As(out _needs)
                        }
                    },
                    new VStack { Spacing = 4f }.As(out _partList)
                }
            };
        }
    }
}
