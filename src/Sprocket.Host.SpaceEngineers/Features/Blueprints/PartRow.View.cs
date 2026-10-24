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
        Grip _grip;
        Glyph _mark;
        Button _name;
        Label _words;
        RichText _about;
        SlotChip _blocks;
        SlotChip _signals;
        Label _problem;
        Glyph _remove;
        Label _marks;

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
                            new Grip { Color = Palette.Faint, HeldColor = Palette.Ink, IsVisible = false, Align = Align.Center, Placement = FixedSize.Size(24f, 24f) }.As(out _grip),
                            new Button
                            {
                                Style = Styles.SlotNameStill,
                                Content = new HStack
                                {
                                    Spacing = 8f,
                                    Children =
                                    {
                                        new Glyph { Style = Styles.Waits, Align = Align.Center }.As(out _mark),
                                        new Label { Style = Styles.Name }.As(out _words)
                                    }
                                }
                            }.As(out _name)
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
                            new Label { Style = Styles.ProblemLine, IsVisible = false }.As(out _problem),
                            new Glyph { Symbol = Symbols.Delete, Style = Styles.Bin, IsVisible = false, Align = Align.Center, Placement = FixedSize.Size(32f, 32f) }.As(out _remove)
                        }
                    },
                    new Label { Style = Styles.WarningLine, Wraps = true, IsVisible = false }.As(out _marks)
                }
            });
        }
    }
}
