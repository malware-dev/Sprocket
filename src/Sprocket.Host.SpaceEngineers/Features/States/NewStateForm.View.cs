using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.States
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
            Base.Spacing = Gaps.Parts;
            Placement = Flex.Grow;

            Children.Add(new FieldSet().As(out _head));
            Children.Add(new FieldSet { IsVisible = false }.As(out _choicesRow));
            Children.Add(new FieldSet().As(out _starts));

            _choicesRow.Rows.Add(new Label { Style = Styles.ValueLabel }.As(out _choicesLabel),
                new HStack
                {
                    Base = { Spacing = Gaps.Parts },
                    Children =
                    {
                        new Flow { Base = { Spacing = Gaps.Parts, LineSpacing = Gaps.Parts } }.As(out _chips),
                        new Button { Style = Styles.Start }.As(out _addChoice)
                    }
                });
        }
    }
}
