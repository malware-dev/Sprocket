using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class TypeRow
    {
        Grip _grip;
        NameChip _name;
        Label _lead;
        Flow _choices;
        Button _add;
        Glyph _remove;

        /// <summary>Makes the grip, the name, the words before the choices, the choices with the way to add one after them, and the bin.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(20f, 4f, 20f, 4f);

            Children.Add(new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            // One line that carries on onto the next where the room runs out: the name, the words, the choices, the way to add one.
            Children.Add(new Flow
            {
                Placement = Flex.Grow,
                Spacing = 12f,
                LineSpacing = 8f,
                Children =
                {
                    new NameChip().As(out _name),
                    new Label { Style = Styles.Aside }.As(out _lead),
                    new Flow { Spacing = 8f, LineSpacing = 8f }.As(out _choices),
                    new Button { Style = Styles.Start }.As(out _add)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _remove));
        }
    }
}
