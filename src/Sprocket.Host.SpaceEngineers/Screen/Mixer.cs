using System.Collections.Generic;
using System.Globalization;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// Where a colour is chosen: the player's own colours, the three that make any other, and what
    /// it is written as.
    /// </summary>
    /// <remarks>
    /// The three are hue, saturation and value because that is what the game uses, down to the
    /// letters beside them: its colour picker labels them H, S and V and puts a Hex box under them.
    /// Only hue has a track worth drawing, and only hue gets one, which is also the game's answer.
    /// <para>
    /// The box is this control's own, and the question hands the caret to it in place of the
    /// field's. Typing, the caret and what is finally sent then all land where somebody is looking,
    /// through the keyboard that was already there rather than through a second route to it.
    /// </para>
    /// </remarks>
    public sealed class Mixer
    {
        /// <summary>How big one of the player's colours is drawn.</summary>
        /// <remarks>
        /// Fourteen of these and the room between them come to just inside the form's own width. They
        /// are laid out by something that starts a new line when one runs out, so a narrower form
        /// wraps them rather than running them off the edge.
        /// </remarks>
        const float SwatchWide = 31f;

        const float SwatchTall = 26f;

        /// <summary>How much room the letter beside a slider takes, so the three tracks line up.</summary>
        const float LetterWide = 18f;

        /// <summary>
        /// How big the colour being chosen is drawn.
        /// </summary>
        /// <remarks>
        /// As tall as the three sliders it stands beside, which is what makes it the thing on the
        /// dialog somebody looks at. It was a strip the height of one slider first, and a strip is
        /// what a control looks like rather than what an answer looks like.
        /// </remarks>
        const float ChosenWide = 96f;

        /// <summary>
        /// How much room the written colour takes.
        /// </summary>
        /// <remarks>
        /// Enough for the eight digits and the hash, and no more. A box given the width of the form
        /// invites a sentence, and what goes in here is nine characters with one shape.
        /// </remarks>
        const float WrittenWide = 148f;

        readonly Stack _root = Widgets.Column(Space.Item);
        readonly Wrap _palette = new Wrap();
        readonly List<Swatch> _swatches = new List<Swatch>();
        readonly List<Hsv> _colours = new List<Hsv>();

        readonly Slider _hue = new Slider();
        readonly Slider _saturation = new Slider();
        readonly Slider _value = new Slider();

        readonly Swatch _chosen = new Swatch(ChosenWide, Slider.Thick * 3f + Space.Item * 2f);
        readonly TextBox _written = Widgets.Box(Lettering.Small, Palette.Page);

        Slider _held;

        /// <summary>
        /// How solid the colour is, kept as it arrived and never invented.
        /// </summary>
        /// <remarks>
        /// Nothing in the game's terminal surface means anything by it, so there is no control for
        /// it. A colour that arrives with one keeps it all the way through, which is the whole reason
        /// it is carried rather than assumed opaque at every turn.
        /// </remarks>
        byte _alpha = 255;

        Hsv _colour;

        /// <summary>
        /// Whether a colour has actually been chosen, as against the one the grips happen to rest on.
        /// </summary>
        /// <remarks>
        /// A step somebody has not finished writing has no colour, and the grips still have to sit
        /// somewhere - so they sit on white, and the swatch shows nothing rather than showing white.
        /// A filled swatch over an empty field is an answer nobody gave.
        /// </remarks>
        bool _chose;

        public Mixer()
        {
            _palette.ItemSpacing = Space.Row;
            _palette.LineSpacing = Space.Row;

            // The one track that says what moving it does. See Symbols.Hue, which is the game's own.
            _hue.Track = Symbols.Hue;
            _hue.TrackTint = Color.White;

            _hue.HighlightEdge = Palette.Ink;
            _saturation.HighlightEdge = Palette.Ink;
            _value.HighlightEdge = Palette.Ink;

            _chosen.Edge = Palette.Line;

            _root.Children.Add(_palette);
            _root.Children.Add(Mixed());
            _root.Children.Add(Spelled());

            // Somewhere for the grips to rest until a colour is chosen. Written straight to the
            // field rather than through Colour, which would count as choosing it.
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

        /// <summary>
        /// The colour as it stands, which every part of this shows at once.
        /// </summary>
        /// <remarks>
        /// Setting it moves the grips, the swatch and the text together, because they are one answer
        /// shown four ways and any of them disagreeing with the others is a bug somebody has to think
        /// about.
        /// </remarks>
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

                // The edge rather than the fill, which is the colour itself and the whole point of
                // the control. HighlightEdge exists for exactly this: things that are mostly outline,
                // or whose middle is spoken for.
                _swatches[i].HighlightEdge = Palette.Ink;
                _palette.Children.Add(_swatches[i]);
            }

            _palette.Collapsed = colours.Count == 0;
        }

        /// <summary>
        /// Takes the colour from whichever of the player's colours was pressed, if one was.
        /// </summary>
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

        /// <summary>
        /// Takes hold of whichever slider was pressed, and moves it there at once.
        /// </summary>
        /// <remarks>
        /// Moved on the press rather than only on the drag, so pressing a point on a track is a way of
        /// setting it. A grip that had to be found and then dragged would be the only control here
        /// that does not answer the first press.
        /// </remarks>
        public bool Grab(Vector2 point)
        {
            _held = Under(point);
            if (_held == null)
                return false;

            Carry(point);
            return true;
        }

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

        public void Drop()
        {
            _held = null;
        }

        /// <summary>
        /// Lights whichever part of this the pointer is resting on.
        /// </summary>
        /// <remarks>
        /// The same bargain the other controls on a form keep: the screen lights what a press would
        /// reach by finding it the way a press does, and anything that takes its own presses has to
        /// answer the pointer itself.
        /// </remarks>
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

        /// <summary>
        /// Brings the rest of the control into line with what has been typed into the box.
        /// </summary>
        /// <remarks>
        /// Read every tick while the question is up, because the text is the one part somebody can
        /// change without this control hearing about it. Anything it cannot read is left alone rather
        /// than corrected, so a half-typed colour does not fight the person typing it.
        /// </remarks>
        public void Reread()
        {
            if (_written.Text == Spelling)
                return;

            byte a;
            Color said;
            if (!Channels.Colour(_written.Text, out said, out a))
            {
                // An empty field is a colour nobody has chosen, and the swatch says so. Anything else
                // is half a colour somebody is still typing, and is left alone rather than corrected.
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

        /// <summary>One slider with the letter the game calls it by in front of it.</summary>
        static Bar Tracked(string letter, Slider track)
        {
            var line = new Bar();
            line.Spacing = Space.Row;

            var held = new Sized(LetterWide);
            held.Content = Widgets.Label(letter, Palette.Muted, Lettering.Small);

            line.Left = held;
            line.Fill = track;
            return line;
        }

        /// <summary>The three sliders, with the colour they make standing beside them.</summary>
        /// <remarks>
        /// Beside rather than below, and as tall as all three, so the answer is the largest thing on
        /// the dialog and the controls that reach it are the smaller ones. The sliders give up the
        /// width it takes, which they have to spare.
        /// </remarks>
        Bar Mixed()
        {
            var tracks = Widgets.Column(Space.Item);
            tracks.Children.Add(Tracked("H", _hue));
            tracks.Children.Add(Tracked("S", _saturation));
            tracks.Children.Add(Tracked("V", _value));

            var line = new Bar();
            line.Spacing = Space.Item;
            line.Fill = tracks;
            line.Right = _chosen;
            return line;
        }

        /// <summary>What the colour is written as, held to the width of a written colour.</summary>
        Sized Spelled()
        {
            var held = new Sized(WrittenWide);
            held.Content = _written;
            return held;
        }

        static string Hex(byte part)
        {
            return part.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
