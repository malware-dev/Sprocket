using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    public partial class NamePlate
    {
        Label _name;
        TextBox _box;
        Glyph _pen;

        /// <summary>Makes the words, the box that takes their place, and the pencil, in a heading's size or a row's.</summary>
        void CreateView()
        {
            Style = Styles.Plate;
            Children.Add(new Label { Style = _title ? Styles.TitleName : Styles.PlateName }.As(out _name));
            Children.Add(new TextBox { Style = _title ? Styles.TitleBox : Styles.PlateBox, IsVisible = false }.As(out _box));
            Children.Add(new Glyph
            {
                Symbol = Symbols.Pencil,
                Style = Styles.Pen,
                IsVisible = false,
                Placement = _title ? FixedSize.Size(32f, 32f) : FixedSize.Size(24f, 24f),
                Base = { Padding = _title ? EditorWindow.LevelWithCapitals : new Thickness(0f) }
            }.As(out _pen));

            if (_title)
                return;

            _pen.Align = Align.Center;
            _box.MaxLength = NameLongest;
        }
    }
}
