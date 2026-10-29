using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class StateRow
    {
        DragHandle _grip;
        NamePlate _name;
        Button _holds;
        Label _startsLead;
        Button _start;
        Label _nowLead;
        Label _now;
        Glyph _remove;

        /// <summary>Makes the grip, the name, what it holds, the value it starts at, what it holds now, and the bin.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;

            Children.Add(new DragHandle { Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            // One sentence: the state's name and what it holds, the value it starts as, and what it holds now.
            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Placement = Flex.Grow,
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Within },
                        Children =
                        {
                            new Glyph { Style = Styles.Waits, Symbol = Symbols.State, Align = Align.Center },
                            new NamePlate(false).As(out _name)
                        }
                    },
                    new Button { Style = Styles.SlotName }.As(out _holds),
                    new Label { Style = Styles.Aside }.As(out _startsLead),
                    new Button { Style = Styles.SlotName }.As(out _start),
                    new Label { Style = Styles.Aside }.As(out _nowLead),
                    new Label { Style = Styles.Phrase }.As(out _now)
                }
            });
            Children.Add(new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, Placement = FixedSize.Size(32f, 32f) }.As(out _remove));
        }
    }
}
