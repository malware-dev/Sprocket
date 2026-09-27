using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    sealed partial class LogicSystemPage
    {
        HStack _using;
        Label _usingLead;
        Slot _usingName;
        Label _triggersHeading;
        Button _when;
        Button _every;
        VStack _triggerList;
        Label _sequencesHeading;
        Button _newSequence;
        Label _noSequences;
        VStack _sequenceList;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    // A blueprint is edited through one of its logic systems, so whose blocks every sentence below
                    // names is said before any of them.
                    new HStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(4f, 8f, 0f, 8f),
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _usingLead),
                            new Slot().As(out _usingName)
                        }
                    }.As(out _using),

                    // Named, because this screen holds two lists of different things and the rows in them read
                    // alike: the ways to add something on top, then what is already here, twice over.
                    new Label { Style = Styles.Heading }.As(out _triggersHeading),
                    new VStack
                    {
                        Children =
                        {
                            new HStack
                            {
                                Spacing = 8f,
                                Children =
                                {
                                    new Button { Style = Styles.Start }.As(out _when),
                                    new Button { Style = Styles.Start }.As(out _every)
                                }
                            },
                            new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) }
                        }
                    },
                    new VStack { Spacing = 8f }.As(out _triggerList),

                    new Label { Style = Styles.NextHeading }.As(out _sequencesHeading),
                    new VStack
                    {
                        Children =
                        {
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _newSequence) } },
                            new Rule(1f, Palette.Line) { Padding = new Thickness(0f, 8f) }
                        }
                    },
                    new Label { Style = Styles.Empty, Wraps = true }.As(out _noSequences),
                    new VStack { Spacing = 8f }.As(out _sequenceList)
                }
            };
        }
    }
}
