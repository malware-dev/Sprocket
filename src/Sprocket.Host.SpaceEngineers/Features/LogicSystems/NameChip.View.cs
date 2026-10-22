using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class NameChip
    {
        Button _name;
        Glyph _mark;
        Label _words;
        TextBox _box;

        /// <summary>Makes the name with its mark inside its button, and the box it is typed in.</summary>
        void CreateView()
        {
            Children.Add(new Button
            {
                Style = Styles.SlotNameStill,
                Content = new HStack
                {
                    Spacing = 8f,
                    Children =
                    {
                        new Glyph { Style = Styles.Waits, Align = Align.Center, IsVisible = false }.As(out _mark),
                        new Label { Style = Styles.Name }.As(out _words)
                    }
                }
            }.As(out _name));
            Children.Add(new TextBox { Style = Styles.Field, IsVisible = false, Longest = NameLongest, Placement = FixedSize.Wide(240f) }.As(out _box));
        }
    }
}
