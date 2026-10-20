using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    partial class CategoryRow
    {
        Grip _grip;
        Button _name;
        TextBox _box;
        Glyph _remove;

        /// <summary>Makes the grip, the name with the box it is typed in, and the bin.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(20f, 24f, 20f, 8f);

            Children.Add(new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            Children.Add(new HStack
            {
                Placement = Flex.Grow,
                Children =
                {
                    new Button { Style = Styles.SlotNameStill }.As(out _name),
                    new TextBox { Style = Styles.Field, IsVisible = false, Longest = NameLongest, Placement = FixedSize.Wide(PartList.Widest) }.As(out _box)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _remove));
        }
    }
}
