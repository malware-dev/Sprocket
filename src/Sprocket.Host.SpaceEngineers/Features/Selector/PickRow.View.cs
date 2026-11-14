using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    partial class PickRow
    {
        CheckBox _mark;
        HStack _content;
        Image _picture;
        MemberName _name;
        Label _detail;
        RichText _onGrid;

        // How far into the row's head the mark's box is drawn, and where the words of a row without one start.
        const float MarkInset = 12f;

        /// <summary>Makes the mark and the picture, then the name with what is quieter about it under it.</summary>
        void CreateView()
        {
            Style = Styles.Pick;

            // The box's control fills the strip at the row's head, down its whole height and out to its edge, so a press
            // anywhere near the box is the box's and never takes the row alone; its padding sets where the box is drawn.
            Children.Add(new CheckBox
            {
                Style = Styles.PickBox,
                Placement = FixedSize.Width(40f),
                Align = Align.Tall,
                Base = { Padding = new Thickness(MarkInset, 0f, 8f, 0f) },
                IsVisible = false
            }.As(out _mark));

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Placement = Flex.Grow,
                Children =
                {
                    // Reserved for the whole column or for none of it, so rows never jog sideways with which kinds are listed.
                    new Image { Style = Styles.Picture, Placement = FixedSize.Size(56f, 56f), IsVisible = false }.As(out _picture),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Within },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new MemberName { Style = Styles.PickName, Wraps = true }.As(out _name),
                            new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _detail),
                            new RichText { Style = Styles.ColumnNoteOnGrid, Align = Align.Left, IsVisible = false }.As(out _onGrid)
                        }
                    }
                }
            }.As(out _content));
        }
    }
}
