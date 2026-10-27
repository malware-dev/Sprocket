using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

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
            Base.Padding = new Thickness(0f, 8f, 0f, 0f);
            Children.Add(new Label { Style = Styles.Empty, Wraps = true, IsVisible = false }.As(out _empty));
            // The buttons start where the rows' grips do. The room above the states' heading is held by a plain holder, since
            // the heading's style sets its own padding.
            Children.Add(new RichText { Style = Styles.SubHeadingTerm, Align = Align.Left }.As(out _typesHeading));
            Children.Add(new CarryList { Spacing = 4f }.As(out _types));
            Children.Add(new HStack { Base = { Padding = new Thickness(12f, 8f, 0f, 0f) }, Children = { new Button { Style = Styles.Start }.As(out _addType) } }.As(out _addTypes));
            Children.Add(new VStack
            {
                Base = { Padding = new Thickness(0f, 24f, 0f, 8f) },
                Children = { new Label { Style = Styles.SubHeading }.As(out _statesHeading) }
            });
            Children.Add(new CarryList { Spacing = 4f }.As(out _states));
            Children.Add(new HStack { Base = { Padding = new Thickness(12f, 8f, 0f, 0f) }, Children = { new Button { Style = Styles.Start }.As(out _addState) } }.As(out _addStates));
        }
    }
}
