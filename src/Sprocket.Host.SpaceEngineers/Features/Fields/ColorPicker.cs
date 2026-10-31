using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>
    /// A color, given on a square of saturation and value beside a strip of hues, by typing its hue, saturation and value, its
    /// red, green and blue, or its code, or from the player's paint colors or the colors they chose last.
    /// </summary>
    public partial class ColorPicker : VStack, IField
    {
        /// <summary>How many swatches stand in a row.</summary>
        public const int Across = 8;

        /// <summary>How many colors chosen last it offers: one row of swatches.</summary>
        public const int RecentCount = Across;

        /// <summary>How wide and how tall one swatch is: they are square.</summary>
        const float SwatchSize = 36f;

        /// <summary>How wide the box one channel is typed in is: three digits.</summary>
        const float ChannelWide = 56f;

        /// <summary>How wide the color being made is shown: a channel box.</summary>
        const float ShownWide = ChannelWide;

        /// <summary>How tall the color being made is shown: as tall as the tabs and their boxes.</summary>
        const float ShownHigh = 80f;

        /// <summary>How wide the box the color is written in is: a whole code, its alpha included.</summary>
        const float CodeWide = 124f;

        /// <summary>How wide the strip of hues is.</summary>
        const float StripWide = 24f;

        /// <summary>How tall the square of saturation and value, and the strip beside it, are.</summary>
        const float PlaneHigh = 176f;

        /// <summary>The room between two swatches.</summary>
        const float Gap = 4f;

        /// <summary>How wide the picker is: a row of swatches exactly, so the rows and everything under them share one edge.</summary>
        public const float Wide = Across * SwatchSize + (Across - 1) * Gap;

        readonly List<Swatch> _recentSwatches = new List<Swatch>();

        readonly ColorOffer _offer;
        readonly List<Color> _recentColors = new List<Color>();
        Hsv _color = Hsv.Of(0f, 0f, 1f);
        byte _alpha = 255;
        bool _chose;

        /// <summary>A field resting on white with no color given, offering what <paramref name="offer" /> holds, whose Enter carries out <paramref name="submit" />, its notes in <paramref name="texts" />.</summary>
        public ColorPicker(ColorOffer offer, ICommand submit, Texts texts)
        {
            _offer = offer;
            CreateView();
            _written.Submit = submit;

            _plane.Tip = CodexTip.For(null, texts.Codex.ColorPlaneAbout);
            _strip.Tip = CodexTip.For(null, texts.Codex.ColorHue);
            _hueBox.Tip = CodexTip.For(null, texts.Codex.ColorHue);
            _saturationBox.Tip = CodexTip.For(null, texts.Codex.ColorSaturation);
            _valueBox.Tip = CodexTip.For(null, texts.Codex.ColorBrightness);
            foreach (var box in Boxes())
                box.Submit = submit;
            _written.Tip = CodexTip.For(null, texts.Codex.ColorCodeAbout);

            var paint = offer.Paint;
            for (var i = 0; i < paint.Count; i++)
            {
                var color = paint[i];
                _palette.Cells[i / Across, i % Across] = Swatch(color.Colour, () => Give(color));
            }

            _palette.IsVisible = paint.Count != 0;

            for (var i = 0; i < RecentCount; i++)
            {
                var at = i;
                var swatch = Swatch(Palette.None, () => Give(Hsv.Of(_recentColors[at])));
                _recentSwatches.Add(swatch);
                _recent.Cells[0, i] = swatch;
            }

            Recall();

            _modes.SelectedIndex = offer.Tab;
            _modes.SelectionChanged += at => _offer.Tab = at;

            _plane.ColorChanged += () => Give(Hsv.Of(_color.Hue, _plane.Saturation, _plane.Value));
            _strip.ValueChanged += () => Give(_color.Hued(_strip.Hue));

            _hueBox.Edited += () => Typed(_hueBox, 360f, part => Give(_color.Hued(part)));
            _saturationBox.Edited += () => Typed(_saturationBox, 100f, part => Give(_color.Saturated(part)));
            _valueBox.Edited += () => Typed(_valueBox, 100f, part => Give(_color.Valued(part)));
            _redBox.Edited += () => Typed(_redBox, 255f, part => GiveRgb(new Color(Byte(part), _color.Colour.G, _color.Colour.B)));
            _greenBox.Edited += () => Typed(_greenBox, 255f, part => GiveRgb(new Color(_color.Colour.R, Byte(part), _color.Colour.B)));
            _blueBox.Edited += () => Typed(_blueBox, 255f, part => GiveRgb(new Color(_color.Colour.R, _color.Colour.G, Byte(part))));
            _written.Edited += () =>
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
            get { return Written; }
            set
            {
                _written.Text = value ?? "";
                Reread();
            }
        }

        /// <inheritdoc />
        public bool IsWrong
        {
            set { _written.Set(LookStates.Wrong, value); }
        }

        /// <inheritdoc />
        public Element Keyboard => _written;

        /// <summary>What is written, with the hash put in front where it was left off and the rest reads as a color.</summary>
        string Written
        {
            get
            {
                var text = _written.Text.Trim();
                Color said;
                byte alpha;
                return text.Length > 0 && text[0] != '#' && ColorChannels.Colour($"#{text}", out said, out alpha) ? $"#{text}" : text;
            }
        }

        /// <summary>The color as the language writes one.</summary>
        string Spelling
        {
            get
            {
                var rgb = _color.Colour;
                var said = $"#{Hex(rgb.R)}{Hex(rgb.G)}{Hex(rgb.B)}";
                return _alpha == 255 ? said : said + Hex(_alpha);
            }
        }

        /// <summary>Shows the colors chosen last as they stand now, the newest first, and the places none has filled yet as an edge alone, which does nothing when pressed.</summary>
        /// <remarks>Called as the picker opens, so a color chosen in another field since shows.</remarks>
        public void Recall()
        {
            _offer.Recent(_recentColors, RecentCount);
            for (var i = 0; i < RecentCount; i++)
            {
                var filled = i < _recentColors.Count;
                _recentSwatches[i].Color = filled ? _recentColors[i] : Palette.None;
                _recentSwatches[i].IsEnabled = filled;
            }
        }

        /// <summary>Puts the color given first among the colors chosen last, where one is.</summary>
        public void Remember()
        {
            if (_chose)
                _offer.Chose(Spelling, RecentCount);
        }

        /// <summary>A swatch of <paramref name="color" /> that does <paramref name="pressed" /> when pressed.</summary>
        static Swatch Swatch(Color color, Action pressed)
        {
            return new Swatch
            {
                Style = Styles.ColorChoice,
                Color = color,
                Placement = FixedSize.Size(SwatchSize, SwatchSize),
                Command = new RelayCommand(pressed)
            };
        }

        /// <summary>The boxes each channel is typed in.</summary>
        IEnumerable<NumberBox> Boxes()
        {
            yield return _hueBox;
            yield return _saturationBox;
            yield return _valueBox;
            yield return _redBox;
            yield return _greenBox;
            yield return _blueBox;
        }

        /// <summary>Gives the channel typed in <paramref name="box" />, out of <paramref name="most" />, to <paramref name="give" /> as a part from nought to one; a box that holds no number yet gives nothing.</summary>
        static void Typed(NumberBox box, float most, Action<float> give)
        {
            double said;
            if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out said))
                return;

            give((float)Math.Max(0d, Math.Min(most, said)) / most);
        }

        /// <summary><paramref name="part" />, from nought to one, as one channel of red, green and blue.</summary>
        static byte Byte(float part)
        {
            return (byte)Math.Round(part * 255f);
        }

        /// <summary>Takes <paramref name="color" />, given by red, green and blue, as the one given.</summary>
        /// <remarks>A gray has no hue of its own, so the hue it had is kept, and the hue's mark does not jump back to red.</remarks>
        void GiveRgb(Color color)
        {
            var given = Hsv.Of(color);
            Give(given.Saturation <= 0f ? Hsv.Of(_color.Hue, given.Saturation, given.Value) : given);
        }

        /// <summary>Takes <paramref name="color" /> as the one given, moving the marks, the boxes, the swatch and the written color with it.</summary>
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
            var written = Written;
            if (written == Spelling && _chose)
                return;

            byte alpha;
            Color said;
            if (!ColorChannels.Colour(written, out said, out alpha))
            {
                if (string.IsNullOrEmpty(written))
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
            _plane.Hue = _color.Hue;
            _plane.Saturation = _color.Saturation;
            _plane.Value = _color.Value;
            _strip.Hue = _color.Hue;
            var rgb = _color.Colour;

            // A box being typed in keeps what is typed, as the rest follow it.
            Say(_hueBox, _color.Hue * 360f);
            Say(_saturationBox, _color.Saturation * 100f);
            Say(_valueBox, _color.Value * 100f);
            Say(_redBox, rgb.R);
            Say(_greenBox, rgb.G);
            Say(_blueBox, rgb.B);
            _new.Color = _chose ? _color.Colour : Palette.Page;
        }

        /// <summary>Writes <paramref name="number" />, rounded, in <paramref name="box" />, unless somebody is typing in it.</summary>
        static void Say(NumberBox box, float number)
        {
            if (!box.IsFocused)
                box.Text = Math.Round(number).ToString(CultureInfo.InvariantCulture);
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
