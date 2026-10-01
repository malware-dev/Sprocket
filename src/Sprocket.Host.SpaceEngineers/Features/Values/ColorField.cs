using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A color, given from the player's own colors, by hue, saturation and value, or by writing it.</summary>
    /// <remarks>
    /// Laid out as the game's own color picker is: sliders lettered H, S and V, only hue with a track drawn, and the written
    /// color under them. Every way of giving it moves the others with it. The elements it is drawn with are declared in
    /// <c>ColorField.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class ColorField : VStack, IField
    {
        Hsv _color = Hsv.Of(0f, 0f, 1f);
        byte _alpha = 255;
        bool _chose;

        /// <summary>A field resting on white with no color given, offering the player's <paramref name="colors" />, whose Enter carries out <paramref name="submit" />, its notes in <paramref name="texts" />.</summary>
        public ColorField(IList<Hsv> colors, ICommand submit, Texts texts)
        {
            CreateView();
            _written.Submit = submit;

            _hue.Tip = Tip.For(null, texts.ColorHue);
            _saturation.Tip = Tip.For(null, texts.ColorSaturation);
            _value.Tip = Tip.For(null, texts.ColorBrightness);
            _written.Tip = Tip.For(null, texts.ColorCodeAbout);

            for (var i = 0; i < colors.Count; i++)
            {
                var color = colors[i];
                _palette.Children.Add(new Swatch
                {
                    Background = color.Colour,
                    Edge = Palette.Chrome,
                    HighlightEdge = Palette.Ink,
                    Placement = Fixed.Size(32f, 28f),
                    Command = new Relay(delegate { Give(color); })
                });
            }

            _palette.IsVisible = colors.Count != 0;

            _hue.Moved += delegate { Give(_color.Hued(_hue.Fraction)); };
            _saturation.Moved += delegate { Give(_color.Saturated(_saturation.Fraction)); };
            _value.Moved += delegate { Give(_color.Valued(_value.Fraction)); };
            _written.Edited += delegate
            {
                Reread();
                Raise();
            };

            Shown();
        }

        /// <inheritdoc />
        public event Action Edited;

        /// <inheritdoc />
        public string Text
        {
            get { return _written.Text; }
            set
            {
                _written.Text = value ?? "";
                Reread();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _written.Style = value ? Styles.ValueWrong : Styles.Value; }
        }

        /// <inheritdoc />
        public Element Keyboard => _written;

        /// <summary>The color as the language writes one.</summary>
        string Spelling
        {
            get
            {
                var rgb = _color.Colour;
                var said = "#" + Hex(rgb.R) + Hex(rgb.G) + Hex(rgb.B);
                return _alpha == 255 ? said : said + Hex(_alpha);
            }
        }

        /// <summary>Takes <paramref name="color" /> as the one given, moving the sliders, the swatch and the written color with it.</summary>
        void Give(Hsv color)
        {
            _color = color;
            _chose = true;
            Shown();
            _written.Text = Spelling;
            Raise();
        }

        /// <summary>Brings the rest into line with what is written, leaving a color that cannot be read yet alone.</summary>
        void Reread()
        {
            if (_written.Text == Spelling && _chose)
                return;

            byte alpha;
            Color said;
            if (!Channels.Colour(_written.Text, out said, out alpha))
            {
                if (string.IsNullOrEmpty(_written.Text))
                {
                    _chose = false;
                    Shown();
                }

                return;
            }

            _alpha = alpha;
            _color = Hsv.Of(said);
            _chose = true;
            Shown();
        }

        void Shown()
        {
            _hue.Fraction = _color.Hue;
            _saturation.Fraction = _color.Saturation;
            _value.Fraction = _color.Value;
            _chosen.Background = _chose ? _color.Colour : Palette.Page;
        }

        void Raise()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }

        static string Hex(byte part)
        {
            return part.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
