using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

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
                Spacing = 12f,
                Children =
                {
                    new SequenceBar().As(out _bar),
                    new StepList().As(out _steps),

                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new PlusMenu().As(out _add),
                            new Label { Style = Styles.Aside, Padding = new Thickness(16f, 4f), IsVisible = false }.As(out _locked)
                        }
                    }
                }
            };
        }
    }
}
