using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class StatePanel
    {
        Label _empty;
        RichText _typesHeading;
        CarryList _types;
        HStack _addTypes;
        Button _addType;
        Label _statesHeading;
        CarryList _states;
        HStack _addStates;
        Button _addState;

        /// <summary>Makes the headings, the lists, and the buttons that add to them.</summary>
        void CreateView()
        {
            Spacing = Gaps.Sections;
            Children.Add(new Label { Style = Styles.Empty, Wraps = true, IsVisible = false }.As(out _empty));
            Children.Add(new Section
            {
                Children =
                {
                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _typesHeading),
                    new VStack
                    {
                        Spacing = Gaps.Items,
                        Children =
                        {
                            new CarryList { Spacing = Gaps.Items }.As(out _types),
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _addType) } }.As(out _addTypes)
                        }
                    }
                }
            });
            Children.Add(new Section
            {
                Children =
                {
                    new Label { Style = Styles.H2 }.As(out _statesHeading),
                    new VStack
                    {
                        Spacing = Gaps.Items,
                        Children =
                        {
                            new CarryList { Spacing = Gaps.Items }.As(out _states),
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _addState) } }.As(out _addStates)
                        }
                    }
                }
            });
        }
    }
}
