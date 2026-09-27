using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    /// <summary>A color, given from the player's own colors, by hue, saturation and value, or by writing it.</summary>
    /// <remarks>
    /// Laid out as the game's own color picker is: sliders lettered H, S and V, only hue with a track drawn, and the written
    /// color under them. Every way of giving it moves the others with it. The elements it is drawn with are declared in
    /// <c>ColourField.View.cs</c>, and nothing else is.
    /// </remarks>
    public partial class ColourField : VStack, IField
    {
        Hsv _colour = Hsv.Of(0f, 0f, 1f);
        byte _alpha = 255;
        bool _chose;

        /// <summary>A field resting on white with no color given, offering the player's <paramref name="colours" />, whose Enter carries out <paramref name="submit" />.</summary>
        public ColourField(IList<Hsv> colours, ICommand submit)
        {
            CreateView();
            _written.Submit = submit;

            for (var i = 0; i < colours.Count; i++)
            {
                var colour = colours[i];
                _palette.Children.Add(new Swatch
                {
                    Background = colour.Colour,
                    Edge = Palette.Chrome,
                    HighlightEdge = Palette.Ink,
                    Placement = Fixed.Size(32f, 28f),
                    Command = new Relay(delegate { Give(colour); })
                });
            }

            _palette.IsVisible = colours.Count != 0;

            _hue.Moved += delegate { Give(_colour.Hued(_hue.Fraction)); };
            _saturation.Moved += delegate { Give(_colour.Saturated(_saturation.Fraction)); };
            _value.Moved += delegate { Give(_colour.Valued(_value.Fraction)); };
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
                var rgb = _colour.Colour;
                var said = "#" + Hex(rgb.R) + Hex(rgb.G) + Hex(rgb.B);
                return _alpha == 255 ? said : said + Hex(_alpha);
            }
        }

        /// <summary>Takes <paramref name="colour" /> as the one given, moving the sliders, the swatch and the written color with it.</summary>
        void Give(Hsv colour)
        {
            _colour = colour;
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
            _colour = Hsv.Of(said);
            _chose = true;
            Shown();
        }

        void Shown()
        {
            _hue.Fraction = _colour.Hue;
            _saturation.Fraction = _colour.Saturation;
            _value.Fraction = _colour.Value;
            _chosen.Background = _chose ? _colour.Colour : Palette.Page;
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
