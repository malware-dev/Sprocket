using Phosphor;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>
    /// What Reactor looks like on this target.
    /// <para>
    /// Core hands over intent - heading, muted, slot, problem - and never colours or sizes, so every
    /// value here is this target's own opinion. The <b>roles</b> are the Avalonia host's, one for one, and
    /// deliberately so: two targets that agree on what a slot is are two targets somebody learns once.
    /// The <b>values</b> are not, and should not be. That host is a desktop application and wears a
    /// desktop's dark theme; this one is a screen in a game with a look of its own, and a panel that
    /// ignored it would read as something bolted on.
    /// </para>
    /// <para>
    /// So: the same neutral slate, turned cold and blue. Space Engineers is steel and glass lit blue,
    /// with hard corners, hairlines rather than borders, and colour saved for the few things that mean
    /// something - which is the same restraint the roles below already had.
    /// </para>
    /// <para>
    /// Most of the values below were taken out of the game's own textures rather than guessed at, and
    /// each one says where it came from. Guessing was the first attempt and it was wrong in a way worth
    /// recording: the eye remembers Space Engineers as dark navy, and it is not - its panels are a light,
    /// almost grey-green steel, and its highlight is a pale ice blue rather than a saturated one.
    /// </para>
    /// </summary>
    public static class Palette
    {
        /// <summary>Darker than a panel, so panels read as sitting on it. Derived from <see cref="Panel" />.</summary>
        /// <remarks>
        /// Opaque, along with everything else here. How much of the world shows through a screen is not
        /// this target's business and never was: the game draws the screen's own background and multiplies
        /// it by <c>UIBkOpacity</c>, which is a slider in the player's options. Reactor draws content into
        /// a screen somebody else opened, so a veil applied here would be a second one over the top of
        /// the player's, in a quantity nobody asked for.
        /// </remarks>
        public static readonly Color Page = new Color(20, 27, 32);

        /// <summary>Header and footer. A step darker again, so the screen's edges settle.</summary>
        public static readonly Color Chrome = new Color(27, 36, 42);

        /// <summary>
        /// DarkBlueBackground.png, which is flat and entirely this colour - and also button_default,
        /// textbox_center and rectangle_button_center. <b>Every control in the game starts from this
        /// tone.</b>
        /// </summary>
        /// <remarks>
        /// It becomes one of two things when a pointer arrives, because the game keeps the same four
        /// states twice over. The <b>panel</b> family is DarkBlueBackground and its variants -
        /// <see cref="PanelHot" />, <see cref="Accent" />, <see cref="Line" />. The <b>control</b> family is
        /// button_default and its variants - <see cref="ControlHot" />, <see cref="ControlFocus" />,
        /// <see cref="SwitchLit" />. They start together and part company under the pointer: a panel is a
        /// surface and lifts towards it, a control is a well and deepens under it.
        /// </remarks>
        public static readonly Color Panel = new Color(41, 54, 62);

        /// <summary>DarkBlueBackground_highlight.png. What the game does to a panel under the pointer.</summary>
        public static readonly Color PanelHot = new Color(60, 76, 82);

        /// <summary>
        /// DarkBlueBackground_active.png, the panel family pressed - which is also the game's mid tone and
        /// reads as a hairline at one pixel, which is what most of this project uses it for.
        /// </summary>
        public static readonly Color Line = new Color(91, 115, 123);

        /// <summary>
        /// button_default_highlight.dds, and textbox_center_highlight, and every other control's - all of
        /// them exactly this. <b>A control under the pointer goes darker in this game.</b>
        /// </summary>
        /// <remarks>
        /// Surprising, and worth not overriding on instinct. Nearly every desktop lightens what the
        /// pointer is on; this game darkens a control and lightens a panel, and the two together are what
        /// make its chrome read as pressed metal rather than as a web page.
        /// <para>
        /// <b>A row you can press is a control.</b> Reactor's lists lifted for a while, which was the
        /// wrong half of the rule and had a second cost: a lift closes the gap between a panel and the
        /// text on it, so the one row somebody had the pointer on was the one row hardest to read. The
        /// smallest text went under 3:1 on hover. Deepening moves every tier the other way.
        /// </para>
        /// </remarks>
        public static readonly Color ControlHot = new Color(12, 18, 22);

        /// <summary>button_default_focus.dds. A control with the keyboard in it.</summary>
        public static readonly Color ControlFocus = new Color(69, 128, 157);

        /// <summary>
        /// The colour the game writes in: every label, list row and field in the terminal is this.
        /// </summary>
        /// <remarks>
        /// Not white, which is what it looks like next to the panels and is the mistake worth avoiding.
        /// <para>
        /// It was the icon colour for a while - Textures/GUI/Icons/*.png are single-colour and drawn a
        /// few steps darker than this - which was a reasonable guess and a measurably wrong one. Sampling
        /// the terminal's own text settled it: the game reserves plain white for the row you have
        /// selected and writes everything else in this.
        /// </para>
        /// </remarks>
        public static readonly Color Ink = new Color(220, 245, 253);

        /// <summary>A step down from <see cref="Ink" />, toward the focus blue.</summary>
        public static readonly Color Muted = new Color(162, 195, 205);

        /// <summary>
        /// A step down again, and the bottom of the ramp. Nothing quieter than this carries a word.
        /// </summary>
        /// <remarks>
        /// Set by the floor rather than by eye: this reads 4.5:1 against <see cref="Panel" />, which is
        /// what the smallest text on these screens needs to be read at all. The first cut of it was three
        /// steps darker and looked right in isolation, which is the trap - it was under 3:1, and a step
        /// number or a "ran 4s ago" written in it was decoration rather than information.
        /// </remarks>
        public static readonly Color Faint = new Color(133, 161, 169);

        /// <summary>
        /// DarkBlueBackground_focus.png. What the game fills a control with when it is the one in hand.
        /// </summary>
        /// <remarks>
        /// Pale, and filled rather than outlined, which is why anything wearing it takes dark text. A
        /// saturated blue was the guess and it was the wrong family altogether.
        /// </remarks>
        public static readonly Color Accent = new Color(142, 188, 207);

        public static readonly Color SlotEdge = new Color(142, 188, 207);
        public static readonly Color SlotFill = new Color(55, 72, 81);



        /// <summary>checkbox_green_checked.png. Vivid, and nothing like a desktop's mint.</summary>
        public static readonly Color Live = new Color(58, 204, 0);

        /// <summary>
        /// The other half of <see cref="Live" />: a light that is off, a border that is not in force, a
        /// control with nothing to do.
        /// </summary>
        /// <remarks>
        /// It used to be exactly <see cref="PanelHot" />, which made an unlit light and the panel behind
        /// it the same colour to within a rounding error - you could not see there was a light there at
        /// all, only that a green one sometimes appeared. Off is a state and has to be drawn.
        /// <para>
        /// Lighter than the panel rather than darker, because nothing can be darker than these panels by
        /// enough to matter: the page is already near black and the arithmetic has no room left below it.
        /// </para>
        /// </remarks>
        public static readonly Color Dim = new Color(95, 114, 120);

        /// <summary>
        /// The grounds that say what kind of thing a row is - running, wrong, worth reading.
        /// </summary>
        /// <remarks>
        /// All three are darker than the panel they interrupt, and that is the whole of why they changed.
        /// They were mid-toned and landed within a few percent of <see cref="Panel" />'s own brightness,
        /// which left the hue carrying the entire signal: correct on this monitor, absent for the one
        /// player in twelve who cannot separate a green wash from a blue one. Deepening them costs
        /// nothing - the text on them was already the brightest thing in the palette - and gives every
        /// eye a second way to see it.
        /// </remarks>
        public static readonly Color LiveWash = new Color(14, 40, 10);

        public static readonly Color Problem = new Color(232, 74, 58);
        public static readonly Color ProblemWash = new Color(36, 12, 10);
        public static readonly Color Notice = new Color(240, 172, 40);
        public static readonly Color NoticeWash = new Color(40, 30, 9);

        public static readonly Color None = new Color(0, 0, 0, 0);

        /// <summary>
        /// switch_on_off_*_checked.dds, which is also button_default_active - the control family pressed.
        /// A checked half of a switch and a held-down button are the same thing to this game.
        /// </summary>
        public static readonly Color SwitchLit = new Color(27, 44, 51);

        /// <summary>
        /// switch_on_off_*.dds. The half that is not.
        /// </summary>
        /// <remarks>
        /// Darker than anything else in this palette, and deliberately so in the game as well: the two
        /// halves are told apart by the difference between them rather than by either one being marked.
        /// </remarks>
        public static readonly Color SwitchDark = new Color(6, 10, 12);
    }

    /// <summary>
    /// Text sizes, named for what they are for.
    /// <para>
    /// The other host says these in pixels because it draws into a window. This one says them as font
    /// scales, because it draws into a fixed UI size that the game stretches. The numbers therefore
    /// differ while the sizes agree, which is the whole reason they are named rather than written out.
    /// </para>
    /// </summary>
    public static class Type
    {
        /// <summary>
        /// A screen's name. The one size the game has no equivalent for, because it has no screens.
        /// </summary>
        public const float Title = 0.9f;

        /// <summary>
        /// Everything that is a sentence, a row or a label. <b>The size the game writes in.</b>
        /// </summary>
        /// <remarks>
        /// Measured rather than chosen. The terminal is almost perfectly uniform - list rows, section
        /// headings, field contents, button captions and the window's own title are all one size, a cap
        /// height of fourteen units - and this is the scale that lands there. The first cut of these was
        /// a quarter smaller across the board, which is what a panel drawn against a mock-up looks like
        /// rather than one drawn against the game beside it.
        /// </remarks>
        public const float Body = 0.64f;

        /// <summary>
        /// A step down, for a control rather than a sentence.
        /// </summary>
        /// <remarks>
        /// Also the game's, and the only other size it has: its tabs and the buttons along the top of
        /// the terminal are cut back to about twelve and a half units, and nothing in it goes below that.
        /// </remarks>
        public const float Small = 0.57f;

        /// <summary>
        /// The quietest thing that still carries a word - a count, a "ran 4s ago", a kind under a name.
        /// </summary>
        /// <remarks>
        /// Below anything the game draws, which is why it is one step rather than three. Reactor has a
        /// tier of detail the terminal has no equivalent for, and it has to be smaller than the name it
        /// belongs to; it does not have to be half of it.
        /// </remarks>
        public const float Tiny = 0.5f;

        /// <summary>
        /// A symbol standing beside a line of text.
        /// </summary>
        /// <remarks>
        /// Larger than the text it accompanies, and deliberately. A letter has its neighbours to lean on
        /// and a symbol has none - it is read alone and at a glance, and the first cut of these was drawn
        /// at the same size as small text and came out as a smudge.
        /// </remarks>
        public const float Symbol = 0.57f;
    }

    /// <summary>
    /// Room, named for what it separates.
    /// </summary>
    /// <remarks>
    /// Here for the same reason <see cref="Type" /> is: these were sixty literals scattered across five
    /// files, every one of them a judgement about how much air something needs, and none of them
    /// findable. Naming them makes a pass over the whole look six numbers rather than an archaeology
    /// expedition.
    /// <para>
    /// They are larger than the Avalonia host's, and that is not a disagreement with the template. That
    /// host draws into a window somebody sized with a mouse; this draws one of the game's own screens,
    /// which is four by three at the full height of the monitor. The same layout in the same proportions
    /// simply has half as much again to work with, and holding onto the smaller numbers would be leaving
    /// a third of the screen empty out of habit.
    /// </para>
    /// </remarks>
    public static class Space
    {
        /// <summary>A rule. One unit is one pixel at the height the UI is defined against.</summary>
        public const float Hair = 1f;

        /// <summary>Between two lines that are one thing - a name, and what it is doing.</summary>
        public const float Line = 3f;

        /// <summary>Between one row of a list and the next.</summary>
        public const float Row = 6f;

        /// <summary>Between two controls sitting side by side.</summary>
        public const float Item = 10f;

        /// <summary>Between one region of a screen and another.</summary>
        public const float Region = 16f;

        /// <summary>Inside a row, across.</summary>
        public const float PadX = 19f;

        /// <summary>Inside a row, down. Less than across, because a line of text is wider than it is tall.</summary>
        public const float PadY = 14f;

        /// <summary>Inside something small that is not a row - a chip, a switch, a quiet control.</summary>
        public const float ChipX = 16f;

        public const float ChipY = 6f;
    }

    /// <summary>Things that are a size rather than a gap.</summary>
    public static class Sizes
    {
        /// <summary>A state light.</summary>
        public const float Dot = 13f;

        /// <summary>
        /// A block's picture.
        /// </summary>
        /// <remarks>
        /// Entirely this target's number. Core hands over a name and says nothing about size, which it
        /// should not: Space Engineers draws recognisable artwork for every block type and it earns the
        /// room, where a game whose things all look alike would want it smaller or absent.
        /// </remarks>
        public const float Icon = 54f;

        /// <summary>One stage of a pick.</summary>
        /// <remarks>
        /// Near enough the width the terminal gives its own block list, which was not the reason for the
        /// number and is a good check on it. There is room for it: a mod screen is handed the same box
        /// the terminal draws in, and two of these columns leave a third of it still empty.
        /// </remarks>
        public const float Column = 460f;

        /// <summary>Anything hanging over the screen - a question, a list that opens downward.</summary>
        public const float Popup = 512f;

        /// <summary>The box at the head of a row in a list of several things to choose between.</summary>
        public const float Mark = 17f;
    }

    /// <summary>A hairline across a block, with room around it.</summary>
    /// <remarks>
    /// The line is drawn rather than being the element's background, so that padding puts space around it
    /// instead of thickening it. Phosphor has no margins - padding is the only spacing there is - and a
    /// background fills the whole box including that padding, which turns a one-pixel rule into a band.
    /// </remarks>
    public class Rule : Element
    {
        public Rule(float thickness, Color color)
        {
            Thickness = thickness;
            Color = color;
        }

        public float Thickness { get; set; }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thickness);
        }

        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Solid,
                new RectangleF(ContentBounds.X, ContentBounds.Y, ContentBounds.Width, Thickness),
                Color, 0f);
        }
    }

    /// <summary>
    /// A state light.
    /// </summary>
    /// <remarks>
    /// Square, where the other host draws a circle. Not a decision - the seam draws sprites by material
    /// id and there is no round one declared yet, and inventing a rounded primitive for it would put a
    /// shape into the graphics abstraction that an <c>IMyTextSurface</c> would then have to reproduce.
    /// A circular texture is the answer when this is worth doing properly.
    /// </remarks>
    public class Dot : Element
    {
        public const float Size = Sizes.Dot;

        public Dot()
        {
            Background = Palette.Dim;
        }

        /// <summary>Whether it is lit right now.</summary>
        public bool IsLit
        {
            set { Background = value ? Palette.Live : Palette.Dim; }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }

    /// <summary>
    /// The handle a row is dragged by: six small squares, two across and three down.
    /// </summary>
    /// <remarks>
    /// Drawn rather than fetched. The game has no grip of its own to borrow - nothing in it is dragged
    /// the way a step in a sequence is - and six rectangles are crisp at any size where a picture
    /// squeezed to eleven pixels across would be six grey smudges.
    /// </remarks>
    public class Grip : Element
    {
        const int Across = 2;
        const int Down = 3;

        /// <summary>How big one square is, and how far apart they sit.</summary>
        const float Square = 3f;

        const float Gap = 3f;

        public Grip(Color color)
        {
            Color = color;
        }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(
                Across * Square + (Across - 1) * Gap,
                Down * Square + (Down - 1) * Gap);
        }

        protected override void DrawContent(IGraphics g)
        {
            float width = Across * Square + (Across - 1) * Gap;
            float height = Down * Square + (Down - 1) * Gap;

            float left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            float top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;

            for (int x = 0; x < Across; x++)
                for (int y = 0; y < Down; y++)
                    g.DrawSprite(Solid, new RectangleF(
                        left + x * (Square + Gap), top + y * (Square + Gap), Square, Square), Color, 0f);
        }
    }

    /// <summary>
    /// The box or circle at the head of a row somebody is choosing from.
    /// </summary>
    /// <remarks>
    /// Square where several may be chosen and round where only one may, which is the one piece of this
    /// screen borrowed from desktops rather than from the game. Space Engineers has a checkbox and no
    /// radio button, because nothing in it asks a question that way; the shape still carries the answer
    /// to "may I pick more than one of these", and that is worth more here than the consistency is.
    /// <para>
    /// The square is drawn and the circle is a sprite, for the same reason everything else here is one or
    /// the other: a rectangle is exact at any size and a curve cannot be made of them.
    /// </para>
    /// </remarks>
    public class Mark : Element
    {
        readonly Glyph _glyph = Widgets.Symbol(Symbols.Ring, Palette.Faint);

        public Mark()
        {
            Color = Palette.Faint;
        }

        /// <summary>Whether more than one of the rows this heads may be chosen.</summary>
        public bool Many { get; set; }

        /// <summary>Whether this one has been.</summary>
        public bool Chosen { get; set; }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            // The circle is measured here even on a row that will draw a square, because which one this
            // is can change between a measure and a draw - a stage that allowed one choice and now allows
            // several - and a glyph arranged without having been measured centres itself against nothing.
            // That was worth an hour: the ring drew low and to the right of where it belonged, which
            // reads as bad artwork rather than as a missed call.
            _glyph.Symbol = Many ? Symbols.Check : (Chosen ? Symbols.Disc : Symbols.Ring);
            _glyph.Scale = Many ? Type.Symbol * 0.58f : Type.Symbol;
            _glyph.Tint = Color;
            _glyph.Measure(g, available);

            return new Vector2(Sizes.Mark, Sizes.Mark);
        }

        protected override void DrawContent(IGraphics g)
        {
            if (!Many)
            {
                // Given the whole box rather than a square cut out of it, because a glyph centres its own
                // line inside whatever it is handed and that line is taller than the square a drawn
                // checkbox wants.
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
                return;
            }

            RectangleF box = new RectangleF(
                ContentBounds.X + (ContentBounds.Width - Sizes.Mark) * 0.5f,
                ContentBounds.Y + (ContentBounds.Height - Sizes.Mark) * 0.5f,
                Sizes.Mark, Sizes.Mark);

            Outline(g, box, Color, Space.Hair);

            // A tick, and not the smaller filled square the game itself draws. That square is what every
            // other interface uses for "partly", and this audience learned checkboxes everywhere except
            // here.
            //
            // Sized down from the ring's scale, which is the one difference between them: the ring is
            // drawn *instead of* a box and may fill the space, while the tick is drawn *inside* one and at
            // full size climbs out of it. Two thirds still caught the top-right corner; this clears it.
            if (Chosen)
            {
                _glyph.Arrange(ContentBounds);
                _glyph.Draw(g);
            }
        }
    }

    /// <summary>
    /// A word with an arrowhead beside it, saying where pressing it goes.
    /// </summary>
    /// <remarks>
    /// Two things rather than the word alone. Back is the only way out of a screen and a list of kinds is
    /// the only thing on a column that opens downward, and an audience who have never used one of these
    /// finds an arrow before it reads anything.
    /// <para>
    /// The arrow leads where it points away from the word - Back, going left - and trails where it points
    /// at what the word is about to open. Both are the same picture turned.
    /// </para>
    /// </remarks>
    public class Signpost : Stack
    {
        readonly Glyph _arrow;
        readonly Label _word;

        public Signpost(string word, string arrow, bool leads)
        {
            Horizontal = true;
            Spacing = Space.Row;

            _arrow = Widgets.Symbol(arrow, Palette.Muted);

            _word = Widgets.Ghost(Widgets.Caps(word));
            _word.Wraps = false;

            if (leads)
            {
                Children.Add(_arrow);
                Children.Add(_word);
            }
            else
            {
                Children.Add(_word);
                Children.Add(_arrow);
            }
        }

        /// <summary>What it says. The arrow beside it does not change.</summary>
        public string Text
        {
            set { _word.Text = value; }
        }

        /// <summary>Colours the arrow and the word together, so the two cannot disagree.</summary>
        public Color Tint
        {
            set
            {
                _arrow.Tint = value;
                _word.Color = value;
            }
        }
    }

    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means, so nothing else may borrow this
    /// look. The line under it is what distinguishes it from a row at a glance, and it is drawn rather
    /// than being a border because the seam has no borders - a rectangle of colour is all either target
    /// can be asked for.
    /// </remarks>
    public class Slot : Label
    {
        public Slot()
        {
            Scale = Type.Body;
            Color = Palette.Ink;
            Background = Palette.SlotFill;
            Highlight = Palette.ControlHot;
            Padding = new Thickness(Space.ChipX, Space.ChipY);
            Underline = Palette.SlotEdge;

            // A slot is one thing named, and a wrapped one would stop reading as a single chip.
            Wraps = false;
        }

        /// <summary>The colour of the line along the bottom.</summary>
        public Color Underline { get; set; }

        protected override void DrawContent(IGraphics g)
        {
            const float thickness = 2f;

            g.DrawSprite(Solid,
                new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - thickness, Bounds.Width, thickness),
                Underline, 0f);

            base.DrawContent(g);
        }
    }

    /// <summary>
    /// The game's own on/off control: two halves side by side, both words always readable, the half that
    /// is in force filled light and the other left dark.
    /// </summary>
    /// <remarks>
    /// Copied from <c>MyGuiControlOnOffSwitch</c> rather than invented. That control is a bordered box
    /// holding two half-width checkboxes and two labels; the state is shown by which half is checked and
    /// by the font each label is drawn in - White on the side in force, Blue on the side that is not.
    /// Every part of it is a flat fill, a one-pixel border or a word, so all of it is reproducible here.
    /// <para>
    /// It says <b>what state the thing is in</b>, where everything else on these screens says what
    /// pressing it does. That is the game's convention and it is worth the inconsistency: an audience
    /// who have never programmed should not have to work out whether a lone word is a label or a button,
    /// and a switch showing both of its positions cannot be read the wrong way round.
    /// </para>
    /// </remarks>
    public class Switch : Element
    {
        readonly ElementCollection _children;
        readonly Label _on;
        readonly Label _off;

        float _half;

        public Switch(float scale)
        {
            _children = new ElementCollection(this);

            _on = Half(Widgets.Caps("on"), scale);
            _off = Half(Widgets.Caps("off"), scale);

            _children.Add(_on);
            _children.Add(_off);

            Edge = Palette.Dim;
            EdgeThickness = Space.Hair;

            // A switch is two fills and a border, so the border is the only response it has room for.
            // The game does the same: its highlight textures differ from its normal ones at the edge far
            // more than in the middle.
            HighlightEdge = Palette.Muted;
        }

        /// <summary>Which way it is set. The lit half and the bright word both follow this.</summary>
        public bool IsOn { get; private set; }

        /// <summary>
        /// Whether this governs everything below it as well as itself.
        /// </summary>
        /// <remarks>
        /// Two of them in a column need telling apart, because stopping a whole airlock is not the same
        /// as stopping one of its sequences. The indent already says which is nested; this says which is
        /// the bigger act, as a brighter border - the weight the old switch carried as a solid fill is
        /// spoken for now that both halves are drawn.
        /// </remarks>
        public bool Governs { get; private set; }

        /// <summary>Sets both halves at once, so the two can never disagree about which is lit.</summary>
        public void Show(bool isOn, bool governs)
        {
            Collapsed = false;
            IsOn = isOn;
            Governs = governs;

            // The game leaves both words nearly as bright as each other - its inactive font is
            // (220, 244, 252) against a white one - and lets the difference between the two fills carry
            // the state. That works at the size it draws a switch, which is a hundred and seventy pixels
            // to a half; ours is a fifth of that, so the word has to help.
            //
            // But only help. This was the unlit colour for a while, which put the off word at 2.2:1
            // against the dark half and made a control whose whole promise is that both of its positions
            // can be read into one that could not be. The bottom of the text ramp is the floor here as
            // everywhere: quiet, and still a word.
            _on.Color = isOn ? Palette.Ink : Palette.Faint;
            _off.Color = isOn ? Palette.Faint : Palette.Ink;

            // Two steps of the text ramp rather than a line tone and an unlit one. Those two used to be
            // far apart and are now within a percent of each other, which would have quietly turned the
            // difference between stopping one sequence and stopping the whole airlock into no difference
            // at all.
            Edge = governs ? Palette.Faint : Palette.Dim;
        }

        /// <summary>Nothing to switch here, so it takes up no room at all.</summary>
        public void Hide()
        {
            Collapsed = true;
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _on.Measure(g, available);
            _off.Measure(g, available);

            // Both halves take the width of the longer word. Otherwise the control would change shape as
            // it was switched, and a row of them down a screen would not line up with each other.
            _half = _on.DesiredSize.X > _off.DesiredSize.X ? _on.DesiredSize.X : _off.DesiredSize.X;

            float height = _on.DesiredSize.Y > _off.DesiredSize.Y ? _on.DesiredSize.Y : _off.DesiredSize.Y;
            return new Vector2(_half * 2f, height);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            _on.Arrange(new RectangleF(bounds.X, bounds.Y, _half, bounds.Height));
            _off.Arrange(new RectangleF(bounds.X + _half, bounds.Y, _half, bounds.Height));
        }

        protected override void DrawContent(IGraphics g)
        {
            // Inside the border rather than over it, so the one pixel the game draws its controls with
            // survives the fills going on top of it.
            float inset = EdgeThickness;
            var inner = new RectangleF(
                Bounds.X + inset, Bounds.Y + inset,
                Bounds.Width - inset * 2f, Bounds.Height - inset * 2f);

            // The dark tone underneath the whole of it, then the lit half over the top. Two halves drawn
            // side by side is the obvious way and it is wrong: a backend rounds each rectangle on its own,
            // so on a fractional width the two either overlap by a pixel or leave a line of whatever is
            // behind them showing through the middle. One rectangle cannot have a seam in it.
            g.DrawSprite(Solid, inner, Palette.SwitchDark, 0f);

            float half = inner.Width * 0.5f;
            g.DrawSprite(Solid, IsOn
                ? new RectangleF(inner.X, inner.Y, half, inner.Height)
                : new RectangleF(inner.X + half, inner.Y, inner.Width - half, inner.Height),
                Palette.SwitchLit, 0f);

            _on.Draw(g);
            _off.Draw(g);
        }

        static Label Half(string text, float scale)
        {
            Label half = Widgets.Label(text, Palette.Faint, scale);
            half.Padding = new Thickness(Space.ChipX, Space.ChipY);
            half.Centres = true;
            half.Wraps = false;
            return half;
        }
    }

    /// <summary>The building blocks, so a row is one call rather than six lines of setting properties.</summary>
    public static class Widgets
    {
        /// <summary>
        /// The default piece of text, and it wraps.
        /// </summary>
        /// <remarks>
        /// Wrapping by default matches the other host, and it matters more here: a block somebody named
        /// "Hangar door - outer, port side" has to be readable in a 250-unit column, and the alternative
        /// is a row that silently says something shorter than what is there.
        /// </remarks>
        public static Label Label(string text, Color color, float scale)
        {
            var label = new Label();
            label.Font = MyFontEnum.White;
            label.Scale = scale;
            label.Color = color;
            label.Wraps = true;
            label.Text = text;
            return label;
        }

        /// <summary>A row that drills into something. The workhorse of every list on every screen.</summary>
        public static Label Row(string text, Color color)
        {
            Label row = Label(text, color, Type.Body);
            row.Background = Palette.Panel;
            row.Highlight = Palette.ControlHot;
            row.Padding = new Thickness(Space.PadX, Space.PadY);
            return row;
        }

        /// <summary>Quiet, for something that is available without being the point.</summary>
        public static Label Ghost(string text)
        {
            Label ghost = Label(text, Palette.Muted, Type.Small);
            ghost.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 0.8f);

            // A control rather than a sentence. One that broke onto two lines would change the height of
            // whatever row it sits in, which is the fidgeting this screen is trying not to do.
            ghost.Wraps = false;
            return ghost;
        }

        /// <summary>
        /// One of a set of narrowings, lit when it is the one in force.
        /// </summary>
        /// <remarks>
        /// Dark words on the lit one. The game's focus colour is a pale ice blue that it fills a control
        /// with rather than outlines, so anything wearing it has to put dark text on top - pale on pale
        /// is the trap a saturated accent hides and this one does not.
        /// </remarks>
        public static Label Chip(string text, bool on)
        {
            Label chip = Label(text, on ? Palette.Chrome : Palette.Muted, Type.Small);
            chip.Background = on ? Palette.Accent : Palette.Panel;
            chip.Padding = new Thickness(Space.ChipX, Space.ChipY);
            chip.Wraps = false;
            return chip;
        }

        /// <summary>
        /// Somewhere to type.
        /// </summary>
        /// <remarks>
        /// Here because it was not, and three boxes made in three places quietly kept Phosphor's own
        /// defaults for the caret, the selection and the hint - three colours on screen that the palette
        /// had never been asked about. Phosphor is right to have them: it knows nothing about Reactor
        /// and something has to be drawn. Reactor is wrong to leave them.
        /// </remarks>
        public static TextBox Box(float scale, Color background)
        {
            var box = new TextBox();
            box.Font = MyFontEnum.White;
            box.Scale = scale;
            box.Color = Palette.Ink;
            box.Background = background;
            box.Edge = Palette.Line;
            box.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.3f);

            box.CaretColor = Palette.Ink;
            box.HintColor = Palette.Faint;

            // A mid tone rather than the focus blue. The words stay their own colour while they are
            // selected, and pale ink on the pale focus fill is two things the same brightness.
            box.SelectionColor = Palette.Line;
            return box;
        }

        /// <summary>Something said against a coloured ground, because it needs reading.</summary>
        public static Label Wash(string text, Color ink, Color background)
        {
            Label wash = Label(text, ink, Type.Small);
            wash.Background = background;
            wash.Padding = new Thickness(Space.ChipX, Space.ChipY * 1.6f);
            return wash;
        }

        /// <summary>
        /// The game's own way of saying "this is a control".
        /// </summary>
        /// <remarks>
        /// Space Engineers puts its buttons and its section headings in capitals, and doing the same is
        /// most of what makes a panel read as belonging to it. Only the chrome: a block called "Hangar
        /// Door" is a name somebody typed, and shouting it back at them would be this target editing
        /// their words.
        /// </remarks>
        public static string Caps(string text)
        {
            return text == null ? "" : text.ToUpperInvariant();
        }

        public static Rule Line()
        {
            return new Rule(Space.Hair, Palette.Line);
        }

        public static Stack Column(float spacing)
        {
            var stack = new Stack();
            stack.Spacing = spacing;
            return stack;
        }

        /// <summary>One symbol, at the size a symbol stands beside a line of text.</summary>
        public static Glyph Symbol(string symbol, Color tint)
        {
            return new Glyph(symbol, Type.Symbol, tint);
        }

        /// <summary>
        /// A note that follows the pointer, saying what a control is when the control cannot.
        /// </summary>
        /// <remarks>
        /// Drawn on <see cref="Palette.Chrome" /> rather than a panel tone, so it reads as sitting above
        /// the screen rather than as another row of it - it is the only thing here that is allowed over
        /// everything, and it should look borrowed rather than built in.
        /// </remarks>
        public static Label Tip()
        {
            Label tip = Label("", Palette.Ink, Type.Small);
            tip.Background = Palette.Chrome;
            tip.Edge = Palette.Line;
            tip.EdgeThickness = Space.Hair;
            tip.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY);

            // One line however long it is. A note that wrapped would change shape as the pointer moved
            // along a row of controls, which is the fidgeting this screen keeps being pulled back from.
            tip.Wraps = false;
            return tip;
        }

        public static Stack RowOf(float spacing)
        {
            var stack = new Stack();
            stack.Horizontal = true;
            stack.Spacing = spacing;
            return stack;
        }
    }
}
