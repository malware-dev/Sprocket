using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class StateRow
    {
        Grip _grip;
        NameChip _name;
        Button _holds;
        Label _startsLead;
        Button _start;
        Label _nowLead;
        Label _now;
        Glyph _remove;

        /// <summary>Makes the grip, the name, what it holds, the value it starts at, what it holds now, and the bin.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(20f, 4f, 20f, 4f);

            Children.Add(new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            // One sentence: the state's name and what it holds, the value it starts as, and what it holds now.
            Children.Add(new HStack
            {
                Spacing = 12f,
                Placement = Flex.Grow,
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Glyph { Style = Styles.Waits, Symbol = Symbols.State },
                            new NameChip().As(out _name)
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
