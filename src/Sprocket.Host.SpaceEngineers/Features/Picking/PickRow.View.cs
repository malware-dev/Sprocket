using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    partial class PickRow
    {
        ChoiceMark _mark;
        HStack _content;
        Image _picture;
        MemberName _name;
        Label _detail;

        /// <summary>Makes the mark and the picture, then the name with what is quieter about it under it.</summary>
        void CreateView()
        {
            Style = Styles.Pick;

            // The box's control fills the strip at the row's head, down its whole height and out to its edge, so a press
            // anywhere near the box is the box's and never takes the row alone; its padding sets where the box is drawn.
            Children.Add(new ChoiceMark
            {
                Placement = FixedSize.Wide(40f),
                Align = Align.Tall,
                Padding = new Thickness(12f, 0f, 8f, 0f),
                IsVisible = false
            }.As(out _mark));

            Children.Add(new HStack
            {
                Spacing = 8f,
                Placement = Flex.Grow,
                Children =
                {
                    // Reserved for the whole column or for none of it, so rows never jog sideways with which kinds are listed.
                    new Image { Style = Styles.Picture, Placement = FixedSize.Size(56f, 56f), IsVisible = false }.As(out _picture),
                    new VStack
                    {
                        Spacing = 4f,
                        Placement = Flex.Grow,
                        Children =
                        {
                            new MemberName { Style = Styles.PickName, Wraps = true }.As(out _name),
                            new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _detail)
                        }
                    }
                }
            }.As(out _content));
        }
    }
}
