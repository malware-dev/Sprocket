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
        Mark _mark;
        Image _picture;
        MemberName _name;
        Label _detail;

        /// <summary>Makes the mark and the picture, then the name with what is quieter about it under it.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Style = Styles.Pick;

            Children.Add(new Mark { Placement = Fixed.Size(20f, 20f), IsVisible = false }.As(out _mark));

            // Reserved for the whole column or for none of it, so rows never jog sideways with which kinds are listed.
            Children.Add(new Image { Style = Styles.Picture, Placement = Fixed.Size(56f, 56f), IsVisible = false }.As(out _picture));

            Children.Add(new VStack
            {
                Spacing = 4f,
                Placement = Flex.Grow,
                Children =
                {
                    new MemberName { Style = Styles.PickName, Wraps = true }.As(out _name),
                    new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _detail)
                }
            });
        }
    }
}
