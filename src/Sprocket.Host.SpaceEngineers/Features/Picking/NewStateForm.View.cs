using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    public partial class NewStateForm
    {
        FieldSet _head;
        FieldSet _choicesRow;
        Label _choicesLabel;
        Flow _chips;
        Button _addChoice;
        FieldSet _starts;

        /// <summary>Makes the name and type, the new type's choices with the way to add one, and the value it starts as, one under the next.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Placement = Flex.Grow;

            Children.Add(new FieldSet().As(out _head));
            Children.Add(new FieldSet { IsVisible = false }.As(out _choicesRow));
            Children.Add(new FieldSet().As(out _starts));

            _choicesRow.Rows.Add(new Label { Style = Styles.ValueLabel }.As(out _choicesLabel),
                new HStack
                {
                    Spacing = 12f,
                    Children =
                    {
                        new Flow { Spacing = 8f, LineSpacing = 8f }.As(out _chips),
                        new Button { Style = Styles.Start }.As(out _addChoice)
                    }
                });
        }
    }
}
