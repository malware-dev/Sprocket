using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    partial class PartRow
    {
        Label _name;
        Glyph _pencil;
        Label _about;
        Slot _blocks;

        /// <summary>Makes the part's name with its pencil and what it says about itself, and the blocks beside them.</summary>
        void CreateView()
        {
            Spacing = 20f;
            Padding = new Thickness(20f, 8f);

            Children.Add(new VStack
            {
                Spacing = 4f,
                Placement = Fixed.Wide(360f),
                Children =
                {
                    new HStack
                    {
                        Spacing = 8f,
                        Children =
                        {
                            new Label { Style = Styles.Name }.As(out _name),
                            new Glyph { Symbol = Symbols.Pencil, Style = Styles.Pen, Placement = Fixed.Size(24f, 24f) }.As(out _pencil)
                        }
                    },
                    new Label { Style = Styles.Detail, IsVisible = false }.As(out _about)
                }
            });

            Children.Add(new Slot().As(out _blocks));
        }
    }
}
