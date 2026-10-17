using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A color on one line: a swatch of it and its code, the picker hanging open under it when pressed.</summary>
    /// <remarks>
    /// One line, so a form asking for several colors beside other values reads as one form, each color under its own
    /// label. The picker is a <see cref="ColorPicker" />, made once with the field and opened over the screen. A color left
    /// empty shows its swatch's edge alone.
    /// </remarks>
    public class ColorField : HStack, IField
    {
        readonly ColorPicker _picker;
        readonly Swatch _swatch;
        readonly Label _code;
        readonly Popup _popup;

        /// <summary>A field holding no color, its picker offering the player's <paramref name="colors" />, Enter in it carrying out <paramref name="submit" />, its notes in <paramref name="texts" />.</summary>
        public ColorField(IList<Hsv> colors, ICommand submit, Texts texts)
        {
            Spacing = 8f;
            Style = Styles.ListLine;
            Focusable = true;

            _picker = new ColorPicker(colors, submit, texts) { Placement = FixedSize.Wide(420f) };
            _picker.Edited += Changed;

            Children.Add(new Swatch { Edge = Palette.Line, Placement = FixedSize.Size(28f, 20f) }.As(out _swatch));
            Children.Add(new Label { Style = Styles.ListWord }.As(out _code));
            Children.Add(ListField.Fold());

            // Hung as every menu is; in a box of its own so it is as wide as the picker and no wider.
            _popup = new Popup { Style = Styles.Menu, Anchor = this, Gap = 4f, Content = new HStack { Children = { _picker } } };
            Command = new Relay(() => _popup.Toggle());
            Shown();
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _picker.Text; }
            set
            {
                _picker.Text = value;
                Shown();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set
            {
                Style = value ? Styles.ListLineWrong : Styles.ListLine;
                _picker.IsWrong = value;
            }
        }

        /// <inheritdoc />
        public Element Keyboard => this;

        /// <summary>The picker hung open under it, made with it.</summary>
        public ColorPicker Picker => _picker;

        /// <summary>Whether the picker is hanging open.</summary>
        public bool IsOpen => _popup.IsOpen;

        void Changed()
        {
            Shown();

            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>Shows the color it holds, its swatch filled and its code beside it, or the swatch's edge alone where none is.</summary>
        void Shown()
        {
            _code.Text = _picker.Text;
            _code.IsVisible = _picker.Text.Length > 0;

            Color color;
            byte alpha;
            _swatch.Background = ColorChannels.Colour(_picker.Text, out color, out alpha) ? color : Palette.None;
        }
    }
}
