using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Look
{
    /// <summary>Styles holding the values of views not yet converted to the size scale, for the controls they share with converted ones.</summary>
    public static class Unconverted
    {
        /// <summary>A switch that governs one thing.</summary>
        public static readonly SwitchStyle Switch = new SwitchStyle
        {
            Base =
            {
                Font = MyFontEnum.White,
                FontSize = Lettering.Small,
                Color = Palette.Ink,
                QuietColor = Palette.Faint,
                LitColor = Palette.SwitchLit,
                UnlitColor = Palette.SwitchDark,
                WordPadding = new Thickness(Space.ChipX, Space.ChipY),
                Edge = Palette.Dim
            },
            Hot = { Edge = Palette.Muted }
        }.Frozen();

        /// <summary>A switch that governs more than one thing, which draws its edge brighter.</summary>
        public static readonly SwitchStyle Governing = new SwitchStyle(Switch)
        {
            Base = { Edge = Palette.Faint }
        }.Frozen();

        /// <summary>A switch standing among the boxes a value is given in, as tall as one.</summary>
        public static readonly SwitchStyle Field = new SwitchStyle(Switch)
        {
            Base = { WordPadding = new Thickness(Space.ChipX, Space.FieldY) }
        }.Frozen();
    }
}
