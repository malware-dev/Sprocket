using System.Collections.Generic;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>Where a colour is chosen: the player's own colours, hue, saturation and value, and what it is written as.</summary>
    /// <remarks>
    ///     Laid out as the game's own colour picker is: sliders lettered H, S and V, only hue with a track drawn,
    ///     and the written colour under them. The question puts its caret in this control's box.
    /// </remarks>
    public sealed class Mixer
    {
        /// <summary>How big one of the player's colours is drawn.</summary>
        /// <remarks>Fourteen of these and the room between them fit just inside the form's width.</remarks>
        const float SwatchWide = 31f;

        const float SwatchTall = 26f;

        /// <summary>How much room the letter beside a slider takes.</summary>
        const float LetterWide = 18f;

        /// <summary>How wide the colour being chosen is drawn.</summary>
        const float ChosenWide = 96f;

        /// <summary>How much room the written colour takes: the hash and eight digits.</summary>
        const float WrittenWide = 148f;

        readonly VStack _root;
        readonly Wrap _palette;
        readonly List<Swatch> _swatches = new List<Swatch>();
        readonly List<Hsv> _colours = new List<Hsv>();

        readonly Slider _hue;
        readonly Slider _saturation;
        readonly Slider _value;

        readonly Swatch _chosen;
        readonly TextBox _written;

        Slider _held;

        /// <summary>How solid the colour is, kept as it arrived.</summary>
        /// <remarks>The game's terminal means nothing by it, so nothing here sets it.</remarks>
        byte _alpha = 255;

        Hsv _colour;

        /// <summary>Whether a colour has been chosen, as against the one the grips rest on.</summary>
        bool _chose;

        /// <summary>A mixer with its grips resting on white and no colour chosen.</summary>
        public Mixer()
        {
            _root = new VStack
            {
                Spacing = Space.Item,
                Children =
                {
                    new Wrap { ItemSpacing = Space.Row, LineSpacing = Space.Row }.As(out _palette),
                    new HStack
                    {
                        Spacing = Space.Item,
                        Children =
                        {
                            new Grid
                            {
                                Placement = Flex.Grow,
                                ColumnSpacing = Space.Row,
                                RowSpacing = Space.Item,
                                Columns = { LetterWide, Length.Flex(1) },
                                Cells =
                                {
                                    [0, 0] = Widgets.Label("H", Palette.Muted, Lettering.Small),
                                    [0, 1] = new Slider
                                    {
                                        Track = Symbols.Hue,
                                        TrackTint = Color.White,
                                        HighlightEdge = Palette.Ink
                                    }.As(out _hue),

                                    [1, 0] = Widgets.Label("S", Palette.Muted, Lettering.Small),
                                    [1, 1] = new Slider { HighlightEdge = Palette.Ink }.As(out _saturation),
                                    [2, 0] = Widgets.Label("V", Palette.Muted, Lettering.Small),
                                    [2, 1] = new Slider { HighlightEdge = Palette.Ink }.As(out _value)
                                }
                            },
                            new Swatch(ChosenWide, Slider.Thick * 3f + Space.Item * 2f) { Edge = Palette.Line }
                                .As(out _chosen)
                        }
                    },
                    new Sized(WrittenWide) { Content = Widgets.Box(Lettering.Small, Palette.Page).As(out _written) }
                }
            };

            // Straight to the field: setting Colour would count as choosing one.
            _colour = Hsv.Of(0f, 0f, 1f);
            Shown();
        }

        /// <summary>What to put in the form.</summary>
        public Element Root
        {
            get { return _root; }
        }

        /// <summary>The box the colour is written in, which is where the question puts the caret.</summary>
        public TextBox Written
        {
            get { return _written; }
        }

        /// <summary>The colour as it stands; setting it moves the grips, the swatch and the written colour together.</summary>
        public Hsv Colour
        {
            get { return _colour; }
            set
            {
                _colour = value;
                _chose = true;
                Shown();
                _written.Text = Spelling;
            }
        }

        /// <summary>The colour as the language writes one.</summary>
        public string Spelling
        {
            get
            {
                Color rgb = _colour.Colour;
                string said = "#" + Hex(rgb.R) + Hex(rgb.G) + Hex(rgb.B);
                return _alpha == 255 ? said : said + Hex(_alpha);
            }
        }

        /// <summary>Shows the colours this player paints with, and hides the row where there are none.</summary>
        public void Offer(IList<Hsv> colours)
        {
            _colours.Clear();
            _palette.Children.Clear();

            for (var i = 0; i < colours.Count; i++)
            {
                _colours.Add(colours[i]);

                while (_swatches.Count <= i)
                    _swatches.Add(new Swatch(SwatchWide, SwatchTall));

                _swatches[i].Background = colours[i].Colour;
                _swatches[i].Edge = Palette.Chrome;
                _swatches[i].HighlightEdge = Palette.Ink;
                _palette.Children.Add(_swatches[i]);
            }

            _palette.Collapsed = colours.Count == 0;
        }

        /// <summary>Takes the colour from whichever of the player's colours was pressed, if one was.</summary>
        public bool Touch(Vector2 point)
        {
            for (var i = 0; i < _colours.Count && i < _swatches.Count; i++)
            {
                if (!_swatches[i].Holds(point))
                    continue;

                Colour = _colours[i];
                return true;
            }

            return false;
        }

        /// <summary>Takes hold of whichever slider was pressed, and moves it to the press at once.</summary>
        public bool Grab(Vector2 point)
        {
            _held = Under(point);
            if (_held == null)
                return false;

            Carry(point);
            return true;
        }

        /// <summary>Moves the slider being held to <paramref name="point" />.</summary>
        public void Carry(Vector2 point)
        {
            if (_held == null)
                return;

            float at = _held.At(point);

            if (_held == _hue)
                Colour = _colour.Hued(at);
            else if (_held == _saturation)
                Colour = _colour.Saturated(at);
            else
                Colour = _colour.Valued(at);
        }

        /// <summary>Lets go of the slider being held.</summary>
        public void Drop()
        {
            _held = null;
        }

        /// <summary>Lights whichever part of this the pointer is resting on.</summary>
        public void Lit(Vector2 point)
        {
            Element hot = Under(point);

            for (var i = 0; i < _colours.Count && i < _swatches.Count; i++)
            {
                if (hot == null && _swatches[i].Holds(point))
                    hot = _swatches[i];
            }

            if (ReferenceEquals(hot, _hot))
                return;

            if (_hot != null)
                _hot.IsHot = false;

            _hot = hot;

            if (_hot != null)
                _hot.IsHot = true;
        }

        Element _hot;

        /// <summary>Whether a slider is being dragged right now.</summary>
        public bool IsDragging
        {
            get { return _held != null; }
        }

        /// <summary>Brings the rest of the control into line with what has been typed into the box.</summary>
        /// <remarks>Called every frame while the question is up, and leaves a colour it cannot read alone.</remarks>
        public void Reread()
        {
            if (_written.Text == Spelling)
                return;

            byte a;
            Color said;
            if (!Channels.Colour(_written.Text, out said, out a))
            {
                if (string.IsNullOrEmpty(_written.Text))
                {
                    _chose = false;
                    Shown();
                }

                return;
            }

            _alpha = a;
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

        Slider Under(Vector2 point)
        {
            if (_hue.Holds(point))
                return _hue;

            if (_saturation.Holds(point))
                return _saturation;

            return _value.Holds(point) ? _value : null;
        }

        static string Hex(byte part)
        {
            return part.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
