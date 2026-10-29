using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class TypeRow
    {
        DragHandle _grip;
        NamePlate _name;
        Label _lead;
        Flow _choices;
        Button _add;
        Glyph _remove;

        /// <summary>Makes the grip, the name, the words before the choices, the choices with the way to add one after them, and the bin.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;

            Children.Add(new DragHandle { Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            // One line that carries on onto the next where the room runs out: the name, the words, the choices, the way to add one.
            Children.Add(new Flow
            {
                Placement = Flex.Grow,
                Base = { Spacing = Gaps.Parts, LineSpacing = Gaps.Parts },
                Children =
                {
                    new NamePlate(false).As(out _name),
                    new Label { Style = Styles.Aside }.As(out _lead),
                    new Flow { Base = { Spacing = Gaps.Parts, LineSpacing = Gaps.Parts } }.As(out _choices),
                    new Button { Style = Styles.Start }.As(out _add)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _remove));
        }
    }
}
