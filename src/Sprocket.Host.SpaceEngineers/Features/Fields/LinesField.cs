using System;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>Text written over several lines, in a box as wide as the form, where Enter starts a new line.</summary>
    /// <remarks>Finished by the form's own way of finishing, since Enter is the box's.</remarks>
    public class LinesField : VStack, IField
    {
        const float High = 120f;

        readonly EditBox _box;

        /// <summary>An empty box a few lines tall.</summary>
        public LinesField()
        {
            Children.Add(new EditBox
            {
                Font = MyFontEnum.White,
                FontSize = 20f,
                Color = Palette.Ink,
                CaretColor = Palette.Ink,
                SelectionColor = Palette.Line,
                HintColor = Palette.Faint,
                Background = Palette.Page,
                Edge = Palette.Line,
                Padding = new Thickness(12f, 8f),
                BarWidth = Sizes.Bar,
                Thumb = Palette.Thumb,
                HeldThumb = Palette.ThumbHeld,
                LeastThumb = 24f,
                Focusable = true,
                Placement = Fixed.High(High)
            }.As(out _box));

            _box.Edited += delegate
            {
                var edited = Edited;
                if (edited != null)
                    edited();
            };
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _box.Text; }
            set { _box.Text = value ?? ""; }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set
            {
                _box.Background = value ? Palette.WrongFill : Palette.Page;
                _box.Edge = value ? Palette.Problem : Palette.Line;
            }
        }

        /// <inheritdoc />
        public Element Keyboard => _box;
    }
}
