using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    partial class CategoryRow
    {
        DragHandle _grip;
        NamePlate _name;
        Glyph _remove;

        /// <summary>Makes the grip, the name that is renamed in place, and the bin.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;
            // A category starts a group among the rows, which stand Items apart, so it holds the rest of a Sections gap over it.
            Base.Padding = new Thickness(0f, Gaps.Sections - Gaps.Items, 0f, 0f);

            Children.Add(new DragHandle { Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            Children.Add(new HStack
            {
                Placement = Flex.Grow,
                Children = { new NamePlate(false) { Words = Styles.CategoryName, WordsBox = Styles.CategoryBox }.As(out _name) }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(24f, 24f) }.As(out _remove));
        }
    }
}
