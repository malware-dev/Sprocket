using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Writing;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    public sealed partial class LogicSystemPage
    {
        Label _panelHeading;
        VStack _describing;
        Label _descriptionHeading;
        Composer _composer;
        RichText _description;
        HStack _using;
        Label _usingLead;
        HStack _usingChoice;
        Label _halted;
        HStack _locked;
        Label _lockedText;
        Label _triggersHeading;
        VStack _addTriggers;
        PlusMenu _addTrigger;
        VStack _triggerList;
        Label _sequencesHeading;
        VStack _addSequence;
        Button _newSequence;
        Label _noSequences;
        VStack _sequenceList;
        Label _slotsHeading;
        PartList _slotList;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(4f, 8f, 0f, 8f),
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _usingLead),
                            new HStack { Placement = Flex.Grow }.As(out _usingChoice)
                        }
                    }.As(out _using),

                    new Label { Style = Styles.Caution, Wraps = true, Padding = new Thickness(4f, 0f, 0f, 8f), IsVisible = false }.As(out _halted),

                    new HStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(4f, 0f, 0f, 8f),
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.Aside }.As(out _lockedText)
                        }
                    }.As(out _locked),

                    new Label { Style = Styles.Heading }.As(out _triggersHeading),
                    new VStack { Spacing = 8f }.As(out _triggerList),
                    new VStack
                    {
                        Padding = new Thickness(0f, 8f, 0f, 0f),
                        Children =
                        {
                            new HStack { Children = { new PlusMenu().As(out _addTrigger) } }
                        }
                    }.As(out _addTriggers),

                    new Label { Style = Styles.NextHeading }.As(out _sequencesHeading),
                    new Label { Style = Styles.Empty, Wraps = true }.As(out _noSequences),
                    new VStack { Spacing = 8f }.As(out _sequenceList),
                    new VStack
                    {
                        Padding = new Thickness(0f, 8f, 0f, 0f),
                        Children = { new HStack { Children = { new Button { Style = Styles.Start }.As(out _newSequence) } } }
                    }.As(out _addSequence),

                    new Label { Style = Styles.NextHeading, IsVisible = false }.As(out _panelHeading),
                    new VStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(0f, 0f, 0f, 16f),
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.SubHeading }.As(out _descriptionHeading),
                            new Composer(120f, Styles.Example).As(out _composer),
                            new RichText { Style = Styles.Description, IsVisible = false }.As(out _description)
                        }
                    }.As(out _describing),
                    new Label { Style = Styles.SubHeading, IsVisible = false }.As(out _slotsHeading),
                    new PartList { Spacing = 4f, IsVisible = false }.As(out _slotList)
                }
            };
        }
    }
}
