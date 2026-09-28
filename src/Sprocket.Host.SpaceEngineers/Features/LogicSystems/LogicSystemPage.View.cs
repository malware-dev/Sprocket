using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    sealed partial class LogicSystemPage
    {
        HStack _using;
        Label _usingLead;
        Label _usingName;
        HStack _usingChoice;
        Button _editPanel;
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
                    // Whose blocks every sentence below names, said before any of them: fixed for one logic system,
                    // and for a blueprint one of the blocksets made from it, with the way to set up its configuration panel.
                    new HStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(4f, 8f, 0f, 8f),
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _usingLead),
                            new Label { Style = Styles.Name }.As(out _usingName),
                            new HStack { Placement = Flex.Grow }.As(out _usingChoice),
                            new Button { Style = Styles.Button }.As(out _editPanel)
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
