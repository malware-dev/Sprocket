using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

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
        SlotPanel _slotPanel;
        Label _noTriggers;
        VStack _noTriggersLine;
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

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = Gaps.Items,
                Children =
                {
                    new HStack
                    {
                        Spacing = Gaps.Parts,
                        Children =
                        {
                            new Label { Style = Styles.Phrase }.As(out _usingLead),
                            new HStack { Placement = Flex.Grow }.As(out _usingChoice)
                        }
                    }.As(out _using),

                    new Label { Style = Styles.WarningLine, Wraps = true, IsVisible = false }.As(out _halted),

                    new HStack
                    {
                        Spacing = Gaps.Parts,
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.Aside }.As(out _lockedText)
                        }
                    }.As(out _locked),

                    new TabView
                    {
                        HeaderStyle = Styles.Tab,
                        Spacing = Gaps.Items,
                        Tabs =
                        {
                            new Tab
                            {
                                Marker = new Glyph { Symbol = Symbols.Warning, Style = Styles.TabMark, IsVisible = false }.As(out _triggersMark),
                                Content = new VStack
                                {
                                    Spacing = Gaps.Items,
                                    Children =
                                    {
                                        new VStack
                                        {
                                            IsVisible = false,
                                            Children = { new Label { Style = Styles.Empty, Wraps = true }.As(out _noTriggers) }
                                        }.As(out _noTriggersLine),
                                        new VStack { Spacing = Gaps.Items }.As(out _triggerList),
                                        new VStack
                                        {
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
                                    Spacing = Gaps.Items,
                                    Children =
                                    {
                                        new Label { Style = Styles.Empty, Wraps = true }.As(out _noSequences),
                                        new VStack { Spacing = Gaps.Items }.As(out _sequenceList),
                                        new VStack
                                        {
                                            Children = { new HStack { Children = { new Button { Style = Styles.Start }.As(out _newSequence) } } }
                                        }.As(out _addSequence)
                                    }
                                }
                            }.As(out _sequencesTab),
                            new Tab { Content = new StatePanel().As(out _statePanel) }.As(out _stateTab),
                            new Tab
                            {
                                Content = new SlotPanel().As(out _slotPanel)
                            }.As(out _panelTab)
                        }
                    }.As(out _tabs)
                }
            };
        }
    }
}
