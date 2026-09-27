using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    partial class PickRow
    {
        Mark _mark;
        Image _picture;
        Glyph _waits;
        Label _name;
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
                    // In the line rather than in a column of its own: only a handful of rows carry it, and a column would
                    // indent every other name for nothing.
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Glyph { Symbol = Symbols.Finish, Style = Styles.Waits, IsVisible = false }.As(out _waits),
                            new Label { Style = Styles.PickName, Wraps = true, Placement = Flex.Grow }.As(out _name)
                        }
                    },
                    new Label { Style = Styles.ColumnNote, IsVisible = false }.As(out _detail)
                }
            });
        }
    }
}
