using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Images;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    public partial class SettingChip
    {
        Label _name;
        Label _default;

        /// <summary>Makes the setting's mark, its name and its default beside it, framed as the value it stands for.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Within;
            Base.Border = Palette.Link;
            Base.Padding = new Thickness(8f, 4f, 8f, 4f);
            Children.Add(new Glyph { Style = Styles.Waits, Base = { Tint = Palette.Link }, Symbol = Symbols.Setting });
            Children.Add(new Label().As(out _name));
            Children.Add(new Label { Style = Styles.ValueLabel, IsVisible = false }.As(out _default));
        }
    }
}
