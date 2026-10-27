using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    partial class CategoryRow
    {
        Grip _grip;
        NamePlate _name;
        Glyph _remove;

        /// <summary>Makes the grip, the name that is renamed in place, and the bin.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Base.Padding = new Thickness(20f, 16f, 20f, 0f);

            Children.Add(new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            Children.Add(new HStack
            {
                Placement = Flex.Grow,
                Children = { new NamePlate(false) { Words = Styles.CategoryName }.As(out _name) }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(24f, 24f) }.As(out _remove));
        }
    }
}
