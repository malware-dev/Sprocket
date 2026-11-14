using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    partial class StatePanel
    {
        Label _empty;
        RichText _typesHeading;
        ReorderList _types;
        HStack _addTypes;
        Button _addType;
        RichText _statesHeading;
        ReorderList _states;
        HStack _addStates;
        Button _addState;

        /// <summary>Makes the headings, the lists, and the buttons that add to them.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Sections;
            Children.Add(new Label { Style = Styles.Empty, TextWrapping = TextWrapping.Wrap, IsVisible = false }.As(out _empty));
            Children.Add(new Section
            {
                Children =
                {
                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _statesHeading),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Items },
                        Children =
                        {
                            new ReorderList { Base = { Spacing = Gaps.Items } }.As(out _states),
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _addState) } }.As(out _addStates)
                        }
                    }
                }
            });
            Children.Add(new Section
            {
                Children =
                {
                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _typesHeading),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Items },
                        Children =
                        {
                            new ReorderList { Base = { Spacing = Gaps.Items } }.As(out _types),
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _addType) } }.As(out _addTypes)
                        }
                    }
                }
            });
        }
    }
}
