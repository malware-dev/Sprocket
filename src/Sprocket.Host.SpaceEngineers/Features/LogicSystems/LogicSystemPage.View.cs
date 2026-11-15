using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;

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
        CategoryList _triggerList;
        Button _newTriggerCategory;
        VStack _addSequence;
        Button _newSequence;
        Label _noSequences;
        VStack _head;
        CategoryList _sequenceList;
        Button _newSequenceCategory;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Sections },
                Children =
                {
                    new VStack
                    {
                        Base = { Spacing = Gaps.Items },
                        Children =
                        {
                            new HStack
                            {
                                Base = { Spacing = Gaps.Parts },
                                Children =
                                {
                                    new Label { Style = Styles.Phrase }.As(out _usingLead),
                                    new HStack { Placement = Flex.Grow }.As(out _usingChoice)
                                }
                            }.As(out _using),

                            new Label { Style = Styles.WarningLine, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _halted),

                            new HStack
                            {
                                Base = { Spacing = Gaps.Parts },
                                IsVisible = false,
                                Children =
                                {
                                    new Label { Style = Styles.Aside }.As(out _lockedText)
                                }
                            }.As(out _locked)
                        }
                    }.As(out _head),

                    new TabView
                    {
                        Base = { Spacing = Gaps.Items },
                        Placement = Flex.Grow,
                        Tabs =
                        {
                            new Tab
                            {
                                Style = Styles.Tab,
                                Badge = new Glyph { Symbol = Symbols.Warning, Style = Styles.TabMark, IsVisible = false }.As(out _triggersMark),
                                Content = new ScrollView
                                {
                                    Style = Styles.ScrollView,
                                    Content = new VStack
                                    {
                                        Base = { Spacing = Gaps.Items },
                                        Children =
                                        {
                                            new VStack
                                            {
                                                IsVisible = false,
                                                Children = { new Label { Style = Styles.Empty, TextWrapping = TextWrapping.Wrap }.As(out _noTriggers) }
                                            }.As(out _noTriggersLine),
                                            new CategoryList { Base = { Spacing = Gaps.Items, IndentWidth = Sizes.Indent } }.As(out _triggerList),
                                            new VStack
                                            {
                                                Children =
                                                {
                                                    new HStack
                                                    {
                                                        Base = { Spacing = Gaps.Parts },
                                                        Children =
                                                        {
                                                            new PlusMenu().As(out _addTrigger),
                                                            new Button { Style = Styles.Start }.As(out _newTriggerCategory)
                                                        }
                                                    }
                                                }
                                            }.As(out _addTriggers)
                                        }
                                    }
                                }
                            }.As(out _triggersTab),
                            new Tab
                            {
                                Style = Styles.Tab,
                                Badge = new Glyph { Symbol = Symbols.Warning, Style = Styles.TabMark, IsVisible = false }.As(out _sequencesMark),
                                Content = new ScrollView
                                {
                                    Style = Styles.ScrollView,
                                    Content = new VStack
                                    {
                                        Base = { Spacing = Gaps.Items },
                                        Children =
                                        {
                                            new Label { Style = Styles.Empty, TextWrapping = TextWrapping.Wrap }.As(out _noSequences),
                                            new CategoryList { Base = { Spacing = Gaps.Items, IndentWidth = Sizes.Indent } }.As(out _sequenceList),
                                            new VStack
                                            {
                                                Children =
                                                {
                                                    new HStack
                                                    {
                                                        Base = { Spacing = Gaps.Parts },
                                                        Children =
                                                        {
                                                            new Button { Style = Styles.Start }.As(out _newSequence),
                                                            new Button { Style = Styles.Start }.As(out _newSequenceCategory)
                                                        }
                                                    }
                                                }
                                            }.As(out _addSequence)
                                        }
                                    }
                                }
                            }.As(out _sequencesTab),
                            new Tab
                            {
                                Style = Styles.Tab,
                                Content = new ScrollView { Style = Styles.ScrollView, Content = new StatePanel().As(out _statePanel) }
                            }.As(out _stateTab),
                            new Tab
                            {
                                Style = Styles.Tab,
                                Content = new ScrollView { Style = Styles.ScrollView, Content = new SlotPanel().As(out _slotPanel) }
                            }.As(out _panelTab)
                        }
                    }.As(out _tabs)
                }
            };
        }
    }
}
