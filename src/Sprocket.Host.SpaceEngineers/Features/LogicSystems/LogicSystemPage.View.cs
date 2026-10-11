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
                    // For a blueprint, what it does and what whoever fills it in needs to know, before anything else.
                    new VStack
                    {
                        Spacing = 8f,
                        Padding = new Thickness(4f, 0f, 0f, 8f),
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.Heading }.As(out _descriptionHeading),
                            new Composer(120f, Styles.Example).As(out _composer),
                            new RichText { Style = Styles.Description, IsVisible = false }.As(out _description)
                        }
                    }.As(out _describing),

                    // For a blueprint, whose blocks every sentence below names, said before any of them: one of the
                    // installations made from it.
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

                    // Why Sprocket switched it off, said before anything it is about.
                    new Label { Style = Styles.Caution, Wraps = true, Padding = new Thickness(4f, 0f, 0f, 8f), IsVisible = false }.As(out _halted),

                    // Why nothing below can be changed, and how to change that, said before any of it.
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

                    // Named, because this screen holds two lists of different things and the rows in them read
                    // alike: what is already here, then the ways to add more on the line under it, twice over -
                    // the way a sequence lays out its steps.
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

                    // For a blueprint, the slots its sentences name, each with the blocks of the installation shown.
                    new Label { Style = Styles.NextHeading, IsVisible = false }.As(out _slotsHeading),
                    new PartList { Spacing = 4f, IsVisible = false }.As(out _slotList)
                }
            };
        }
    }
}
