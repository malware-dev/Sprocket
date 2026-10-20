using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    partial class PartRow
    {
        VStack _column;
        Glyph _mark;
        Button _name;
        RichText _about;
        SlotChip _blocks;
        SlotChip _signals;
        Label _problem;
        Label _marks;
        Label _aside;

        /// <summary>Makes the part's name and what it says about itself, with the blocks beside them and what is still to do under those.</summary>
        void CreateView()
        {
            Spacing = 20f;
            Padding = new Thickness(20f, 8f);

            Children.Add(new VStack
            {
                Spacing = 4f,
                Align = Align.Top,
                Placement = FixedSize.Wide(PartList.Widest),
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Glyph { Style = Styles.Waits }.As(out _mark),
                            new Button { Style = Styles.SlotNameStill }.As(out _name)
                        }
                    },
                    new RichText { Style = Styles.SlotDescription, IsVisible = false }.As(out _about)
                }
            }.As(out _column));

            Children.Add(new VStack
            {
                Spacing = 4f,
                Align = Align.Top,
                Children =
                {
                    new HStack
                    {
                        Spacing = 20f,
                        Children =
                        {
                            new SlotChip(false) { Spacing = 4f }.As(out _blocks),
                            new SlotChip { IsVisible = false, Align = Align.Top }.As(out _signals),
                            new Label { Style = Styles.Problem, IsVisible = false }.As(out _problem)
                        }
                    },
                    new Label { Style = Styles.Caution, Wraps = true, IsVisible = false }.As(out _marks),
                    new Label { Style = Styles.Aside, Wraps = true, IsVisible = false }.As(out _aside)
                }
            });
        }
    }
}
