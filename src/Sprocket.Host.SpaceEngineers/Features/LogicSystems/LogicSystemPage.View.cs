using Lumel;
using Lumel.Controls;
using Lumel.Images;
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
        TabView _tabs;
        Tab _triggersTab;
        Glyph _triggersMark;
        Tab _sequencesTab;
        Glyph _sequencesMark;
        Tab _stateTab;
        StatePanel _statePanel;
        Tab _panelTab;
        Label _noPanel;
        Label _noTriggers;
        VStack _noTriggersLine;
        VStack _noPanelLine;
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
        VStack _addTriggers;
        PlusMenu _addTrigger;
        VStack _triggerList;
        VStack _addSequence;
        Button _newSequence;
        Label _noSequences;
        VStack _sequenceList;
        PartList _slotList;
        VStack _addCategory;
        Button _newCategory;
        Button _optimize;

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
                        Base = { Padding = new Thickness(4f, 8f, 0f, 8f) },
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _usingLead),
                            new HStack { Placement = Flex.Grow }.As(out _usingChoice)
                        }
                    }.As(out _using),

                    new Label { Style = Styles.WarningLine, Wraps = true, IsVisible = false }.As(out _halted),

                    new HStack
                    {
                        Spacing = 8f,
                        Base = { Padding = new Thickness(4f, 0f, 0f, 8f) },
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.Aside }.As(out _lockedText)
                        }
                    }.As(out _locked),

                    new TabView
                    {
                        HeaderStyle = Styles.Tab,
                        Base = { Padding = new Thickness(0f, 8f, 0f, 0f) },
                        Spacing = 8f,
                        Tabs =
                        {
                            new Tab
                            {
                                Marker = new Glyph { Symbol = Symbols.Warning, Style = Styles.TabMark, IsVisible = false }.As(out _triggersMark),
                                Content = new VStack
                                {
                                    Children =
                                    {
                                        new VStack
                                        {
                                            Base = { Padding = new Thickness(0f, 8f, 0f, 0f) },
                                            IsVisible = false,
                                            Children = { new Label { Style = Styles.Empty, Wraps = true }.As(out _noTriggers) }
                                        }.As(out _noTriggersLine),
                                        new VStack { Spacing = 0f }.As(out _triggerList),
                                        new VStack
                                        {
                                            Base = { Padding = new Thickness(0f, 8f, 0f, 0f) },
                                            Children =
                                            {
                                                new HStack { Children = { new PlusMenu().As(out _addTrigger) } }
                                            }
                                        }.As(out _addTriggers)
                                    }
                                }
                            }.As(out _triggersTab),
                            new Tab
                            {
                                Marker = new Glyph { Symbol = Symbols.Warning, Style = Styles.TabMark, IsVisible = false }.As(out _sequencesMark),
                                Content = new VStack
                                {
                                    Base = { Padding = new Thickness(0f, 8f, 0f, 0f) },
                                    Children =
                                    {
                                        new Label { Style = Styles.Empty, Wraps = true }.As(out _noSequences),
                                        new VStack { Spacing = 8f }.As(out _sequenceList),
                                        new VStack
                                        {
                                            Base = { Padding = new Thickness(0f, 8f, 0f, 0f) },
                                            Children = { new HStack { Children = { new Button { Style = Styles.Start }.As(out _newSequence) } } }
                                        }.As(out _addSequence)
                                    }
                                }
                            }.As(out _sequencesTab),
                            new Tab { Content = new StatePanel().As(out _statePanel) }.As(out _stateTab),
                            new Tab
                            {
                                Content = new VStack
                                {
                                    Base = { Padding = new Thickness(0f, 4f, 0f, 0f) },
                                    Children =
                                    {
                                        new VStack
                                        {
                                            Base = { Padding = new Thickness(0f, 4f, 0f, 0f) },
                                            IsVisible = false,
                                            Children = { new Label { Style = Styles.Empty, Wraps = true }.As(out _noPanel) }
                                        }.As(out _noPanelLine),
                                        new VStack
                                        {
                                            Spacing = 4f,
                                            Base = { Padding = new Thickness(0f, 0f, 0f, 16f) },
                                            IsVisible = false,
                                            Children =
                                            {
                                                new Label { Style = Styles.SubHeading }.As(out _descriptionHeading),
                                                new Composer(120f, Styles.Example).As(out _composer),
                                                new RichText { Style = Styles.Description, IsVisible = false }.As(out _description)
                                            }
                                        }.As(out _describing),
                                        new PartList { Spacing = 16f, IsVisible = false }.As(out _slotList),
                                        new VStack
                                        {
                                            Base = { Padding = new Thickness(12f, 16f, 0f, 0f) },
                                            IsVisible = false,
                                            Children =
                                            {
                                                new HStack
                                                {
                                                    Children =
                                                    {
                                                        new Button { Style = Styles.Start }.As(out _newCategory),
                                                        new HStack { Placement = Flex.Spare },
                                                        new Button { Style = Styles.Button, Align = Align.Center }.As(out _optimize)
                                                    }
                                                }
                                            }
                                        }.As(out _addCategory)
                                    }
                                }
                            }.As(out _panelTab)
                        }
                    }.As(out _tabs)
                }
            };
        }
    }
}
