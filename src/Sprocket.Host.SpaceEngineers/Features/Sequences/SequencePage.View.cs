using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    sealed partial class SequencePage
    {
        SequenceBar _bar;
        StepList _steps;
        Button _doSomething;
        Button _wait;
        Button _run;
        Button _stop;
        Button _if;
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

                    // The ways to add a step and the reason there are none share one line, so locking changes what the
                    // line says and not where anything stands. The reason is padded as the buttons are, so its words
                    // start where theirs did, and the plus stands in the column the steps' grips do.
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Button { Style = Styles.Start }.As(out _doSomething),
                            new Button { Style = Styles.Start }.As(out _wait),
                            new Button { Style = Styles.Start }.As(out _run),
                            new Button { Style = Styles.Start }.As(out _stop),
                            new Button { Style = Styles.Start }.As(out _if),
                            new Label { Style = Styles.Aside, Padding = new Thickness(16f, 4f), IsVisible = false }.As(out _locked)
                        }
                    }
                }
            };
        }
    }
}
