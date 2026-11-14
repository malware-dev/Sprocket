using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    public sealed partial class SequencePage
    {
        SequenceBar _bar;
        StepList _steps;
        PlusMenu _add;
        Label _locked;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new SequenceBar().As(out _bar),
                    new StepList().As(out _steps),

                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new PlusMenu().As(out _add),
                            // In the place of the button it stands for, its words where the button's were.
                            new Label { Base = { Padding = new Thickness(16f, 4f) }, Style = Styles.Aside, IsVisible = false }.As(out _locked)
                        }
                    }
                }
            };
        }
    }
}
